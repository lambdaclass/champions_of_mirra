defmodule DarkWorldsServer.Engine.BotPlayer do
  use GenServer, restart: :transient
  require Logger
  alias DarkWorldsServer.Communication
  alias DarkWorldsServer.Engine.ActionOk
  alias DarkWorldsServer.Engine.Runner
  alias LambdaGameEngine.MyrraEngine.Position
  alias LambdaGameEngine.MyrraEngine.RelativePosition

  # This variable will decide how much time passes between bot decisions in milis
  @decide_delay_ms 500

  # We'll decide the view range of a bot measured in grid cells
  # e.g. from {x=1, y=1} to {x=5, y=1} you have 4 cells
  @visibility_max_range_cells 2000

  # This number determines the amount of players needed in proximity for the bot to flee
  @amount_of_players_to_flee 3

  # The numbers of cell close to the bot in wich the enemies will count to flee
  @range_of_players_to_flee 500

  #######
  # API #
  #######
  def start_link(game_pid, tick_rate) do
    GenServer.start_link(__MODULE__, {game_pid, tick_rate})
  end

  def add_bot(bot_pid, bot_id) do
    GenServer.cast(bot_pid, {:add_bot, bot_id})
  end

  def enable_bots(bot_pid) do
    GenServer.cast(bot_pid, {:bots_enabled, true})
  end

  def disable_bots(bot_pid) do
    GenServer.cast(bot_pid, {:bots_enabled, false})
  end

  #######################
  # GenServer callbacks #
  #######################
  @impl GenServer
  def init({game_pid, tick_rate}) do
    game_id = Communication.pid_to_external_id(game_pid)
    Phoenix.PubSub.subscribe(DarkWorldsServer.PubSub, "game_play_#{game_id}")

    {:ok, %{game_pid: game_pid, bots_enabled: true, game_tick_rate: tick_rate, players: [], bots: %{}, game_state: %{}}}
  end

  @impl GenServer
  def handle_cast({:add_bot, bot_id}, state) do
    send(self(), {:decide_action, bot_id})
    send(self(), {:do_action, bot_id})

    {:noreply, put_in(state, [:bots, bot_id], %{alive: true, objective: :random_movement})}
  end

  def handle_cast({:bots_enabled, toggle}, state) do
    {:noreply, %{state | bots_enabled: toggle}}
  end

  @impl GenServer
  def handle_info({:decide_action, bot_id}, state) do
    bot_state = get_in(state, [:bots, bot_id])

    new_bot_state =
      case bot_state do
        %{action: :die} ->
          bot_state

        bot_state ->
          Process.send_after(self(), {:decide_action, bot_id}, @decide_delay_ms)

          closest_entities = get_closest_entities(state.game_state, bot_id)

          bot_state
          |> decide_objective(state, bot_id, closest_entities)
          |> decide_action(bot_id, state.players, state, closest_entities)
      end

    state =
      put_in(state, [:bots, bot_id], new_bot_state)

    {:noreply, state}
  end

  def handle_info({:do_action, bot_id}, state) do
    bot_state = get_in(state, [:bots, bot_id])

    if bot_state.alive do
      Process.send_after(self(), {:do_action, bot_id}, state.game_tick_rate)
      do_action(bot_id, state.game_pid, state.players, bot_state)
    end

    {:noreply, state}
  end

  def handle_info({:game_update, game_state}, state) do
    players =
      game_state.client_game_state.game.myrra_state.players
      |> Enum.map(&Map.take(&1, [:id, :health, :position]))
      |> Enum.sort_by(& &1.health, :desc)

    bots =
      Enum.reduce(players, state.bots, fn player, acc_bots ->
        case {player.health <= 0, acc_bots[player.id]} do
          {true, bot} when not is_nil(bot) -> put_in(acc_bots, [player.id, :alive], false)
          _ -> acc_bots
        end
      end)

    Enum.each(bots, fn {bot_id, _} -> send(self(), {:think_and_do, bot_id}) end)

    {:noreply, %{state | players: players, bots: bots, game_state: game_state.client_game_state.game}}
  end

  def handle_info(_msg, state) do
    {:noreply, state}
  end

  #############################
  # Callbacks implementations #
  #############################
  defp decide_action(%{alive: false} = bot_state, _, _, _game_state, _closest_entities) do
    Map.put(bot_state, :action, :die)
  end

  defp decide_action(%{objective: :random_movement} = bot_state, _bot_id, _players, _game_state, _closest_entities) do
    movement = Enum.random([{1.0, 1.0}, {-1.0, 1.0}, {1.0, -1.0}, {-1.0, -1.0}, {0.0, 0.0}])
    Map.put(bot_state, :action, {:move, movement})
  end

  defp decide_action(%{objective: :attack_enemy} = bot_state, _bot_id, _players, _game_state, closest_entities) do
    Map.put(bot_state, :action, {:try_attack, closest_entities})
  end

  defp decide_action(%{objective: :flee_from_zone} = bot_state, bot_id, players, state, _closest_entities) do
    bot = Enum.find(players, fn player -> player.id == bot_id end)

    target =
      calculate_circle_point(
        bot.position,
        state.game_state.myrra_state.shrinking_center
      )

    Map.put(bot_state, :action, {:move, target})
  end

  defp decide_action(bot_state, _bot_id, _players, _game_state, _closest_entities) do
    bot_state
    |> Map.put(:action, {:nothing, nil})
  end

  defp do_action(bot_id, game_pid, _players, %{action: {:move, {x, y}}}) do
    Runner.play(game_pid, bot_id, %ActionOk{action: :move_with_joystick, value: %{x: x, y: y}, timestamp: nil})
  end

  defp do_action(bot_id, game_pid, _players, %{
         action: {:try_attack, %{players: players, loots: loots}}
       }) do
    %{direction_to_entity: {closest_entity_x, closest_entity_y}} =
      closest_entity = Enum.min_by([List.first(players), List.first(loots)], fn e -> if e, do: e.distance_to_entity end)

    # TODO replace this 400 with a function that determines if any skill would hit the enemy
    # If the entity detected is in attack range we should perfom an attack

    amount_of_players_in_flee_proximity =
      players
      |> Enum.filter(fn p -> p.distance_to_entity < @range_of_players_to_flee end)
      |> Enum.count()

    cond do
      amount_of_players_in_flee_proximity >= @amount_of_players_to_flee ->
        %{direction_to_entity: {x, y}} = hd(players)
        Runner.play(game_pid, bot_id, %ActionOk{action: :move_with_joystick, value: %{x: -x, y: -y}, timestamp: nil})

      closest_entity.type == :enemy and closest_entity.distance_to_entity <= 400 ->
        Runner.play(game_pid, bot_id, %ActionOk{
          action: :basic_attack,
          value: %RelativePosition{x: closest_entity_x, y: closest_entity_y},
          timestamp: nil
        })

      true ->
        Runner.play(game_pid, bot_id, %ActionOk{
          action: :move_with_joystick,
          value: %{x: closest_entity_x, y: closest_entity_y},
          timestamp: nil
        })
    end
  end

  defp do_action(_bot_id, _game_pid, _players, _) do
    nil
  end

  ####################
  # Internal helpers #
  ####################
  def calculate_circle_point(%{x: start_x, y: start_y}, %{x: end_x, y: end_y}) do
    calculate_circle_point(start_x, start_y, end_x, end_y)
  end

  def calculate_circle_point(cx, cy, x, y) do
    radius = 1
    angle = Nx.atan2(x - cx, y - cy)
    x = Nx.cos(angle) |> Nx.multiply(radius) |> Nx.to_number()
    y = Nx.sin(angle) |> Nx.multiply(radius) |> Nx.to_number()
    {x, -y}
  end

  def decide_objective(bot_state, %{bots_enabled: false}, _bot_id, _closest_entity) do
    Map.put(bot_state, :objective, :nothing)
  end

  def decide_objective(bot_state, %{game_state: %{myrra_state: myrra_state}}, bot_id, closest_entities) do
    bot = Enum.find(myrra_state.players, fn player -> player.id == bot_id end)

    objective =
      case bot do
        nil ->
          :waiting_game_update

        bot ->
          out_of_area? = Enum.any?(bot.effects, fn {k, _v} -> k == :out_of_area end)

          cond do
            out_of_area? ->
              :flee_from_zone

            not Enum.empty?(closest_entities.loots) or not Enum.empty?(closest_entities.players) ->
              :attack_enemy

            true ->
              :random_movement
          end
      end

    Map.put(bot_state, :objective, objective)
  end

  def decide_objective(bot_state, _, _, _), do: Map.put(bot_state, :objective, :nothing)

  defp get_closest_entities(%{myrra_state: game_state}, bot_id) do
    # TODO maybe we could add a priority to the entities.
    # e.g. if the bot has low health priorities the loot boxes
    bot = Enum.find(game_state.players, fn player -> player.id == bot_id end)

    case bot do
      nil ->
        %{}

      bot ->
        players_distances =
          game_state.players
          |> Enum.filter(fn player -> player.status == :alive and player.id != bot.id end)
          |> map_entities(bot, :enemy)

        loots_distances =
          game_state.loots
          |> map_entities(bot, :loot)

        %{
          players: players_distances,
          loots: loots_distances
        }
    end
  end

  defp get_closest_entities(_, _) do
    %{}
  end

  defp get_distance_to_point(%Position{x: start_x, y: start_y}, %Position{x: end_x, y: end_y}) do
    diagonal_movement_cost = 14
    straight_movement_cost = 10

    x_distance = abs(end_x - start_x)
    y_distance = abs(end_y - start_y)
    remaining = abs(x_distance - y_distance)

    (diagonal_movement_cost * Enum.min([x_distance, y_distance]) + remaining * straight_movement_cost)
    |> div(10)
  end

  defp map_entities(entities, bot, type) do
    entities
    |> Enum.map(fn entity ->
      %{
        type: type,
        entity_id: entity.id,
        distance_to_entity: get_distance_to_point(bot.position, entity.position),
        direction_to_entity: calculate_circle_point(bot.position, entity.position)
      }
    end)
    |> Enum.sort_by(fn distances -> distances.distance_to_entity end, :asc)
    |> Enum.filter(fn distances -> distances.distance_to_entity <= @visibility_max_range_cells end)
  end
end

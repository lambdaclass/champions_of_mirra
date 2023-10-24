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

  # We'll decide the view range of a bot
  @visibility_max_range 2000

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

    wandering_position = %{x: :rand.uniform(10000), y: :rand.uniform(10000)}
    already_visited_positions = [wandering_position]

    {:noreply,
     put_in(state, [:bots, bot_id], %{
       alive: true,
       objective: :wander,
       current_wandering_position: wandering_position,
       already_visited_positions: already_visited_positions
     })}
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

          closest_entity = get_closes_entity(state.game_state, bot_id)

          decide_action(bot_id, state.players, bot_state, state, closest_entity)
          |> decide_objective(state, bot_id, closest_entity)
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
  defp decide_action(_, _, %{alive: false} = bot_state, _game_state, _closest_entity) do
    Map.put(bot_state, :action, :die)
  end

  defp decide_action(
         bot_id,
         players,
         %{objective: :wander, current_wandering_position: wandering_position} = bot_state,
         _game_state,
         _closest_entity
       ) do
    bot = Enum.find(players, fn player -> player.id == bot_id end)

    if bot do
      target =
        calculate_circle_point(
          bot.position,
          wandering_position
        )

      Map.put(bot_state, :action, {:move, target})
    else
      Map.put(bot_state, :action, {:nothing, nil})
    end
  end

  defp decide_action(_bot_id, _players, %{objective: :attack_enemy} = bot_state, _game_state, closest_entity) do
    Map.put(bot_state, :action, {:try_attack, closest_entity})
  end

  defp decide_action(bot_id, players, %{objective: :flee_from_zone} = bot_state, state, _closest_entity) do
    bot = Enum.find(players, fn player -> player.id == bot_id end)

    target =
      calculate_circle_point(
        bot.position,
        state.game_state.myrra_state.shrinking_center
      )

    Map.put(bot_state, :action, {:move, target})
  end

  defp decide_action(_bot_id, _players, bot_state, _game_state, _closest_entity) do
    bot_state
    |> Map.put(:action, {:nothing, nil})
  end

  defp do_action(bot_id, game_pid, _players, %{action: {:move, {x, y}}}) do
    Runner.play(game_pid, bot_id, %ActionOk{action: :move_with_joystick, value: %{x: x, y: y}, timestamp: nil})
  end

  defp do_action(bot_id, game_pid, _players, %{
         action: {:try_attack, %{type: :enemy, direction_to_entity: {x, y}} = direction_to_entity}
       }) do
    if direction_to_entity.distance_to_entity > 200 do
      Runner.play(game_pid, bot_id, %ActionOk{action: :move_with_joystick, value: %{x: x, y: y}, timestamp: nil})
    else
      Runner.play(game_pid, bot_id, %ActionOk{
        action: :basic_attack,
        value: %RelativePosition{x: x, y: y},
        timestamp: nil
      })
    end
  end

  defp do_action(bot_id, game_pid, _players, %{
         action: {:try_attack, %{direction_to_entity: {x, y}}}
       }) do
    Runner.play(game_pid, bot_id, %ActionOk{action: :move_with_joystick, value: %{x: x, y: y}, timestamp: nil})
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

  def decide_objective(bot_state, %{game_state: %{myrra_state: myrra_state}}, bot_id, closest_entity) do
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

            true ->
              :wander
          end
      end

    if objective == :wander do
      maybe_generate_wandering_position(bot, bot_state, myrra_state)
    else
      Map.put(bot_state, :objective, objective)
    end
  end

  def decide_objective(bot_state, _, _, _), do: Map.put(bot_state, :objective, :nothing)

  defp get_closes_entity(%{myrra_state: game_state}, bot_id) do
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

        cond do
          Enum.empty?(loots_distances) and Enum.empty?(players_distances) ->
            %{}

          Enum.empty?(loots_distances) ->
            hd(players_distances)

          Enum.empty?(players_distances) ->
            hd(loots_distances)

          true ->
            Enum.min_by([hd(loots_distances), hd(players_distances)], fn entity -> entity.distance_to_entity end)
        end
    end
  end

  defp get_closes_entity(_, _) do
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
    |> Enum.filter(fn distances -> distances.distance_to_entity <= @visibility_max_range end)
  end

  def maybe_generate_wandering_position(
        bot,
        %{objective: :wander, current_wandering_position: current_wandering_position} = bot_state,
        myrra_state
      ) do
    if get_distance_to_point(bot.position, %Position{x: current_wandering_position.x, y: current_wandering_position.y}) <
         500 do
      generate_wandering_position(bot_state, myrra_state)
    else
      bot_state
    end
  end

  def maybe_generate_wandering_position(_bot, bot_state, myrra_state),
    do: generate_wandering_position(bot_state, myrra_state)

  def generate_wandering_position(%{already_visited_positions: already_visited_positions} = bot_state, myrra_state) do
    left_x = myrra_state.shrinking_center.x - div(myrra_state.playable_radius, 2)
    rigth_x = myrra_state.shrinking_center.x + div(myrra_state.playable_radius, 2)
    down_y = myrra_state.shrinking_center.y - div(myrra_state.playable_radius, 2)
    up_y = myrra_state.shrinking_center.y + div(myrra_state.playable_radius, 2)

    wandering_position = %{
      x: Enum.random(max(left_x, 0)..min(rigth_x, myrra_state.board.width)),
      y: Enum.random(max(down_y, 0)..min(up_y, myrra_state.board.height))
    }

    if Enum.member?(already_visited_positions, wandering_position) do
      generate_wandering_position(bot_state, myrra_state)
    else
      Map.merge(bot_state, %{
        objective: :wander,
        current_wandering_position: wandering_position,
        already_visited_positions: bot_state.already_visited_positions ++ [wandering_position]
      })
    end
  end
end

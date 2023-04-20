defmodule LoadTest.Player do
  use WebSockex
  require Logger
  use Tesla

  @server_host "localhost:4000"

  def move(player, :up), do: _move(player, "up")
  def move(player, :down), do: _move(player, "down")
  def move(player, :left), do: _move(player, "left")
  def move(player, :right), do: _move(player, "right")

  def attack(player, :up), do: _attack(player, "up")
  def attack(player, :down), do: _attack(player, "down")
  def attack(player, :left), do: _attack(player, "left")
  def attack(player, :right), do: _attack(player, "right")

  def attack_aoe(player, position) do
    %{
      "player" => player,
      "action" => "attack_aoe",
      "value" => %{"x" => position.x, "y" => position.y}
    }
    |> send_command()
  end

  defp _move(player, direction) do
    %{"player" => player, "action" => "move", "value" => direction}
    |> send_command()
  end

  defp _attack(player, direction) do
    %{"player" => player, "action" => "attack", "value" => direction}
    |> send_command()
  end

  def start_link({player_number, session_id}) do
    WebSockex.start_link("ws://#{@server_host}/play/#{session_id}", __MODULE__, %{
      player_number: player_number,
      session_id: session_id
    })
  end

  def handle_frame({type, msg}, state) do
    Logger.info("Received Message: #{inspect(msg)}")
    {:ok, state}
  end

  def handle_cast({:send, {type, msg} = frame}, state) do
    Logger.info("Sending frame with payload: #{msg}")
    {:reply, frame, state}
  end

  def handle_info(:play, state) do
    direction = Enum.random([:up, :down, :left, :right])
    action = Enum.random([:move, :attack, :attack_aoe])

    # Melee attacks pretty much never ever land, but in general we have to rework how
    # both melee and aoe attacks work in general, so w/e
    case action do
      :move ->
        move(state.player_number, direction)

      :attack ->
        attack(state.player_number, direction)

      :attack_aoe ->
        random_x = Enum.random(0..9)
        random_y = Enum.random(0..9)
        random_position = %{x: random_x, y: random_y}
        attack_aoe(state.player_number, random_position)
    end

    Process.send_after(self(), :play, 100, [])
    {:ok, state}
  end

  defp send_command(command) do
    WebSockex.cast(self(), {:send, {:text, Jason.encode!(command)}})
  end
end

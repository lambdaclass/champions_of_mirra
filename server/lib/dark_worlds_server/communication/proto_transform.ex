defmodule DarkWorldsServer.Communication.ProtoTransform do
  alias DarkWorldsServer.Communication.Proto.GameAction
  alias DarkWorldsServer.Communication.Proto.CharacterConfig
  alias DarkWorldsServer.Communication.Proto.CharacterConfigItem
  alias DarkWorldsServer.Communication.Proto.EffectInfo
  alias DarkWorldsServer.Communication.Proto.GameEvent.SelectedCharactersEntry
  alias DarkWorldsServer.Communication.Proto.KillEvent
  alias DarkWorldsServer.Communication.Proto.LootPackage
  alias DarkWorldsServer.Communication.Proto.MillisTime, as: ProtoMillisTime
  alias DarkWorldsServer.Communication.Proto.Player, as: ProtoPlayer
  alias DarkWorldsServer.Communication.Proto.Player.EffectsEntry
  alias DarkWorldsServer.Communication.Proto.PlayerInformation, as: ProtoPlayerInformation
  alias DarkWorldsServer.Communication.Proto.Position, as: ProtoPosition
  alias DarkWorldsServer.Communication.Proto.Projectile, as: ProtoProjectile
  alias DarkWorldsServer.Communication.Proto.RelativePosition, as: ProtoRelativePosition
  alias DarkWorldsServer.Communication.Proto.RunnerConfig
  alias DarkWorldsServer.Communication.Proto.ServerGameSettings
  alias DarkWorldsServer.Communication.Proto.SkillConfigItem
  alias DarkWorldsServer.Communication.Proto.SkillsConfig
  alias DarkWorldsServer.Communication.Proto.Status
  alias LambdaGameEngine.MyrraEngine.Player, as: EnginePlayer
  alias LambdaGameEngine.MyrraEngine.Position, as: EnginePosition
  alias LambdaGameEngine.MyrraEngine.Projectile, as: EngineProjectile
  alias LambdaGameEngine.MyrraEngine.RelativePosition, as: EngineRelativePosition
  alias DarkWorldsServer.Communication.Proto.Move
  alias DarkWorldsServer.Communication.Proto.GameAction

  @behaviour Protobuf.TransformModule

  ###########
  # ENCODES #
  ###########

  def encode(status, Status) do
    status
  end

  def encode(effect, EffectsEntry) do
    effect_encode(effect)
  end

  def encode(effect_info, EffectInfo) do
    effect_info
  end

  def encode(entry, SelectedCharactersEntry) do
    entry
  end

  def encode(millis_time, ProtoMillisTime) do
    millis_time
  end

  def encode(skill_config, SkillsConfig) do
    skill_config
  end

  def encode(skill_config_item, SkillConfigItem) do
    skill_config_item
  end

  def encode(runner_config, RunnerConfig) do
    runner_config
  end

  def encode(character_config, CharacterConfig) do
    character_config
  end

  def encode(character_config_item, CharacterConfigItem) do
    character_config_item
  end

  @impl Protobuf.TransformModule
  def encode(
        %{
          character_config: character_config,
          runner_config: runner_config,
          skills_config: skills_config
        },
        ServerGameSettings
      ) do
    %{
      Name: name,
      board_height: board_height,
      board_width: board_width,
      game_timeout_ms: game_timeout_ms,
      server_tickrate_ms: server_tickrate_ms,
      map_shrink_wait_ms: map_shrink_wait_ms,
      map_shrink_interval: map_shrink_interval,
      out_of_area_damage: out_of_area_damage,
      map_shrink_minimum_radius: map_shrink_minimum_radius,
      use_proxy: use_proxy,
      spawn_loot_interval_ms: spawn_loot_interval_ms
    } = runner_config

    runner_config = %RunnerConfig{
      Name: name,
      board_height: board_height,
      board_width: board_width,
      game_timeout_ms: game_timeout_ms,
      server_tickrate_ms: server_tickrate_ms,
      map_shrink_wait_ms: map_shrink_wait_ms,
      map_shrink_interval: map_shrink_interval,
      out_of_area_damage: out_of_area_damage,
      map_shrink_minimum_radius: map_shrink_minimum_radius,
      use_proxy: use_proxy,
      spawn_loot_interval_ms: spawn_loot_interval_ms
    }

    character_config = %CharacterConfig{
      Items: character_config[:Items]
    }

    skills_config = %SkillsConfig{
      Items: skills_config[:Items]
    }

    %ServerGameSettings{
      runner_config: runner_config,
      character_config: character_config,
      skills_config: skills_config
    }
  end

  def encode(%EnginePosition{} = position, ProtoPosition) do
    %{x: x, y: y} = position
    %ProtoPosition{x: x, y: y}
  end

  def encode(%EngineRelativePosition{} = position, ProtoRelativePosition) do
    %{x: x, y: y} = position
    %ProtoRelativePosition{x: x, y: y}
  end

  def encode(%EnginePlayer{} = player, ProtoPlayer) do
    %EnginePlayer{
      id: id,
      health: health,
      position: position,
      status: status,
      action: action,
      aoe_position: aoe_position,
      kill_count: kill_count,
      death_count: death_count,
      basic_skill_cooldown_left: basic_skill_cooldown_left,
      skill_1_cooldown_left: skill_1_cooldown_left,
      skill_2_cooldown_left: skill_2_cooldown_left,
      skill_3_cooldown_left: skill_3_cooldown_left,
      skill_4_cooldown_left: skill_4_cooldown_left,
      character_name: name,
      effects: effects,
      direction: direction,
      body_size: body_size
    } = player

    %ProtoPlayer{
      id: id,
      health: health,
      position: position,
      status: player_status_encode(status),
      action: player_action_encode(action),
      aoe_position: aoe_position,
      kill_count: kill_count,
      death_count: death_count,
      basic_skill_cooldown_left: basic_skill_cooldown_left,
      skill_1_cooldown_left: skill_1_cooldown_left,
      skill_2_cooldown_left: skill_2_cooldown_left,
      skill_3_cooldown_left: skill_3_cooldown_left,
      skill_4_cooldown_left: skill_4_cooldown_left,
      character_name: name,
      effects: effects,
      direction: direction,
      body_size: body_size
    }
  end

  def encode(%EngineProjectile{} = projectile, ProtoProjectile) do
    %{
      id: id,
      position: position,
      direction: direction,
      speed: speed,
      range: range,
      player_id: player_id,
      damage: damage,
      remaining_ticks: remaining_ticks,
      projectile_type: projectile_type,
      status: status,
      last_attacked_player_id: last_attacked_player_id,
      pierce: pierce,
      skill_name: skill_name
    } = projectile

    %ProtoProjectile{
      id: id,
      position: position,
      direction: direction,
      speed: speed,
      range: range,
      player_id: player_id,
      damage: damage,
      remaining_ticks: remaining_ticks,
      projectile_type: projectile_encode(projectile_type),
      status: projectile_status_encode(status),
      last_attacked_player_id: last_attacked_player_id,
      pierce: pierce,
      skill_name: skill_name
    }
  end

  def encode({killed_by, killed}, KillEvent) do
    %KillEvent{killed_by: killed_by, killed: killed}
  end

  def encode(%ProtoPlayerInformation{} = player_information, ProtoPlayerInformation) do
    player_information
  end

  def encode(loot, LootPackage) do
    %LootPackage{
      id: loot.id,
      loot_type: loot_type_encode(loot.loot_type),
      position: loot.position
    }
  end

  ###########
  # DECODES #
  ###########

  @impl Protobuf.TransformModule
  def decode(value, GameAction) do
    value
  end

  @impl Protobuf.TransformModule
  def decode(value, Move) do
    value
  end

  @impl Protobuf.TransformModule
  def decode(%ProtoPosition{} = position, ProtoPosition) do
    %{x: x, y: y} = position
    %EnginePosition{x: x, y: y}
  end

  @impl Protobuf.TransformModule
  def decode(%ProtoRelativePosition{} = position, ProtoRelativePosition) do
    %{x: x, y: y} = position
    %EngineRelativePosition{x: x, y: y}
  end

  def decode(%ProtoPlayer{} = player, ProtoPlayer) do
    %ProtoPlayer{
      id: id,
      health: health,
      position: position,
      status: status,
      action: action,
      aoe_position: aoe_position,
      kill_count: kill_count,
      death_count: death_count,
      basic_skill_cooldown_left: basic_skill_cooldown_left,
      skill_1_cooldown_left: skill_1_cooldown_left,
      skill_2_cooldown_left: skill_2_cooldown_left,
      skill_3_cooldown_left: skill_3_cooldown_left,
      skill_4_cooldown_left: skill_4_cooldown_left,
      character_name: name,
      effects: effects,
      direction: direction,
      body_size: body_size
    } = player

    %EnginePlayer{
      id: id,
      health: health,
      position: position,
      status: player_status_decode(status),
      action: player_action_decode(action),
      aoe_position: aoe_position,
      kill_count: kill_count,
      death_count: death_count,
      basic_skill_cooldown_left: basic_skill_cooldown_left,
      skill_1_cooldown_left: skill_1_cooldown_left,
      skill_2_cooldown_left: skill_2_cooldown_left,
      skill_3_cooldown_left: skill_3_cooldown_left,
      skill_4_cooldown_left: skill_4_cooldown_left,
      character_name: name,
      effects: effects,
      direction: direction,
      body_size: body_size
    }
  end

  def decode(%ProtoProjectile{} = projectile, ProtoProjectile) do
    %{
      id: id,
      position: position,
      direction: direction,
      speed: speed,
      range: range,
      player_id: player_id,
      damage: damage,
      remaining_ticks: remaining_ticks,
      projectile_type: projectile_type,
      status: status,
      last_attacked_player_id: last_attacked_player_id,
      pierce: pierce,
      skill_name: skill_name
    } = projectile

    %EngineProjectile{
      id: id,
      position: position,
      direction: direction,
      speed: speed,
      range: range,
      player_id: player_id,
      damage: damage,
      remaining_ticks: remaining_ticks,
      projectile_type: projectile_decode(projectile_type),
      status: projectile_status_decode(status),
      last_attacked_player_id: last_attacked_player_id,
      pierce: pierce,
      skill_name: skill_name
    }
  end

  def decode(%struct{} = msg, struct) do
    Map.from_struct(msg)
  end

  ###############################
  # Helpers for transformations #
  ###############################
  defp player_status_encode(:alive), do: :ALIVE
  defp player_status_encode(:dead), do: :DEAD

  defp player_status_decode(:ALIVE), do: :alive
  defp player_status_decode(:DEAD), do: :dead

  defp player_action_encode(:attacking), do: :ATTACKING
  defp player_action_encode(:nothing), do: :NOTHING
  defp player_action_encode(:attackingaoe), do: :ATTACKING_AOE
  defp player_action_encode(:startingskill1), do: :STARTING_SKILL_1
  defp player_action_encode(:startingskill2), do: :STARTING_SKILL_2
  defp player_action_encode(:startingskill3), do: :STARTING_SKILL_3
  defp player_action_encode(:startingskill4), do: :STARTING_SKILL_4
  defp player_action_encode(:executingskill1), do: :EXECUTING_SKILL_1
  defp player_action_encode(:executingskill2), do: :EXECUTING_SKILL_2
  defp player_action_encode(:executingskill3), do: :EXECUTING_SKILL_3
  defp player_action_encode(:executingskill4), do: :EXECUTING_SKILL_4
  defp player_action_encode(:moving), do: :MOVING

  defp player_action_decode(:ATTACKING), do: :attacking
  defp player_action_decode(:NOTHING), do: :nothing
  defp player_action_decode(:ATTACKING_AOE), do: :attackingaoe
  defp player_action_decode(:STARTING_SKILL_1), do: :startingskill1
  defp player_action_decode(:STARTING_SKILL_2), do: :startingskill2
  defp player_action_decode(:STARTING_SKILL_3), do: :startingskill3
  defp player_action_decode(:STARTING_SKILL_4), do: :startingskill4
  defp player_action_decode(:EXECUTING_SKILL_1), do: :executingskill1
  defp player_action_decode(:EXECUTING_SKILL_2), do: :executingskill2
  defp player_action_decode(:EXECUTING_SKILL_3), do: :executingskill3
  defp player_action_decode(:EXECUTING_SKILL_4), do: :executingskill4
  defp player_action_decode(:MOVING), do: :moving

  defp projectile_encode(:bullet), do: :BULLET
  defp projectile_encode(:disarmingbullet), do: :DISARMING_BULLET
  defp projectile_decode(:BULLET), do: :bullet
  defp projectile_decode(:DISARMING_BULLET), do: :disarmingbullet

  defp projectile_status_encode(:active), do: :ACTIVE
  defp projectile_status_encode(:exploded), do: :EXPLODED

  defp projectile_status_decode(:ACTIVE), do: :active
  defp projectile_status_decode(:EXPLODED), do: :exploded

  defp effect_encode({:petrified, %{ends_at: ends_at, caused_by: caused_by}}),
    do: {0, %{ends_at: ends_at, caused_by: caused_by}}

  defp effect_encode({:disarmed, %{ends_at: ends_at, caused_by: caused_by}}),
    do: {1, %{ends_at: ends_at, caused_by: caused_by}}

  defp effect_encode({:denial_of_service, %{ends_at: ends_at, caused_by: caused_by}}),
    do: {2, %{ends_at: ends_at, caused_by: caused_by}}

  defp effect_encode({:raged, %{ends_at: ends_at, caused_by: caused_by}}),
    do: {3, %{ends_at: ends_at, caused_by: caused_by}}

  defp effect_encode({:neon_crashing, %{ends_at: ends_at, caused_by: caused_by}}),
    do: {4, %{ends_at: ends_at, caused_by: caused_by}}

  defp effect_encode({:leaping, %{ends_at: ends_at, caused_by: caused_by}}),
    do: {5, %{ends_at: ends_at, caused_by: caused_by}}

  defp effect_encode({:out_of_area, %{ends_at: ends_at, caused_by: caused_by}}),
    do: {6, %{ends_at: ends_at, caused_by: caused_by}}

  defp effect_encode({:elnar_mark, %{ends_at: ends_at, caused_by: caused_by}}),
    do: {7, %{ends_at: ends_at, caused_by: caused_by}}

  defp effect_encode({:yugen_mark, %{ends_at: ends_at, caused_by: caused_by}}),
    do: {8, %{ends_at: ends_at, caused_by: caused_by}}

  defp effect_encode({:xanda_mark, %{ends_at: ends_at, caused_by: caused_by}}),
    do: {9, %{ends_at: ends_at, caused_by: caused_by}}

  defp effect_encode({:xanda_mark_owner, %{ends_at: ends_at, caused_by: caused_by}}),
    do: {10, %{ends_at: ends_at, caused_by: caused_by}}

  defp effect_encode({:poisoned, %{ends_at: ends_at, caused_by: caused_by}}),
    do: {11, %{ends_at: ends_at, caused_by: caused_by}}

  defp effect_encode({:slowed, %{ends_at: ends_at, caused_by: caused_by}}),
    do: {12, %{ends_at: ends_at, caused_by: caused_by}}

  defp effect_encode({:fiery_rampage, %{ends_at: ends_at, caused_by: caused_by}}),
    do: {13, %{ends_at: ends_at, caused_by: caused_by}}

  defp effect_encode({:burned, %{ends_at: ends_at, caused_by: caused_by}}),
    do: {14, %{ends_at: ends_at, caused_by: caused_by}}

  defp effect_encode({:scherzo, %{ends_at: ends_at, caused_by: caused_by}}),
    do: {15, %{ends_at: ends_at, caused_by: caused_by}}

  defp effect_encode({:danse_macabre, %{ends_at: ends_at, caused_by: caused_by}}),
    do: {16, %{ends_at: ends_at, caused_by: caused_by}}

  defp effect_encode({:paralyzed, %{ends_at: ends_at, caused_by: caused_by}}),
    do: {17, %{ends_at: ends_at, caused_by: caused_by}}

  defp loot_type_encode({:health, _}), do: :LOOT_HEALTH
end

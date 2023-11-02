defmodule DarkWorldsServer.Communication.Proto.Config do
  @moduledoc false

  use Protobuf, syntax: :proto3, protoc_gen_elixir_version: "0.12.0"

  field :characters_config, 1,
    type: DarkWorldsServer.Communication.Proto.GameCharactersConfig,
    json_name: "charactersConfig"

  field :effects_config, 2,
    type: DarkWorldsServer.Communication.Proto.GameEffectsConfig,
    json_name: "effectsConfig"

  field :game_config, 3,
    type: DarkWorldsServer.Communication.Proto.GameStateConfig,
    json_name: "gameConfig"

  field :loots_config, 4,
    type: DarkWorldsServer.Communication.Proto.GameLootsConfig,
    json_name: "lootsConfig"

  field :projectiles_config, 5,
    type: DarkWorldsServer.Communication.Proto.GameProjectilesConfig,
    json_name: "projectilesConfig"

  field :game_skills_config, 6,
    type: DarkWorldsServer.Communication.Proto.GameSkillsConfig,
    json_name: "gameSkillsConfig"

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end

defmodule DarkWorldsServer.Communication.Proto.GameCharactersConfig do
  @moduledoc false

  use Protobuf, syntax: :proto3, protoc_gen_elixir_version: "0.12.0"

  field :characters, 1, repeated: true, type: DarkWorldsServer.Communication.Proto.GameCharacter

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end

defmodule DarkWorldsServer.Communication.Proto.GameEffectsConfig do
  @moduledoc false

  use Protobuf, syntax: :proto3, protoc_gen_elixir_version: "0.12.0"

  field :effects, 1, repeated: true, type: DarkWorldsServer.Communication.Proto.GameEffect

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end

defmodule DarkWorldsServer.Communication.Proto.GameStateConfig do
  @moduledoc false

  use Protobuf, syntax: :proto3, protoc_gen_elixir_version: "0.12.0"

  field :width, 1, type: :uint64
  field :height, 2, type: :uint64

  field :map_modification, 3,
    type: DarkWorldsServer.Communication.Proto.MapModification,
    json_name: "mapModification"

  field :loot_interval_ms, 4, type: :uint64, json_name: "lootIntervalMs"

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end

defmodule DarkWorldsServer.Communication.Proto.GameLootsConfig do
  @moduledoc false

  use Protobuf, syntax: :proto3, protoc_gen_elixir_version: "0.12.0"

  field :loots, 1, repeated: true, type: DarkWorldsServer.Communication.Proto.GameLoot

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end

defmodule DarkWorldsServer.Communication.Proto.GameProjectilesConfig do
  @moduledoc false

  use Protobuf, syntax: :proto3, protoc_gen_elixir_version: "0.12.0"

  field :projectiles_config, 1,
    repeated: true,
    type: DarkWorldsServer.Communication.Proto.GameProjectile,
    json_name: "projectilesConfig"

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end

defmodule DarkWorldsServer.Communication.Proto.GameSkillsConfig do
  @moduledoc false

  use Protobuf, syntax: :proto3, protoc_gen_elixir_version: "0.12.0"

  field :skills, 1, repeated: true, type: DarkWorldsServer.Communication.Proto.GameSkill

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end

defmodule DarkWorldsServer.Communication.Proto.MapModification do
  @moduledoc false

  use Protobuf, syntax: :proto3, protoc_gen_elixir_version: "0.12.0"

  field :modification, 1, type: DarkWorldsServer.Communication.Proto.Modification
  field :starting_radius, 2, type: :uint64, json_name: "startingRadius"
  field :minimum_radius, 3, type: :uint64, json_name: "minimumRadius"
  field :max_radius, 4, type: :uint64, json_name: "maxRadius"

  field :outside_radius_effects, 5,
    repeated: true,
    type: :string,
    json_name: "outsideRadiusEffects"

  field :inside_radius_effects, 6, repeated: true, type: :string, json_name: "insideRadiusEffects"

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end

defmodule DarkWorldsServer.Communication.Proto.Modification do
  @moduledoc false

  use Protobuf, syntax: :proto3, protoc_gen_elixir_version: "0.12.0"

  field :modifier, 1, type: :string
  field :value, 2, type: :int64

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end

defmodule DarkWorldsServer.Communication.Proto.GameLoot do
  @moduledoc false

  use Protobuf, syntax: :proto3, protoc_gen_elixir_version: "0.12.0"

  field :name, 1, type: :string
  field :size, 2, type: :uint64
  field :effects, 3, repeated: true, type: :string

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end

defmodule DarkWorldsServer.Communication.Proto.GameProjectile do
  @moduledoc false

  use Protobuf, syntax: :proto3, protoc_gen_elixir_version: "0.12.0"

  field :name, 1, type: :string
  field :base_damage, 2, type: :uint64, json_name: "baseDamage"
  field :base_speed, 3, type: :uint64, json_name: "baseSpeed"
  field :base_size, 4, type: :uint64, json_name: "baseSize"
  field :player_collision, 5, type: :bool, json_name: "playerCollision"
  field :on_hit_effect, 6, repeated: true, type: :string, json_name: "onHitEffect"
  field :max_distance, 7, type: :uint64, json_name: "maxDistance"
  field :duration_ms, 8, type: :float, json_name: "durationMs"

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end

defmodule DarkWorldsServer.Communication.Proto.GameCharacter do
  @moduledoc false

  use Protobuf, syntax: :proto3, protoc_gen_elixir_version: "0.12.0"

  field :name, 1, type: :string
  field :active, 2, type: :bool
  field :base_speed, 3, type: :uint64, json_name: "baseSpeed"
  field :base_size, 4, type: :uint64, json_name: "baseSize"
  field :skills, 5, repeated: true, type: DarkWorldsServer.Communication.Proto.GameSkill

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end

defmodule DarkWorldsServer.Communication.Proto.GameSkill do
  @moduledoc false

  use Protobuf, syntax: :proto3, protoc_gen_elixir_version: "0.12.0"

  field :name, 1, type: :string
  field :cooldown_ms, 2, type: :uint64, json_name: "cooldownMs"
  field :is_passive, 3, type: :bool, json_name: "isPassive"
  field :mechanics, 4, repeated: true, type: DarkWorldsServer.Communication.Proto.Mechanic

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end

defmodule DarkWorldsServer.Communication.Proto.Mechanic do
  @moduledoc false

  use Protobuf, syntax: :proto3, protoc_gen_elixir_version: "0.12.0"

  field :name, 1, type: :string
  field :effects, 2, repeated: true, type: :string
  field :damage, 3, type: :uint64
  field :range, 4, type: :uint64
  field :cone_angle, 5, type: :uint64, json_name: "coneAngle"
  field :on_hit_effects, 6, repeated: true, type: :string, json_name: "onHitEffects"
  field :projectile, 7, type: DarkWorldsServer.Communication.Proto.GameProjectile
  field :count, 8, type: :uint64
  field :duration_ms, 9, type: :uint64, json_name: "durationMs"
  field :max_range, 10, type: :uint64, json_name: "maxRange"

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end

defmodule DarkWorldsServer.Communication.Proto.GameEffect.Duration do
  @moduledoc false

  use Protobuf, syntax: :proto3, protoc_gen_elixir_version: "0.12.0"

  field :type, 1, type: :string
  field :duration_ms, 2, type: :uint64, json_name: "durationMs"

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end

defmodule DarkWorldsServer.Communication.Proto.GameEffect.Periodic do
  @moduledoc false

  use Protobuf, syntax: :proto3, protoc_gen_elixir_version: "0.12.0"

  field :type, 1, type: :string
  field :instant_application, 2, type: :string, json_name: "instantApplication"
  field :interval_ms, 3, type: :uint64, json_name: "intervalMs"
  field :trigger_count, 4, type: :uint64, json_name: "triggerCount"

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end

defmodule DarkWorldsServer.Communication.Proto.GameEffect do
  @moduledoc false

  use Protobuf, syntax: :proto3, protoc_gen_elixir_version: "0.12.0"

  oneof :effect_type, 0

  field :name, 1, type: :string
  field :simple_type, 2, type: :string, json_name: "simpleType", oneof: 0

  field :duration_type, 3,
    type: DarkWorldsServer.Communication.Proto.GameEffect.Duration,
    json_name: "durationType",
    oneof: 0

  field :periodic_type, 4,
    type: DarkWorldsServer.Communication.Proto.GameEffect.Periodic,
    json_name: "periodicType",
    oneof: 0

  def transform_module(), do: DarkWorldsServer.Communication.ProtoTransform
end
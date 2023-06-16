defmodule DarkWorldsServer.Leaderboard do
    use Ecto.Schema
    import Ecto.Changeset

    schema "leaderboard" do
        field(:kills, :integer)
        field(:deaths, :integer)
        field(:lobby_id, :string)
        field(:user_id, :integer)

        timestamps()
    end

    @required []
    @permitted [:kills, :deaths, :lobby_id, :user_id] ++ @required

    def changeset(leaderboard, params) do
        leaderboard
        |> cast(params, @permitted)
        |> validate_required(@required)
    end
end
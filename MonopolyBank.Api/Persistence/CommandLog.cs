using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace MonopolyBank.Api.Persistence;

/// <summary>
/// Append-only SQLite log of game commands. Holds a single open connection for
/// its lifetime (required for "Data Source=:memory:", where every new connection
/// is a fresh database); access is serialized with a lock.
/// </summary>
public sealed class CommandLog : IDisposable
{
    private const int SchemaVersion = 1;

    private readonly SqliteConnection _connection;
    private readonly Lock _lock = new();

    public CommandLog(string connectionString)
    {
        _connection = new SqliteConnection(connectionString);
        _connection.Open();

        using var create = _connection.CreateCommand();
        create.CommandText =
            """
            CREATE TABLE IF NOT EXISTS game_commands (
                gameId  TEXT    NOT NULL,
                seq     INTEGER NOT NULL,
                version INTEGER NOT NULL,
                type    TEXT    NOT NULL,
                payload TEXT    NOT NULL,
                PRIMARY KEY (gameId, seq)
            );
            """;
        create.ExecuteNonQuery();
    }

    public void Append(Guid gameId, GameCommand command)
    {
        lock (_lock)
        {
            using var insert = _connection.CreateCommand();
            insert.CommandText =
                """
                INSERT INTO game_commands (gameId, seq, version, type, payload)
                VALUES (
                    $gameId,
                    (SELECT COALESCE(MAX(seq), -1) + 1 FROM game_commands WHERE gameId = $gameId),
                    $version, $type, $payload);
                """;
            insert.Parameters.AddWithValue("$gameId", gameId.ToString());
            insert.Parameters.AddWithValue("$version", SchemaVersion);
            insert.Parameters.AddWithValue("$type", command.GetType().Name);
            insert.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(command, command.GetType()));
            insert.ExecuteNonQuery();
        }
    }

    public IReadOnlyList<GameCommand> Read(Guid gameId)
    {
        lock (_lock)
        {
            using var select = _connection.CreateCommand();
            select.CommandText =
                """
                SELECT type, payload FROM game_commands
                WHERE gameId = $gameId
                ORDER BY seq;
                """;
            select.Parameters.AddWithValue("$gameId", gameId.ToString());

            var commands = new List<GameCommand>();
            using var reader = select.ExecuteReader();
            while (reader.Read())
                commands.Add(Deserialize(reader.GetString(0), reader.GetString(1)));

            return commands;
        }
    }

    public IReadOnlyList<Guid> GameIds()
    {
        lock (_lock)
        {
            using var select = _connection.CreateCommand();
            select.CommandText =
                """
                SELECT gameId FROM game_commands
                GROUP BY gameId
                ORDER BY MIN(rowid);
                """;

            var ids = new List<Guid>();
            using var reader = select.ExecuteReader();
            while (reader.Read())
                ids.Add(Guid.Parse(reader.GetString(0)));

            return ids;
        }
    }

    public void Dispose() => _connection.Dispose();

    private static GameCommand Deserialize(string type, string payload) => type switch
    {
        nameof(GameCommand.CreateGame) => JsonSerializer.Deserialize<GameCommand.CreateGame>(payload)!,
        nameof(GameCommand.AddUser) => JsonSerializer.Deserialize<GameCommand.AddUser>(payload)!,
        nameof(GameCommand.SetStartAmount) => JsonSerializer.Deserialize<GameCommand.SetStartAmount>(payload)!,
        nameof(GameCommand.SetCurrency) => JsonSerializer.Deserialize<GameCommand.SetCurrency>(payload)!,
        nameof(GameCommand.Start) => JsonSerializer.Deserialize<GameCommand.Start>(payload)!,
        nameof(GameCommand.PlayerTransfer) => JsonSerializer.Deserialize<GameCommand.PlayerTransfer>(payload)!,
        nameof(GameCommand.BankerTransfer) => JsonSerializer.Deserialize<GameCommand.BankerTransfer>(payload)!,
        _ => throw new InvalidOperationException($"Unknown command type '{type}' in the command log.")
    };
}

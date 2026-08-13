using System.Collections.Concurrent;
using MonopolyBank.Api.Persistence;
using MonopolyBank.Domain;

namespace MonopolyBank.Api;

/// <summary>
/// The API's game registry: live domain objects in memory, durability via the
/// command log. Every mutation is applied to the domain FIRST (so domain guards
/// can reject it) and appended to the log only on success. A game missing from
/// the cache is rehydrated by replaying its log.
/// </summary>
public class GameStore(CommandLog log)
{
    private readonly ConcurrentDictionary<Guid, Game> _games = new();

    public Guid CreateGame()
    {
        var gameId = Guid.NewGuid();
        _games[gameId] = new Game();
        log.Append(gameId, new GameCommand.CreateGame());
        return gameId;
    }

    public IReadOnlyList<Guid> GameIds() => log.GameIds();

    public Game? Find(Guid gameId)
    {
        if (_games.TryGetValue(gameId, out var cached))
            return cached;

        var commands = log.Read(gameId);
        if (commands.Count == 0)
            return null;

        var game = Replay(commands);
        _games[gameId] = game;
        return game;
    }

    public void Execute(Guid gameId, GameCommand command)
    {
        var game = Find(gameId) ?? throw new GameNotFoundException(gameId);
        Apply(game, command);
        log.Append(gameId, command);
    }

    private static Game Replay(IReadOnlyList<GameCommand> commands)
    {
        var game = new Game();
        foreach (var command in commands.Where(c => c is not GameCommand.CreateGame))
            Apply(game, command);
        return game;
    }

    private static void Apply(Game game, GameCommand command)
    {
        switch (command)
        {
            case GameCommand.AddUser(var name, var type):
                game.AddUser(new User(name, type));
                break;
            case GameCommand.SetStartAmount(var amount):
                BankerOf(game).SetStartAmount(amount);
                break;
            case GameCommand.SetCurrency(var currency):
                BankerOf(game).SetCurrency(currency);
                break;
            case GameCommand.Start:
                game.Start();
                break;
            case GameCommand.PlayerTransfer(var from, var to, var amount):
                PlayerOf(game, from).TransferMoney(PlayerOf(game, to), amount);
                break;
            case GameCommand.BankerTransfer(var to, var amount):
                BankerOf(game).TransferMoney(PlayerOf(game, to), amount);
                break;
            default:
                throw new InvalidOperationException($"Cannot apply command {command.GetType().Name}.");
        }
    }

    private static Banker BankerOf(Game game) =>
        game.Users.First(u => u.IsBanker).Banker;

    private static Player PlayerOf(Game game, string name) =>
        game.Users.Select(u => u.Player).First(p => p.Name == name);
}

public class GameNotFoundException(Guid gameId)
    : KeyNotFoundException($"No game with id '{gameId}'.");

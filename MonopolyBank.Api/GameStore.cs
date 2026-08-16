using System.Collections.Concurrent;
using MonopolyBank.Api.Persistence;
using MonopolyBank.Domain;

namespace MonopolyBank.Api;

/// <summary>
/// The API's game registry: live domain objects in memory, durability via the
/// command log. Every mutation is applied to the domain FIRST (so domain guards
/// can reject it) and appended to the log only on success. A game missing from
/// the cache is rehydrated by replaying its log. Users are identified by the
/// UserId minted at creation; the Nth AddUser command corresponds to the Nth
/// game user (only successful adds are logged, in order), so id resolution is
/// positional and exact even with duplicate names — the domain stays id-free.
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

    /// <summary>
    /// The users added to a game, straight from the command log: exactly the
    /// successful adds, in order, with the identity the API minted for them.
    /// </summary>
    public IReadOnlyList<GameCommand.AddUser> UsersOf(Guid gameId) =>
        log.Read(gameId).OfType<GameCommand.AddUser>().ToList();

    /// <summary>A user's identity joined with their live domain state.</summary>
    public GameUser? FindUser(Guid gameId, Guid userId)
    {
        var game = Find(gameId);
        if (game is null)
            return null;

        var added = UsersOf(gameId);
        var index = added.TakeWhile(u => u.UserId != userId).Count();
        if (index == added.Count)
            return null;

        var player = game.Users.ElementAt(index).Player;
        return new GameUser(userId, added[index].Name, added[index].Role, new Money(player.Money), player.BankCard);
    }

    public void Execute(Guid gameId, GameCommand command)
    {
        var game = Find(gameId) ?? throw new GameNotFoundException(gameId);
        Apply(game, command, UsersOf(gameId).Select(u => u.UserId).ToList());
        log.Append(gameId, command);
    }

    private static Game Replay(IReadOnlyList<GameCommand> commands)
    {
        var game = new Game();
        var userIds = new List<Guid>();
        foreach (var command in commands.Where(c => c is not GameCommand.CreateGame))
        {
            Apply(game, command, userIds);
            if (command is GameCommand.AddUser added)
                userIds.Add(added.UserId);
        }

        return game;
    }

    private static void Apply(Game game, GameCommand command, IReadOnlyList<Guid> userIds)
    {
        switch (command)
        {
            case GameCommand.AddUser(_, var name, var role):
                game.AddUser(new User(name, role));
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
                UserAt(from).Player.TransferMoney(UserAt(to).Player, amount);
                break;
            case GameCommand.BankerTransfer(var from, var to, var amount):
                UserAt(from).Banker.TransferMoney(UserAt(to).Player, amount);
                break;
            default:
                throw new InvalidOperationException($"Cannot apply command {command.GetType().Name}.");
        }

        return;

        User UserAt(Guid userId) => game.Users.ElementAt(userIds.TakeWhile(id => id != userId).Count());
    }

    private static Banker BankerOf(Game game) =>
        game.Users.First(u => u.IsBanker).Banker;
}

public record GameUser(Guid UserId, string Name, UserRole Role, Money Money, BankCard BankCard);

public record Money(int Amount);

public class GameNotFoundException(Guid gameId)
    : KeyNotFoundException($"No game with id '{gameId}'.");

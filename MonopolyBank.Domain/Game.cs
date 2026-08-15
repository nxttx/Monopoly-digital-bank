namespace MonopolyBank.Domain;

public class Game
{
    public const int DefaultStartAmount = 1500;

    private readonly List<User> _users = [];

    public IReadOnlyCollection<User> Users => _users;

    public Currencies Currency { get; internal set; } = Currencies.Monopolonian;

    public bool Started { get; private set; }

    internal List<Transaction> Ledger { get; } = [];

    private User? BankerUser => _users.FirstOrDefault(u => u.IsBanker);

    public void Start()
    {
        if (Started)
            throw new AlreadyStartedGameException();

        if (BankerUser is not { } banker || _users.Count(u => u.IsPlayer) < 2)
            throw new AmountOfPlayersException();

        Started = true;

        foreach (var user in _users)
            Ledger.Add(new Transaction(banker.Banker, user.Player, banker.Banker.StartAmount));
    }

    public void AddUser(User user)
    {
        if (user.Player.Game is not null)
            throw new UserAlreadyInGameException();

        if (user.IsBanker && BankerUser is not null)
            throw new DuplicateBankerException();

        _users.Add(user);
        user.Player.Game = this;
        user.Banker.Game = this;

        if (BankerUser is not { } banker)
            return;
        
        user.Player.Adjust(banker.Banker.StartAmount - user.Player.Money);

        if (Started)
            Ledger.Add(new Transaction(banker.Banker, user.Player, banker.Banker.StartAmount));
    }
}

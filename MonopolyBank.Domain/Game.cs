namespace MonopolyBank.Domain;

public class Game
{
    private readonly List<User> _users = [];

    public IReadOnlyCollection<User> Users => _users;

    public Currencies Currency { get; internal set; } = Currencies.Monopolonian;

    internal bool Started { get; private set; }

    internal List<Transaction> Ledger { get; } = [];

    public void Start()
    {
        if (_users.Count(u => u.IsBanker) < 1 || _users.Count(u => u.IsPlayer) < 2)
            throw new AmountOfPlayersException();

        Started = true;

        var banker = _users.First(u => u.IsBanker);
        foreach (var user in _users)
            Ledger.Add(new Transaction(banker.Banker, user.Player, banker.Banker.StartAmount));
    }

    public void AddUser(User user)
    {
        if (user.Player.Game is not null)
            throw new UserAlreadyInGameException();

        if (user.IsBanker && _users.Any(u => u.IsBanker))
            throw new DuplicateBankerException();

        _users.Add(user);
        user.Player.Game = this;
        user.Banker.Game = this;

        var banker = _users.FirstOrDefault(u => u.IsBanker);
        if (banker is null)
            return;

        user.Player.Adjust(banker.Banker.StartAmount - user.Player.Money);

        if (Started)
            Ledger.Add(new Transaction(banker.Banker, user.Player, banker.Banker.StartAmount));
    }
}

namespace MonopolyBank.Domain;

public class Game
{
    internal const int DefaultStartAmount = 1500;

    private readonly List<User> _users = [];
    private readonly List<Transaction> _ledger = [];

    public IReadOnlyList<User> Users => _users;

    public int StartAmount { get; private set; } = DefaultStartAmount;

    public Currencies Currency { get; private set; }

    public bool HasStarted { get; private set; }

    internal IReadOnlyList<Transaction> Ledger => _ledger;

    public void AddUser(User user)
    {
        if (user.Game is not null && user.Game != this)
            throw new UserAlreadyInGameException("A user cannot be added to two different games.");
        if (user.IsBanker && _users.Any(u => u.IsBanker))
            throw new DuplicateBankerException("There can only be one banker per game.");

        _users.Add(user);
        user.Game = this;
        user.Player.Money = StartAmount;

        if (HasStarted && user.IsPlayer)
            RecordStartingMoney(user);
    }

    public void Start()
    {
        if (_users.Count(u => u.IsBanker) < 1 || _users.Count(u => u.IsPlayer) < 2)
            throw new AmountOfPlayersException("A game requires at least one banker and two players.");

        HasStarted = true;
        foreach (var user in _users.Where(u => u.IsPlayer))
            RecordStartingMoney(user);
    }

    internal void SetStartAmount(int amount)
    {
        StartAmount = amount;
        foreach (var user in _users)
            user.Player.Money = amount;
    }

    internal void SetCurrency(Currencies currency) => Currency = currency;

    internal void Record(Transaction transaction) => _ledger.Add(transaction);

    private void RecordStartingMoney(User user) =>
        _ledger.Add(new Transaction(TheBanker, user.Player, StartAmount));

    private Banker TheBanker => _users.First(u => u.IsBanker).Banker;
}

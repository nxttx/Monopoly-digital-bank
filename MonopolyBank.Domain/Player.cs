namespace MonopolyBank.Domain;

public class Player : Party
{
    private readonly User? _user;

    public Player(string name) : this(name, null)
    {
    }

    internal Player(string name, User? user)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Name cannot be empty.");

        Name = name;
        _user = user;
    }

    public string Name { get; }

    public BankCard BankCard { get; } = new();

    public int Money { get; internal set; } = Game.DefaultStartAmount;

    public IReadOnlyList<Transaction> History =>
        CurrentGame?.Ledger.Where(t => t.From == this || t.To == this).ToList() ?? [];

    private Game? CurrentGame => _user?.Game;

    public void TransferMoney(Player to, int amount)
    {
        if (CurrentGame != to.CurrentGame)
            throw new CrossGameAccessException("Cannot transfer money to another user from another game.");
        if (CurrentGame is not { HasStarted: true })
            throw new GameNotStartedException("Cannot transfer money before the game has started.");
        if (_user is { IsPlayer: false })
            throw new MissingRoleException("User does not have the player role.");
        if (amount < 0)
            throw new NegativeTransferException("A player cannot transfer a negative amount.");
        if (Money - amount < 0)
            throw new InsufficientBalanceException("A player's balance cannot go negative.");

        Money -= amount;
        to.Money += amount;
        CurrentGame.Record(new Transaction(this, to, amount));
    }
}

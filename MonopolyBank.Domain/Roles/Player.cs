namespace MonopolyBank.Domain;

public class Player : Role
{
    public Player(string name) : this(name, true)
    {
    }

    internal Player(string name, bool hasRole) : base(hasRole, "player")
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Name cannot be empty.");

        Name = name;
    }

    public string Name { get; }

    public BankCard BankCard { get; } = new();

    public int Money { get; private set; } = MonopolyBank.Domain.Game.DefaultStartAmount;

    public void TransferMoney(Player to, int amount)
    {
        if (Game != to.Game)
            throw new CrossGameAccessException();

        var game = EnsureGameStarted();
        EnsureHasRole();

        if (amount < 0)
            throw new NegativeTransferException();

        Adjust(-amount);
        to.Adjust(amount);
        game.Ledger.Add(new Transaction(this, to, amount));
    }

    internal void Adjust(int amount)
    {
        if (Money + amount < 0)
            throw new InsufficientBalanceException();

        Money += amount;
    }
}

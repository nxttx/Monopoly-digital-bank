namespace MonopolyBank.Domain;

public class Player(bool hasRole)
{
    public Player(string name) : this(true)
    {
        Name = name;
    }

    public string Name { get; } = "";

    public string CardNumber { get; } = Guid.NewGuid().ToString();

    public DateTime CardExpiry { get; } = DateTime.Now.AddYears(1);

    public string CardCvv { get; } = Random.Shared.Next(1000).ToString("D3");

    public int Money { get; private set; } = 1000;

    internal Game? Game { get; set; }

    public void TransferMoney(Player to, int amount)
    {
        if (Game != to.Game)
            throw new CrossGameAccessException();

        if (!hasRole)
            throw new MissingRoleException("player");

        if (amount < 0)
            throw new NegativeTransferException();

        Adjust(-amount);
        to.Adjust(amount);
    }

    internal void Adjust(int amount)
    {
        if (Money + amount < 0)
            throw new InsufficientBalanceException();

        Money += amount;
    }
}

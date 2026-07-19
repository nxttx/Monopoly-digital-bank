namespace MonopolyBank.Domain;

public class Player(bool hasRole)
{
    public int Money { get; private set; } = 1000;

    public void TransferMoney(Player to, int amount)
    {
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

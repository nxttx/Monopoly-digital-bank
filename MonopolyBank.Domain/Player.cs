namespace MonopolyBank.Domain;

public class Player(bool hasRole)
{
    public int Money { get; private set; } = 1000;

    public void TransferMoney(Player to, int amount)
    {
        if (!hasRole)
            throw new InvalidOperationException("User does not have the player role.");

        if (amount < 0)
            throw new InvalidOperationException("A player cannot transfer a negative amount.");

        Adjust(-amount);
        to.Adjust(amount);
    }

    internal void Adjust(int amount)
    {
        if (Money + amount < 0)
            throw new InvalidOperationException("A player's balance cannot go negative.");

        Money += amount;
    }
}

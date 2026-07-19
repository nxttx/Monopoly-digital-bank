namespace MonopolyBank.Domain;

public class Player
{
    public int Money { get; internal set; } = 1000;

    public void TransferMoney(Player to, int amount)
    {
        Money -= amount;
        to.Money += amount;
    }
}

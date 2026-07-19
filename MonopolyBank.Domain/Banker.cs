namespace MonopolyBank.Domain;

public class Banker(bool hasRole)
{
    public void TransferMoney(Player to, int amount)
    {
        if (!hasRole)
            throw new InvalidOperationException("User does not have the banker role.");

        if (to.Money + amount < 0)
            throw new InvalidOperationException("The bank cannot take more money than the player has.");

        to.Money += amount;
    }
}

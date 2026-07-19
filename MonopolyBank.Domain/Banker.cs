namespace MonopolyBank.Domain;

public class Banker(bool hasRole)
{
    public void TransferMoney(Player to, int amount)
    {
        if (!hasRole)
            throw new InvalidOperationException("User does not have the banker role.");

        to.Adjust(amount);
    }
}

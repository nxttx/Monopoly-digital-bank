namespace MonopolyBank.Domain;

public class Banker(bool hasRole)
{
    public void TransferMoney(Player to, int amount)
    {
        if (!hasRole)
            throw new MissingRoleException("banker");

        to.Adjust(amount);
    }
}

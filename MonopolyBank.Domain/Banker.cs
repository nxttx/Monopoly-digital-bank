namespace MonopolyBank.Domain;

public class Banker : Role
{
    internal Banker(bool hasRole) : base(hasRole, "banker")
    {
    }

    public void TransferMoney(Player to, int amount)
    {
        EnsureHasRole();

        to.Adjust(amount);
    }
}

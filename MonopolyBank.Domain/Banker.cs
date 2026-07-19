namespace MonopolyBank.Domain;

public class Banker : Role
{
    internal Banker(bool hasRole) : base(hasRole, "banker")
    {
    }

    internal Game? Game { get; set; }

    public void TransferMoney(Player to, int amount)
    {
        if (Game is not { Started: true })
            throw new GameNotStartedException();

        EnsureHasRole();

        to.Adjust(amount);
    }
}

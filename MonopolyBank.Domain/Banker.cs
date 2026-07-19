namespace MonopolyBank.Domain;

public class Banker : Role
{
    internal Banker(bool hasRole) : base(hasRole, "banker")
    {
    }

    internal Game? Game { get; set; }

    public IEnumerable<Player> Players =>
        Game?.Users.Where(u => u.IsPlayer).Select(u => u.Player) ?? [];

    public void TransferMoney(Player to, int amount)
    {
        if (Game is not { Started: true })
            throw new GameNotStartedException();

        EnsureHasRole();

        to.Adjust(amount);
    }
}

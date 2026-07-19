namespace MonopolyBank.Domain;

public class Banker : Role
{
    internal Banker(bool hasRole) : base(hasRole, "banker")
    {
    }

    public IReadOnlyList<Transaction> GlobalHistory => Game?.Ledger ?? [];

    public IEnumerable<Player> Players =>
        Game?.Users.Where(u => u.IsPlayer).Select(u => u.Player) ?? [];

    public int StartAmount { get; private set; } = 1500;

    public void SetStartAmount(int amount)
    {
        EnsureHasRole();

        if (Game is { Started: true })
            throw new GameHasStartedException();

        StartAmount = amount;

        if (Game is null)
            return;

        foreach (var user in Game.Users)
            user.Player.Adjust(amount - user.Player.Money);
    }

    public void TransferMoney(Player to, int amount)
    {
        if (Game is not { Started: true })
            throw new GameNotStartedException();

        EnsureHasRole();

        to.Adjust(amount);
        Game.Ledger.Add(new Transaction(this, to, amount));
    }
}

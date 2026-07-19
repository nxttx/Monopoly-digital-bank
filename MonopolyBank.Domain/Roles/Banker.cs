namespace MonopolyBank.Domain;

public class Banker : Role
{
    internal Banker(bool hasRole) : base(hasRole, "banker")
    {
    }

    public int StartAmount { get; private set; } = MonopolyBank.Domain.Game.DefaultStartAmount;

    public IReadOnlyList<Transaction> GlobalHistory => Game?.Ledger ?? [];

    public IEnumerable<Player> Players =>
        Game?.Users.Where(u => u.IsPlayer).Select(u => u.Player) ?? [];

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

    public void SetCurrency(Currencies currency)
    {
        EnsureHasRole();

        if (Game is not null)
            Game.Currency = currency;
    }

    public void TransferMoney(Player to, int amount)
    {
        var game = EnsureGameStarted();
        EnsureHasRole();

        to.Adjust(amount);
        game.Ledger.Add(new Transaction(this, to, amount));
    }
}

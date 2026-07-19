namespace MonopolyBank.Domain;

public class Banker : Party
{
    private readonly User _user;

    internal Banker(User user)
    {
        _user = user;
    }

    private Game? CurrentGame => _user.Game;

    public int StartAmount => CurrentGame?.StartAmount ?? Game.DefaultStartAmount;

    public IEnumerable<Player> Players =>
        CurrentGame?.Users.Where(u => u.IsPlayer).Select(u => u.Player) ?? [];

    public IReadOnlyList<Transaction> History =>
        CurrentGame?.Ledger.Where(t => t.From == this || t.To == this).ToList() ?? [];

    public IReadOnlyList<Transaction> GlobalHistory => CurrentGame?.Ledger ?? [];

    public void TransferMoney(Player to, int amount)
    {
        RequireBankerRole();
        if (CurrentGame is not { HasStarted: true })
            throw new GameNotStartedException("Cannot transfer money before the game has started.");
        if (to.Money + amount < 0)
            throw new InsufficientBalanceException("A player's balance cannot go negative.");

        to.Money += amount;
        CurrentGame.Record(new Transaction(this, to, amount));
    }

    public void SetStartAmount(int amount)
    {
        RequireBankerRole();
        if (CurrentGame is { HasStarted: true })
            throw new GameHasStartedException("Cannot set the start amount of money when the game has started.");

        CurrentGame?.SetStartAmount(amount);
    }

    public void SetCurrency(Currencies currency)
    {
        RequireBankerRole();
        CurrentGame?.SetCurrency(currency);
    }

    private void RequireBankerRole()
    {
        if (!_user.IsBanker)
            throw new MissingRoleException("User does not have the banker role.");
    }
}

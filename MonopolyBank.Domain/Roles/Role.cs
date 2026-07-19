namespace MonopolyBank.Domain;

public abstract class Role(bool hasRole, string roleName)
{
    internal Game? Game { get; set; }

    public IReadOnlyList<Transaction> History =>
        Game?.Ledger.Where(t => t.From == this || t.To == this).ToList() ?? [];

    protected void EnsureHasRole()
    {
        if (!hasRole)
            throw new MissingRoleException(roleName);
    }

    protected Game EnsureGameStarted()
    {
        if (Game is not { Started: true })
            throw new GameNotStartedException();

        return Game;
    }
}

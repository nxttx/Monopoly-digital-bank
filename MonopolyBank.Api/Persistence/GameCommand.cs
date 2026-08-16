using MonopolyBank.Domain;

namespace MonopolyBank.Api.Persistence;

/// <summary>
/// One entry in a game's command log. A game is rehydrated by replaying its
/// commands, in order, through the untouched domain — the domain's own guards
/// re-validate every step.
/// </summary>
public abstract record GameCommand
{
    public sealed record CreateGame : GameCommand;

    public sealed record AddUser(Guid UserId, string Name, UserRole Role) : GameCommand;

    public sealed record SetStartAmount(int Amount) : GameCommand;

    public sealed record SetCurrency(Currencies Currency) : GameCommand;

    public sealed record Start : GameCommand;

    public sealed record PlayerTransfer(Guid From, Guid To, int Amount) : GameCommand;

    public sealed record BankerTransfer(Guid From, Guid To, int Amount) : GameCommand;
}

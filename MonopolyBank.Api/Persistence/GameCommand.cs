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

    public sealed record AddUser(string Name, UserType Type) : GameCommand;

    public sealed record SetStartAmount(int Amount) : GameCommand;

    public sealed record SetCurrency(Currencies Currency) : GameCommand;

    public sealed record Start : GameCommand;

    public sealed record PlayerTransfer(string From, string To, int Amount) : GameCommand;

    public sealed record BankerTransfer(string To, int Amount) : GameCommand;
}

namespace MonopolyBank.Domain;

public class User
{
    public User(UserType type = UserType.Player)
    {
        IsPlayer = type is UserType.Player or UserType.Both;
        IsBanker = type is UserType.Banker or UserType.Both;
        Player = new Player(IsPlayer);
        Banker = new Banker(IsBanker);
    }

    public Player Player { get; }

    public Banker Banker { get; }

    public bool IsPlayer { get; }

    public bool IsBanker { get; }
}

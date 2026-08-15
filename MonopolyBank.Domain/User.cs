namespace MonopolyBank.Domain;

public class User
{
    public User(string name, UserRole role = UserRole.Player)
    {
        if (!Enum.IsDefined(role))
            throw new ArgumentException("Invalid user type.");

        IsPlayer = role is UserRole.Player or UserRole.Both;
        IsBanker = role is UserRole.Banker or UserRole.Both;
        Player = new Player(name, IsPlayer);
        Banker = new Banker(IsBanker);
    }

    public Player Player { get; }

    public Banker Banker { get; }

    public bool IsPlayer { get; }

    public bool IsBanker { get; }
}

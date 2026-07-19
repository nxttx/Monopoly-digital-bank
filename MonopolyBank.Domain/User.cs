namespace MonopolyBank.Domain;

public class User(UserType type = UserType.Player)
{
    public Player Player { get; } = new(type is UserType.Player or UserType.Both);

    public Banker Banker { get; } = new(type is UserType.Banker or UserType.Both);

    public bool IsPlayer => type is UserType.Player or UserType.Both;

    public bool IsBanker => type is UserType.Banker or UserType.Both;
}

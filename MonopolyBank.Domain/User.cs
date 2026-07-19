namespace MonopolyBank.Domain;

public class User
{
    private readonly UserType _type;

    public User(string name, UserType type = UserType.Player)
    {
        _type = type;
        Player = new Player(name, this);
        Banker = new Banker(this);
    }

    public bool IsPlayer => _type is UserType.Player or UserType.Both;

    public bool IsBanker => _type is UserType.Banker or UserType.Both;

    public Player Player { get; }

    public Banker Banker { get; }

    public Game? Game { get; internal set; }
}

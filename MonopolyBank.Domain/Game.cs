namespace MonopolyBank.Domain;

public class Game
{
    private readonly List<User> _users = [];

    public IReadOnlyCollection<User> Users => _users;

    internal bool Started { get; private set; }

    public void Start()
    {
        if (_users.Count(u => u.IsBanker) < 1 || _users.Count(u => u.IsPlayer) < 2)
            throw new AmountOfPlayersException();

        Started = true;
    }

    public void AddUser(User user)
    {
        if (user.Player.Game is not null)
            throw new UserAlreadyInGameException();

        if (user.IsBanker && _users.Any(u => u.IsBanker))
            throw new DuplicateBankerException();

        _users.Add(user);
        user.Player.Game = this;
        user.Banker.Game = this;

        var banker = _users.FirstOrDefault(u => u.IsBanker);
        if (banker is not null)
            user.Player.Adjust(banker.Banker.StartAmount - user.Player.Money);
    }
}

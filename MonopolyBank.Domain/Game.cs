namespace MonopolyBank.Domain;

public class Game
{
    private readonly List<User> _users = [];

    public IReadOnlyCollection<User> Users => _users;

    public void Start()
    {
        if (_users.Count(u => u.IsBanker) < 1 || _users.Count(u => u.IsPlayer) < 2)
            throw new AmountOfPlayersException();
    }

    public void AddUser(User user)
    {
        if (user.Player.Game is not null)
            throw new UserAlreadyInGameException();

        if (user.IsBanker && _users.Any(u => u.IsBanker))
            throw new DuplicateBankerException();

        _users.Add(user);
        user.Player.Game = this;
    }
}

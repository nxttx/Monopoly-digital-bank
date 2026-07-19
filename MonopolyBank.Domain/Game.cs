namespace MonopolyBank.Domain;

public class Game
{
    private readonly List<User> _users = [];

    public IReadOnlyCollection<User> Users => _users;

    public void AddUser(User user)
    {
        if (user.IsBanker && _users.Any(u => u.IsBanker))
            throw new DuplicateBankerException();

        _users.Add(user);
        user.Player.Game = this;
    }
}

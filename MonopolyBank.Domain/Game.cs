namespace MonopolyBank.Domain;

public class Game
{
    private readonly List<User> _users = [];

    public IReadOnlyCollection<User> Users => _users;

    public void AddUser(User user)
    {
        if (user.IsBanker && _users.Any(u => u.IsBanker))
            throw new InvalidOperationException("There can only be one banker per game.");

        _users.Add(user);
    }
}

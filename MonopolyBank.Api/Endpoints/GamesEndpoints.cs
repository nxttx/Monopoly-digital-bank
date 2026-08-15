using System.Net;
using MonopolyBank.Api.Persistence;
using MonopolyBank.Domain;

namespace MonopolyBank.Api.Endpoints;

public static class GamesEndpoints
{
    public static ApiResult<GameCreated> CreateGame(GameStore store)
    {
        var gameId = store.CreateGame();
        return new ApiResult<GameCreated>(
            new HttpCall("/games/", HttpMethod.Post, HttpStatusCode.Created),
            new GameCreated(gameId),
            new Dictionary<string, Link> { ["Get details"] = new($"/games/{gameId}/", HttpMethod.Get) });
    }

    public static ApiResult<IReadOnlyList<GameSummary>> GetAllGames(GameStore store)
    {
        var summaries = store.GameIds()
            .Select(id => new GameSummary(
                id,
                new Dictionary<string, Link> { ["Get details"] = new($"/games/{id}/", HttpMethod.Get) }))
            .ToList();

        return new ApiResult<IReadOnlyList<GameSummary>>(
            new HttpCall("/games/", HttpMethod.Get, HttpStatusCode.OK),
            summaries,
            new Dictionary<string, Link> { ["Add game"] = new("/games/", HttpMethod.Post) });
    }

    public static ApiResult<GameDetails> GetGame(GameStore store, Guid gameId)
    {
        var game = store.Find(gameId);
        if (game is null)
            return new ApiResult<GameDetails>(
                new HttpCall($"/games/{gameId}/", HttpMethod.Get, HttpStatusCode.NotFound),
                null,
                new Dictionary<string, Link> { ["Add game"] = new("/games/", HttpMethod.Post) });

        var users = store.UsersOf(gameId)
            .Select(u => new UserSummary(u.UserId, u.Name, u.Type))
            .ToList();

        return new ApiResult<GameDetails>(
            new HttpCall($"/games/{gameId}/", HttpMethod.Get, HttpStatusCode.OK),
            new GameDetails(
                gameId,
                users,
                game.Currency,
                game.Started,
                Game.DefaultStartAmount,
                users.FirstOrDefault(u => u.Type is UserType.Banker or UserType.Both)),
            new Dictionary<string, Link> { ["Add user"] = new($"/games/{gameId}/users/", HttpMethod.Post) });
    }

    public static ApiResult<UserCreated> CreateUser(GameStore store, Guid gameId, CreateUserRequest request)
    {
        var userId = Guid.NewGuid();
        store.Execute(gameId, new GameCommand.AddUser(userId, request.Name, request.Type));
        return new ApiResult<UserCreated>(
            new HttpCall($"/games/{gameId}/users/", HttpMethod.Post, HttpStatusCode.Created),
            new UserCreated(userId, gameId, request.Name, request.Type),
            new Dictionary<string, Link> { ["Get details"] = new($"/games/{gameId}/", HttpMethod.Get) });
    }
}

public record GameCreated(Guid GameId);

public record GameSummary(Guid GameId, Dictionary<string, Link> Links);

public record GameDetails(
    Guid GameId,
    IReadOnlyList<UserSummary> Users,
    Currencies Currency,
    bool Started,
    int DefaultStartAmount,
    UserSummary? BankerUser);

public record UserSummary(Guid UserId, string Name, UserType Type);

public record CreateUserRequest(string Name, UserType Type);

public record UserCreated(Guid UserId, Guid GameId, string Name, UserType Type);

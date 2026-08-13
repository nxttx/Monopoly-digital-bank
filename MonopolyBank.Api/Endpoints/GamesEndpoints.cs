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
            new Dictionary<string, string> { ["Get details"] = $"/games/{gameId}/" });
    }

    public static ApiResult<IReadOnlyList<GameSummary>> GetAllGames(GameStore store)
    {
        var gameIds = store.GameIds();
        var links = new Dictionary<string, string>();
        for (var i = 0; i < gameIds.Count; i++)
            links[$"Get details game {i + 1}"] = $"/games/{gameIds[i]}/";

        return new ApiResult<IReadOnlyList<GameSummary>>(
            new HttpCall("/games/", HttpMethod.Get, HttpStatusCode.OK),
            gameIds.Select(id => new GameSummary(id)).ToList(),
            links);
    }

    public static ApiResult<GameDetails> GetGame(GameStore store, Guid gameId)
    {
        var game = store.Find(gameId);
        if (game is null)
            return new ApiResult<GameDetails>(
                new HttpCall($"/games/{gameId}/", HttpMethod.Get, HttpStatusCode.NotFound),
                null,
                new Dictionary<string, string> { ["Add game"] = "/games/" });

        return new ApiResult<GameDetails>(
            new HttpCall($"/games/{gameId}/", HttpMethod.Get, HttpStatusCode.OK),
            new GameDetails(
                gameId,
                game.Users.Select(u => u.Player.Name).ToList(),
                game.Currency,
                game.Started,
                Game.DefaultStartAmount,
                game.Users.FirstOrDefault(u => u.IsBanker)?.Player.Name),
            new Dictionary<string, string> { ["Add user"] = $"/games/{gameId}/users/" });
    }

    public static ApiResult<UserCreated> CreateUser(GameStore store, Guid gameId, CreateUserRequest request)
    {
        store.Execute(gameId, new GameCommand.AddUser(request.Name, request.Type));
        var userId = Guid.NewGuid();
        return new ApiResult<UserCreated>(
            new HttpCall($"/games/{gameId}/users/", HttpMethod.Post, HttpStatusCode.Created),
            new UserCreated(userId, gameId),
            new Dictionary<string, string> { ["Get details"] = $"/games/{gameId}/" });
    }
}

public record GameCreated(Guid GameId);

public record GameSummary(Guid GameId);

public record GameDetails(
    Guid GameId,
    IReadOnlyList<string> Users,
    Currencies Currency,
    bool Started,
    int DefaultStartAmount,
    string? BankerUser);

public record CreateUserRequest(string Name, UserType Type);

public record UserCreated(Guid UserId, Guid GameId);

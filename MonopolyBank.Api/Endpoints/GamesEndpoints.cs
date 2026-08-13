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
            new Dictionary<string, string> { ["Add user"] = $"/games/{gameId}/users/" });
    }

    public static ApiResult<UserCreated> CreateUser(GameStore store, Guid gameId, CreateUserRequest request)
    {
        store.Execute(gameId, new GameCommand.AddUser(request.Name, request.Type));
        var userId = Guid.NewGuid();
        return new ApiResult<UserCreated>(
            new HttpCall($"/games/{gameId}/users/", HttpMethod.Post, HttpStatusCode.Created),
            new UserCreated(userId),
            new Dictionary<string, string>());
    }
}

public record GameCreated(Guid GameId);

public record CreateUserRequest(string Name, UserType Type);

public record UserCreated(Guid UserId);

using System.Net;
using MonopolyBank.Api.Persistence;
using MonopolyBank.Domain;

namespace MonopolyBank.Api.Endpoints;

public static class GamesUsersEndpoints
{
    public static void RegisterApiRoutes(WebApplication app)
    {
        app.MapPost("/games/{gameId:guid}/users", (GameStore store, Guid gameId, CreateUserRequest request) =>
                CreateUser(store, gameId, request).ToHttpResult())
            .Produces<ApiResult<UserCreated>>(StatusCodes.Status201Created);
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

public record CreateUserRequest(string Name, UserType Type);

public record UserCreated(Guid UserId, Guid GameId, string Name, UserType Type);

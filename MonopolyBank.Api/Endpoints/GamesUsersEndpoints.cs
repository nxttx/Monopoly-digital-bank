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
            .Produces<ApiResult<UserCreated>>(StatusCodes.Status201Created)
            .Produces<ApiError>(StatusCodes.Status400BadRequest);
    }

    public static ApiResponse CreateUser(GameStore store, Guid gameId, CreateUserRequest request)
    {
        if (!Enum.TryParse<UserType>(request.Role, out var type) || !Enum.IsDefined(type))
            return new ApiError(
                new HttpCall($"/games/{gameId}/users/", HttpMethod.Post, HttpStatusCode.BadRequest),
                [new FieldError("Role", $"Role must be one of the following: {string.Join(", ", Enum.GetNames<UserType>())}.")],
                new Dictionary<string, Link> { ["Add user"] = new($"/games/{gameId}/users/", HttpMethod.Post) });

        var userId = Guid.NewGuid();
        try
        {
            store.Execute(gameId, new GameCommand.AddUser(userId, request.Name, type));
        }
        catch (ArgumentException e)
        {
            return new ApiError(
                new HttpCall($"/games/{gameId}/users/", HttpMethod.Post, HttpStatusCode.BadRequest),
                [new FieldError("Name", e.Message)],
                new Dictionary<string, Link> { ["Add user"] = new($"/games/{gameId}/users/", HttpMethod.Post) });
        }

        return new ApiResult<UserCreated>(
            new HttpCall($"/games/{gameId}/users/", HttpMethod.Post, HttpStatusCode.Created),
            new UserCreated(userId, gameId, request.Name, type),
            new Dictionary<string, Link> { ["Get details"] = new($"/games/{gameId}/", HttpMethod.Get) });
    }
}

public record CreateUserRequest(string Name, string Role);

public record UserCreated(Guid UserId, Guid GameId, string Name, UserType Type);

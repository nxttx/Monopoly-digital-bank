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
        app.MapGet("/games/{gameId:guid}/users/{userId:guid}", (GameStore store, Guid gameId, Guid userId) =>
                GetUser(store, gameId, userId).ToHttpResult())
            .Produces<ApiResult<UserInformation>>()
            .Produces<ApiError>(StatusCodes.Status404NotFound);
    }

    public static ApiResponse CreateUser(GameStore store, Guid gameId, CreateUserRequest request)
    {
        var location = ApiRoutes.GameUsers(gameId);

        if (!Enum.TryParse<UserRole>(request.Role, out var role) || !Enum.IsDefined(role))
            return BadRequest(new FieldError("Role",
                $"Role must be one of the following: {string.Join(", ", Enum.GetNames<UserRole>())}."));

        var userId = Guid.NewGuid();
        try
        {
            store.Execute(gameId, new GameCommand.AddUser(userId, request.Name, role));
        }
        catch (ArgumentException e)
        {
            return BadRequest(new FieldError("Name", e.Message));
        }
        catch (DuplicateBankerException)
        {
            return BadRequest(new FieldError("Role", "Only one banker can be added to a game."));
        }

        return new ApiResult<UserCreated>(
            new HttpCall(location, HttpMethod.Post, HttpStatusCode.Created),
            new UserCreated(userId, gameId, request.Name, role),
            new Dictionary<string, Link>
            {
                ["Get game details"] = new(ApiRoutes.Game(gameId), HttpMethod.Get),
                ["Get user details"] = new(ApiRoutes.GameUser(gameId, userId), HttpMethod.Get),
            });

        ApiError BadRequest(FieldError error) => new(
            new HttpCall(location, HttpMethod.Post, HttpStatusCode.BadRequest),
            [error],
            new Dictionary<string, Link> { ["Add user"] = new(location, HttpMethod.Post) });
    }

    public static ApiResponse GetUser(GameStore store, Guid gameId, Guid userId)
    {
        var location = ApiRoutes.GameUser(gameId, userId);

        var game = store.Find(gameId);
        if (game is null)
            return new ApiError(
                new HttpCall(location, HttpMethod.Get, HttpStatusCode.NotFound),
                [new FieldError("GameId", "Game not found.")],
                new Dictionary<string, Link> { ["Add game"] = new(ApiRoutes.Games, HttpMethod.Post) });

        // The Nth AddUser command corresponds to the Nth game user (only successful
        // adds are logged, in order), so this join stays exact even with duplicate names.
        var added = store.UsersOf(gameId);
        var index = added.TakeWhile(u => u.UserId != userId).Count();
        if (index == added.Count)
            return new ApiError(
                new HttpCall(location, HttpMethod.Get, HttpStatusCode.NotFound),
                [new FieldError("UserId", "User not found.")],
                new Dictionary<string, Link>
                {
                    ["Get game details"] = new(ApiRoutes.Game(gameId), HttpMethod.Get),
                    ["Add user"] = new(ApiRoutes.GameUsers(gameId), HttpMethod.Post),
                });

        var user = added[index];
        var player = game.Users.ElementAt(index).Player;

        return new ApiResult<UserInformation>(
            new HttpCall(location, HttpMethod.Get, HttpStatusCode.OK),
            new UserInformation(userId, user.Name, user.Role, player.Money, player.BankCard, gameId),
            new Dictionary<string, Link> { ["Get game details"] = new(ApiRoutes.Game(gameId), HttpMethod.Get) });
    }
}

public record CreateUserRequest(string Name, string Role);

public record UserCreated(Guid UserId, Guid GameId, string Name, UserRole Role);

public record UserInformation(Guid UserId, string Name, UserRole Role, int Balance, BankCard BankCard, Guid GameId);

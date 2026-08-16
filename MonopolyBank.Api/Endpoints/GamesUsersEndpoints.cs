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
            .Produces<ApiError>(StatusCodes.Status400BadRequest)
            .Produces<ApiError>(StatusCodes.Status404NotFound);
        app.MapGet("/games/{gameId:guid}/users/{userId:guid}", (GameStore store, Guid gameId, Guid userId) =>
                GetUser(store, gameId, userId).ToHttpResult())
            .Produces<ApiResult<UserInformation>>()
            .Produces<ApiError>(StatusCodes.Status404NotFound);
        app.MapPost("/games/{gameId:guid}/users/{fromUserId:guid}/give-money/{toUserId:guid}",
                (GameStore store, Guid gameId, Guid fromUserId, Guid toUserId, int amount) =>
                    GiveMoney(store, gameId, fromUserId, toUserId, amount).ToHttpResult())
            .Produces<ApiResult<MoneyTransferred>>();
        app.MapPost("/games/{gameId:guid}/users/{fromUserId:guid}/transfer-money/{toUserId:guid}",
                (GameStore store, Guid gameId, Guid fromUserId, Guid toUserId, int amount) =>
                    TransferMoney(store, gameId, fromUserId, toUserId, amount).ToHttpResult())
            .Produces<ApiResult<MoneyTransferred>>();
    }

    public static ApiResponse CreateUser(GameStore store, Guid gameId, CreateUserRequest request)
    {
        var location = ApiRoutes.GameUsers(gameId);

        if (store.Find(gameId) is null)
            return ApiErrors.GameNotFound(location, HttpMethod.Post);

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
            return ApiErrors.GameNotFound(location, HttpMethod.Get);

        var user = store.FindUser(gameId, userId);
        if (user is null)
            return new ApiError(
                new HttpCall(location, HttpMethod.Get, HttpStatusCode.NotFound),
                [new FieldError("UserId", "User not found.")],
                new Dictionary<string, Link>
                {
                    ["Get game details"] = new(ApiRoutes.Game(gameId), HttpMethod.Get),
                    ["Add user"] = new(ApiRoutes.GameUsers(gameId), HttpMethod.Post),
                });

        return new ApiResult<UserInformation>(
            new HttpCall(location, HttpMethod.Get, HttpStatusCode.OK),
            new UserInformation(userId, user.Name, user.Role, user.Money.Amount, user.BankCard, gameId),
            new Dictionary<string, Link> { ["Get game details"] = new(ApiRoutes.Game(gameId), HttpMethod.Get) });
    }

    public static ApiResult<MoneyTransferred> GiveMoney(GameStore store, Guid gameId, Guid fromUserId, Guid toUserId, int amount)
    {
        store.Execute(gameId, new GameCommand.BankerTransfer(fromUserId, toUserId, amount));

        return new ApiResult<MoneyTransferred>(
            new HttpCall(ApiRoutes.GiveMoney(gameId, fromUserId, toUserId), HttpMethod.Post, HttpStatusCode.OK),
            new MoneyTransferred(gameId, amount, fromUserId, toUserId),
            new Dictionary<string, Link> { ["Get game details"] = new(ApiRoutes.Game(gameId), HttpMethod.Get) });
    }

    public static ApiResult<MoneyTransferred> TransferMoney(GameStore store, Guid gameId, Guid fromUserId, Guid toUserId, int amount)
    {
        store.Execute(gameId, new GameCommand.PlayerTransfer(fromUserId, toUserId, amount));

        return new ApiResult<MoneyTransferred>(
            new HttpCall(ApiRoutes.TransferMoney(gameId, fromUserId, toUserId), HttpMethod.Post, HttpStatusCode.OK),
            new MoneyTransferred(gameId, amount, fromUserId, toUserId),
            new Dictionary<string, Link> { ["Get game details"] = new(ApiRoutes.Game(gameId), HttpMethod.Get) });
    }
}

public record CreateUserRequest(string Name, string Role);

public record UserCreated(Guid UserId, Guid GameId, string Name, UserRole Role);

public record UserInformation(Guid UserId, string Name, UserRole Role, int Balance, BankCard BankCard, Guid GameId);

public record MoneyTransferred(Guid GameId, int Amount, Guid From, Guid To);

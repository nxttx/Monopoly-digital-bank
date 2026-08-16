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
            .Produces<ApiResult<MoneyTransferred>>()
            .Produces<ApiError>(StatusCodes.Status400BadRequest);
        app.MapPost("/games/{gameId:guid}/users/{chargerUserId:guid}/charge-player/{targetUserId:guid}",
                (GameStore store, Guid gameId, Guid chargerUserId, Guid targetUserId, int amount) =>
                    ChargePlayer(store, gameId, chargerUserId, targetUserId, amount).ToHttpResult())
            .Produces<ApiResult<MoneyTransferred>>()
            .Produces<ApiError>(StatusCodes.Status400BadRequest);
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

    public static ApiResponse TransferMoney(GameStore store, Guid gameId, Guid fromUserId, Guid toUserId, int amount)
    {
        var location = ApiRoutes.TransferMoney(gameId, fromUserId, toUserId);

        // The domain guards only the SENDER's role (receiving is deliberately
        // unguarded — its types made paying a banker unrepresentable). Resolving
        // users by id hands out any Player facet, so the API restores that rule.
        if (store.FindUser(gameId, toUserId) is { Role: UserRole.Banker })
            return new ApiError(
                new HttpCall(location, HttpMethod.Post, HttpStatusCode.BadRequest),
                [new FieldError("GenericMessage", "A player cannot transfer money to a banker, a banker can only charge a player")],
                new Dictionary<string, Link>
                {
                    ["Get game details"] = new(ApiRoutes.Game(gameId), HttpMethod.Get),
                    ["Charge player (banker -> player)"] = new(ApiRoutes.ChargePlayer(gameId, toUserId, fromUserId), HttpMethod.Post),
                });

        try
        {
            store.Execute(gameId, new GameCommand.PlayerTransfer(fromUserId, toUserId, amount));
        }
        catch (NegativeTransferException)
        {
            return new ApiError(
                new HttpCall(location, HttpMethod.Post, HttpStatusCode.BadRequest),
                [new FieldError("GenericMessage", "A player cannot transfer a negative amount of money")],
                new Dictionary<string, Link>
                {
                    ["Get game details"] = new(ApiRoutes.Game(gameId), HttpMethod.Get),
                    ["Charge player (banker -> player)"] = new(ApiRoutes.ChargePlayer(gameId, toUserId, fromUserId), HttpMethod.Post),
                });
        }
        catch (MissingRoleException)
        {
            return new ApiError(
                new HttpCall(location, HttpMethod.Post, HttpStatusCode.BadRequest),
                [new FieldError("GenericMessage", "A banker cannot transfer money to another player, a banker can only give money to player")],
                new Dictionary<string, Link>
                {
                    ["Get game details"] = new(ApiRoutes.Game(gameId), HttpMethod.Get),
                    ["Give money (banker -> player)"] = new(ApiRoutes.GiveMoney(gameId, fromUserId, toUserId), HttpMethod.Post),
                    ["Transfer money (player -> player)"] = new(ApiRoutes.TransferMoney(gameId, fromUserId, toUserId), HttpMethod.Post),
                });
        }

        return new ApiResult<MoneyTransferred>(
            new HttpCall(location, HttpMethod.Post, HttpStatusCode.OK),
            new MoneyTransferred(gameId, amount, fromUserId, toUserId),
            new Dictionary<string, Link> { ["Get game details"] = new(ApiRoutes.Game(gameId), HttpMethod.Get) });
    }

    public static ApiResponse ChargePlayer(GameStore store, Guid gameId, Guid chargerUserId, Guid targetUserId, int amount)
    {
        var location = ApiRoutes.ChargePlayer(gameId, chargerUserId, targetUserId);

        try
        {
            // A charge is the bank collecting: the domain models it as the banker
            // sending a negative amount (the one negative-transfer door that exists).
            store.Execute(gameId, new GameCommand.BankerTransfer(chargerUserId, targetUserId, -amount));
        }
        catch (MissingRoleException)
        {
            return new ApiError(
                new HttpCall(location, HttpMethod.Post, HttpStatusCode.BadRequest),
                [new FieldError("GenericMessage", "A user cannot charge money from another player or a banker.")],
                new Dictionary<string, Link>
                {
                    ["Get game details"] = new(ApiRoutes.Game(gameId), HttpMethod.Get),
                    ["Transfer money (player -> player)"] = new(ApiRoutes.TransferMoney(gameId, targetUserId, chargerUserId), HttpMethod.Post),
                });
        }

        return new ApiResult<MoneyTransferred>(
            new HttpCall(location, HttpMethod.Post, HttpStatusCode.OK),
            new MoneyTransferred(gameId, amount, targetUserId, chargerUserId),
            new Dictionary<string, Link> { ["Get game details"] = new(ApiRoutes.Game(gameId), HttpMethod.Get) });
    }
}

public record CreateUserRequest(string Name, string Role);

public record UserCreated(Guid UserId, Guid GameId, string Name, UserRole Role);

public record UserInformation(Guid UserId, string Name, UserRole Role, int Balance, BankCard BankCard, Guid GameId);

public record MoneyTransferred(Guid GameId, int Amount, Guid From, Guid To);

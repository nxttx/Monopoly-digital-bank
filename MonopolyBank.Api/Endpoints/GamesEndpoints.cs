using System.Net;
using MonopolyBank.Api.Persistence;
using MonopolyBank.Domain;

namespace MonopolyBank.Api.Endpoints;

public static class GamesEndpoints
{
    public static void RegisterApiRoutes(WebApplication app)
    {
        app.MapGet("/games", (GameStore store) =>
                GetAllGames(store).ToHttpResult())
            .Produces<ApiResult<IReadOnlyList<GameSummary>>>();
        app.MapPost("/games", (GameStore store) =>
                CreateGame(store).ToHttpResult())
            .Produces<ApiResult<GameCreated>>(StatusCodes.Status201Created);
        app.MapGet("/games/{gameId:guid}", (GameStore store, Guid gameId) =>
                GetGame(store, gameId).ToHttpResult())
            .Produces<ApiResult<GameDetails>>()
            .Produces<ApiError>(StatusCodes.Status404NotFound);
        app.MapPost("/games/{gameId:guid}/start", (GameStore store, Guid gameId) =>
                StartGame(store, gameId).ToHttpResult())
            .Produces<ApiResult<GameStarted>>()
            .Produces<ApiError>(StatusCodes.Status400BadRequest)
            .Produces<ApiError>(StatusCodes.Status404NotFound);
    }


    public static ApiResult<GameCreated> CreateGame(GameStore store)
    {
        var gameId = store.CreateGame();
        return new ApiResult<GameCreated>(
            new HttpCall(ApiRoutes.Games, HttpMethod.Post, HttpStatusCode.Created),
            new GameCreated(gameId),
            new Dictionary<string, Link> { ["Get details"] = new(ApiRoutes.Game(gameId), HttpMethod.Get) });
    }

    public static ApiResult<IReadOnlyList<GameSummary>> GetAllGames(GameStore store)
    {
        var summaries = store.GameIds()
            .Select(id => new GameSummary(
                id,
                new Dictionary<string, Link> { ["Get details"] = new(ApiRoutes.Game(id), HttpMethod.Get) }))
            .ToList();

        return new ApiResult<IReadOnlyList<GameSummary>>(
            new HttpCall(ApiRoutes.Games, HttpMethod.Get, HttpStatusCode.OK),
            summaries,
            new Dictionary<string, Link> { ["Add game"] = new(ApiRoutes.Games, HttpMethod.Post) });
    }

    public static ApiResponse StartGame(GameStore store, Guid gameId)
    {
        var location = ApiRoutes.GameStart(gameId);

        if (store.Find(gameId) is null)
            return ApiErrors.GameNotFound(location, HttpMethod.Post);

        try
        {
            store.Execute(gameId, new GameCommand.Start());
        }
        catch (AmountOfPlayersException e)
        {
            return new ApiError(
                new HttpCall(location, HttpMethod.Post, HttpStatusCode.BadRequest),
                [new FieldError("GenericMessage", e.Message)],
                new Dictionary<string, Link>
                {
                    ["Get game details"] = new(ApiRoutes.Game(gameId), HttpMethod.Get),
                    ["Add user"] = new(ApiRoutes.GameUsers(gameId), HttpMethod.Post),
                });
        }
        catch (AlreadyStartedGameException)
        {
            return new ApiError(
                new HttpCall(location, HttpMethod.Post, HttpStatusCode.BadRequest),
                [new FieldError("GenericMessage", "A game can only be started once.")],
                new Dictionary<string, Link> { ["Get game details"] = new(ApiRoutes.Game(gameId), HttpMethod.Get) });
        }

        return new ApiResult<GameStarted>(
            new HttpCall(location, HttpMethod.Post, HttpStatusCode.OK),
            new GameStarted(gameId, store.Find(gameId)!.Started),
            new Dictionary<string, Link> { ["Get game details"] = new(ApiRoutes.Game(gameId), HttpMethod.Get) });
    }

    public static ApiResponse GetGame(GameStore store, Guid gameId)
    {
        var location = ApiRoutes.Game(gameId);
        var game = store.Find(gameId);
        if (game is null)
            return ApiErrors.GameNotFound(location, HttpMethod.Get);

        var users = store.UsersOf(gameId)
            .Select(u => new UserSummary(
                u.UserId,
                u.Name,
                u.Role,
                new Dictionary<string, Link> { ["Get user details"] = new(ApiRoutes.GameUser(gameId, u.UserId), HttpMethod.Get) }))
            .ToList();

        return new ApiResult<GameDetails>(
            new HttpCall(location, HttpMethod.Get, HttpStatusCode.OK),
            new GameDetails(
                gameId,
                users,
                game.Currency,
                game.Started,
                Game.DefaultStartAmount,
                users.FirstOrDefault(u => u.Role is UserRole.Banker or UserRole.Both)),
            new Dictionary<string, Link> { ["Add user"] = new(ApiRoutes.GameUsers(gameId), HttpMethod.Post) });
    }
}

public record GameCreated(Guid GameId);

public record GameStarted(Guid GameId, bool Started);

public record GameSummary(Guid GameId, Dictionary<string, Link> Actions);

public record GameDetails(
    Guid GameId,
    IReadOnlyList<UserSummary> Users,
    Currencies Currency,
    bool Started,
    int DefaultStartAmount,
    UserSummary? BankerUser);

public record UserSummary(Guid UserId, string Name, UserRole Role, Dictionary<string, Link> Actions);

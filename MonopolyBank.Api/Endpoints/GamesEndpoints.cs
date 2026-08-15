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

    public static ApiResponse GetGame(GameStore store, Guid gameId)
    {
        var location = ApiRoutes.Game(gameId);
        var game = store.Find(gameId);
        if (game is null)
            return new ApiError(
                new HttpCall(location, HttpMethod.Get, HttpStatusCode.NotFound),
                [new FieldError("GameId", "Game not found.")],
                new Dictionary<string, Link> { ["Add game"] = new(ApiRoutes.Games, HttpMethod.Post) });

        var users = store.UsersOf(gameId)
            .Select(u => new UserSummary(u.UserId, u.Name, u.Role))
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

public record GameSummary(Guid GameId, Dictionary<string, Link> Actions);

public record GameDetails(
    Guid GameId,
    IReadOnlyList<UserSummary> Users,
    Currencies Currency,
    bool Started,
    int DefaultStartAmount,
    UserSummary? BankerUser);

public record UserSummary(Guid UserId, string Name, UserRole Role);

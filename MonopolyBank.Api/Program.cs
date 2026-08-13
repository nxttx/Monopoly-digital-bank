using System.Text.Json.Serialization;
using MonopolyBank.Api;
using MonopolyBank.Api.Endpoints;
using MonopolyBank.Api.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new HttpStatusCodeJsonConverter());
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddOpenApi();
builder.Services.AddSingleton(_ => new CommandLog(
    builder.Configuration.GetConnectionString("MonopolyBank") ?? "Data Source=monopolybank.db"));
builder.Services.AddSingleton<GameStore>();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

// Route registrations go here, one per user-tested handler.
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })); // temporary scaffold check
app.MapPost("/games", (GameStore store) =>
        GamesEndpoints.CreateGame(store).ToHttpResult())
    .Produces<ApiResult<GameCreated>>(StatusCodes.Status201Created);
app.MapGet("/games", (GameStore store) =>
        GamesEndpoints.GetAllGames(store).ToHttpResult())
    .Produces<ApiResult<IReadOnlyList<GameSummary>>>();
app.MapGet("/games/{gameId:guid}", (GameStore store, Guid gameId) =>
        GamesEndpoints.GetGame(store, gameId).ToHttpResult())
    .Produces<ApiResult<GameDetails>>()
    .Produces<ApiResult<GameDetails>>(StatusCodes.Status404NotFound);
app.MapPost("/games/{gameId:guid}/users", (GameStore store, Guid gameId, CreateUserRequest request) =>
        GamesEndpoints.CreateUser(store, gameId, request).ToHttpResult())
    .Produces<ApiResult<UserCreated>>(StatusCodes.Status201Created);

app.Run();

public partial class Program;

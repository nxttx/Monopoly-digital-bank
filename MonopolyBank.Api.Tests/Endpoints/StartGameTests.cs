using System.Net;
using MonopolyBank.Api.Endpoints;
using MonopolyBank.Domain;
using Shouldly;
using Xunit;

namespace MonopolyBank.Api.Tests.Endpoints;

public class StartGameTests : EndpointTestBase
{
    [Fact]
    public void AGameShouldBeStartable()
    {
        var store = CreateGameStore();
        var gameId = CreateGame(store);

        GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Bob", nameof(UserRole.Banker))).ShouldBeOfType<ApiResult<UserCreated>>();
        GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Mick", nameof(UserRole.Player))).ShouldBeOfType<ApiResult<UserCreated>>();
        GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Allice", nameof(UserRole.Player))).ShouldBeOfType<ApiResult<UserCreated>>();

        var result = GamesEndpoints.StartGame(store, gameId).ShouldBeOfType<ApiResult<GameStarted>>();

        result.Http.Location.ShouldBe($"/games/{gameId}/start/");
        result.Http.Method.ShouldBe(HttpMethod.Post);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Value.ShouldNotBeNull();
        result.Value.GameId.ShouldBe(gameId);
        result.Value.Started.ShouldBeTrue();

        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Get game details", new Link($"/games/{gameId}/", HttpMethod.Get)));
    }

    [Fact]
    public void AGameShouldNotBeStartableIfThereAreNoBankers()
    {
        var store = CreateGameStore();
        var gameId = CreateGame(store);
        GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Allice", nameof(UserRole.Player))).ShouldBeOfType<ApiResult<UserCreated>>();
        GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Bob", nameof(UserRole.Player))).ShouldBeOfType<ApiResult<UserCreated>>();

        var result = GamesEndpoints.StartGame(store, gameId).ShouldBeOfType<ApiError>();

        result.Http.Location.ShouldBe($"/games/{gameId}/start/");
        result.Http.Method.ShouldBe(HttpMethod.Post);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        VerifyResultDoesNotContainValueProperty(result);
        result.Errors.ShouldContain(e => e.Field == "GenericMessage" && e.Message == "A game requires at least one banker and two players.");
        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Get game details", new Link($"/games/{gameId}/", HttpMethod.Get)));
        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Add user", new Link($"/games/{gameId}/users/", HttpMethod.Post)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void AGameShouldNotBeStartableIfThereAreNoOrToLittlePlayers(int amountOfPlayers)
    {
        var store = CreateGameStore();
        var gameId = CreateGame(store);
        GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Allice", nameof(UserRole.Banker))).ShouldBeOfType<ApiResult<UserCreated>>();
        for (var i = 0; i < amountOfPlayers; i++)
        {
            GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest($"Player {i}", nameof(UserRole.Player))).ShouldBeOfType<ApiResult<UserCreated>>();
        }
        var result = GamesEndpoints.StartGame(store, gameId).ShouldBeOfType<ApiError>();

        result.Http.Location.ShouldBe($"/games/{gameId}/start/");
        result.Http.Method.ShouldBe(HttpMethod.Post);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        VerifyResultDoesNotContainValueProperty(result);
        result.Errors.ShouldContain(e => e.Field == "GenericMessage" && e.Message == "A game requires at least one banker and two players.");
        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Get game details", new Link($"/games/{gameId}/", HttpMethod.Get)));
        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Add user", new Link($"/games/{gameId}/users/", HttpMethod.Post)));
    }

    [Fact]
    public void AGameShouldNotBeStartableIfItIsAlreadyStarted()
    {
        var store = CreateGameStore();
        var gameId = CreateGame(store);
        GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Allice", nameof(UserRole.Banker))).ShouldBeOfType<ApiResult<UserCreated>>();
        GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Robert", nameof(UserRole.Player))).ShouldBeOfType<ApiResult<UserCreated>>();
        GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Bob", nameof(UserRole.Player))).ShouldBeOfType<ApiResult<UserCreated>>();

        GamesEndpoints.StartGame(store, gameId).ShouldBeOfType<ApiResult<GameStarted>>();

        var result = GamesEndpoints.StartGame(store, gameId).ShouldBeOfType<ApiError>();
        result.Http.Location.ShouldBe($"/games/{gameId}/start/");
        result.Http.Method.ShouldBe(HttpMethod.Post);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        VerifyResultDoesNotContainValueProperty(result);
        result.Errors.ShouldContain(e => e.Field == "GenericMessage" && e.Message == "A game can only be started once.");
        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Get game details", new Link($"/games/{gameId}/", HttpMethod.Get)));

    }

    [Fact]
    public void StartUnknownGame_Returns404()
    {
        var store = CreateGameStore();
        var gameId = Guid.NewGuid();

        var result = GamesEndpoints.StartGame(store, gameId).ShouldBeOfType<ApiError>();

        result.Http.Location.ShouldBe($"/games/{gameId}/start/");
        result.Http.Method.ShouldBe(HttpMethod.Post);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        VerifyResultDoesNotContainValueProperty(result);
        result.Errors.ShouldContain(e => e.Field == "GameId" && e.Message == "Game not found.");
        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Add game", new Link($"/games/", HttpMethod.Post)));
    }
}

using System.Net;
using MonopolyBank.Api.Endpoints;
using MonopolyBank.Domain;
using Shouldly;
using Xunit;

namespace MonopolyBank.Api.Tests;

public class CreateUserTests : EndpointTestBase
{
    [Fact]
    public void CreateUserInGame_ReturnsCreatedWithAUserIdThatCanBeFoundInTheStore()
    {
        var store = CreateGameStore();
        var gameId = CreateGame(store);

        var result = GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Robert", nameof(UserRole.Banker)))
            .ShouldBeOfType<ApiResult<UserCreated>>();

        result.Http.Location.ShouldBe($"/games/{gameId}/users/");
        result.Http.Method.ShouldBe(HttpMethod.Post);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.Created);

        result.Value.ShouldNotBeNull();
        result.Value.UserId.ShouldNotBe(Guid.Empty);
        result.Value.GameId.ShouldBe(gameId);

        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Get game details", new Link($"/games/{gameId}/", HttpMethod.Get)));
        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Get user details", new Link($"/games/{gameId}/users/{result.Value!.UserId}/", HttpMethod.Get)));

        var storedGame = store.Find(gameId).ShouldNotBeNull();
        storedGame.Users.ShouldContain(u => u.Player.Name == "Robert");
    }

    [Fact]
    public void CreateUserInGameWithInvalidName_Returns400()
    {
        var store = CreateGameStore();
        var gameId = CreateGame(store);

        var result = GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("", nameof(UserRole.Banker)))
            .ShouldBeOfType<ApiError>();

        result.Http.Location.ShouldBe($"/games/{gameId}/users/");
        result.Http.Method.ShouldBe(HttpMethod.Post);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        VerifyResultDoesNotContainValueProperty(result);

        result.Errors.ShouldContain(e => e.Field == "Name" && e.Message == "Name cannot be empty.");
        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Add user", new Link($"/games/{gameId}/users/", HttpMethod.Post)));
    }

    [Fact]
    public void CreateUserInGameWithInvalidRole_Returns400()
    {
        var store = CreateGameStore();
        var gameId = CreateGame(store);

        var result = GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Bob", "WrongRole"))
            .ShouldBeOfType<ApiError>();

        result.Http.Location.ShouldBe($"/games/{gameId}/users/");
        result.Http.Method.ShouldBe(HttpMethod.Post);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        VerifyResultDoesNotContainValueProperty(result);

        result.Errors.ShouldContain(e => e.Field == "Role" && e.Message == $"Role must be one of the following: {string.Join(", ", Enum.GetNames(typeof(UserRole)))}.");
        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Add user", new Link($"/games/{gameId}/users/", HttpMethod.Post)));
    }

    [Fact]
    public void CreateUserInUnknownGame_Returns404()
    {
        var store = CreateGameStore();
        var gameId = Guid.NewGuid();

        var result = GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Bob", nameof(UserRole.Player))).ShouldBeOfType<ApiError>();

        result.Http.Location.ShouldBe($"/games/{gameId}/users/");
        result.Http.Method.ShouldBe(HttpMethod.Post);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        VerifyResultDoesNotContainValueProperty(result);
        result.Errors.ShouldContain(e => e.Field == "GameId" && e.Message == "Game not found.");
        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Add game", new Link($"/games/", HttpMethod.Post)));
    }

    [Theory]
    [InlineData(UserRole.Banker)]
    [InlineData(UserRole.Both)]
    public void AddTwoBankersToGame_ShouldReturnError(UserRole role)
    {
        var store = CreateGameStore();
        var gameId = CreateGame(store);

        GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Bob", nameof(UserRole.Banker))).ShouldBeOfType<ApiResult<UserCreated>>();

        var result = GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Mick", role.ToString()))
            .ShouldBeOfType<ApiError>();

        result.Http.Location.ShouldBe($"/games/{gameId}/users/");
        result.Http.Method.ShouldBe(HttpMethod.Post);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        VerifyResultDoesNotContainValueProperty(result);
        result.Errors.ShouldContain(e => e.Field == "Role" && e.Message == "Only one banker can be added to a game.");
        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Add user", new Link($"/games/{gameId}/users/", HttpMethod.Post)));
    }
}

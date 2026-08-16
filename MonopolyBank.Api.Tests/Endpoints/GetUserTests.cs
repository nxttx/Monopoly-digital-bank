using System.Net;
using MonopolyBank.Api.Endpoints;
using MonopolyBank.Domain;
using Shouldly;
using Xunit;

namespace MonopolyBank.Api.Tests;

public class GetUserTests : EndpointTestBase
{
    [Fact]
    public void GetUser_ReturnsUserWithTheCorrectIdAndInfo()
    {
        var store = CreateGameStore();
        var gameId = CreateGame(store);

        var user =
            GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Bob", nameof(UserRole.Player))).ShouldBeOfType<ApiResult<UserCreated>>();
        var userId = user.Value!.UserId;

        var result = GamesUsersEndpoints.GetUser(store, gameId, userId).ShouldBeOfType<ApiResult<UserInformation>>();

        result.Http.Location.ShouldBe($"/games/{gameId}/users/{userId}/");
        result.Http.Method.ShouldBe(HttpMethod.Get);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Value.ShouldNotBeNull();
        result.Value.UserId.ShouldBe(userId);
        result.Value.Name.ShouldBe("Bob");
        result.Value.Role.ShouldBe(UserRole.Player);
        result.Value.Balance.ShouldBe(1500);
        result.Value.BankCard.Number.ShouldNotBeNullOrEmpty();
        result.Value.BankCard.Expiry.ShouldBeGreaterThan(DateTime.Now);
        result.Value.BankCard.Cvv.ShouldNotBeNullOrEmpty();
        result.Value.GameId.ShouldBe(gameId);

        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Get game details", new Link($"/games/{gameId}/", HttpMethod.Get)));
    }

    [Fact]
    public void GetUserInUnknownGame_Returns404()
    {
        var store = CreateGameStore();
        var gameId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var result = GamesUsersEndpoints.GetUser(store, gameId, userId).ShouldBeOfType<ApiError>();

        result.Http.Location.ShouldBe($"/games/{gameId}/users/{userId}/");
        result.Http.Method.ShouldBe(HttpMethod.Get);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        VerifyResultDoesNotContainValueProperty(result);
        result.Errors.ShouldContain(e => e.Field == "GameId" && e.Message == "Game not found.");
        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Add game", new Link($"/games/", HttpMethod.Post)));
    }

    [Fact]
    public void GetUnknownUserInGame_Returns404()
    {
        var store = CreateGameStore();
        var gameId = CreateGame(store);
        var userId = Guid.NewGuid();

        var result = GamesUsersEndpoints.GetUser(store, gameId, userId).ShouldBeOfType<ApiError>();

        result.Http.Location.ShouldBe($"/games/{gameId}/users/{userId}/");
        result.Http.Method.ShouldBe(HttpMethod.Get);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        VerifyResultDoesNotContainValueProperty(result);
        result.Errors.ShouldContain(e => e.Field == "UserId" && e.Message == "User not found.");
        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Get game details", new Link($"/games/{gameId}/", HttpMethod.Get)));
        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Add user", new Link($"/games/{gameId}/users/", HttpMethod.Post)));
    }
}

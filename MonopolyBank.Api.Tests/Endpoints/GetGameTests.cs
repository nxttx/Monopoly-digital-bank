using System.Net;
using MonopolyBank.Api.Endpoints;
using MonopolyBank.Domain;
using Shouldly;
using Xunit;

namespace MonopolyBank.Api.Tests;

public class GetGameTests : EndpointTestBase
{
    [Fact]
    public void GetGame_ReturnsGameWithTheCorrectIdAndInfo()
    {
        var store = CreateGameStore();
        var gameId = CreateGame(store);

        var result = GamesEndpoints.GetGame(store, gameId).ShouldBeOfType<ApiResult<GameDetails>>();

        result.Http.Location.ShouldBe($"/games/{gameId}/");
        result.Http.Method.ShouldBe(HttpMethod.Get);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.OK);

        result.Value.ShouldNotBeNull();
        result.Value.GameId.ShouldBe(gameId);
        result.Value.Users.ShouldBeEmpty();
        result.Value.Currency.ShouldBe(Currencies.Monopolonian);
        result.Value.Started.ShouldBe(false);
        result.Value.DefaultStartAmount.ShouldBe(1500);
        result.Value.BankerUser.ShouldBe(null);

        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Add user", new Link($"/games/{result.Value.GameId}/users/", HttpMethod.Post)));
    }

    [Fact]
    public void GetUnknownGame_Returns404()
    {
        var store = CreateGameStore();
        var gameId = Guid.NewGuid();


        var result = GamesEndpoints.GetGame(store, gameId).ShouldBeOfType<ApiError>();

        result.Http.Location.ShouldBe($"/games/{gameId}/");
        result.Http.Method.ShouldBe(HttpMethod.Get);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        VerifyResultDoesNotContainValueProperty(result);
        result.Errors.ShouldContain(e => e.Field == "GameId" && e.Message == "Game not found.");

        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Add game", new Link($"/games/", HttpMethod.Post)));
    }

    [Fact]
    public void GetGameWithUsers_ReturnsGameWithTheCorrectIdAndUserInfo()
    {
        var store = CreateGameStore();
        var gameId = CreateGame(store);
        var banker = GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Robert", nameof(UserRole.Both)))
            .ShouldBeOfType<ApiResult<UserCreated>>();
        var player = GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Bob", nameof(UserRole.Player)))
            .ShouldBeOfType<ApiResult<UserCreated>>();
        var player2 = GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Allice", nameof(UserRole.Player)))
            .ShouldBeOfType<ApiResult<UserCreated>>();

        var result = GamesEndpoints.GetGame(store, gameId).ShouldBeOfType<ApiResult<GameDetails>>();

        result.Http.Location.ShouldBe($"/games/{gameId}/");
        result.Http.Method.ShouldBe(HttpMethod.Get);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.OK);

        result.Value.ShouldNotBeNull();
        result.Value.GameId.ShouldBe(gameId);
        result.Value.Users.ShouldContain(u => u.UserId == banker.Value!.UserId && u.Name == banker.Value.Name && u.Role == banker.Value.Role);
        result.Value.Users.First().Actions.ShouldContain(new KeyValuePair<string, Link>("Get user details", new Link($"/games/{gameId}/users/{banker.Value!.UserId}/", HttpMethod.Get)));

        result.Value.Users.ShouldContain(u => u.UserId == player.Value!.UserId && u.Name == player.Value.Name && u.Role == player.Value.Role);
        result.Value.Users[1].Actions.ShouldContain(new KeyValuePair<string, Link>("Get user details", new Link($"/games/{gameId}/users/{player.Value!.UserId}/", HttpMethod.Get)));

        result.Value.Users.ShouldContain(u => u.UserId == player2.Value!.UserId && u.Name == player2.Value.Name && u.Role == player2.Value.Role);
        result.Value.Users.Last().Actions.ShouldContain(new KeyValuePair<string, Link>("Get user details", new Link($"/games/{gameId}/users/{player2.Value!.UserId}/", HttpMethod.Get)));

        result.Value.Currency.ShouldBe(Currencies.Monopolonian);
        result.Value.Started.ShouldBe(false);
        result.Value.DefaultStartAmount.ShouldBe(1500);
        result.Value.BankerUser.ShouldNotBeNull();
        result.Value.BankerUser.UserId.ShouldBe(banker.Value!.UserId);
        result.Value.BankerUser.Name.ShouldBe(banker.Value.Name);

        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Add user", new Link($"/games/{gameId}/users/", HttpMethod.Post)));
    }
}

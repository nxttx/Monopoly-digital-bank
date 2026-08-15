using System.Net;
using MonopolyBank.Api.Endpoints;
using MonopolyBank.Api.Persistence;
using MonopolyBank.Domain;
using Shouldly;
using Xunit;

namespace MonopolyBank.Api.Tests;

public class GamesEndpointsTests
{
    [Fact]
    public void CreateGame_ReturnsCreatedWithAGameIdThatCanBeFoundInTheStore()
    {
        var store = CreateGameStore();

        var result = GamesEndpoints.CreateGame(store);

        result.Http.Location.ShouldBe($"/games/");
        result.Http.Method.ShouldBe(HttpMethod.Post);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.Created);
        
        result.Value.ShouldNotBeNull();
        result.Value.GameId.ShouldNotBe(Guid.Empty);
        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Get details", new Link($"/games/{result.Value.GameId}/", HttpMethod.Get)));

        store.Find(result.Value.GameId).ShouldNotBeNull();
    }
    [Fact]
    public void GetAllGames_ReturnsGamesWithAGameIdThatCanBeFoundInTheStore()
    {
        var store = CreateGameStore();
        var game = GamesEndpoints.CreateGame(store);
        var gameId = game.Value!.GameId;
        var game2 = GamesEndpoints.CreateGame(store);
        var gameId2 = game2.Value!.GameId;

        var result = GamesEndpoints.GetAllGames(store);

        result.Http.Location.ShouldBe($"/games/");
        result.Http.Method.ShouldBe(HttpMethod.Get);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        result.Value.ShouldNotBeNull();
        result.Value.ShouldNotBeEmpty();
        result.Value.ShouldContain(g => g.GameId == gameId);
        result.Value.ShouldContain(g => g.GameId == gameId2);

        var summary = result.Value.Single(g => g.GameId == gameId);
        summary.Actions.ShouldContain(new KeyValuePair<string, Link>("Get details", new Link($"/games/{gameId}/", HttpMethod.Get)));
        var summary2 = result.Value.Single(g => g.GameId == gameId2);
        summary2.Actions.ShouldContain(new KeyValuePair<string, Link>("Get details", new Link($"/games/{gameId2}/", HttpMethod.Get)));

        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Add game", new Link($"/games/", HttpMethod.Post)));
    }

    [Fact]
    public void GetGame_ReturnsGameWithTheCorrectIdAndInfo()
    {
        var store = CreateGameStore();
        var game = GamesEndpoints.CreateGame(store);
        var gameId = game.Value!.GameId;
        
        
        var result = GamesEndpoints.GetGame(store, gameId);
        
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
        
        
        var result = GamesEndpoints.GetGame(store, gameId);
        
        result.Http.Location.ShouldBe($"/games/{gameId}/");
        result.Http.Method.ShouldBe(HttpMethod.Get);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        result.Value.ShouldBeNull();
        
        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Add game", new Link($"/games/", HttpMethod.Post)));
    }
    
    [Fact]
    public void CreateUserInGame_ReturnsCreatedWithAUserIdThatCanBeFoundInTheStore()
    {
        var store = CreateGameStore();
        var game = GamesEndpoints.CreateGame(store);
        var gameId = game.Value!.GameId;

        var result = GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Robert", UserType.Banker));
        
        result.Http.Location.ShouldBe($"/games/{gameId}/users/");
        result.Http.Method.ShouldBe(HttpMethod.Post);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.Created);
        
        result.Value.ShouldNotBeNull();
        result.Value.UserId.ShouldNotBe(Guid.Empty);
        result.Value.GameId.ShouldBe(gameId);
        
        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Get details", new Link($"/games/{gameId}/", HttpMethod.Get)));
        
        var storedGame = store.Find(gameId).ShouldNotBeNull();
        storedGame.Users.ShouldContain(u => u.Player.Name == "Robert");
    }

    [Fact]
    public void GetGameWithUsers_ReturnsGameWithTheCorrectIdAndUserInfo()
    {
        var store = CreateGameStore();
        var game = GamesEndpoints.CreateGame(store);
        var gameId = game.Value!.GameId;
        var banker = GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Robert", UserType.Both));
        var player = GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Bob", UserType.Player));
        var player2 = GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Allice", UserType.Player));
        
        var result = GamesEndpoints.GetGame(store, gameId);
        
        result.Http.Location.ShouldBe($"/games/{gameId}/");
        result.Http.Method.ShouldBe(HttpMethod.Get);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        result.Value.ShouldNotBeNull();
        result.Value.GameId.ShouldBe(gameId);
        result.Value.Users.ShouldContain(u => u.UserId == banker.Value.UserId && u.Name == banker.Value.Name && u.Type == banker.Value.Type);
        
        result.Value.Users.ShouldContain(u => u.UserId == player.Value.UserId && u.Name == player.Value.Name && u.Type == player.Value.Type);
        result.Value.Users.ShouldContain(u => u.UserId == player2.Value.UserId && u.Name == player2.Value.Name && u.Type == player2.Value.Type);
        result.Value.Currency.ShouldBe(Currencies.Monopolonian);
        result.Value.Started.ShouldBe(false);
        result.Value.DefaultStartAmount.ShouldBe(1500);
        result.Value.BankerUser.ShouldNotBeNull();
        result.Value.BankerUser.UserId.ShouldBe(banker.Value.UserId);
        result.Value.BankerUser.Name.ShouldBe(banker.Value.Name);

        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Add user", new Link($"/games/{gameId}/users/", HttpMethod.Post)));
    }

    private static GameStore CreateGameStore()
    {
        var log = new CommandLog("Data Source=:memory:");
        return new GameStore(log);
    }
}

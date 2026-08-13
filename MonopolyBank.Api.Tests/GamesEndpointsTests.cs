using System.Net;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using MonopolyBank.Api;
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
        result.Links.ShouldContain(new KeyValuePair<string, string>("Add user", $"/games/{result.Value.GameId}/users/"));


        store.Find(result.Value.GameId).ShouldNotBeNull();
    }

    [Fact]
    public void CreateUserInGame_ReturnsCreatedWithAUserIdThatCanBeFoundInTheStore()
    {
        var store = CreateGameStore();
        var game = GamesEndpoints.CreateGame(store);
        var gameId = game.Value!.GameId;

        var result = GamesEndpoints.CreateUser(store, gameId, new CreateUserRequest("Robert", UserType.Banker));

        
        result.Http.Location.ShouldBe($"/games/{gameId}/users/");
        result.Http.Method.ShouldBe(HttpMethod.Post);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.Created);
        
        result.Value.ShouldNotBeNull();
        result.Value.UserId.ShouldNotBe(Guid.Empty);
        
        result.Links.ShouldBeEmpty();
        
        var storedGame = store.Find(gameId).ShouldNotBeNull();
        storedGame.Users.ShouldContain(u => u.Player.Name == "Robert");
    }

    private static GameStore CreateGameStore()
    {
        var log = new CommandLog("Data Source=:memory:");
        return new GameStore(log);
    }
}

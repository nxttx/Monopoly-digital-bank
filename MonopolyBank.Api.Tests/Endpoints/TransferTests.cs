using System.Net;
using MonopolyBank.Api.Endpoints;
using MonopolyBank.Domain;
using Shouldly;
using Xunit;

namespace MonopolyBank.Api.Tests.Endpoints;

public class TransferTests: EndpointTestBase
{
    [Fact]
    public void ABankerShouldBeAbleToGivePlayersMoney()
    { 
        var store = CreateGameStore();
        var gameId = CreateGame(store);
        const int transferAmount = 100;

        var bankerId = GamesUsersEndpoints
            .CreateUser(store, gameId, new CreateUserRequest("Bob", nameof(UserRole.Banker)))
            .ShouldBeOfType<ApiResult<UserCreated>>().Value!.UserId;
        var playerId = GamesUsersEndpoints
            .CreateUser(store, gameId, new CreateUserRequest("Mick", nameof(UserRole.Player)))
            .ShouldBeOfType<ApiResult<UserCreated>>().Value!.UserId;
        GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Allice", nameof(UserRole.Player)))
            .ShouldBeOfType<ApiResult<UserCreated>>();

        GamesEndpoints.StartGame(store, gameId).ShouldBeOfType<ApiResult<GameStarted>>();

        var result = GamesUsersEndpoints.GiveMoney(store, gameId, bankerId, playerId, transferAmount).ShouldBeOfType<ApiResult<MoneyTransferred>>();

        result.Http.Location.ShouldBe($"/games/{gameId}/users/{bankerId}/give-money/{playerId}/");
        result.Http.Method.ShouldBe(HttpMethod.Post);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Value.ShouldNotBeNull();
        result.Value.GameId.ShouldBe(gameId);
        result.Value.Amount.ShouldBe(transferAmount);
        result.Value.From.ShouldBe(bankerId);
        result.Value.To.ShouldBe(playerId);

        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Get game details", new Link($"/games/{gameId}/", HttpMethod.Get)));
        
        store.Find(gameId).ShouldNotBeNull();
        var user = store.FindUser(gameId, playerId);
        user!.Money.Amount.ShouldBe(Game.DefaultStartAmount + transferAmount);
    }

    [Fact]
    public void APlayerShouldBeAbleToTransferMoneyToAnotherPlayer()
    {
        var store = CreateGameStore();
        var gameId = CreateGame(store);
        const int transferAmount = 100;

        GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Bob", nameof(UserRole.Banker)))
            .ShouldBeOfType<ApiResult<UserCreated>>();
        var toId = GamesUsersEndpoints
            .CreateUser(store, gameId, new CreateUserRequest("Mick", nameof(UserRole.Player)))
            .ShouldBeOfType<ApiResult<UserCreated>>().Value!.UserId;
        var fromId = GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Allice", nameof(UserRole.Player)))
            .ShouldBeOfType<ApiResult<UserCreated>>().Value!.UserId;

        GamesEndpoints.StartGame(store, gameId).ShouldBeOfType<ApiResult<GameStarted>>();

        var result = GamesUsersEndpoints.TransferMoney(store, gameId, fromId, toId, transferAmount).ShouldBeOfType<ApiResult<MoneyTransferred>>();

        result.Http.Location.ShouldBe($"/games/{gameId}/users/{fromId}/transfer-money/{toId}/");
        result.Http.Method.ShouldBe(HttpMethod.Post);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Value.ShouldNotBeNull();
        result.Value.GameId.ShouldBe(gameId);
        result.Value.Amount.ShouldBe(transferAmount);
        result.Value.From.ShouldBe(fromId);
        result.Value.To.ShouldBe(toId);

        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Get game details", new Link($"/games/{gameId}/", HttpMethod.Get)));
        
        store.Find(gameId).ShouldNotBeNull();
        var fromUser = store.FindUser(gameId, fromId);
        fromUser!.Money.Amount.ShouldBe(Game.DefaultStartAmount - transferAmount);
        
        var toUser = store.FindUser(gameId, toId);
        toUser!.Money.Amount.ShouldBe(Game.DefaultStartAmount + transferAmount);
        
    }
    
    [Fact(Skip = "not yet written")]
    public void APlayerShouldNotBeAbleToStealFromABanker()
    {

    }

    [Fact(Skip = "not yet written")]
    public void APlayerShouldNotBeAbleToGiveMoneyToABanker()
    {
        
    }

    [Fact(Skip = "not yet written")]
    public void APlayerShouldNotBeAbleToStealFromAnotherPlayer()
    {
        
    }
}
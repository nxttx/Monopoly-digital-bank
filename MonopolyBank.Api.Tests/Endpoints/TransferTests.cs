using System.Net;
using MonopolyBank.Api.Endpoints;
using MonopolyBank.Domain;
using Shouldly;
using Xunit;

namespace MonopolyBank.Api.Tests.Endpoints;

public class TransferTests : EndpointTestBase
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

        var result = GamesUsersEndpoints.GiveMoney(store, gameId, bankerId, playerId, transferAmount)
            .ShouldBeOfType<ApiResult<MoneyTransferred>>();

        result.Http.Location.ShouldBe($"/games/{gameId}/users/{bankerId}/give-money/{playerId}/");
        result.Http.Method.ShouldBe(HttpMethod.Post);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Value.ShouldNotBeNull();
        result.Value.GameId.ShouldBe(gameId);
        result.Value.Amount.ShouldBe(transferAmount);
        result.Value.From.ShouldBe(bankerId);
        result.Value.To.ShouldBe(playerId);

        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Get game details",
            new Link($"/games/{gameId}/", HttpMethod.Get)));

        store.Find(gameId).ShouldNotBeNull();
        var user = store.FindUser(gameId, playerId);
        user!.Money.Amount.ShouldBe(Game.DefaultStartAmount + transferAmount);
    }

    [Theory]
    [InlineData(UserRole.Player)]
    [InlineData(UserRole.Both)]
    public void APlayerShouldBeAbleToTransferMoneyToAnotherPlayer(UserRole receiverRole)
    {
        var store = CreateGameStore();
        var gameId = CreateGame(store);
        const int transferAmount = 100;

        if (receiverRole == UserRole.Player)
        {
            GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Bob", nameof(UserRole.Banker)))
                .ShouldBeOfType<ApiResult<UserCreated>>();
        }

        var toId = GamesUsersEndpoints
            .CreateUser(store, gameId, new CreateUserRequest("Mick", receiverRole.ToString()))
            .ShouldBeOfType<ApiResult<UserCreated>>().Value!.UserId;
        var fromId = GamesUsersEndpoints
            .CreateUser(store, gameId, new CreateUserRequest("Allice", nameof(UserRole.Player)))
            .ShouldBeOfType<ApiResult<UserCreated>>().Value!.UserId;

        GamesEndpoints.StartGame(store, gameId).ShouldBeOfType<ApiResult<GameStarted>>();

        var result = GamesUsersEndpoints.TransferMoney(store, gameId, fromId, toId, transferAmount)
            .ShouldBeOfType<ApiResult<MoneyTransferred>>();

        result.Http.Location.ShouldBe($"/games/{gameId}/users/{fromId}/transfer-money/{toId}/");
        result.Http.Method.ShouldBe(HttpMethod.Post);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Value.ShouldNotBeNull();
        result.Value.GameId.ShouldBe(gameId);
        result.Value.Amount.ShouldBe(transferAmount);
        result.Value.From.ShouldBe(fromId);
        result.Value.To.ShouldBe(toId);

        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Get game details",
            new Link($"/games/{gameId}/", HttpMethod.Get)));

        store.Find(gameId).ShouldNotBeNull();
        var fromUser = store.FindUser(gameId, fromId);
        fromUser!.Money.Amount.ShouldBe(Game.DefaultStartAmount - transferAmount);

        var toUser = store.FindUser(gameId, toId);
        toUser!.Money.Amount.ShouldBe(Game.DefaultStartAmount + transferAmount);
    }

    [Fact]
    public void ABankerShouldBeAbleToChargeAPlayersMoney()
    {
        var store = CreateGameStore();
        var gameId = CreateGame(store);
        const int transferAmount = 100;


        var bankerId = GamesUsersEndpoints
            .CreateUser(store, gameId, new CreateUserRequest("Bob", nameof(UserRole.Banker)))
            .ShouldBeOfType<ApiResult<UserCreated>>().Value!.UserId;

        var fromId = GamesUsersEndpoints
            .CreateUser(store, gameId, new CreateUserRequest("Mick", nameof(UserRole.Player)))
            .ShouldBeOfType<ApiResult<UserCreated>>().Value!.UserId;

        GamesUsersEndpoints
            .CreateUser(store, gameId, new CreateUserRequest("Alice", nameof(UserRole.Player)))
            .ShouldBeOfType<ApiResult<UserCreated>>();

        GamesEndpoints.StartGame(store, gameId).ShouldBeOfType<ApiResult<GameStarted>>();

        var result = GamesUsersEndpoints.ChargePlayer(store, gameId, bankerId, fromId, transferAmount)
            .ShouldBeOfType<ApiResult<MoneyTransferred>>();

        result.Http.Location.ShouldBe($"/games/{gameId}/users/{bankerId}/charge-player/{fromId}/");
        result.Http.Method.ShouldBe(HttpMethod.Post);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Value.ShouldNotBeNull();
        result.Value.GameId.ShouldBe(gameId);
        result.Value.Amount.ShouldBe(transferAmount);
        result.Value.From.ShouldBe(fromId);
        result.Value.To.ShouldBe(bankerId);

        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Get game details",
            new Link($"/games/{gameId}/", HttpMethod.Get)));

        store.Find(gameId).ShouldNotBeNull();
        var fromUser = store.FindUser(gameId, fromId);
        fromUser!.Money.Amount.ShouldBe(Game.DefaultStartAmount - transferAmount);
    }

    [Fact]
    public void APlayerShouldNotBeAbleToChargeAnotherUser()
    {
        var store = CreateGameStore();
        var gameId = CreateGame(store);
        const int transferAmount = 100;

        var bankerAndPlayerId = GamesUsersEndpoints
            .CreateUser(store, gameId, new CreateUserRequest("Bob", nameof(UserRole.Both)))
            .ShouldBeOfType<ApiResult<UserCreated>>().Value!.UserId;

        var toId = GamesUsersEndpoints
            .CreateUser(store, gameId, new CreateUserRequest("Mick", nameof(UserRole.Player)))
            .ShouldBeOfType<ApiResult<UserCreated>>().Value!.UserId;
        GamesUsersEndpoints
            .CreateUser(store, gameId, new CreateUserRequest("Allice", nameof(UserRole.Player)))
            .ShouldBeOfType<ApiResult<UserCreated>>();

        GamesEndpoints.StartGame(store, gameId).ShouldBeOfType<ApiResult<GameStarted>>();

        var result = GamesUsersEndpoints.ChargePlayer(store, gameId, toId, bankerAndPlayerId, transferAmount)
            .ShouldBeOfType<ApiError>();

        result.Http.Location.ShouldBe($"/games/{gameId}/users/{toId}/charge-player/{bankerAndPlayerId}/");
        result.Http.Method.ShouldBe(HttpMethod.Post);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        VerifyResultDoesNotContainValueProperty(result);
        result.Errors.ShouldContain(e => e.Field == "GenericMessage" && e.Message ==
            "A user cannot charge money from another player or a banker.");

        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Get game details",
            new Link($"/games/{gameId}/", HttpMethod.Get)));
        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Transfer money (player -> player)",
            new Link($"/games/{gameId}/users/{bankerAndPlayerId}/transfer-money/{toId}/", HttpMethod.Post)));


        store.Find(gameId).ShouldNotBeNull();
        var toUser = store.FindUser(gameId, toId);
        toUser!.Money.Amount.ShouldBe(Game.DefaultStartAmount);
    }

    [Fact]
    public void ABankerShouldNotBeAbleToUseTransferMoney()
    {
        var store = CreateGameStore();
        var gameId = CreateGame(store);
        const int transferAmount = 100;

        var fromId = GamesUsersEndpoints
            .CreateUser(store, gameId, new CreateUserRequest("Bob", nameof(UserRole.Banker)))
            .ShouldBeOfType<ApiResult<UserCreated>>().Value!.UserId;


        var toId = GamesUsersEndpoints
            .CreateUser(store, gameId, new CreateUserRequest("Mick", nameof(UserRole.Player)))
            .ShouldBeOfType<ApiResult<UserCreated>>().Value!.UserId;
        GamesUsersEndpoints
            .CreateUser(store, gameId, new CreateUserRequest("Allice", nameof(UserRole.Player)))
            .ShouldBeOfType<ApiResult<UserCreated>>();

        GamesEndpoints.StartGame(store, gameId).ShouldBeOfType<ApiResult<GameStarted>>();

        var result = GamesUsersEndpoints.TransferMoney(store, gameId, fromId, toId, transferAmount)
            .ShouldBeOfType<ApiError>();

        result.Http.Location.ShouldBe($"/games/{gameId}/users/{fromId}/transfer-money/{toId}/");
        result.Http.Method.ShouldBe(HttpMethod.Post);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        VerifyResultDoesNotContainValueProperty(result);
        result.Errors.ShouldContain(e => e.Field == "GenericMessage" && e.Message ==
            "A banker cannot transfer money to another player, a banker can only give money to player");

        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Get game details",
            new Link($"/games/{gameId}/", HttpMethod.Get)));
        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Give money (banker -> player)",
            new Link($"/games/{gameId}/users/{fromId}/give-money/{toId}/", HttpMethod.Post)));
        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Transfer money (player -> player)",
            new Link($"/games/{gameId}/users/{fromId}/transfer-money/{toId}/", HttpMethod.Post)));


        store.Find(gameId).ShouldNotBeNull();
        var toUser = store.FindUser(gameId, toId);
        toUser!.Money.Amount.ShouldBe(Game.DefaultStartAmount);
    }

    [Fact]
    public void APlayerShouldNotBeAbleToGiveMoneyToABanker()
    {
        var store = CreateGameStore();
        var gameId = CreateGame(store);
        const int transferAmount = 100;


        var toId = GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Bob", nameof(UserRole.Banker)))
            .ShouldBeOfType<ApiResult<UserCreated>>().Value!.UserId;

        GamesUsersEndpoints
            .CreateUser(store, gameId, new CreateUserRequest("Mick", nameof(UserRole.Player)))
            .ShouldBeOfType<ApiResult<UserCreated>>();
        var fromId = GamesUsersEndpoints
            .CreateUser(store, gameId, new CreateUserRequest("Allice", nameof(UserRole.Player)))
            .ShouldBeOfType<ApiResult<UserCreated>>().Value!.UserId;

        GamesEndpoints.StartGame(store, gameId).ShouldBeOfType<ApiResult<GameStarted>>();

        var result = GamesUsersEndpoints.TransferMoney(store, gameId, fromId, toId, transferAmount)
            .ShouldBeOfType<ApiError>();

        result.Http.Location.ShouldBe($"/games/{gameId}/users/{fromId}/transfer-money/{toId}/");
        result.Http.Method.ShouldBe(HttpMethod.Post);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        VerifyResultDoesNotContainValueProperty(result);
        result.Errors.ShouldContain(e => e.Field == "GenericMessage" && e.Message ==
            "A player cannot transfer money to a banker, a banker can only charge a player");


        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Get game details",
            new Link($"/games/{gameId}/", HttpMethod.Get)));
        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Charge player (banker -> player)",
            new Link($"/games/{gameId}/users/{toId}/charge-player/{fromId}/", HttpMethod.Post)));

        store.Find(gameId).ShouldNotBeNull();
        var fromUser = store.FindUser(gameId, fromId);
        fromUser!.Money.Amount.ShouldBe(Game.DefaultStartAmount);
    }

    [Fact]
    public void APlayerShouldNotBeAbleToStealFromAnotherPlayer()
    {
        var store = CreateGameStore();
        var gameId = CreateGame(store);
        const int transferAmount = -100;
        
        GamesUsersEndpoints.CreateUser(store, gameId, new CreateUserRequest("Bob", nameof(UserRole.Banker)))
            .ShouldBeOfType<ApiResult<UserCreated>>();

        var toId = GamesUsersEndpoints
            .CreateUser(store, gameId, new CreateUserRequest("Mick", nameof(UserRole.Player)))
            .ShouldBeOfType<ApiResult<UserCreated>>().Value!.UserId;
        var fromId = GamesUsersEndpoints
            .CreateUser(store, gameId, new CreateUserRequest("Allice", nameof(UserRole.Player)))
            .ShouldBeOfType<ApiResult<UserCreated>>().Value!.UserId;

        GamesEndpoints.StartGame(store, gameId).ShouldBeOfType<ApiResult<GameStarted>>();

        var result = GamesUsersEndpoints.TransferMoney(store, gameId, fromId, toId, transferAmount)
            .ShouldBeOfType<ApiError>();

        result.Http.Location.ShouldBe($"/games/{gameId}/users/{fromId}/transfer-money/{toId}/");
        result.Http.Method.ShouldBe(HttpMethod.Post);
        result.Http.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        VerifyResultDoesNotContainValueProperty(result);
        result.Errors.ShouldContain(e => e.Field == "GenericMessage" && e.Message ==
            "A player cannot transfer a negative amount of money");


        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Get game details",
            new Link($"/games/{gameId}/", HttpMethod.Get)));
        result.Actions.ShouldContain(new KeyValuePair<string, Link>("Charge player (banker -> player)",
            new Link($"/games/{gameId}/users/{toId}/charge-player/{fromId}/", HttpMethod.Post)));

        store.Find(gameId).ShouldNotBeNull();
        
        var toUser = store.FindUser(gameId, toId);
        toUser!.Money.Amount.ShouldBe(Game.DefaultStartAmount);
        
        var fromUser = store.FindUser(gameId, fromId);
        fromUser!.Money.Amount.ShouldBe(Game.DefaultStartAmount);
    }
}
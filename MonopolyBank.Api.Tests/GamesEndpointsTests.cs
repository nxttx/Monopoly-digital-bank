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
        var gameId =CreateGame(store);
        var gameId2 =CreateGame(store);

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
        var gameId =CreateGame(store);

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
    public void CreateUserInGame_ReturnsCreatedWithAUserIdThatCanBeFoundInTheStore()
    {
        var store = CreateGameStore();
        var gameId =CreateGame(store);

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
    public void GetGameWithUsers_ReturnsGameWithTheCorrectIdAndUserInfo()
    {
        var store = CreateGameStore();
        var gameId =CreateGame(store);
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

    [Fact]
    public void CreateUserInGameWithInvalidName_Returns400()
    {
        var store = CreateGameStore();
        var gameId =CreateGame(store);
    
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
        var gameId =CreateGame(store);
    
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

    [Fact]
    public void GetUser_ReturnsUserWithTheCorrectIdAndInfo()
    {
        var store = CreateGameStore();
        var gameId =CreateGame(store);

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
        var gameId =CreateGame(store);
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

    [Theory]
    [InlineData(UserRole.Banker)]
    [InlineData(UserRole.Both)]
    public void AddTwoBankersToGame_ShouldReturnError(UserRole role)
    { 
        var store = CreateGameStore();
        var gameId =CreateGame(store);
        
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


    private static void VerifyResultDoesNotContainValueProperty(ApiResponse result)
    {
        // design guard: an error response's type must not expose a Value property
        result.GetType().GetProperty("Value").ShouldBeNull();
    }

    private static GameStore CreateGameStore()
    {
        var log = new CommandLog("Data Source=:memory:");
        return new GameStore(log);
    }
    
    private static Guid CreateGame(GameStore store)
    {
        var game = GamesEndpoints.CreateGame(store);
        return game.Value!.GameId;
    }
}
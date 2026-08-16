using System.Net;
using MonopolyBank.Api.Endpoints;
using Shouldly;
using Xunit;

namespace MonopolyBank.Api.Tests.Endpoints;

public class GetAllGamesTests : EndpointTestBase
{
    [Fact]
    public void GetAllGames_ReturnsGamesWithAGameIdThatCanBeFoundInTheStore()
    {
        var store = CreateGameStore();
        var gameId = CreateGame(store);
        var gameId2 = CreateGame(store);

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
}

using System.Net;
using MonopolyBank.Api.Endpoints;
using Shouldly;
using Xunit;

namespace MonopolyBank.Api.Tests;

public class CreateGameTests : EndpointTestBase
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
}

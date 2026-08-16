using MonopolyBank.Api.Endpoints;
using MonopolyBank.Api.Persistence;
using Shouldly;

namespace MonopolyBank.Api.Tests.Endpoints;

public abstract class EndpointTestBase
{
    protected static GameStore CreateGameStore()
    {
        var log = new CommandLog("Data Source=:memory:");
        return new GameStore(log);
    }

    protected static Guid CreateGame(GameStore store)
    {
        var game = GamesEndpoints.CreateGame(store);
        return game.Value!.GameId;
    }

    protected static void VerifyResultDoesNotContainValueProperty(ApiResponse result)
    {
        // design guard: an error response's type must not expose a Value property
        result.GetType().GetProperty("Value").ShouldBeNull();
    }
}

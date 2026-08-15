using MonopolyBank.Api;
using MonopolyBank.Api.Persistence;
using MonopolyBank.Domain;
using Shouldly;
using Xunit;

namespace MonopolyBank.Api.Tests;

/// <summary>
/// Temporary plumbing check for the scaffold (Claude-written, approved 2026-07-23).
/// Proves store → command log → SQLite → replay round-trips. Delete or replace
/// freely once real handler tests exist.
/// </summary>
public class ScaffoldSmokeTest
{
    [Fact]
    public void Persistance_AGameSurvivesAReplayFromTheCommandLog()
    {
        using var log = new CommandLog("Data Source=:memory:");
        var store = new GameStore(log);

        var id = store.CreateGame();
        store.Execute(id, new GameCommand.AddUser(Guid.NewGuid(), "Jan", UserType.Player));
        store.Execute(id, new GameCommand.AddUser(Guid.NewGuid(), "Bob", UserType.Player));
        store.Execute(id, new GameCommand.AddUser(Guid.NewGuid(), "Mick", UserType.Banker));
        store.Execute(id, new GameCommand.Start());
        store.Execute(id, new GameCommand.PlayerTransfer("Jan", "Bob", 100));

        // A second store on the same log knows nothing in memory — it must replay.
        var rebuilt = new GameStore(log).Find(id);

        rebuilt.ShouldNotBeNull();
        rebuilt.Users.Count.ShouldBe(3);
        rebuilt.Users.Single(u => u.Player.Name == "Jan").Player.Money.ShouldBe(1400);
        rebuilt.Users.Single(u => u.Player.Name == "Bob").Player.Money.ShouldBe(1600);
        rebuilt.Users.Single(u => u.Player.Name == "Bob").Player.History.Count.ShouldBe(2);
    }
}

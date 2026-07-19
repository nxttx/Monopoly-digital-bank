using MonopolyBank.Domain;
using Shouldly;
using Xunit;

namespace MonopolyBank.Tests;

public class Tests
{
    [Fact]
    public void AGameShouldHaveUsers()
    {
        var user = new User();
        user.ShouldNotBeNull();
        
        var game = new Game();
        game.ShouldNotBeNull();
        
        game.AddUser(user);
        game.Users.Count.ShouldBe(1);
        game.Users.ShouldContain(user);
    }
}
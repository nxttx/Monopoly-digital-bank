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

    [Fact]
    public void AUserShouldHaveMoney()
    {
        var user = new User();
        user.ShouldNotBeNull();

        user.Player.Money.ShouldBe(1000);
    }

    [Fact]
    public void AUserCanBeAPlayerOrABankerOrBoth()
    {
        User banker = new User(UserType.Banker);
        User player = new User();
        User both = new User(UserType.Both);

        banker.ShouldNotBeNull();
        player.ShouldNotBeNull();
        both.ShouldNotBeNull();

        banker.IsPlayer.ShouldBeFalse();
        banker.IsBanker.ShouldBeTrue();

        player.IsPlayer.ShouldBeTrue();
        player.IsBanker.ShouldBeFalse();

        both.IsPlayer.ShouldBeTrue();
        both.IsBanker.ShouldBeTrue();
    }

    [Fact]
    public void AnUserCanTransferMoneyToAnotherUser()
    {
        var user1 = new User();
        var user2 = new User();

        user1.Player.Money.ShouldBe(1000);
        user2.Player.Money.ShouldBe(1000);

        user1.Player.TransferMoney(user2.Player, 100);

        user1.Player.Money.ShouldBe(900);
        user2.Player.Money.ShouldBe(1100);
    }


    [Fact]
    public void ABankerCanGiveMoneyToAnotherUser()
    {
        var banker = new User(UserType.Banker);
        var user = new User();

        user.Player.Money.ShouldBe(1000);

        banker.Banker.TransferMoney(user.Player, 100);

        user.Player.Money.ShouldBe(1100);
    }

    [Fact]
    public void AUserWithRoleBothCanGiveMoneyAsABankerAndTransferMoneyAsAPlayer()
    {
        var user = new User(UserType.Both);
        var anotherUser = new User();

        user.IsBanker.ShouldBeTrue();
        user.IsPlayer.ShouldBeTrue();
        anotherUser.IsPlayer.ShouldBeTrue();

        user.Banker.TransferMoney(anotherUser.Player, 100);

        user.Player.TransferMoney(anotherUser.Player, 100);

        user.Player.Money.ShouldBe(900);
        anotherUser.Player.Money.ShouldBe(1200);
    }

    [Fact]
    public void AUserWithOutRoleBankerCannotGiveMoney()
    {
        var user = new User();
        var anotherUser = new User();

        user.IsBanker.ShouldBeFalse();
        user.IsPlayer.ShouldBeTrue();
        anotherUser.IsPlayer.ShouldBeTrue();

        Should.Throw<InvalidOperationException>(() => user.Banker.TransferMoney(anotherUser.Player, 100));
    }
    
    [Fact]
    public void ABankerWithoutPlayerRoleCannotGiveMoney()
    {
        var banker = new User(UserType.Banker);
        var anotherUser = new User();

        banker.IsBanker.ShouldBeTrue();
        banker.IsPlayer.ShouldBeFalse();
        anotherUser.IsPlayer.ShouldBeTrue();

        Should.Throw<InvalidOperationException>(() => banker.Player.TransferMoney(anotherUser.Player, 100));
    }

    [Fact]
    public void ThereShouldAlwaysOnlyBeOneBanker()
    {
        var game = new Game();
        var banker1 = new User(UserType.Banker);
        var banker2 = new User(UserType.Banker);
        var player = new User();
        var player2 = new User();
        

        banker1.IsBanker.ShouldBeTrue();
        banker2.IsBanker.ShouldBeTrue();
        player.IsPlayer.ShouldBeTrue();
        player.IsBanker.ShouldBeFalse();
        player2.IsPlayer.ShouldBeTrue();
        
        game.AddUser(player);
        game.AddUser(banker1);
        Should.Throw<InvalidOperationException>(() => game.AddUser(banker2), "There can only be one banker per game.");
        game.AddUser(player2);
        
        game.Users.Count.ShouldBe(3);
    }
    
    
}
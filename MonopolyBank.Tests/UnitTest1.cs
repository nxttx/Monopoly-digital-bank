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

        Should.Throw<MissingRoleException>(() => user.Banker.TransferMoney(anotherUser.Player, 100))
            .Message.ShouldBe("User does not have the banker role.");
    }
    
    [Fact]
    public void ABankerWithoutPlayerRoleCannotGiveMoney()
    {
        var banker = new User(UserType.Banker);
        var anotherUser = new User();

        banker.IsBanker.ShouldBeTrue();
        banker.IsPlayer.ShouldBeFalse();
        anotherUser.IsPlayer.ShouldBeTrue();

        Should.Throw<MissingRoleException>(() => banker.Player.TransferMoney(anotherUser.Player, 100))
            .Message.ShouldBe("User does not have the player role.");
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
        Should.Throw<DuplicateBankerException>(() => game.AddUser(banker2))
            .Message.ShouldBe("There can only be one banker per game.");
        game.AddUser(player2);
        
        game.Users.Count.ShouldBe(3);
    }
    
    [Fact]
    public void APlayerCannotStealFromAnotherPlayer()
    {
        var user1 = new User();
        var user2 = new User();
        
        user1.Player.Money.ShouldBe(1000);
        user2.Player.Money.ShouldBe(1000);
        
        Should.Throw<NegativeTransferException>(() => user1.Player.TransferMoney(user2.Player, -100))
            .Message.ShouldBe("A player cannot transfer a negative amount.");
        user1.Player.Money.ShouldBe(1000);
        user2.Player.Money.ShouldBe(1000);
    }
    
    [Fact]
    public void ABankerCanTakeMoneyFromAnotherPlayer()
    {
        var banker = new User(UserType.Banker);
        var user = new User();
        
        user.Player.Money.ShouldBe(1000);
        banker.Banker.TransferMoney(user.Player, -100);
        user.Player.Money.ShouldBe(900);
    }
    
    [Fact]
    public void APlayerCannotPayMoreMoneyThanTheyHave()
    {
        var user = new User();
        var anotherUser = new User();
        
        user.Player.Money.ShouldBe(1000);
        anotherUser.Player.Money.ShouldBe(1000);
        
        Should.Throw<InsufficientBalanceException>(() => user.Player.TransferMoney(anotherUser.Player, 1100))
            .Message.ShouldBe("A player's balance cannot go negative.");
        user.Player.Money.ShouldBe(1000);
        anotherUser.Player.Money.ShouldBe(1000);
    }
    
    [Fact]
    public void ABankerCannotTakeMoreMoneyThanTheyPlayerHas()
    {
        var banker = new User(UserType.Banker);
        var user = new User();

        user.Player.Money.ShouldBe(1000);
       
        Should.Throw<InsufficientBalanceException>(() => banker.Banker.TransferMoney(user.Player, -1100))
            .Message.ShouldBe("A player's balance cannot go negative.");
        user.Player.Money.ShouldBe(1000);
    }

    [Fact]
    public void AnUserCannotTransferMoneyToAnotherUserFromAnotherGame()
    {
        var user = new User();
        var anotherUser = new User();

        var game1 = new Game();
        var game2 = new Game();
        game1.AddUser(user);
        game2.AddUser(anotherUser);
        Should.Throw<CrossGameAccessException>(() => user.Player.TransferMoney(anotherUser.Player, 100))
            .Message.ShouldBe("Cannot transfer money to another user from another game.");
        
    }
    
    [Fact]
    public void AUserCannotTransferMoneyToAnotherUserFromAnotherGameWithBankerRole()
    {
        var user = new User(UserType.Banker);
        var anotherUser = new User();

        var game1 = new Game();
        var game2 = new Game();
        game1.AddUser(user);
        game2.AddUser(anotherUser);
        Should.Throw<CrossGameAccessException>(() => user.Player.TransferMoney(anotherUser.Player, 100))
            .Message.ShouldBe("Cannot transfer money to another user from another game.");
    }
    
    [Fact]
    public void AUserCannotBeAddedToTwoDifferentGames()
    {
        var user = new User(UserType.Player);
        var anotherUser = new User();

        var game1 = new Game();
        var game2 = new Game();
        game1.AddUser(user);
        Should.Throw<InvalidOperationException>(() => game2.AddUser(user))
            .Message.ShouldBe("A user cannot be added to two different games.");
    }
}
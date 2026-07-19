using MonopolyBank.Domain;
using Shouldly;
using Xunit;

namespace MonopolyBank.Tests;

public class Tests
{
    [Fact]
    public void AGameShouldHaveUsers()
    {
        var user = new User("Jan");
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
        var user = new User("Jan");
        user.ShouldNotBeNull();

        user.Player.Money.ShouldBe(1000);
    }

    [Fact]
    public void AUserCanBeAPlayerOrABankerOrBoth()
    {
        User banker = new User("Mick", UserType.Banker);
        User player = new User("Paul");
        User both = new User("Bella", UserType.Both);

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
        var user1 = new User("Jan");
        var user2 = new User("Bob");

        user1.Player.Money.ShouldBe(1000);
        user2.Player.Money.ShouldBe(1000);

        user1.Player.TransferMoney(user2.Player, 100);

        user1.Player.Money.ShouldBe(900);
        user2.Player.Money.ShouldBe(1100);
    }


    [Fact]
    public void ABankerCanGiveMoneyToAnotherUser()
    {
        var banker = new User("Mick", UserType.Banker);
        var user = new User("Jan");

        user.Player.Money.ShouldBe(1000);

        banker.Banker.TransferMoney(user.Player, 100);

        user.Player.Money.ShouldBe(1100);
    }

    [Fact]
    public void AUserWithRoleBothCanGiveMoneyAsABankerAndTransferMoneyAsAPlayer()
    {
        var user = new User("Bella", UserType.Both);
        var anotherUser = new User("Bob");

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
        var user = new User("Jan");
        var anotherUser = new User("Bob");

        user.IsBanker.ShouldBeFalse();
        user.IsPlayer.ShouldBeTrue();
        anotherUser.IsPlayer.ShouldBeTrue();

        Should.Throw<MissingRoleException>(() => user.Banker.TransferMoney(anotherUser.Player, 100))
            .Message.ShouldBe("User does not have the banker role.");
    }

    [Fact]
    public void ABankerWithoutPlayerRoleCannotGiveMoney()
    {
        var banker = new User("Mick", UserType.Banker);
        var anotherUser = new User("Bob");

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
        var banker1 = new User("Mick", UserType.Banker);
        var banker2 = new User("Ben", UserType.Banker);
        var player = new User("Paul");
        var player2 = new User("Anne");


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
        var user1 = new User("Jan");
        var user2 = new User("Bob");

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
        var banker = new User("Mick", UserType.Banker);
        var user = new User("Jan");

        user.Player.Money.ShouldBe(1000);
        banker.Banker.TransferMoney(user.Player, -100);
        user.Player.Money.ShouldBe(900);
    }

    [Fact]
    public void APlayerCannotPayMoreMoneyThanTheyHave()
    {
        var user = new User("Jan");
        var anotherUser = new User("Bob");

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
        var banker = new User("Mick", UserType.Banker);
        var user = new User("Jan");

        user.Player.Money.ShouldBe(1000);

        Should.Throw<InsufficientBalanceException>(() => banker.Banker.TransferMoney(user.Player, -1100))
            .Message.ShouldBe("A player's balance cannot go negative.");
        user.Player.Money.ShouldBe(1000);
    }

    [Fact]
    public void AnUserCannotTransferMoneyToAnotherUserFromAnotherGame()
    {
        var user = new User("Jan");
        var anotherUser = new User("Bob");

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
        var user = new User("Mick", UserType.Banker);
        var anotherUser = new User("Bob");

        var game1 = new Game();
        var game2 = new Game();
        game1.AddUser(user);
        game2.AddUser(anotherUser);
        Should.Throw<CrossGameAccessException>(() => user.Player.TransferMoney(anotherUser.Player, 100))
            .Message.ShouldBe("Cannot transfer money to another user from another game.");
    }

    [Fact]
    public void AUserCannotBeAddedToMoreThanOneGame()
    {
        var user = new User("Jan");

        var game1 = new Game();
        game1.AddUser(user);
        
        for (int i = 0; i < 10; i++)
        {
            var game2 = new Game();

            Should.Throw<UserAlreadyInGameException>(() => game2.AddUser(user))
                .Message.ShouldBe("A user cannot be added to two different games.");
        }
    }
    
    
    [Fact]
    public void APlayerShouldHaveANameAndFakeBankingInfo()
    {
        var player = new Player("Janne");
        player.Name.ShouldNotBeNullOrEmpty();
        player.Name.ShouldBe("Janne");
        player.CardNumber.ShouldNotBeNullOrEmpty();
        player.CardNumber.ShouldNotBe(Guid.Empty.ToString());
        
        player.CardExpiry.ShouldBeBetween(DateTime.Now, DateTime.Now.AddYears(2));
        player.CardCvv.ShouldNotBeNullOrEmpty();
        player.CardCvv.Length.ShouldBe(3);
    }
    
    [Fact]
    public void APlayersNameShouldNotBeEmpty()
    {
        Should.Throw<ArgumentException>(() => new Player(""))
            .Message.ShouldBe("Name cannot be empty.");
        
        var userName = "Janne";
        var user = new User(userName);
        user.Player.Name.ShouldNotBeNullOrEmpty();
        user.Player.Name.ShouldBe(userName);
     
        Should.Throw<ArgumentException>(() => new User(""))
            .Message.ShouldBe("Name cannot be empty.");
        
    }
}

internal static class ShouldlyExtensions
{
    public static void ShouldBeBetween(this DateTime date, DateTime start, DateTime end)
    {
        date.ShouldBeGreaterThanOrEqualTo(start);
        date.ShouldBeLessThanOrEqualTo(end);
    }
}
using MonopolyBank.Domain;
using Shouldly;
using Xunit;
using Xunit.Internal;

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

        user.Player.Money.ShouldBe(1500);
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
        var banker = new User("Mick", UserType.Banker);

        var game = new Game();
        game.AddUser(user1);
        game.AddUser(user2);
        game.AddUser(banker);
        game.Start();

        user1.Player.Money.ShouldBe(1500);
        user2.Player.Money.ShouldBe(1500);

        user1.Player.TransferMoney(user2.Player, 100);

        user1.Player.Money.ShouldBe(1400);
        user2.Player.Money.ShouldBe(1600);
    }


    [Fact]
    public void ABankerCanGiveMoneyToAnotherUser()
    {
        var banker = new User("Mick", UserType.Banker);
        var user = new User("Jan");
        var anotherUser = new User("Anne");

        var game = new Game();
        game.AddUser(banker);
        game.AddUser(user);
        game.AddUser(anotherUser);
        game.Start();

        user.Player.Money.ShouldBe(1500);

        banker.Banker.TransferMoney(user.Player, 100);

        user.Player.Money.ShouldBe(1600);
    }

    [Fact]
    public void AUserWithRoleBothCanGiveMoneyAsABankerAndTransferMoneyAsAPlayer()
    {
        var user = new User("Bella", UserType.Both);
        var anotherUser = new User("Bob");

        var game = new Game();
        game.AddUser(user);
        game.AddUser(anotherUser);
        game.Start();

        user.IsBanker.ShouldBeTrue();
        user.IsPlayer.ShouldBeTrue();
        anotherUser.IsPlayer.ShouldBeTrue();

        user.Banker.TransferMoney(anotherUser.Player, 100);

        user.Player.TransferMoney(anotherUser.Player, 100);

        user.Player.Money.ShouldBe(1400);
        anotherUser.Player.Money.ShouldBe(1700);
    }

    [Fact]
    public void AUserWithOutRoleBankerCannotGiveMoney()
    {
        var user = new User("Jan");
        var anotherUser = new User("Bob");
        var banker = new User("Mick", UserType.Banker);

        var game = new Game();
        game.AddUser(user);
        game.AddUser(anotherUser);
        game.AddUser(banker);
        game.Start();

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
        var thirdUser = new User("Anne");

        var game = new Game();
        game.AddUser(banker);
        game.AddUser(anotherUser);
        game.AddUser(thirdUser);
        game.Start();

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
        var banker = new User("Mick", UserType.Banker);

        var game = new Game();
        game.AddUser(user1);
        game.AddUser(user2);
        game.AddUser(banker);
        game.Start();

        user1.Player.Money.ShouldBe(1500);
        user2.Player.Money.ShouldBe(1500);

        Should.Throw<NegativeTransferException>(() => user1.Player.TransferMoney(user2.Player, -100))
            .Message.ShouldBe("A player cannot transfer a negative amount.");
        user1.Player.Money.ShouldBe(1500);
        user2.Player.Money.ShouldBe(1500);
    }

    [Fact]
    public void ABankerCanTakeMoneyFromAnotherPlayer()
    {
        var banker = new User("Mick", UserType.Banker);
        var user = new User("Jan");
        var anotherUser = new User("Anne");

        var game = new Game();
        game.AddUser(banker);
        game.AddUser(user);
        game.AddUser(anotherUser);
        game.Start();

        user.Player.Money.ShouldBe(1500);
        banker.Banker.TransferMoney(user.Player, -100);
        user.Player.Money.ShouldBe(1400);
    }

    [Fact]
    public void APlayerCannotPayMoreMoneyThanTheyHave()
    {
        var user = new User("Jan");
        var anotherUser = new User("Bob");
        var banker = new User("Mick", UserType.Banker);

        var game = new Game();
        game.AddUser(user);
        game.AddUser(anotherUser);
        game.AddUser(banker);
        game.Start();

        user.Player.Money.ShouldBe(1500);
        anotherUser.Player.Money.ShouldBe(1500);

        Should.Throw<InsufficientBalanceException>(() => user.Player.TransferMoney(anotherUser.Player, 1600))
            .Message.ShouldBe("A player's balance cannot go negative.");
        user.Player.Money.ShouldBe(1500);
        anotherUser.Player.Money.ShouldBe(1500);
    }

    [Fact]
    public void ABankerCannotTakeMoreMoneyThanTheyPlayerHas()
    {
        var banker = new User("Mick", UserType.Banker);
        var user = new User("Jan");
        var anotherUser = new User("Anne");

        var game = new Game();
        game.AddUser(banker);
        game.AddUser(user);
        game.AddUser(anotherUser);
        game.Start();

        user.Player.Money.ShouldBe(1500);

        Should.Throw<InsufficientBalanceException>(() => banker.Banker.TransferMoney(user.Player, -1600))
            .Message.ShouldBe("A player's balance cannot go negative.");
        user.Player.Money.ShouldBe(1500);
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

        player.BankCard.ShouldNotBeNull();
        player.BankCard.Number.ShouldNotBeNullOrEmpty();
        player.BankCard.Number.ShouldNotBe(Guid.Empty.ToString());
        player.BankCard.Expiry.ShouldBeBetween(DateTime.Now, DateTime.Now.AddYears(2));
        player.BankCard.Cvv.ShouldNotBeNullOrEmpty();
        player.BankCard.Cvv.Length.ShouldBe(3);
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

    [Fact]
    public void AGameRequiresAtLeastOneBankerAndTwoPlayers()
    {
        var user = new User("Janne");
        var game = new Game();
        game.AddUser(user);
        game.Users.Count.ShouldBe(1);
        Should.Throw<AmountOfPlayersException>(() => game.Start())
            .Message.ShouldBe("A game requires at least one banker and two players.");
        
        var user2 = new User("Bob");
        game.AddUser(user2);
        game.Users.Count.ShouldBe(2);
        Should.Throw<AmountOfPlayersException>(() => game.Start())
            .Message.ShouldBe("A game requires at least one banker and two players.");
        game.AddUser(new User("Mick", UserType.Banker));
        game.Users.Count.ShouldBe(3);
        game.Start();
    }

    [Fact]
    public void APlayerCannotTransferMoneyBeforeTheGameHasStarted()
    {
        var user1 = new User("Jan");
        var user2 = new User("Bob");
        var banker = new User("Mick", UserType.Banker);

        Should.Throw<GameNotStartedException>(() => user1.Player.TransferMoney(user2.Player, 100))
            .Message.ShouldBe("Cannot transfer money before the game has started.");

        var game = new Game();
        game.AddUser(user1);
        game.AddUser(user2);
        game.AddUser(banker);

        Should.Throw<GameNotStartedException>(() => user1.Player.TransferMoney(user2.Player, 100))
            .Message.ShouldBe("Cannot transfer money before the game has started.");

        game.Start();

        user1.Player.TransferMoney(user2.Player, 100);
        user1.Player.Money.ShouldBe(1400);
        user2.Player.Money.ShouldBe(1600);
    }

    [Fact]
    public void ABankerCannotTransferMoneyBeforeTheGameHasStarted()
    {
        var user1 = new User("Jan");
        var user2 = new User("Bob");
        var banker = new User("Mick", UserType.Banker);

        Should.Throw<GameNotStartedException>(() => banker.Banker.TransferMoney(user1.Player, 100))
            .Message.ShouldBe("Cannot transfer money before the game has started.");

        var game = new Game();
        game.AddUser(user1);
        game.AddUser(user2);
        game.AddUser(banker);

        Should.Throw<GameNotStartedException>(() => banker.Banker.TransferMoney(user1.Player, 100))
            .Message.ShouldBe("Cannot transfer money before the game has started.");

        game.Start();

        banker.Banker.TransferMoney(user1.Player, 100);
        user1.Player.Money.ShouldBe(1600);
    }
    
    [Fact]
    public void TheBankerShouldKnowTheAmountOfMoneyAllPlayersHave()
    {
        var user = new User("Jan");
        var anotherUser = new User("Bob");
        var banker = new User("Mick", UserType.Banker);
        var game = new Game();
        game.AddUser(user);
        game.AddUser(anotherUser);
        game.AddUser(banker);
        game.Start();

        banker.Banker.Players.ShouldAllBe(p => p.Money == 1500);

        user.Player.TransferMoney(anotherUser.Player, 100);
        banker.Banker.Players.ShouldContain(p => p == user.Player);
        banker.Banker.Players.ShouldContain(p => p == anotherUser.Player);
        
        var bankersUser = banker.Banker.Players.FirstOrDefault(p => p == user.Player);
        bankersUser.ShouldNotBeNull();
        bankersUser.Money.ShouldBe(1400);
        
        var bankersAnotherUser = banker.Banker.Players.FirstOrDefault(p => p == anotherUser.Player);
        bankersAnotherUser.ShouldNotBeNull();
        bankersAnotherUser.Money.ShouldBe(1600);
    }

    [Fact]
    public void BankerShouldBeAbleToSetTheBeginningAmountOfMoney()
    {
        var user = new User("Jan");
        var anotherUser = new User("Bob");
        var banker = new User("Mick", UserType.Banker);
        var game = new Game();
        game.AddUser(user);
        game.AddUser(anotherUser);
        game.AddUser(banker);

        banker.Banker.SetStartAmount(2000);

        banker.Banker.StartAmount.ShouldBe(2000);
        game.Users.ShouldAllBe(u => u.Player.Money == 2000);
        
        game.Start();
        
        banker.Banker.Players.ShouldAllBe(p => p.Money == 2000);
    }
    
    [Fact]
    public void APlayerShouldNotBeAbleToSetTheBeginningAmountOfMoney()
    {
        var user = new User("Jan");
        var anotherUser = new User("Bob");
        var banker = new User("Mick", UserType.Banker);
        var game = new Game();
        game.AddUser(user);
        game.AddUser(anotherUser);
        game.AddUser(banker);

        Assert.Throws<MissingRoleException>(() => anotherUser.Banker.SetStartAmount(2000))
            .Message.ShouldBe("User does not have the banker role.");
    }
    
    [Fact]
    public void APlayerShouldNotBeAbleToSetTheBeginningAmountOfMoneyWhenTheGameHasStarted()
    {
        var user = new User("Jan");
        var anotherUser = new User("Bob");
        var banker = new User("Mick", UserType.Banker);
        var game = new Game();
        game.AddUser(user);
        game.AddUser(anotherUser);
        game.AddUser(banker);
        game.Start();

        Assert.Throws<InvalidOperationException>(() => banker.Banker.SetStartAmount(2000));
    }
    
    [Fact]
    public void APlayerThatJoinsTheGameLaterShouldGetTheAjustedAmountOfStartingMoney()
    {
        var user = new User("Jan");
        var anotherUser = new User("Bob");
        var banker = new User("Mick", UserType.Banker);
        var game = new Game();
        game.AddUser(user);
        game.AddUser(anotherUser);
        game.AddUser(banker);
        
        banker.Banker.SetStartAmount(2000);
        game.Start();
        
        user.Player.Money.ShouldBe(2000);
        anotherUser.Player.Money.ShouldBe(2000);
        
        var thirdUser = new User("Anne");
        game.AddUser(thirdUser);
        thirdUser.Player.Money.ShouldBe(2000);
    }

    [Fact]
    public void ALaterJoinedUserShouldAlsoBeSeenByTheBanker()
    {
        var user = new User("Jan");
        var anotherUser = new User("Bob");
        var banker = new User("Mick", UserType.Banker);
        var game = new Game();
        game.AddUser(user);
        game.AddUser(anotherUser);
        game.AddUser(banker);
        
        game.Start();
        
        var thirdUser = new User("Anne");
        game.AddUser(thirdUser);
        game.Users.ShouldContain(p => p == thirdUser);
        banker.Banker.Players.ShouldContain(p => p == thirdUser.Player);
        
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
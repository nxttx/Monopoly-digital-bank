namespace MonopolyBank.Domain;

public class AmountOfPlayersException()
    : InvalidOperationException("A game requires at least one banker and two players.");

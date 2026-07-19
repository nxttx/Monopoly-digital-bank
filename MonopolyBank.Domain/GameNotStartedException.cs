namespace MonopolyBank.Domain;

public class GameNotStartedException()
    : InvalidOperationException("Cannot transfer money before the game has started.");

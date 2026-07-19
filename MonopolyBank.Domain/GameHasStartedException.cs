namespace MonopolyBank.Domain;

public class GameHasStartedException()
    : InvalidOperationException("Cannot set the start amount of money when the game has started.");

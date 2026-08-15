namespace MonopolyBank.Domain;

public class AlreadyStartedGameException()
    : InvalidOperationException("Game already started.");

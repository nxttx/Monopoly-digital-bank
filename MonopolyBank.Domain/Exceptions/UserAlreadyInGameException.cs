namespace MonopolyBank.Domain;

public class UserAlreadyInGameException()
    : InvalidOperationException("A user cannot be added to two different games.");

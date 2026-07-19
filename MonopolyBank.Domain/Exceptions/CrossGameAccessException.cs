namespace MonopolyBank.Domain;

public class CrossGameAccessException()
    : InvalidOperationException("Cannot transfer money to another user from another game.");

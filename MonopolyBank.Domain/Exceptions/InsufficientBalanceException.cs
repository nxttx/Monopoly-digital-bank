namespace MonopolyBank.Domain;

public class InsufficientBalanceException()
    : InvalidOperationException("A player's balance cannot go negative.");

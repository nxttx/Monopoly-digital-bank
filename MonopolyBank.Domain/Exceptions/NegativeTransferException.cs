namespace MonopolyBank.Domain;

public class NegativeTransferException()
    : InvalidOperationException("A player cannot transfer a negative amount.");

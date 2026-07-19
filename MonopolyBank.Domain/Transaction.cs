namespace MonopolyBank.Domain;

public record Transaction(Party From, Party To, int Amount);

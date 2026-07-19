namespace MonopolyBank.Domain;

public record Transaction(Role From, Role To, int Amount);

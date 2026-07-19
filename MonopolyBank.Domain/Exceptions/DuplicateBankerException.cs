namespace MonopolyBank.Domain;

public class DuplicateBankerException()
    : InvalidOperationException("There can only be one banker per game.");

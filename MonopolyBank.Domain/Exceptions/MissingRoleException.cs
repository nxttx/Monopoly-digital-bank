namespace MonopolyBank.Domain;

public class MissingRoleException(string role)
    : InvalidOperationException($"User does not have the {role} role.");

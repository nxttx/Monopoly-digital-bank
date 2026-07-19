namespace MonopolyBank.Domain;

public class MissingRoleException(string message) : Exception(message);

public class DuplicateBankerException(string message) : Exception(message);

public class NegativeTransferException(string message) : Exception(message);

public class InsufficientBalanceException(string message) : Exception(message);

public class CrossGameAccessException(string message) : Exception(message);

public class UserAlreadyInGameException(string message) : Exception(message);

public class AmountOfPlayersException(string message) : Exception(message);

public class GameNotStartedException(string message) : Exception(message);

public class GameHasStartedException(string message) : Exception(message);

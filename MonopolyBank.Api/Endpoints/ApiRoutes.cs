namespace MonopolyBank.Api.Endpoints;

/// <summary>
/// The API's route vocabulary — the single place that spells the concrete
/// locations used in envelopes (HttpCall) and actions (Link). The route
/// TEMPLATES in the registrations ("{gameId:guid}") stay literal; keeping
/// them aligned with these shapes is part of the envelope-must-not-lie
/// convention.
/// </summary>
public static class ApiRoutes
{
    public const string Games = "/games/";

    public static string Game(Guid gameId) => $"/games/{gameId}/";

    public static string GameStart(Guid gameId) => $"/games/{gameId}/start/";

    public static string GameUsers(Guid gameId) => $"/games/{gameId}/users/";

    public static string GameUser(Guid gameId, Guid userId) => $"/games/{gameId}/users/{userId}/";

    public static string GiveMoney(Guid gameId, Guid fromUserId, Guid toUserId) =>
        $"/games/{gameId}/users/{fromUserId}/give-money/{toUserId}/";

    public static string TransferMoney(Guid gameId, Guid fromUserId, Guid toUserId) =>
        $"/games/{gameId}/users/{fromUserId}/transfer-money/{toUserId}/";
}

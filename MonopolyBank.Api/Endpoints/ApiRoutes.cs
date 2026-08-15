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

    public static string GameUsers(Guid gameId) => $"/games/{gameId}/users/";
}

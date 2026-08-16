using System.Net;

namespace MonopolyBank.Api.Endpoints;

/// <summary>
/// The API's response family: every response says which call it answers and
/// which named follow-up actions a client can take. Success and error are
/// sibling types — an <see cref="ApiError"/> deliberately has no Value
/// property, so "an error carries no value" is a type-level guarantee.
/// </summary>
public abstract record ApiResponse(HttpCall Http, Dictionary<string, Link> Actions);

public record ApiResult<T>(HttpCall Http, T? Value, Dictionary<string, Link> Actions)
    : ApiResponse(Http, Actions);

public record ApiError(HttpCall Http, IReadOnlyList<FieldError> Errors, Dictionary<string, Link> Actions)
    : ApiResponse(Http, Actions);

public record FieldError(string Field, string Message);

/// <summary>Error envelopes shared by multiple endpoints.</summary>
public static class ApiErrors
{
    public static ApiError GameNotFound(string location, HttpMethod method) => new(
        new HttpCall(location, method, HttpStatusCode.NotFound),
        [new FieldError("GameId", "Game not found.")],
        new Dictionary<string, Link> { ["Add game"] = new(ApiRoutes.Games, HttpMethod.Post) });
}

public record HttpCall(string Location, HttpMethod Method, HttpStatusCode StatusCode);

public record Link(string Location, HttpMethod Method);

public static class ApiResultExtensions
{
    /// <summary>
    /// Turns a response into the real HTTP response it describes: the wire
    /// status code comes from <c>Http.StatusCode</c>, the response is the body
    /// (serialized as its runtime type, so an error body has no value key).
    /// Route registrations must match <c>Http.Location</c>/<c>Http.Method</c>.
    /// </summary>
    public static IResult ToHttpResult(this ApiResponse response) =>
        Results.Json((object)response, statusCode: (int)response.Http.StatusCode);
}

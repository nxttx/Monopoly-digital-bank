using System.Net;

namespace MonopolyBank.Api.Endpoints;

/// <summary>
/// The API's own response envelope: which call this answers, the payload,
/// and named follow-up links a client can take from here.
/// </summary>
public record ApiResult<T>(HttpCall Http, T? Value, Dictionary<string, Link> Links);

public record HttpCall(string Location, HttpMethod Method, HttpStatusCode StatusCode);

public record Link(string Location, HttpMethod Method);

public static class ApiResultExtensions
{
    /// <summary>
    /// Turns the envelope into the real HTTP response it describes: the wire
    /// status code comes from <c>Http.StatusCode</c>, the envelope is the body.
    /// Route registrations must match <c>Http.Location</c>/<c>Http.Method</c>.
    /// </summary>
    public static IResult ToHttpResult<T>(this ApiResult<T> result) =>
        Results.Json(result, statusCode: (int)result.Http.StatusCode);
}

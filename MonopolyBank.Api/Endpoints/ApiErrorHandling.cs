using System.Net;
using System.Text.Json;

namespace MonopolyBank.Api.Endpoints;

/// <summary>
/// Catches request-binding failures (invalid enum strings, malformed JSON,
/// missing bodies — thrown because RouteHandlerOptions.ThrowOnBadRequest is on)
/// and answers them in the API's own ApiError envelope instead of the
/// framework's empty-bodied 400. Handlers never see these requests, so this is
/// wire-level infrastructure, verified with curl rather than handler tests.
/// </summary>
public static class ApiErrorHandling
{
    public static void UseBadRequestEnvelope(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            try
            {
                await next(context);
            }
            catch (BadHttpRequestException exception)
            {
                var location = context.Request.Path.Value is { Length: > 0 } path ? path : "/";
                if (!location.EndsWith('/'))
                    location += "/";
                var method = new HttpMethod(context.Request.Method);

                var error = new ApiError(
                    new HttpCall(location, method, HttpStatusCode.BadRequest),
                    [new FieldError(FieldFrom(exception), "Invalid request body.")],
                    new Dictionary<string, Link> { ["Retry"] = new(location, method) });

                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync<object>(error);
            }
        });
    }

    private static string FieldFrom(Exception exception)
    {
        for (var inner = exception.InnerException; inner is not null; inner = inner.InnerException)
            if (inner is JsonException { Path: { Length: > 2 } path } && path.StartsWith("$."))
                return char.ToUpperInvariant(path[2]) + path[3..];
        return "Body";
    }
}

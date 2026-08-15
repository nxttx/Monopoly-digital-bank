using System.Text.Json;
using System.Text.Json.Serialization;

namespace MonopolyBank.Api.Endpoints;

/// <summary>
/// Serializes an HttpMethod as its plain name: <c>"POST"</c> instead of the
/// default <c>{"method": "POST"}</c> (HttpMethod is a class, not an enum, so
/// the string-enum converter never applies). Output-only — no request
/// contract carries a method.
/// </summary>
public class HttpMethodJsonConverter : JsonConverter<HttpMethod>
{
    public override void Write(Utf8JsonWriter writer, HttpMethod value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Method);

    public override HttpMethod Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException("HttpMethod is an output-only part of the API contract.");
}

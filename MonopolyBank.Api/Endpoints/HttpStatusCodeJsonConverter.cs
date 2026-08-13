using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MonopolyBank.Api.Endpoints;

/// <summary>
/// Serializes an HttpStatusCode as both its number and its name:
/// <c>{"code": 201, "name": "Created"}</c>. Output-only — no request
/// contract carries a status code. Must be registered BEFORE the global
/// JsonStringEnumConverter, which would otherwise claim the enum.
/// </summary>
public class HttpStatusCodeJsonConverter : JsonConverter<HttpStatusCode>
{
    public override void Write(Utf8JsonWriter writer, HttpStatusCode value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("code", (int)value);
        writer.WriteString("name", value.ToString());
        writer.WriteEndObject();
    }

    public override HttpStatusCode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException("HttpStatusCode is an output-only part of the API contract.");
}

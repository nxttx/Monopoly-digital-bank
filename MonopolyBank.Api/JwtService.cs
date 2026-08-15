using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MonopolyBank.Api;

/// <summary>
/// Hand-rolled HS256 JWTs: base64url(header).base64url(payload).base64url(hmac).
/// HS256 is the only supported algorithm — generation refuses to mint anything
/// else, and validation checks the token's alg claim against the same whitelist
/// before verifying the signature, so an "alg":"none" forgery can never pass.
/// </summary>
public static class JwtService
{
    public static string GenerateJwt(string? headerAlg, string headerTyp, Dictionary<string, object> payload, string secret)
    {
        EnsureSupportedAlg(headerAlg);

        var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { alg = headerAlg, typ = headerTyp }));
        var body = Base64Url(JsonSerializer.SerializeToUtf8Bytes(payload));
        return $"{header}.{body}.{Sign($"{header}.{body}", secret)}";
    }

    public static bool ValidateJwt(string jwt, string secret)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3)
            return false;

        EnsureSupportedAlg(ReadHeaderAlg(parts[0]));

        var expected = Sign($"{parts[0]}.{parts[1]}", secret);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(parts[2]),
            Encoding.UTF8.GetBytes(expected));
    }

    public static Dictionary<string, object> ReadJwt(string jwt, string secret)
    {
        if (!ValidateJwt(jwt, secret))
            throw new ArgumentException("Invalid token.");

        var payloadJson = Encoding.UTF8.GetString(FromBase64Url(jwt.Split('.')[1]));
        return JsonSerializer.Deserialize<Dictionary<string, string>>(payloadJson)!
            .ToDictionary(claim => claim.Key, claim => (object)claim.Value);
    }

    private static void EnsureSupportedAlg(string? alg)
    {
        if (alg != "HS256")
            throw new ArgumentException("Alg must be \"HS256\".");
    }

    private static string? ReadHeaderAlg(string headerSegment)
    {
        try
        {
            using var header = JsonDocument.Parse(FromBase64Url(headerSegment));
            return header.RootElement.TryGetProperty("alg", out var alg) ? alg.GetString() : null;
        }
        catch (FormatException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Sign(string data, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Base64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '='));
    }
}

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MonopolyBank.Api;

/// <summary>
/// Hand-rolled HS256 JWTs: base64url(header).base64url(payload).base64url(hmac).
/// Validation deliberately ignores the token's alg header and always recomputes
/// HS256 — honoring a client-supplied alg (e.g. "none") is the classic JWT
/// forgery vector, and this API only ever signs HS256.
/// </summary>
public static class JwtService
{
    public static string GenerateJwt(string headerAlg, string headerTyp, Dictionary<string, object> payload, string secret)
    {
        var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { alg = headerAlg, typ = headerTyp }));
        var body = Base64Url(JsonSerializer.SerializeToUtf8Bytes(payload));
        return $"{header}.{body}.{Sign($"{header}.{body}", secret)}";
    }

    public static bool ValidateJwt(string jwt, string secret)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3)
            return false;

        var expected = Sign($"{parts[0]}.{parts[1]}", secret);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(parts[2]),
            Encoding.UTF8.GetBytes(expected));
    }

    private static string Sign(string data, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Base64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

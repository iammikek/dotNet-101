using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using dotNet101.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace dotNet101.Infrastructure.Security;

public sealed class JwtTokenService : ITokenService
{
    private readonly string _secret;
    private readonly int _expireMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        _secret = configuration["JWT_SECRET"] ?? "change-me-in-production";
        _expireMinutes = int.TryParse(configuration["JWT_EXPIRE_MINUTES"], out var minutes) ? minutes : 60;
    }

    public string CreateAccessToken(string subject)
    {
        var headerJson = JsonSerializer.Serialize(new { alg = "HS256", typ = "JWT" });
        var expiration = DateTimeOffset.UtcNow.AddMinutes(_expireMinutes).ToUnixTimeSeconds();
        var payloadJson = JsonSerializer.Serialize(new { sub = subject, exp = expiration });

        var encodedHeader = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
        var encodedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var unsignedToken = $"{encodedHeader}.{encodedPayload}";
        var signature = ComputeSignature(unsignedToken);

        return $"{unsignedToken}.{signature}";
    }

    public string? ValidateAndGetSubject(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return null;
        }

        var unsignedToken = $"{parts[0]}.{parts[1]}";
        var expectedSignature = ComputeSignature(unsignedToken);
        if (!FixedTimeEquals(parts[2], expectedSignature))
        {
            return null;
        }

        try
        {
            var payloadBytes = Base64UrlDecode(parts[1]);
            using var document = JsonDocument.Parse(payloadBytes);
            var root = document.RootElement;

            if (!root.TryGetProperty("sub", out var subjectElement) || subjectElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            if (!root.TryGetProperty("exp", out var expirationElement) || !expirationElement.TryGetInt64(out var expiration))
            {
                return null;
            }

            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= expiration)
            {
                return null;
            }

            return subjectElement.GetString();
        }
        catch (JsonException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private string ComputeSignature(string value)
    {
        var secretBytes = Encoding.UTF8.GetBytes(_secret);
        using var hmac = new HMACSHA256(secretBytes);
        return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        var padding = base64.Length % 4;
        if (padding > 0)
        {
            base64 = base64.PadRight(base64.Length + (4 - padding), '=');
        }

        return Convert.FromBase64String(base64);
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}

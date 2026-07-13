using System.Text.Json.Serialization;
using dotNet101.Domain.Entities;

namespace dotNet101.Application.Models;

public sealed class UserResponse
{
    public int Id { get; init; }
    public string Email { get; init; } = string.Empty;

    public static UserResponse From(User user) => new()
    {
        Id = user.Id,
        Email = user.Email
    };
}

public sealed class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = "bearer";
}

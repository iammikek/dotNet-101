namespace dotNet101.Application.Models;

public sealed class UserCreateRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

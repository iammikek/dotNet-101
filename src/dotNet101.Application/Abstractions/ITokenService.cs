namespace dotNet101.Application.Abstractions;

public interface ITokenService
{
    string CreateAccessToken(string subject);
    string? ValidateAndGetSubject(string token);
}

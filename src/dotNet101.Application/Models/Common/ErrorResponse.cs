namespace dotNet101.Application.Models;

public sealed class ErrorResponse
{
    public string Detail { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
}

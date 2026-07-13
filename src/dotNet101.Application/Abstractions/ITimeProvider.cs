namespace dotNet101.Application.Abstractions;

public interface ITimeProvider
{
    DateTimeOffset UtcNow { get; }
}

using dotNet101.Application.Abstractions;

namespace dotNet101.Infrastructure.Time;

public sealed class SystemTimeProvider : ITimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

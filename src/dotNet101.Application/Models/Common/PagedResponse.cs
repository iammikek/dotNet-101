namespace dotNet101.Application.Models;

public sealed class PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Total { get; init; }
    public int Skip { get; init; }
    public int Limit { get; init; }
}

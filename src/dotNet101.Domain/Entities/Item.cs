namespace dotNet101.Domain.Entities;

public sealed class Item
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public decimal Price { get; init; }
    public string? Description { get; init; }
    public int? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
}

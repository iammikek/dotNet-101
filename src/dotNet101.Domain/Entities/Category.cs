namespace dotNet101.Domain.Entities;

public sealed class Category
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
}

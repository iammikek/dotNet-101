namespace dotNet101.Domain.Entities;

public sealed class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
}

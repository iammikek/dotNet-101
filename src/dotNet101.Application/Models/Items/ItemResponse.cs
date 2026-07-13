using System.Text.Json.Serialization;
using dotNet101.Domain.Entities;

namespace dotNet101.Application.Models;

public sealed class ItemResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }

    [JsonPropertyName("category_id")]
    public int? CategoryId { get; init; }

    public CategoryResponse? Category { get; init; }

    public static ItemResponse From(Item item, Category? category) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Description = item.Description,
        Price = item.Price,
        CategoryId = item.CategoryId,
        Category = category is null ? null : CategoryResponse.From(category)
    };
}

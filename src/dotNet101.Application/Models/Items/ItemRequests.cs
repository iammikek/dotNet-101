using System.Text.Json.Serialization;

namespace dotNet101.Application.Models;

public sealed class ItemCreateRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }

    [JsonPropertyName("category_id")]
    public int? CategoryId { get; init; }
}

public sealed class ItemUpdateRequest
{
    public bool HasName { get; init; }
    public string? Name { get; init; }
    public bool HasDescription { get; init; }
    public string? Description { get; init; }
    public bool HasPrice { get; init; }
    public decimal? Price { get; init; }
    public bool HasCategoryId { get; init; }

    [JsonPropertyName("category_id")]
    public int? CategoryId { get; init; }
}

public sealed class ItemListFilters
{
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public int? CategoryId { get; init; }
    public string? NameContains { get; init; }
}

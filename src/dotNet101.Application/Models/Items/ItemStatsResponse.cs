using System.Text.Json.Serialization;

namespace dotNet101.Application.Models;

public sealed class CategoryItemStatsResponse
{
    [JsonPropertyName("category_id")]
    public int CategoryId { get; init; }

    [JsonPropertyName("category_name")]
    public string CategoryName { get; init; } = string.Empty;

    [JsonPropertyName("item_count")]
    public int ItemCount { get; init; }

    [JsonPropertyName("average_price")]
    public decimal AveragePrice { get; init; }
}

public sealed class ItemStatsResponse
{
    [JsonPropertyName("total_items")]
    public int TotalItems { get; init; }

    [JsonPropertyName("average_price")]
    public decimal AveragePrice { get; init; }

    [JsonPropertyName("min_price")]
    public decimal? MinPrice { get; init; }

    [JsonPropertyName("max_price")]
    public decimal? MaxPrice { get; init; }

    [JsonPropertyName("uncategorized_count")]
    public int UncategorizedCount { get; init; }

    [JsonPropertyName("by_category")]
    public IReadOnlyList<CategoryItemStatsResponse> ByCategory { get; init; } = Array.Empty<CategoryItemStatsResponse>();
}

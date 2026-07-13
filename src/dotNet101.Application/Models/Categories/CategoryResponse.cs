using dotNet101.Domain.Entities;

namespace dotNet101.Application.Models;

public sealed class CategoryResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }

    public static CategoryResponse From(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Description = category.Description
    };
}

namespace dotNet101.Application.Models;

public sealed class CategoryCreateRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public sealed class CategoryUpdateRequest
{
    public bool HasName { get; init; }
    public string? Name { get; init; }
    public bool HasDescription { get; init; }
    public string? Description { get; init; }
}

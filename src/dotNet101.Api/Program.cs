using dotNet101.Application.Abstractions;
using dotNet101.Domain.Entities;
using dotNet101.Infrastructure.Time;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ITimeProvider, SystemTimeProvider>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

var categories = new[]
{
    new Category { Id = 1, Name = "Hardware", Description = "Physical devices and components" },
    new Category { Id = 2, Name = "Software", Description = "Apps, licenses, and tools" }
};

var items = new[]
{
    new Item
    {
        Id = 1,
        Name = "Mechanical Keyboard",
        Price = 119.99m,
        Description = "Tactile keyboard for coding sessions",
        CategoryId = 1,
        CategoryName = "Hardware",
        CreatedAtUtc = DateTimeOffset.Parse("2026-01-15T09:30:00Z")
    },
    new Item
    {
        Id = 2,
        Name = "DotNet Monitor",
        Price = 49.00m,
        Description = "Diagnostics toolkit subscription",
        CategoryId = 2,
        CategoryName = "Software",
        CreatedAtUtc = DateTimeOffset.Parse("2026-03-01T12:00:00Z")
    }
};

app.MapGet("/", () => Results.Ok(new
{
    message = "Hello from dotNet-101"
}));

app.MapGet("/health", (ITimeProvider timeProvider) => Results.Ok(new
{
    status = "ok",
    framework = ".NET 8",
    timestamp = timeProvider.UtcNow
}));

app.MapGet("/categories", () => Results.Ok(categories));

app.MapGet("/items", (int? skip, int? limit) =>
{
    const int defaultLimit = 10;
    var normalizedSkip = Math.Max(skip ?? 0, 0);
    var normalizedLimit = limit is null or <= 0 ? defaultLimit : Math.Min(limit.Value, 100);
    var pagedItems = items.Skip(normalizedSkip).Take(normalizedLimit);

    return Results.Ok(new
    {
        items = pagedItems,
        total = items.Length,
        skip = normalizedSkip,
        limit = normalizedLimit
    });
});

app.MapGet("/items/stats/summary", () => Results.Ok(new
{
    total_items = items.Length,
    average_price = items.Average(item => item.Price),
    categories = categories.Select(category => new
    {
        id = category.Id,
        name = category.Name,
        count = items.Count(item => item.CategoryId == category.Id)
    })
}));

app.Run();

public partial class Program;

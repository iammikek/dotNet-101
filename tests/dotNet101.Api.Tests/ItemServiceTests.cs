using dotNet101.Application.Exceptions;
using dotNet101.Application.Models;
using dotNet101.Application.Services;
using dotNet101.Infrastructure.Persistence;
using Xunit;

namespace dotNet101.Api.Tests;

public sealed class ItemServiceTests
{
    [Fact]
    public void GetById_ReturnsItem_WhenExists()
    {
        var store = new AppStore();
        var categoryService = new CategoryService(store);
        var itemService = new ItemService(store);
        var category = categoryService.Create(new CategoryCreateRequest { Name = "Tools" });
        var created = itemService.Create(new ItemCreateRequest { Name = "Gadget", Price = 12.50m, CategoryId = category.Id });

        var item = itemService.GetById(created.Id);

        Assert.Equal("Gadget", item.Name);
        Assert.Equal(12.50m, item.Price);
        Assert.Equal(category.Id, item.CategoryId);
    }

    [Fact]
    public void GetById_Throws_WhenMissing()
    {
        var store = new AppStore();
        var service = new ItemService(store);

        var exception = Assert.Throws<ItemNotFoundException>(() => service.GetById(999));

        Assert.Equal(999, exception.ItemId);
    }

    [Fact]
    public void Create_PersistsItem()
    {
        var store = new AppStore();
        var categoryService = new CategoryService(store);
        var itemService = new ItemService(store);
        var category = categoryService.Create(new CategoryCreateRequest { Name = "Tools" });

        var created = itemService.Create(new ItemCreateRequest
        {
            Name = "New Item",
            Price = 19.99m,
            CategoryId = category.Id
        });

        Assert.True(created.Id >= 1);
        Assert.Equal("New Item", created.Name);
        Assert.Equal(category.Id, created.CategoryId);
        Assert.Equal("New Item", itemService.GetById(created.Id).Name);
    }

    [Fact]
    public void Delete_RemovesItem()
    {
        var store = new AppStore();
        var service = new ItemService(store);
        var created = service.Create(new ItemCreateRequest { Name = "Widget", Price = 10.00m });

        service.Delete(created.Id);

        Assert.Throws<ItemNotFoundException>(() => service.GetById(created.Id));
    }

    [Fact]
    public void GetStats_Empty_ReturnsZeroedResult()
    {
        var store = new AppStore();
        var service = new ItemService(store);

        var stats = service.GetStats();

        Assert.Equal(0, stats.TotalItems);
        Assert.Equal(0.0m, stats.AveragePrice);
        Assert.Null(stats.MinPrice);
        Assert.Null(stats.MaxPrice);
        Assert.Equal(0, stats.UncategorizedCount);
        Assert.Empty(stats.ByCategory);
    }

    [Fact]
    public void GetStats_ByCategory_ReturnsBreakdown()
    {
        var store = new AppStore();
        var categoryService = new CategoryService(store);
        var itemService = new ItemService(store);
        var tools = categoryService.Create(new CategoryCreateRequest { Name = "Tools" });
        var books = categoryService.Create(new CategoryCreateRequest { Name = "Books" });
        itemService.Create(new ItemCreateRequest { Name = "Hammer", Price = 10.00m, CategoryId = tools.Id });
        itemService.Create(new ItemCreateRequest { Name = "Drill", Price = 30.00m, CategoryId = tools.Id });
        itemService.Create(new ItemCreateRequest { Name = "Novel", Price = 15.00m, CategoryId = books.Id });
        itemService.Create(new ItemCreateRequest { Name = "Loose", Price = 5.00m });

        var stats = itemService.GetStats();

        Assert.Equal(4, stats.TotalItems);
        Assert.Equal(1, stats.UncategorizedCount);
        Assert.Equal(2, stats.ByCategory.Count);
        Assert.Equal("Books", stats.ByCategory[0].CategoryName);
        Assert.Equal(1, stats.ByCategory[0].ItemCount);
        Assert.Equal("Tools", stats.ByCategory[1].CategoryName);
        Assert.Equal(2, stats.ByCategory[1].ItemCount);
        Assert.Equal(20.00m, stats.ByCategory[1].AveragePrice);
    }

    [Fact]
    public void Update_Partial_UpdatesOnlyProvidedFields()
    {
        var store = new AppStore();
        var service = new ItemService(store);
        var created = service.Create(new ItemCreateRequest { Name = "Widget", Price = 10.00m });

        var updated = service.Update(created.Id, new ItemUpdateRequest { HasPrice = true, Price = 5.50m });

        Assert.Equal("Widget", updated.Name);
        Assert.Equal(5.50m, updated.Price);
    }

    [Fact]
    public void ListItems_FiltersByCategory()
    {
        var store = new AppStore();
        var categoryService = new CategoryService(store);
        var itemService = new ItemService(store);
        var tools = categoryService.Create(new CategoryCreateRequest { Name = "Tools" });
        categoryService.Create(new CategoryCreateRequest { Name = "Books" });
        itemService.Create(new ItemCreateRequest { Name = "A", Price = 10.00m, CategoryId = tools.Id });
        itemService.Create(new ItemCreateRequest { Name = "B", Price = 12.00m });

        var page = itemService.List(skip: 0, limit: 10, new ItemListFilters { CategoryId = tools.Id });

        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        Assert.Equal("A", page.Items[0].Name);
    }
}

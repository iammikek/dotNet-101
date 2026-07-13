using dotNet101.Application.Exceptions;
using dotNet101.Application.Models;
using dotNet101.Application.Services;
using dotNet101.Infrastructure.Persistence;
using Xunit;

namespace dotNet101.Api.Tests;

public sealed class CategoryServiceTests
{
    [Fact]
    public void GetById_ReturnsCategory_WhenExists()
    {
        var store = new AppStore();
        var service = new CategoryService(store);
        var created = service.Create(new CategoryCreateRequest { Name = "Tools", Description = "Hand tools" });

        var category = service.GetById(created.Id);

        Assert.Equal("Tools", category.Name);
        Assert.Equal("Hand tools", category.Description);
    }

    [Fact]
    public void GetById_Throws_WhenMissing()
    {
        var store = new AppStore();
        var service = new CategoryService(store);

        var exception = Assert.Throws<CategoryNotFoundException>(() => service.GetById(999));

        Assert.Equal(999, exception.CategoryId);
    }

    [Fact]
    public void Create_PersistsCategory()
    {
        var store = new AppStore();
        var service = new CategoryService(store);

        var created = service.Create(new CategoryCreateRequest { Name = "Books" });

        Assert.True(created.Id >= 1);
        Assert.Equal("Books", created.Name);
        Assert.Equal("Books", service.GetById(created.Id).Name);
    }

    [Fact]
    public void Create_ThrowsDuplicateName()
    {
        var store = new AppStore();
        var service = new CategoryService(store);
        service.Create(new CategoryCreateRequest { Name = "Tools" });

        var exception = Assert.Throws<CategoryNameExistsException>(() =>
            service.Create(new CategoryCreateRequest { Name = "Tools" }));

        Assert.Equal("Tools", exception.Name);
    }

    [Fact]
    public void Update_Partial_UpdatesOnlyProvidedFields()
    {
        var store = new AppStore();
        var service = new CategoryService(store);
        var created = service.Create(new CategoryCreateRequest { Name = "Tools", Description = "Old" });

        var updated = service.Update(created.Id, new CategoryUpdateRequest
        {
            HasDescription = true,
            Description = "New description"
        });

        Assert.Equal("Tools", updated.Name);
        Assert.Equal("New description", updated.Description);
    }

    [Fact]
    public void Update_ThrowsDuplicateName()
    {
        var store = new AppStore();
        var service = new CategoryService(store);
        service.Create(new CategoryCreateRequest { Name = "Tools" });
        var books = service.Create(new CategoryCreateRequest { Name = "Books" });

        var exception = Assert.Throws<CategoryNameExistsException>(() =>
            service.Update(books.Id, new CategoryUpdateRequest { HasName = true, Name = "Tools" }));

        Assert.Equal("Tools", exception.Name);
    }

    [Fact]
    public void Delete_RemovesCategory()
    {
        var store = new AppStore();
        var service = new CategoryService(store);
        var created = service.Create(new CategoryCreateRequest { Name = "Tools" });

        service.Delete(created.Id);

        Assert.Throws<CategoryNotFoundException>(() => service.GetById(created.Id));
    }

    [Fact]
    public void Delete_Throws_WhenInUse()
    {
        var store = new AppStore();
        var categoryService = new CategoryService(store);
        var itemService = new ItemService(store);
        var category = categoryService.Create(new CategoryCreateRequest { Name = "Tools" });
        itemService.Create(new ItemCreateRequest { Name = "Hammer", Price = 10.00m, CategoryId = category.Id });

        var exception = Assert.Throws<CategoryInUseException>(() => categoryService.Delete(category.Id));

        Assert.Equal(category.Id, exception.CategoryId);
    }

    [Fact]
    public void List_ReturnsPaginatedCategories()
    {
        var store = new AppStore();
        var service = new CategoryService(store);
        service.Create(new CategoryCreateRequest { Name = "Alpha" });
        service.Create(new CategoryCreateRequest { Name = "Beta" });
        service.Create(new CategoryCreateRequest { Name = "Gamma" });

        var page = service.List(skip: 1, limit: 1);

        Assert.Equal(3, page.Total);
        Assert.Single(page.Items);
        Assert.Equal("Beta", page.Items[0].Name);
    }
}


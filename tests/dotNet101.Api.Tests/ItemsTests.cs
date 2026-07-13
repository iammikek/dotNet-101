using System.Net;
using System.Net.Http.Json;
using dotNet101.Api.Tests.Infrastructure;
using dotNet101.Application.Models;
using Xunit;

namespace dotNet101.Api.Tests;

public sealed class ItemsTests : ApiTestBase
{
    public ItemsTests(ApiApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task ListItems_Empty_ReturnsPaginatedResult()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/items");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<ItemResponse>>();
        Assert.NotNull(payload);
        Assert.Empty(payload.Items);
        Assert.Equal(0, payload.Total);
        Assert.Equal(0, payload.Skip);
        Assert.Equal(10, payload.Limit);
    }

    [Fact]
    public async Task ListItems_WithPagination_ReturnsSlice()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        await CreateItemAsync(client, authHeader, "A", 1.0m);
        await CreateItemAsync(client, authHeader, "B", 2.0m);
        await CreateItemAsync(client, authHeader, "C", 3.0m);

        var response = await client.GetAsync("/items?skip=1&limit=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<ItemResponse>>();
        Assert.NotNull(payload);
        Assert.Equal(3, payload.Total);
        Assert.Equal(1, payload.Skip);
        Assert.Equal(2, payload.Limit);
        Assert.Equal(2, payload.Items.Count);
        Assert.Equal("B", payload.Items[0].Name);
        Assert.Equal("C", payload.Items[1].Name);
    }

    [Fact]
    public async Task ListItems_FilterByMinPrice_ReturnsMatches()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        await CreateItemAsync(client, authHeader, "Cheap", 5.0m);
        await CreateItemAsync(client, authHeader, "Mid", 10.0m);
        await CreateItemAsync(client, authHeader, "Premium", 25.0m);

        var response = await client.GetAsync("/items?min_price=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<ItemResponse>>();
        Assert.NotNull(payload);
        var names = payload.Items.Select(item => item.Name).ToHashSet();
        Assert.Equal(2, payload.Total);
        Assert.True(names.SetEquals(["Mid", "Premium"]));
    }

    [Fact]
    public async Task ListItems_FilterByMaxPrice_ReturnsMatches()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        await CreateItemAsync(client, authHeader, "Cheap", 5.0m);
        await CreateItemAsync(client, authHeader, "Mid", 10.0m);
        await CreateItemAsync(client, authHeader, "Premium", 25.0m);

        var response = await client.GetAsync("/items?max_price=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<ItemResponse>>();
        Assert.NotNull(payload);
        var names = payload.Items.Select(item => item.Name).ToHashSet();
        Assert.Equal(2, payload.Total);
        Assert.True(names.SetEquals(["Cheap", "Mid"]));
    }

    [Fact]
    public async Task ListItems_FilterByCategory_ReturnsMatches()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        var tools = await CreateCategoryAsync(client, authHeader, "Tools");
        var books = await CreateCategoryAsync(client, authHeader, "Books");
        await CreateItemAsync(client, authHeader, "Hammer", 10.0m, categoryId: tools.Id);
        await CreateItemAsync(client, authHeader, "Novel", 12.0m, categoryId: books.Id);
        await CreateItemAsync(client, authHeader, "Wrench", 15.0m, categoryId: tools.Id);

        var response = await client.GetAsync($"/items?category_id={tools.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<ItemResponse>>();
        Assert.NotNull(payload);
        var names = payload.Items.Select(item => item.Name).ToHashSet();
        Assert.Equal(2, payload.Total);
        Assert.True(names.SetEquals(["Hammer", "Wrench"]));
    }

    [Fact]
    public async Task ListItems_FilterByNameContains_ReturnsCaseInsensitiveMatches()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        await CreateItemAsync(client, authHeader, "Blue Widget", 10.0m);
        await CreateItemAsync(client, authHeader, "Red Gadget", 12.0m);
        await CreateItemAsync(client, authHeader, "green widget", 15.0m);

        var response = await client.GetAsync("/items?name_contains=widget");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<ItemResponse>>();
        Assert.NotNull(payload);
        var names = payload.Items.Select(item => item.Name).ToHashSet();
        Assert.Equal(2, payload.Total);
        Assert.True(names.SetEquals(["Blue Widget", "green widget"]));
    }

    [Fact]
    public async Task ListItems_CombinedFilters_ReturnExpectedSlice()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        var tools = await CreateCategoryAsync(client, authHeader, "Tools");
        var books = await CreateCategoryAsync(client, authHeader, "Books");
        await CreateItemAsync(client, authHeader, "Budget Tool", 8.0m, categoryId: tools.Id);
        await CreateItemAsync(client, authHeader, "Pro Tool", 20.0m, categoryId: tools.Id);
        await CreateItemAsync(client, authHeader, "Budget Book", 8.0m, categoryId: books.Id);

        var response = await client.GetAsync($"/items?category_id={tools.Id}&min_price=10&max_price=25");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<ItemResponse>>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload.Total);
        Assert.Single(payload.Items);
        Assert.Equal("Pro Tool", payload.Items[0].Name);
    }

    [Theory]
    [InlineData("limit=101")]
    [InlineData("skip=-1")]
    [InlineData("min_price=-1")]
    public async Task ListItems_InvalidQuery_ReturnsValidationError(string query)
    {
        using var client = CreateClient();

        var response = await client.GetAsync($"/items?{query}");

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task GetItem_ReturnsItem()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        var item = await CreateItemAsync(client, authHeader);

        var response = await client.GetAsync($"/items/{item.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ItemResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Widget", payload.Name);
    }

    [Fact]
    public async Task GetItem_NotFound_ReturnsParityError()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/items/99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Item not found", payload.Detail);
        Assert.Equal("ITEM_NOT_FOUND", payload.Code);
    }

    [Fact]
    public async Task GetItemsStats_Empty_ReturnsFastApiShape()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/items/stats/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ItemStatsResponse>();
        Assert.NotNull(payload);
        Assert.Equal(0, payload.TotalItems);
        Assert.Equal(0.0m, payload.AveragePrice);
        Assert.Null(payload.MinPrice);
        Assert.Null(payload.MaxPrice);
        Assert.Equal(0, payload.UncategorizedCount);
        Assert.Empty(payload.ByCategory);
    }

    [Fact]
    public async Task GetItemsStats_ByCategory_ReturnsBreakdown()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        var tools = await CreateCategoryAsync(client, authHeader, "Tools");
        var books = await CreateCategoryAsync(client, authHeader, "Books");
        await CreateItemAsync(client, authHeader, "Hammer", 10.0m, categoryId: tools.Id);
        await CreateItemAsync(client, authHeader, "Drill", 30.0m, categoryId: tools.Id);
        await CreateItemAsync(client, authHeader, "Novel", 15.0m, categoryId: books.Id);
        await CreateItemAsync(client, authHeader, "Loose", 5.0m);

        var response = await client.GetAsync("/items/stats/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ItemStatsResponse>();
        Assert.NotNull(payload);
        Assert.Equal(4, payload.TotalItems);
        Assert.Equal(1, payload.UncategorizedCount);
        Assert.Equal(2, payload.ByCategory.Count);
        Assert.Equal("Books", payload.ByCategory[0].CategoryName);
        Assert.Equal(15.0m, payload.ByCategory[0].AveragePrice);
        Assert.Equal("Tools", payload.ByCategory[1].CategoryName);
        Assert.Equal(20.0m, payload.ByCategory[1].AveragePrice);
    }

    [Fact]
    public async Task CreateItem_WithCategory_ReturnsNestedCategory()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        var category = await CreateCategoryAsync(client, authHeader, "Electronics");

        var item = await CreateItemAsync(client, authHeader, "Gadget", 15.0m, categoryId: category.Id);

        Assert.Equal(category.Id, item.CategoryId);
        Assert.NotNull(item.Category);
        Assert.Equal("Electronics", item.Category.Name);
    }

    [Fact]
    public async Task CreateItem_InvalidCategoryId_ReturnsNotFound()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        client.DefaultRequestHeaders.Authorization = authHeader;

        var response = await client.PostAsJsonAsync("/items", new
        {
            name = "Gadget",
            price = 15.0m,
            category_id = 999
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal("CATEGORY_NOT_FOUND", payload.Code);
    }

    [Fact]
    public async Task CreateItem_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/items", new
        {
            name = "Thing",
            price = 5.0m
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(payload);
        Assert.Equal("Not authenticated", payload["detail"]);
    }

    [Theory]
    [InlineData(null, 1.0)]
    [InlineData("", 1.0)]
    [InlineData("Bad", -1.0)]
    public async Task CreateItem_InvalidPayload_ReturnsValidationError(string? name, decimal price)
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        client.DefaultRequestHeaders.Authorization = authHeader;

        var payload = new Dictionary<string, object?>();
        if (name is not null)
        {
            payload["name"] = name;
        }

        payload["price"] = price;

        var response = await client.PostAsJsonAsync("/items", payload);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task UpdateItem_Category_ReturnsNestedCategory()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        var item = await CreateItemAsync(client, authHeader);
        var category = await CreateCategoryAsync(client, authHeader, "Tools");

        client.DefaultRequestHeaders.Authorization = authHeader;
        var response = await client.PatchAsJsonAsync($"/items/{item.Id}", new
        {
            category_id = category.Id
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ItemResponse>();
        Assert.NotNull(payload);
        Assert.Equal(category.Id, payload.CategoryId);
        Assert.NotNull(payload.Category);
        Assert.Equal("Tools", payload.Category.Name);
    }

    [Fact]
    public async Task UpdateItem_Partial_PreservesOtherFields()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        var item = await CreateItemAsync(client, authHeader, "Widget", 10.0m, "Original");

        client.DefaultRequestHeaders.Authorization = authHeader;
        var response = await client.PatchAsJsonAsync($"/items/{item.Id}", new
        {
            price = 5.99m
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ItemResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Widget", payload.Name);
        Assert.Equal("Original", payload.Description);
        Assert.Equal(5.99m, payload.Price);
    }

    [Fact]
    public async Task UpdateItem_Full_UpdatesAllFields()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        var item = await CreateItemAsync(client, authHeader, "Old", 1.0m);

        client.DefaultRequestHeaders.Authorization = authHeader;
        var response = await client.PatchAsJsonAsync($"/items/{item.Id}", new
        {
            name = "New",
            description = "Updated",
            price = 2.5m
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ItemResponse>();
        Assert.NotNull(payload);
        Assert.Equal(item.Id, payload.Id);
        Assert.Equal("New", payload.Name);
        Assert.Equal("Updated", payload.Description);
        Assert.Equal(2.5m, payload.Price);
    }

    [Fact]
    public async Task UpdateItem_NotFound_ReturnsParityError()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        client.DefaultRequestHeaders.Authorization = authHeader;

        var response = await client.PatchAsJsonAsync("/items/99", new
        {
            name = "Nope"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Item not found", payload.Detail);
    }

    [Fact]
    public async Task UpdateItem_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        var item = await CreateItemAsync(client, authHeader);
        client.DefaultRequestHeaders.Authorization = null;

        var response = await client.PatchAsJsonAsync($"/items/{item.Id}", new
        {
            name = "Nope"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(payload);
        Assert.Equal("Not authenticated", payload["detail"]);
    }

    [Fact]
    public async Task DeleteItem_WithJwt_DeletesItem()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        var item = await CreateItemAsync(client, authHeader);

        client.DefaultRequestHeaders.Authorization = authHeader;
        var response = await client.DeleteAsync($"/items/{item.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var getResponse = await client.GetAsync($"/items/{item.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteItem_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        var item = await CreateItemAsync(client, authHeader);
        client.DefaultRequestHeaders.Authorization = null;

        var response = await client.DeleteAsync($"/items/{item.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(payload);
        Assert.Equal("Not authenticated", payload["detail"]);
    }

    [Fact]
    public async Task DeleteItem_InvalidJwt_ReturnsUnauthorized()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        var item = await CreateItemAsync(client, authHeader);
        client.DefaultRequestHeaders.Authorization = new("Bearer", "invalid-token");

        var response = await client.DeleteAsync($"/items/{item.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(payload);
        Assert.Equal("Could not validate credentials", payload["detail"]);
    }

    [Fact]
    public async Task DeleteItem_NotFound_ReturnsParityError()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        client.DefaultRequestHeaders.Authorization = authHeader;

        var response = await client.DeleteAsync("/items/99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Item not found", payload.Detail);
    }
}

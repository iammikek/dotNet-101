using System.Net;
using System.Net.Http.Json;
using dotNet101.Api.Tests.Infrastructure;
using dotNet101.Application.Models;
using Xunit;

namespace dotNet101.Api.Tests;

public sealed class CategoriesTests : ApiTestBase
{
    public CategoriesTests(ApiApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task ListCategories_Empty_ReturnsPaginatedResult()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(payload);
        Assert.Equal("0", payload["total"]?.ToString());
        Assert.Equal("0", payload["skip"]?.ToString());
        Assert.Equal("10", payload["limit"]?.ToString());
    }

    [Fact]
    public async Task ListCategories_WithPagination_ReturnsSlice()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        await CreateCategoryAsync(client, authHeader, "A");
        await CreateCategoryAsync(client, authHeader, "B");
        await CreateCategoryAsync(client, authHeader, "C");

        var response = await client.GetAsync("/categories?skip=1&limit=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<CategoryResponse>>();
        Assert.NotNull(payload);
        Assert.Equal(3, payload.Total);
        Assert.Equal(1, payload.Skip);
        Assert.Equal(2, payload.Limit);
        Assert.Equal(2, payload.Items.Count);
        Assert.Equal("B", payload.Items[0].Name);
        Assert.Equal("C", payload.Items[1].Name);
    }

    [Fact]
    public async Task GetCategory_ReturnsCategory()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        var category = await CreateCategoryAsync(client, authHeader, "Books");

        var response = await client.GetAsync($"/categories/{category.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Books", payload.Name);
    }

    [Fact]
    public async Task GetCategory_NotFound_ReturnsParityError()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/categories/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal("CATEGORY_NOT_FOUND", payload.Code);
    }

    [Fact]
    public async Task CreateCategory_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/categories", new
        {
            name = "Tools"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_DuplicateName_ReturnsConflict()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        await CreateCategoryAsync(client, authHeader, "Tools");

        client.DefaultRequestHeaders.Authorization = authHeader;
        var response = await client.PostAsJsonAsync("/categories", new
        {
            name = "Tools",
            description = "duplicate"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal("CATEGORY_NAME_EXISTS", payload.Code);
    }

    [Fact]
    public async Task UpdateCategory_UpdatesFields()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        var category = await CreateCategoryAsync(client, authHeader, "Old Name");

        client.DefaultRequestHeaders.Authorization = authHeader;
        var response = await client.PatchAsJsonAsync($"/categories/{category.Id}", new
        {
            name = "New Name",
            description = "Updated"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.NotNull(payload);
        Assert.Equal("New Name", payload.Name);
        Assert.Equal("Updated", payload.Description);
    }

    [Fact]
    public async Task DeleteCategory_RemovesUnusedCategory()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        var category = await CreateCategoryAsync(client, authHeader, "Temporary");

        client.DefaultRequestHeaders.Authorization = authHeader;
        var response = await client.DeleteAsync($"/categories/{category.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var getResponse = await client.GetAsync($"/categories/{category.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_InUse_ReturnsConflict()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        var category = await CreateCategoryAsync(client, authHeader, "Tools");
        await CreateItemAsync(client, authHeader, "Hammer", 10.0m, categoryId: category.Id);

        client.DefaultRequestHeaders.Authorization = authHeader;
        var response = await client.DeleteAsync($"/categories/{category.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal("CATEGORY_IN_USE", payload.Code);
    }
}

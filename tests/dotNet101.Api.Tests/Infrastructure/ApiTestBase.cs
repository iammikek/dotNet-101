using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using dotNet101.Application.Models;
using Xunit;

namespace dotNet101.Api.Tests.Infrastructure;

public abstract class ApiTestBase : IClassFixture<ApiApplicationFactory>
{
    protected ApiTestBase(ApiApplicationFactory factory)
    {
        Factory = factory;
        Factory.ResetStore();
    }

    protected ApiApplicationFactory Factory { get; }

    protected HttpClient CreateClient() => Factory.CreateClient();

    protected async Task<UserResponse> RegisterUserAsync(HttpClient client, string email = "test@example.com", string password = "secret123")
    {
        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            email,
            password
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(payload);
        return payload;
    }

    protected async Task<AuthenticationHeaderValue> CreateAuthHeaderAsync(HttpClient client, string email = "test@example.com", string password = "secret123")
    {
        await RegisterUserAsync(client, email, password);

        var response = await client.PostAsync("/auth/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = email,
            ["password"] = password
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(token);
        return new AuthenticationHeaderValue("Bearer", token.AccessToken);
    }

    protected async Task<CategoryResponse> CreateCategoryAsync(HttpClient client, AuthenticationHeaderValue authHeader, string name = "Tools", string? description = null)
    {
        client.DefaultRequestHeaders.Authorization = authHeader;

        var response = await client.PostAsJsonAsync("/categories", new
        {
            name,
            description
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.NotNull(payload);
        return payload;
    }

    protected async Task<ItemResponse> CreateItemAsync(HttpClient client, AuthenticationHeaderValue authHeader, string name = "Widget", decimal price = 9.99m, string? description = null, int? categoryId = null)
    {
        client.DefaultRequestHeaders.Authorization = authHeader;

        var response = await client.PostAsJsonAsync("/items", new
        {
            name,
            price,
            description,
            category_id = categoryId
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ItemResponse>();
        Assert.NotNull(payload);
        return payload;
    }
}

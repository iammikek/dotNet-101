using System.Net;
using System.Net.Http.Json;
using dotNet101.Api.Tests.Infrastructure;
using dotNet101.Application.Models;
using Xunit;

namespace dotNet101.Api.Tests;

public sealed class AuthTests : ApiTestBase
{
    public AuthTests(ApiApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task RegisterUser_CreatesUser_AndHidesPassword()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            email = "alice@example.com",
            password = "password123"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(payload);
        Assert.Equal("alice@example.com", payload["email"]?.ToString());
        Assert.False(payload.ContainsKey("password"));
        Assert.False(payload.ContainsKey("hashedPassword"));
    }

    [Fact]
    public async Task RegisterDuplicateEmail_ReturnsConflict()
    {
        using var client = CreateClient();
        await RegisterUserAsync(client);

        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            email = "test@example.com",
            password = "secret123"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(payload);
        Assert.Equal("USER_EMAIL_EXISTS", payload["code"]?.ToString());
    }

    [Fact]
    public async Task LoginSuccess_ReturnsBearerToken()
    {
        using var client = CreateClient();
        await RegisterUserAsync(client);

        var response = await client.PostAsync("/auth/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = "test@example.com",
            ["password"] = "secret123"
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(payload);
        Assert.Equal("bearer", payload.TokenType);
        Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
    }

    [Fact]
    public async Task LoginInvalidPassword_ReturnsUnauthorized()
    {
        using var client = CreateClient();
        await RegisterUserAsync(client);

        var response = await client.PostAsync("/auth/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = "test@example.com",
            ["password"] = "wrong-password"
        }));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(payload);
        Assert.Equal("Incorrect email or password", payload["detail"]);
    }

    [Fact]
    public async Task ReadCurrentUser_ReturnsAuthenticatedUser()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        client.DefaultRequestHeaders.Authorization = authHeader;

        var response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(payload);
        Assert.Equal("test@example.com", payload.Email);
    }

    [Fact]
    public async Task ReadCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

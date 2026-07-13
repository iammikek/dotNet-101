using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using dotNet101.Api.Tests.Infrastructure;
using dotNet101.Application.Models;
using dotNet101.Infrastructure.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace dotNet101.Api.Tests;

public sealed class RateLimitTests : ApiTestBase
{
    public RateLimitTests(ApiApplicationFactory factory)
        : base(factory)
    {
        using var scope = Factory.Services.CreateScope();
        var limiter = scope.ServiceProvider.GetRequiredService<RequestRateLimiter>();
        limiter.Enabled = true;
        limiter.Reset();
    }

    [Fact]
    public async Task Login_RateLimitExceeded_Returns429()
    {
        using var client = CreateClient();
        await RegisterUserAsync(client);

        for (var i = 0; i < 10; i++)
        {
            var response = await client.PostAsync("/auth/login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = "test@example.com",
                ["password"] = "wrong-password"
            }));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        var exceeded = await client.PostAsync("/auth/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = "test@example.com",
            ["password"] = "wrong-password"
        }));

        Assert.Equal((HttpStatusCode)429, exceeded.StatusCode);
        var payload = await exceeded.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal("RATE_LIMIT_EXCEEDED", payload.Code);
    }

    [Fact]
    public async Task Register_RateLimitExceeded_Returns429()
    {
        using var client = CreateClient();

        for (var i = 0; i < 10; i++)
        {
            var response = await client.PostAsJsonAsync("/auth/register", new
            {
                email = $"user{i}@example.com",
                password = "password123"
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        var exceeded = await client.PostAsJsonAsync("/auth/register", new
        {
            email = "one-too-many@example.com",
            password = "password123"
        });

        Assert.Equal((HttpStatusCode)429, exceeded.StatusCode);
        var payload = await exceeded.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal("RATE_LIMIT_EXCEEDED", payload.Code);
    }

    [Fact]
    public async Task CreateItem_RateLimitExceeded_Returns429()
    {
        using var client = CreateClient();
        var authHeader = await CreateAuthHeaderAsync(client);
        client.DefaultRequestHeaders.Authorization = authHeader;

        for (var i = 0; i < 60; i++)
        {
            var response = await client.PostAsJsonAsync("/items", new
            {
                name = $"Widget {i}",
                price = 1.0m
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        var exceeded = await client.PostAsJsonAsync("/items", new
        {
            name = "One too many",
            price = 1.0m
        });

        Assert.Equal((HttpStatusCode)429, exceeded.StatusCode);
        var payload = await exceeded.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal("RATE_LIMIT_EXCEEDED", payload.Code);
    }
}


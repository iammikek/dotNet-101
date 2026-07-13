using System.Net;
using System.Net.Http.Json;
using dotNet101.Api.Tests.Infrastructure;
using Xunit;

namespace dotNet101.Api.Tests;

public sealed class AppTests : ApiTestBase
{
    public AppTests(ApiApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Root_ReturnsHelloMessage()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(payload);
        Assert.Equal("Hello from FastAPI!", payload["message"]);
    }

    [Fact]
    public async Task Health_ReturnsConnectedDatabase()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(payload);
        Assert.Equal("ok", payload["status"]);
        Assert.Equal("connected", payload["database"]);
    }
}

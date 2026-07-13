using dotNet101.Application.Abstractions;
using dotNet101.Infrastructure.RateLimiting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace dotNet101.Api.Tests.Infrastructure;

public sealed class ApiApplicationFactory : WebApplicationFactory<Program>
{
    public void ResetStore()
    {
        using var scope = Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IAppStore>();
        store.Reset();

        var limiter = scope.ServiceProvider.GetRequiredService<RequestRateLimiter>();
        limiter.Enabled = false;
        limiter.Reset();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}

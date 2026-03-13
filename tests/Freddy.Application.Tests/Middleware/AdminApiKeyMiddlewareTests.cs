using FluentAssertions;
using Freddy.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Freddy.Application.Tests.Middleware;

public sealed class AdminApiKeyMiddlewareTests
{
    private const string ValidApiKey = "test-admin-key";

    private static AdminApiKeyMiddleware CreateMiddleware(RequestDelegate next)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
(StringComparer.Ordinal)
            {
                ["Admin:ApiKey"] = ValidApiKey,
            })
            .Build();

        return new AdminApiKeyMiddleware(
            next,
            configuration,
            NullLogger<AdminApiKeyMiddleware>.Instance);
    }

    [Fact]
    public async Task InvokeAsync_AdminRoute_WithValidKey_CallsNext()
    {
        // Arrange
        bool nextCalled = false;
        AdminApiKeyMiddleware middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/admin/packages";
        context.Request.Headers["X-Admin-Api-Key"] = ValidApiKey;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_AdminRoute_WithoutKey_Returns401()
    {
        // Arrange
        bool nextCalled = false;
        AdminApiKeyMiddleware middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/admin/packages";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_AdminRoute_WithInvalidKey_Returns401()
    {
        // Arrange
        bool nextCalled = false;
        AdminApiKeyMiddleware middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/admin/packages";
        context.Request.Headers["X-Admin-Api-Key"] = "wrong-key";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_NonAdminRoute_WithoutKey_CallsNext()
    {
        // Arrange
        bool nextCalled = false;
        AdminApiKeyMiddleware middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/chat/conversations";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_AdminRoute_WithMissingConfig_Returns401()
    {
        // Arrange
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        bool nextCalled = false;
        var middleware = new AdminApiKeyMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            configuration,
            NullLogger<AdminApiKeyMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/admin/packages";
        context.Request.Headers["X-Admin-Api-Key"] = "some-key";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(401);
    }
}

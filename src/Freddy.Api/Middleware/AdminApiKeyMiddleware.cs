using Microsoft.AspNetCore.Mvc;

namespace Freddy.Api.Middleware;

/// <summary>
/// Middleware that validates the X-Admin-Api-Key header for admin API routes (/api/admin/).
/// </summary>
public sealed class AdminApiKeyMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    ILogger<AdminApiKeyMiddleware> logger)
{
    private const string AdminApiKeyHeaderName = "X-Admin-Api-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/admin", StringComparison.OrdinalIgnoreCase))
        {
            string? expectedKey = configuration["Admin:ApiKey"];

            if (string.IsNullOrEmpty(expectedKey))
            {
                logger.LogError("Admin API key is not configured. Rejecting request to {Path}", context.Request.Path);
                await WriteUnauthorizedResponseAsync(context, "Admin API key is not configured.").ConfigureAwait(false);
                return;
            }

            if (!context.Request.Headers.TryGetValue(AdminApiKeyHeaderName, out Microsoft.Extensions.Primitives.StringValues apiKeyHeader)
                || !string.Equals(apiKeyHeader.ToString(), expectedKey, StringComparison.Ordinal))
            {
                logger.LogWarning("Invalid or missing admin API key for request to {Path}", context.Request.Path);
                await WriteUnauthorizedResponseAsync(context, "Invalid or missing admin API key.").ConfigureAwait(false);
                return;
            }
        }

        await next(context).ConfigureAwait(false);
    }

    private static async Task WriteUnauthorizedResponseAsync(HttpContext context, string detail)
    {
        ProblemDetails problemDetails = new()
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = "Unauthorized",
            Status = StatusCodes.Status401Unauthorized,
            Detail = detail,
        };

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails).ConfigureAwait(false);
    }
}

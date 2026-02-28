using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Freddy.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Validation failed");
            await WriteValidationErrorResponseAsync(context, ex).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Request was cancelled");
            context.Response.StatusCode = 499; // Client Closed Request
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred");
            await WriteInternalErrorResponseAsync(context).ConfigureAwait(false);
        }
    }

    private static async Task WriteValidationErrorResponseAsync(HttpContext context, ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray(),
                StringComparer.Ordinal);

        ValidationProblemDetails problemDetails = new(errors)
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = "Validation Error",
            Status = StatusCodes.Status400BadRequest,
            Detail = "One or more validation errors occurred.",
        };

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails).ConfigureAwait(false);
    }

    private static async Task WriteInternalErrorResponseAsync(HttpContext context)
    {
        ProblemDetails problemDetails = new()
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = "Internal Server Error",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "An unexpected error occurred. Please try again later.",
        };

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails).ConfigureAwait(false);
    }
}

using Freddy.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Freddy.Api.Extensions;

public static class ResultExtensions
{
    public static ActionResult ToActionResult<T>(this Result<T> result)
    {
        return result.Type switch
        {
            ResultType.Success => new OkObjectResult(result.Value),
            ResultType.NotFound => new NotFoundObjectResult(CreateProblemDetails(
                StatusCodes.Status404NotFound, "Not Found", result.Error)),
            ResultType.ValidationError => new BadRequestObjectResult(CreateProblemDetails(
                StatusCodes.Status400BadRequest, "Validation Error", result.Error)),
            ResultType.Error => new ObjectResult(CreateProblemDetails(
                StatusCodes.Status500InternalServerError, "Internal Server Error", result.Error))
            { StatusCode = StatusCodes.Status500InternalServerError },
            _ => new ObjectResult(CreateProblemDetails(
                StatusCodes.Status500InternalServerError, "Internal Server Error", result.Error))
            { StatusCode = StatusCodes.Status500InternalServerError },
        };
    }

    private static ProblemDetails CreateProblemDetails(int status, string title, string? detail) => new()
    {
        Type = "https://tools.ietf.org/html/rfc7807",
        Status = status,
        Title = title,
        Detail = detail,
    };
}

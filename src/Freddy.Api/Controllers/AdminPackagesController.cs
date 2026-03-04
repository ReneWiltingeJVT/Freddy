using Freddy.Api.Extensions;
using Freddy.Api.Models;
using Freddy.Application.Common;
using Freddy.Application.Features.Admin.Packages.Commands;
using Freddy.Application.Features.Admin.Packages.DTOs;
using Freddy.Application.Features.Admin.Packages.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freddy.Api.Controllers;

/// <summary>
/// Admin API for managing packages.
/// </summary>
[ApiController]
[Route("api/admin/packages")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class AdminPackagesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Lists all packages with optional filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PackageSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PackageSummaryDto>>> ListAsync(
        [FromQuery] bool? isPublished,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<PackageSummaryDto>> result = await mediator.Send(
            new ListPackagesQuery(isPublished, search), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Gets a package by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PackageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PackageDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        Result<PackageDto> result = await mediator.Send(
            new GetPackageQuery(id), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Creates a new package.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PackageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PackageDto>> CreateAsync(
        [FromBody] CreatePackageRequest request,
        CancellationToken cancellationToken)
    {
        Result<PackageDto> result = await mediator.Send(
            new CreatePackageCommand(
                request.Title,
                request.Description,
                request.Content,
                request.Tags ?? [],
                request.Synonyms ?? [],
                request.RequiresConfirmation),
            cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction("GetById", new { id = result.Value!.Id }, result.Value)
            : result.ToActionResult();
    }

    /// <summary>
    /// Updates an existing package.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PackageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PackageDto>> UpdateAsync(
        Guid id,
        [FromBody] UpdatePackageRequest request,
        CancellationToken cancellationToken)
    {
        Result<PackageDto> result = await mediator.Send(
            new UpdatePackageCommand(
                id,
                request.Title,
                request.Description,
                request.Content,
                request.Tags ?? [],
                request.Synonyms ?? [],
                request.RequiresConfirmation),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Deletes a package.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await mediator.Send(
            new DeletePackageCommand(id), cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToActionResult();
    }

    /// <summary>
    /// Publishes a package, making it visible to chat users.
    /// </summary>
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(PackageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PackageDto>> PublishAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        Result<PackageDto> result = await mediator.Send(
            new PublishPackageCommand(id), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Unpublishes a package, hiding it from chat users.
    /// </summary>
    [HttpPost("{id:guid}/unpublish")]
    [ProducesResponseType(typeof(PackageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PackageDto>> UnpublishAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        Result<PackageDto> result = await mediator.Send(
            new UnpublishPackageCommand(id), cancellationToken);
        return result.ToActionResult();
    }
}

using Freddy.Api.Extensions;
using Freddy.Api.Models;
using Freddy.Application.Common;
using Freddy.Application.Features.Admin.Clients.Commands;
using Freddy.Application.Features.Admin.Clients.DTOs;
using Freddy.Application.Features.Admin.Clients.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freddy.Api.Controllers;

/// <summary>
/// Admin API for managing clients (care recipients).
/// </summary>
[ApiController]
[Route("api/admin/clients")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class AdminClientsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Lists all clients with optional filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ClientDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClientDto>>> ListAsync(
        [FromQuery] bool? isActive,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<ClientDto>> result = await mediator.Send(
            new ListClientsQuery(isActive, search), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Gets a client by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        Result<ClientDto> result = await mediator.Send(
            new GetClientQuery(id), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Creates a new client.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClientDto>> CreateAsync(
        [FromBody] CreateClientRequest request,
        CancellationToken cancellationToken)
    {
        Result<ClientDto> result = await mediator.Send(
            new CreateClientCommand(
                request.DisplayName,
                request.Aliases ?? []),
            cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction("GetById", new { id = result.Value!.Id }, result.Value)
            : result.ToActionResult();
    }

    /// <summary>
    /// Updates an existing client.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientDto>> UpdateAsync(
        Guid id,
        [FromBody] UpdateClientRequest request,
        CancellationToken cancellationToken)
    {
        Result<ClientDto> result = await mediator.Send(
            new UpdateClientCommand(
                id,
                request.DisplayName,
                request.Aliases ?? [],
                request.IsActive),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Deletes a client.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        Result<bool> result = await mediator.Send(
            new DeleteClientCommand(id), cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToActionResult();
    }
}

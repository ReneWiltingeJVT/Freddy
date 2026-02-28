using Freddy.Api.Extensions;
using Freddy.Api.Models;
using Freddy.Application.Common;
using Freddy.Application.Features.Chat.Commands;
using Freddy.Application.Features.Chat.DTOs;
using Freddy.Application.Features.Chat.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freddy.Api.Controllers;

/// <summary>
/// Manages chat conversations and messages.
/// </summary>
[ApiController]
[Route("api/v1/chat")]
[Authorize]
[Produces("application/json")]
public sealed class ChatController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Creates a new conversation.
    /// </summary>
    [HttpPost("conversations")]
    [ProducesResponseType(typeof(ConversationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConversationDto>> CreateConversationAsync(
        [FromBody] CreateConversationRequest request,
        CancellationToken cancellationToken)
    {
        Result<ConversationDto> result = await mediator.Send(
            new CreateConversationCommand(request.Title), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(
                "GetConversationMessages",
                new { conversationId = result.Value!.Id },
                result.Value)
            : result.ToActionResult();
    }

    /// <summary>
    /// Gets all conversations for the current user.
    /// </summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(IReadOnlyList<ConversationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ConversationDto>>> GetConversationsAsync(
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<ConversationDto>> result = await mediator.Send(
            new GetConversationsQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Gets all messages for a specific conversation.
    /// </summary>
    [HttpGet("conversations/{conversationId:guid}/messages")]
    [ProducesResponseType(typeof(IReadOnlyList<MessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<MessageDto>>> GetConversationMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<MessageDto>> result = await mediator.Send(
            new GetConversationMessagesQuery(conversationId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Sends a user message and receives an AI response.
    /// </summary>
    [HttpPost("conversations/{conversationId:guid}/messages")]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageDto>> SendMessageAsync(
        Guid conversationId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        Result<MessageDto> result = await mediator.Send(
            new SendMessageCommand(conversationId, request.Content), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(
                "GetConversationMessages",
                new { conversationId },
                result.Value)
            : result.ToActionResult();
    }
}

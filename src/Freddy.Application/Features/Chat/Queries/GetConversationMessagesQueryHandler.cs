using System.Text.Json;
using Freddy.Application.Common;
using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Freddy.Application.Features.Chat.DTOs;
using MediatR;

namespace Freddy.Application.Features.Chat.Queries;

public sealed class GetConversationMessagesQueryHandler(
    IConversationRepository repository) : IRequestHandler<GetConversationMessagesQuery, Result<IReadOnlyList<MessageDto>>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<Result<IReadOnlyList<MessageDto>>> Handle(
        GetConversationMessagesQuery request,
        CancellationToken cancellationToken)
    {
        Conversation? conversation = await repository.GetByIdAsync(request.ConversationId, cancellationToken).ConfigureAwait(false);
        if (conversation is null)
        {
            return Result<IReadOnlyList<MessageDto>>.NotFound($"Conversation {request.ConversationId} not found.");
        }

        IReadOnlyList<Message> messages = await repository.GetMessagesAsync(request.ConversationId, cancellationToken).ConfigureAwait(false);

        var dtos = messages
            .Select(m => new MessageDto(
                m.Id,
                MapRole(m.Role),
                m.Content,
                m.CreatedAt,
                DeserializeAttachments(m.AttachmentsJson)))
            .ToList();

        return Result<IReadOnlyList<MessageDto>>.Success(dtos);
    }

    private static IReadOnlyList<AttachmentDto>? DeserializeAttachments(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<List<AttachmentDto>>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static string MapRole(MessageRole role) => role switch
    {
        MessageRole.User => "user",
        MessageRole.Assistant => "assistant",
        _ => "unknown",
    };
}

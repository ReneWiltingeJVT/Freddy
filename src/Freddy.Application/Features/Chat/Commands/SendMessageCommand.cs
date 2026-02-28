using Freddy.Application.Common;
using Freddy.Application.Features.Chat.DTOs;
using MediatR;

namespace Freddy.Application.Features.Chat.Commands;

public sealed record SendMessageCommand(Guid ConversationId, string Content) : IRequest<Result<MessageDto>>;

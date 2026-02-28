using Freddy.Application.Common;
using MediatR;

namespace Freddy.Application.Features.Chat.Commands;

public sealed record DeleteConversationCommand(Guid ConversationId) : IRequest<Result<bool>>;

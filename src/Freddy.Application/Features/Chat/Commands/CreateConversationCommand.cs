using Freddy.Application.Common;
using Freddy.Application.Features.Chat.DTOs;
using MediatR;

namespace Freddy.Application.Features.Chat.Commands;

public sealed record CreateConversationCommand(string? Title) : IRequest<Result<ConversationDto>>;

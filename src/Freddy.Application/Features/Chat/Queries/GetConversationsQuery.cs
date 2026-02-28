using Freddy.Application.Common;
using Freddy.Application.Features.Chat.DTOs;
using MediatR;

namespace Freddy.Application.Features.Chat.Queries;

public sealed record GetConversationsQuery : IRequest<Result<IReadOnlyList<ConversationDto>>>;

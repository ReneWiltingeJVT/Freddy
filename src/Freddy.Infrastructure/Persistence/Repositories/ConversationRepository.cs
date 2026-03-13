#pragma warning disable IDE0058 // Expression value is never used — repository operations with discarded returns

using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Microsoft.EntityFrameworkCore;

namespace Freddy.Infrastructure.Persistence.Repositories;

public sealed class ConversationRepository(FreddyDbContext dbContext) : IConversationRepository
{
    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Conversations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Conversation>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Conversations
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Conversation> CreateAsync(Conversation conversation, CancellationToken cancellationToken)
    {
        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return conversation;
    }

    public async Task<Message> AddMessageAsync(Message message, CancellationToken cancellationToken)
    {
        dbContext.Messages.Add(message);

        await dbContext.Conversations
            .Where(c => c.Id == message.ConversationId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken)
            .ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return message;
    }

    public async Task<IReadOnlyList<Message>> GetMessagesAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        return await dbContext.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        await dbContext.Messages
            .Where(m => m.ConversationId == conversationId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        await dbContext.Conversations
            .Where(c => c.Id == conversationId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SetPendingStateAsync(
        Guid conversationId,
        Guid? packageId,
        ConversationPendingState state,
        CancellationToken cancellationToken)
    {
        await dbContext.Conversations
            .Where(c => c.Id == conversationId)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(c => c.PendingPackageId, packageId)
                    .SetProperty(c => c.PendingState, state),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SetPendingClientIdAsync(
        Guid conversationId,
        Guid? clientId,
        CancellationToken cancellationToken)
    {
        await dbContext.Conversations
            .Where(c => c.Id == conversationId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(c => c.PendingClientId, clientId),
                cancellationToken)
            .ConfigureAwait(false);
    }
}

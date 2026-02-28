#pragma warning disable IDE0058 // Expression value is never used — EF fluent configuration chains

using Freddy.Application.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Freddy.Infrastructure.Persistence.Configurations;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id");

        builder.Property(m => m.ConversationId)
            .HasColumnName("conversation_id")
            .IsRequired();

        builder.Property(m => m.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.Content)
            .HasColumnName("content")
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(m => m.ConversationId)
            .HasDatabaseName("ix_messages_conversation_id");

        builder.HasIndex(m => new { m.ConversationId, m.CreatedAt })
            .HasDatabaseName("ix_messages_conversation_id_created_at");
    }
}

#pragma warning disable IDE0058 // Expression value is never used — EF fluent configuration chains

using Freddy.Application.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Freddy.Infrastructure.Persistence.Configurations;

public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id");

        builder.Property(c => c.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Aliases)
            .HasColumnName("aliases")
            .HasColumnType("text[]");

        builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(value: true);

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(c => c.Aliases)
            .HasMethod("gin")
            .HasDatabaseName("ix_clients_aliases");

        builder.HasIndex(c => c.IsActive)
            .HasDatabaseName("ix_clients_is_active");

        builder.HasIndex(c => c.DisplayName)
            .HasDatabaseName("ix_clients_display_name");
    }
}

#pragma warning disable IDE0058 // Expression value is never used — EF fluent configuration chains

using Freddy.Application.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Freddy.Infrastructure.Persistence.Configurations;

public sealed class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    public void Configure(EntityTypeBuilder<Package> builder)
    {
        builder.ToTable("packages");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(p => p.Content)
            .HasColumnName("content")
            .IsRequired();

        builder.Property(p => p.Keywords)
            .HasColumnName("keywords")
            .HasColumnType("text[]");

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(value: true);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(p => p.IsActive)
            .HasDatabaseName("ix_packages_is_active");

        builder.HasIndex(p => p.Keywords)
            .HasMethod("gin")
            .HasDatabaseName("ix_packages_keywords");
    }
}

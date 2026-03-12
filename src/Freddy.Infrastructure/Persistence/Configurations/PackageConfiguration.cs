#pragma warning disable IDE0058 // Expression value is never used — EF fluent configuration chains

using Freddy.Application.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Freddy.Infrastructure.Persistence.Configurations;

public sealed class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    public void Configure(EntityTypeBuilder<Package> builder)
    {
        builder.ToTable("packages", t =>
        {
            // PersonalPlan must have client_id; Protocol/WorkInstruction must NOT
            t.HasCheckConstraint(
                "ck_packages_category_client",
                "(category != 2 AND client_id IS NULL) OR (category = 2 AND client_id IS NOT NULL)");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(p => p.Content)
            .HasColumnName("content")
            .IsRequired();

        builder.Property(p => p.Tags)
            .HasColumnName("tags")
            .HasColumnType("text[]");

        builder.Property(p => p.Synonyms)
            .HasColumnName("synonyms")
            .HasColumnType("text[]");

        builder.Property(p => p.Category)
            .HasColumnName("category")
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(PackageCategory.Protocol);

        builder.Property(p => p.ClientId)
            .HasColumnName("client_id")
            .IsRequired(false);

        builder.Property(p => p.IsPublished)
            .HasColumnName("is_published")
            .HasDefaultValue(value: false);

        builder.Property(p => p.RequiresConfirmation)
            .HasColumnName("requires_confirmation")
            .HasDefaultValue(value: false);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(p => p.IsPublished)
            .HasDatabaseName("ix_packages_is_published");

        builder.HasIndex(p => p.Tags)
            .HasMethod("gin")
            .HasDatabaseName("ix_packages_tags");

        builder.HasIndex(p => p.Synonyms)
            .HasMethod("gin")
            .HasDatabaseName("ix_packages_synonyms");

        builder.HasIndex(p => p.Category)
            .HasDatabaseName("ix_packages_category");

        builder.HasIndex(p => p.ClientId)
            .HasDatabaseName("ix_packages_client_id");

        builder.HasMany(p => p.Documents)
            .WithOne(d => d.Package)
            .HasForeignKey(d => d.PackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Client)
            .WithMany(c => c.Packages)
            .HasForeignKey(p => p.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

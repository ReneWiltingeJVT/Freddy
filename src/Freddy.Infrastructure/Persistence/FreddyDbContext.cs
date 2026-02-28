using Freddy.Application.Entities;
using Microsoft.EntityFrameworkCore;

namespace Freddy.Infrastructure.Persistence;

public sealed class FreddyDbContext(DbContextOptions<FreddyDbContext> options) : DbContext(options)
{
    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<Package> Packages => Set<Package>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FreddyDbContext).Assembly);
}

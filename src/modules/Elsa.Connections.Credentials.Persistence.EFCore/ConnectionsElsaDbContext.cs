using Elsa.Connections.Models;
using Elsa.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Connections.Credentials.Persistence.EFCore;

public sealed class ConnectionsElsaDbContext(DbContextOptions<ConnectionsElsaDbContext> options, IServiceProvider serviceProvider)
    : ElsaDbContextBase(options, serviceProvider)
{
    public DbSet<IntegrationConnection> Connections { get; set; } = null!;
    public DbSet<ConnectionGenerationCleanup> GenerationCleanups { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new IntegrationConnectionConfiguration());
        modelBuilder.ApplyConfiguration(new ConnectionGenerationCleanupConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}

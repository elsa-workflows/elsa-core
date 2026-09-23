using Elsa.Connections.Models;
using Elsa.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Connections.Persistence.EFCore;

public sealed class ConnectionsElsaDbContext(DbContextOptions<ConnectionsElsaDbContext> options, IServiceProvider serviceProvider)
    : ElsaDbContextBase(options, serviceProvider)
{
    public DbSet<IntegrationConnection> Connections { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new IntegrationConnectionConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}

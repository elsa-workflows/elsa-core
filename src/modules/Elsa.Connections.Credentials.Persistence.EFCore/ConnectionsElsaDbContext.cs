using Elsa.Connections.Models;
using Elsa.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Connections.Credentials.Persistence.EFCore;

public sealed class ConnectionsElsaDbContext(DbContextOptions<ConnectionsElsaDbContext> options, IServiceProvider serviceProvider)
    : ElsaDbContextBase(options, serviceProvider)
{
    public DbSet<IntegrationConnection> Connections { get; set; } = null!;
    public DbSet<ConnectionGenerationCleanup> GenerationCleanups { get; set; } = null!;
    public DbSet<ConnectionCredentialBinding> CredentialBindings { get; set; } = null!;
    public DbSet<ConnectionCredentialUseGrant> CredentialUseGrants { get; set; } = null!;
    public DbSet<ConnectionOffboardingOperation> OffboardingOperations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new IntegrationConnectionConfiguration());
        modelBuilder.ApplyConfiguration(new ConnectionGenerationCleanupConfiguration());
        modelBuilder.ApplyConfiguration(new ConnectionCredentialBindingConfiguration());
        modelBuilder.ApplyConfiguration(new ConnectionCredentialUseGrantConfiguration());
        modelBuilder.ApplyConfiguration(new ConnectionOffboardingOperationConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}

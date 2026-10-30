using Elsa.Common.Multitenancy;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;

namespace Elsa.Persistence.EFCore.Modules.Tenants;

/// <summary>
/// DB context for the runtime module.
/// </summary>
[UsedImplicitly]
public class TenantsElsaDbContext : ElsaDbContextBase
{
    /// <inheritdoc />
    public TenantsElsaDbContext(DbContextOptions<TenantsElsaDbContext> options, IServiceProvider serviceProvider) : base(options, serviceProvider)
    {
    }

    /// <summary>
    /// The alteration plans.
    /// </summary>
    public DbSet<Tenant> Tenants { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var configuration = new Configurations();
        modelBuilder.ApplyConfiguration(configuration);
        base.OnModelCreating(modelBuilder);
    }
}
using Elsa.EntityFrameworkCore.Common;
using Elsa.Secrets.Management;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Secrets.Persistence.EntityFrameworkCore;

/// DB context for the secrets module.
[UsedImplicitly]
public class SecretsDbContext : ElsaDbContextBase
{
    /// <inheritdoc />
    public SecretsDbContext(DbContextOptions<SecretsDbContext> options, IServiceProvider serviceProvider) : base(options, serviceProvider)
    {
    }
    
    /// The API Keys DB set.
    public DbSet<Secret> Secrets { get; set; } = default!;
    
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var configuration = new Configurations();
        modelBuilder.ApplyConfiguration(configuration);
        base.OnModelCreating(modelBuilder);
    }
}
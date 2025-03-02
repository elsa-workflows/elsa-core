using Elsa.EntityFrameworkCore;
using Elsa.Secrets.Management;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Secrets.Persistence.EntityFrameworkCore;

/// <summary>
/// DB context for the secrets module.
/// </summary>
[UsedImplicitly]
public class SecretsDbContext : ElsaDbContextBase
{
    /// <inheritdoc />
    public SecretsDbContext(DbContextOptions<SecretsDbContext> options, IServiceProvider serviceProvider) : base(options, serviceProvider)
    {
    }
    
    /// <summary>
    /// The API Keys DB set.
    /// </summary>
    public DbSet<Secret> Secrets { get; set; } = default!;
    
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var configuration = new Configurations();
        modelBuilder.ApplyConfiguration(configuration);
        base.OnModelCreating(modelBuilder);
    }
}
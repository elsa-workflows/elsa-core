using Elsa.Persistence.EFCore;
using Elsa.Secrets.Models;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Secrets.Persistence.EFCore;

/// <summary>
/// The database context for the Secrets module.
/// </summary>
public class SecretsElsaDbContext : ElsaDbContextBase
{
    /// <inheritdoc />
    public SecretsElsaDbContext(DbContextOptions<SecretsElsaDbContext> options, IServiceProvider serviceProvider) : base(options, serviceProvider)
    {
    }

    /// <summary>
    /// The secrets.
    /// </summary>
    public DbSet<Secret> Secrets { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<SecretPayload>();
        modelBuilder.Ignore<SecretVersion>();
        modelBuilder.ApplyConfiguration(new SecretConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}

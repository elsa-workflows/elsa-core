using Elsa.Connections.Models;
using Elsa.Persistence.EFCore;
using Elsa.Connections.Persistence.Entities;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Connections.Persistence.EFCore;


/// <summary>
/// Db Context for the Connection Module
/// </summary>
[UsedImplicitly]
public class ConnectionDbContext : ElsaDbContextBase
{
    /// <inheritdoc />
    public ConnectionDbContext(DbContextOptions<ConnectionDbContext> options, IServiceProvider serviceProvider) 
        : base(options, serviceProvider)
    {
    }

    /// <summary>
    /// The Connection DB set.
    /// </summary>
    public DbSet<ConnectionDefinition> ConnectionDefinitions { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var configuration = new Configurations();
        modelBuilder.ApplyConfiguration<ConnectionDefinition>(configuration);
        base.OnModelCreating(modelBuilder);
    }
}

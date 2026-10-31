using Elsa.Persistence.EFCore;
using Elsa.Agents.Persistence.Entities;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Agents.Persistence.EFCore;

/// <summary>
/// DB context for the Agents module.
/// </summary>
[UsedImplicitly]
public class AgentsDbContext : ElsaDbContextBase
{
    /// <inheritdoc />
    public AgentsDbContext(DbContextOptions<AgentsDbContext> options, IServiceProvider serviceProvider) : base(options, serviceProvider)
    {
    }
    
    /// <summary>
    /// The Services DB set.
    /// </summary>
    [UsedImplicitly] public DbSet<AgentDefinition> AgentDefinitions { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var configuration = new Configurations();
        modelBuilder.ApplyConfiguration(configuration);
        base.OnModelCreating(modelBuilder);
    }
}
using Elsa.Agents.Persistence.Entities;
using Elsa.EntityFrameworkCore;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Agents.Persistence.EntityFrameworkCore;

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
    /// The API Keys DB set.
    /// </summary>
    public DbSet<ApiKeyDefinition> ApiKeysDefinitions { get; set; } = default!;
    
    /// <summary>
    /// The Services DB set.
    /// </summary>
    public DbSet<ServiceDefinition> ServicesDefinitions { get; set; } = default!;
    
    /// <summary>
    /// The Services DB set.
    /// </summary>
    public DbSet<AgentDefinition> AgentDefinitions { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var configuration = new Configurations();
        modelBuilder.ApplyConfiguration<ApiKeyDefinition>(configuration);
        modelBuilder.ApplyConfiguration<ServiceDefinition>(configuration);
        modelBuilder.ApplyConfiguration<AgentDefinition>(configuration);
        base.OnModelCreating(modelBuilder);
    }
}
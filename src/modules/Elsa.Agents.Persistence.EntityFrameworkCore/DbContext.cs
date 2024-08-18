using Elsa.Agents.Persistence.Entities;
using Elsa.EntityFrameworkCore.Common;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Agents.Persistence.EntityFrameworkCore;

/// DB context for the Agents module.
[UsedImplicitly]
public class AgentsElsaDbContext : ElsaDbContextBase
{
    /// <inheritdoc />
    public AgentsElsaDbContext(DbContextOptions<AgentsElsaDbContext> options, IServiceProvider serviceProvider) : base(options, serviceProvider)
    {
    }
    
    /// The API Keys DB set.
    public DbSet<ApiKeyDefinition> ApiKeysDefinitions { get; set; } = default!;
    
    /// The Services DB set.
    public DbSet<ServiceDefinition> ServicesDefinitions { get; set; } = default!;
    
    /// The Services DB set.
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
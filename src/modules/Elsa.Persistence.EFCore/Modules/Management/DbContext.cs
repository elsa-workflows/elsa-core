using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Models;
using Elsa.Workflows.State;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Modules.Management;

/// <summary>
/// The database context for the Management module.
/// </summary>
public class ManagementElsaDbContext : ElsaDbContextBase
{
    /// <inheritdoc />
    public ManagementElsaDbContext(DbContextOptions<ManagementElsaDbContext> options, IServiceProvider serviceProvider) : base(options, serviceProvider)
    {
    }

    /// <summary>
    /// The workflow definitions.
    /// </summary>
    public DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; } = null!;

    /// <summary>
    /// The workflow instances.
    /// </summary>
    public DbSet<WorkflowInstance> WorkflowInstances { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<WorkflowState>();
        modelBuilder.Ignore<ActivityIncident>();
        
        var config = new Configurations();
        modelBuilder.ApplyConfiguration<WorkflowDefinition>(config);
        modelBuilder.ApplyConfiguration<WorkflowInstance>(config);
        
        base.OnModelCreating(modelBuilder);
    }
}

using Elsa.EntityFrameworkCore.Common;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Entities;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore.Modules.Management;

/// <summary>
/// The database context for the Management module.
/// </summary>
public class ManagementElsaDbContext : ElsaDbContextBase
{
    /// <inheritdoc />
    public ManagementElsaDbContext(DbContextOptions options) : base(options)
    {
    }

    /// <summary>
    /// The workflow definitions.
    /// </summary>
    public DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; } = default!;
    
    /// <summary>
    /// The workflow instances.
    /// </summary>
    public DbSet<WorkflowInstance> WorkflowInstances { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Ignore<WorkflowState>();
    }

    /// <inheritdoc />
    protected override void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        var config = new Configurations();
        modelBuilder.ApplyConfiguration<WorkflowDefinition>(config);
        modelBuilder.ApplyConfiguration<WorkflowInstance>(config);
    }

    /// <inheritdoc />
    protected override void SetupForOracle(ModelBuilder modelBuilder)
    {
        // In order to use data more than 2000 char we have to use NCLOB.
        // In oracle we have to explicitly say the column is NCLOB otherwise it would be considered nvarchar(2000).
        modelBuilder.Entity<WorkflowInstance>().Property("Data").HasColumnType("NCLOB");
        modelBuilder.Entity<WorkflowDefinition>().Property("StringData").HasColumnType("NCLOB");
        modelBuilder.Entity<WorkflowDefinition>().Property("Data").HasColumnType("NCLOB");
    }
}

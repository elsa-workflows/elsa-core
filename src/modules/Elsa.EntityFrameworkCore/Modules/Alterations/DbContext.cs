using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Models;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Workflows.Management.Models;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore.Modules.Alterations;

/// <summary>
/// DB context for the runtime module.
/// </summary>
public class AlterationsElsaDbContext : ElsaDbContextBase
{
    /// <inheritdoc />
    public AlterationsElsaDbContext(DbContextOptions options) : base(options)
    {
    }
    
    /// <summary>
    /// The alteration plans.
    /// </summary>
    public DbSet<AlterationPlan> AlterationPlans { get; set; } = default!;
    
    /// <summary>
    /// The alteration jobs.
    /// </summary>
    public DbSet<AlterationJob> AlterationJobs { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<AlterationLogEntry>();
        modelBuilder.Ignore<AlterationWorkflowInstanceFilter>();
        base.OnModelCreating(modelBuilder);
    }

    /// <inheritdoc />
    protected override void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        var config = new Configurations();
        modelBuilder.Ignore<TimestampFilter>();
        modelBuilder.ApplyConfiguration<AlterationPlan>(config);
        modelBuilder.ApplyConfiguration<AlterationJob>(config);
    }
    
    /// <inheritdoc />
    protected override void SetupForOracle(ModelBuilder modelBuilder)
    {
        // In order to use data more than 2000 char we have to use NCLOB.
        // In Oracle we have to explicitly say the column is NCLOB otherwise it would be considered nvarchar(2000).
        modelBuilder.Entity<AlterationPlan>().Property("SerializedAlterations").HasColumnType("NCLOB");
        modelBuilder.Entity<AlterationPlan>().Property("SerializedWorkflowInstanceFilter").HasColumnType("NCLOB");
        modelBuilder.Entity<AlterationJob>().Property("SerializedLog").HasColumnType("NCLOB");
    }
}
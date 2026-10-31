using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Models;
using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Modules.Alterations;

/// <summary>
/// DB context for the runtime module.
/// </summary>
[UsedImplicitly]
public class AlterationsElsaDbContext : ElsaDbContextBase
{
    /// <inheritdoc />
    public AlterationsElsaDbContext(DbContextOptions<AlterationsElsaDbContext> options, IServiceProvider serviceProvider) : base(options, serviceProvider)
    {
    }

    /// <summary>
    /// The alteration plans.
    /// </summary>
    public DbSet<AlterationPlan> AlterationPlans { get; set; } = null!;

    /// <summary>
    /// The alteration jobs.
    /// </summary>
    public DbSet<AlterationJob> AlterationJobs { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<AlterationLogEntry>();
        modelBuilder.Ignore<AlterationWorkflowInstanceFilter>();
        modelBuilder.Ignore<TimestampFilter>();
        
        var configuration = new Configurations();
        modelBuilder.ApplyConfiguration<AlterationPlan>(configuration);
        modelBuilder.ApplyConfiguration<AlterationJob>(configuration);
        base.OnModelCreating(modelBuilder);
    }
}
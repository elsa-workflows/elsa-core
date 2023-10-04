using Elsa.Alterations.Core.Entities;
using Elsa.EntityFrameworkCore.Common;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore.Modules.Alterations;

/// <summary>
/// DB context for the runtime module.
/// </summary>
public class AlterationsDbContext : ElsaDbContextBase
{
    /// <inheritdoc />
    public AlterationsDbContext(DbContextOptions options) : base(options)
    {
    }
    
    /// <summary>
    /// The workflow triggers.
    /// </summary>
    public DbSet<AlterationPlan> AlterationPlans { get; set; } = default!;

    /// <inheritdoc />
    protected override void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        var config = new Configurations();
        modelBuilder.ApplyConfiguration<AlterationPlan>(config);
    }
}
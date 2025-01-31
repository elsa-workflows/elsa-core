using Elsa.Alterations.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Elsa.EntityFrameworkCore.Oracle;

/// <summary>
/// Represents a class that handles entity model creation for SQLite databases.
/// </summary>
public class SetupForAlterations : IEntityModelCreatingHandler
{
    /// <inheritdoc />
    public void Handle(ElsaDbContextBase dbContext, ModelBuilder modelBuilder, IMutableEntityType entityType)
    {
        if(!dbContext.Database.IsOracle())
            return;
        
        // In order to use data more than 2000 char we have to use NCLOB.
        // In Oracle, we have to explicitly say the column is NCLOB otherwise it would be considered nvarchar(2000).
        modelBuilder.Entity<AlterationPlan>().Ignore(x => x.Alterations);
        modelBuilder.Entity<AlterationPlan>().Ignore(x => x.WorkflowInstanceFilter);
        modelBuilder.Entity<AlterationPlan>().Property<string>("SerializedAlterations").HasColumnType("NCLOB");
        modelBuilder.Entity<AlterationPlan>().Property<string>("SerializedWorkflowInstanceFilter").HasColumnType("NCLOB");
        modelBuilder.Entity<AlterationJob>().Ignore(x => x.Log);
        modelBuilder.Entity<AlterationJob>().Property<string>("SerializedLog").HasColumnType("NCLOB");
    }
}
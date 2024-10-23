using Elsa.Alterations.Core.Entities;
using Elsa.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Elsa.EntityFrameworkCore.Modules.Alterations;

/// <summary>
/// Represents a class that handles entity model creation for SQLite databases.
/// </summary>
public class SetupForOracle : IEntityModelCreatingHandler
{
    /// <inheritdoc />
    public void Handle(ElsaDbContextBase dbContext, ModelBuilder modelBuilder, IMutableEntityType entityType)
    {
        if(!dbContext.Database.IsOracle())
            return;
        
        // In order to use data more than 2000 char we have to use NCLOB.
        // In Oracle, we have to explicitly say the column is NCLOB otherwise it would be considered nvarchar(2000).
        modelBuilder.Entity<AlterationPlan>().Property("SerializedAlterations").HasColumnType("NCLOB");
        modelBuilder.Entity<AlterationPlan>().Property("SerializedWorkflowInstanceIds").HasColumnType("NCLOB");
        modelBuilder.Entity<AlterationJob>().Property("SerializedLog").HasColumnType("NCLOB");
    }
}
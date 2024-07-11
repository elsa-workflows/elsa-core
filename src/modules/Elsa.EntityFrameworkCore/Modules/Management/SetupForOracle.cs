using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Common.Contracts;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.Workflows.Management.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Elsa.EntityFrameworkCore.Modules.Management;

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
        modelBuilder.Entity<WorkflowInstance>().Property("Data").HasColumnType("NCLOB");
        modelBuilder.Entity<WorkflowDefinition>().Property("StringData").HasColumnType("NCLOB");
        modelBuilder.Entity<WorkflowDefinition>().Property("Data").HasColumnType("NCLOB");
    }
}
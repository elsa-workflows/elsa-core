using Elsa.Workflows.Management.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Elsa.Persistence.EFCore.MySql.Handlers;

/// <summary>
/// Represents a class that handles entity model creation for SQLite databases.
/// </summary>
public class SetupForMySql : IEntityModelCreatingHandler
{
    /// <inheritdoc />
    public void Handle(ElsaDbContextBase dbContext, ModelBuilder modelBuilder, IMutableEntityType entityType)
    {
        if (!dbContext.Database.IsMySql())
            return;

        // Configure the WorkflowDefinition entity to use the PostgreSQL JSONB type for the StringData property:
        if (entityType.ClrType == typeof(WorkflowDefinition))
        {
            modelBuilder
                .Entity(entityType.Name)
                .Property("StringData")
                .HasColumnType("JSON");
        }
    }
}
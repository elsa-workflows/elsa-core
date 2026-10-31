using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Elsa.Persistence.EFCore.MySql.Handlers;

/// <summary>
/// Represents a class that handles entity model creation for MySQL databases.
/// </summary>
public class SetupForMySql : IEntityModelCreatingHandler
{
    /// <inheritdoc />
    public void Handle(ElsaDbContextBase dbContext, ModelBuilder modelBuilder, IMutableEntityType entityType)
    {
        if (!dbContext.Database.IsMySql())
        {
            return;
        }

        // Configure the WorkflowDefinition entity to use the MySQL JSON type for the StringData property:
        if (entityType.ClrType == typeof(WorkflowDefinition))
        {
            modelBuilder
                .Entity(entityType.Name)
                .Property("StringData")
                .HasColumnType("JSON");
        }

        if (entityType.ClrType != typeof(StoredTrigger))
        {
            return;
        }

        // Shared Configurations.cs uses a 69-character, unprefixed name. MySQL identifiers cap at 64,
        // and four varchar(255) utf8mb4 columns exceed InnoDB's 3,072-byte unique-key limit.
        // Prefix uniqueness covers the first 191 characters of each column, not the full 255.
        modelBuilder.Entity<StoredTrigger>()
            .HasIndex(x => new
            {
                x.WorkflowDefinitionId,
                x.Hash,
                x.ActivityId,
                x.TenantId
            })
            .IsUnique()
            .HasDatabaseName(StoredTriggerMySqlUniqueIndex.Name)
            .HasPrefixLength(
                StoredTriggerMySqlUniqueIndex.PrefixLength,
                StoredTriggerMySqlUniqueIndex.PrefixLength,
                StoredTriggerMySqlUniqueIndex.PrefixLength,
                StoredTriggerMySqlUniqueIndex.PrefixLength);
    }
}
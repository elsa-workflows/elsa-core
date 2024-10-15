using Elsa.EntityFrameworkCore.Contracts;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.KeyValues.Entities;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <summary>
/// Represents a class that handles entity model creation for SQLite databases.
/// </summary>
public class SetupForOracle : IEntityModelCreatingHandler
{
    /// <inheritdoc />
    public void Handle(ElsaDbContextBase dbContext, ModelBuilder modelBuilder, IMutableEntityType entityType)
    {
        if (!dbContext.Database.IsOracle())
            return;

        // To use data more than 2000 char we have to use NCLOB.
        // In Oracle, we have to explicitly say the column is NCLOB otherwise it would be considered nvarchar(2000).
        modelBuilder.Entity<StoredTrigger>().Property("SerializedPayload").HasColumnType("NCLOB");

        modelBuilder.Entity<WorkflowExecutionLogRecord>().Property("SerializedActivityState").HasColumnType("NCLOB");
        modelBuilder.Entity<WorkflowExecutionLogRecord>().Property("SerializedPayload").HasColumnType("NCLOB");

        modelBuilder.Entity<ActivityExecutionRecord>().Property("SerializedActivityState").HasColumnType("NCLOB");
        modelBuilder.Entity<ActivityExecutionRecord>().Property("SerializedException").HasColumnType("NCLOB");
        modelBuilder.Entity<ActivityExecutionRecord>().Property("SerializedPayload").HasColumnType("NCLOB");
        modelBuilder.Entity<ActivityExecutionRecord>().Property("SerializedOutputs").HasColumnType("NCLOB");

        modelBuilder.Entity<StoredBookmark>().Property("SerializedPayload").HasColumnType("NCLOB");
        modelBuilder.Entity<StoredBookmark>().Property("SerializedMetadata").HasColumnType("NCLOB");

        modelBuilder.Entity<WorkflowInboxMessage>().Property("SerializedInput").HasColumnType("NCLOB");
        modelBuilder.Entity<WorkflowInboxMessage>().Property("SerializedBookmarkPayload").HasColumnType("NCLOB");

        modelBuilder.Entity<SerializedKeyValuePair>().Property("SerializedValue").HasColumnType("NCLOB");
    }
}
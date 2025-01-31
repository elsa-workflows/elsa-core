using Elsa.KeyValues.Entities;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Elsa.EntityFrameworkCore.Oracle;

/// <summary>
/// Represents a class that handles entity model creation for SQLite databases.
/// </summary>
public class SetupForRuntime : IEntityModelCreatingHandler
{
    /// <inheritdoc />
    public void Handle(ElsaDbContextBase dbContext, ModelBuilder modelBuilder, IMutableEntityType entityType)
    {
        if (!dbContext.Database.IsOracle())
            return;

        // To use data more than 2000 char we have to use NCLOB.
        // In Oracle, we have to explicitly say the column is NCLOB otherwise it would be considered nvarchar(2000).
        modelBuilder.Entity<StoredTrigger>().Property<string>("SerializedPayload").HasColumnType("NCLOB");

        modelBuilder.Entity<WorkflowExecutionLogRecord>().Ignore(x => x.ActivityState);
        modelBuilder.Entity<WorkflowExecutionLogRecord>().Ignore(x => x.Payload);
        modelBuilder.Entity<WorkflowExecutionLogRecord>().Property<string>("SerializedActivityState").HasColumnType("NCLOB");
        modelBuilder.Entity<WorkflowExecutionLogRecord>().Property<string>("SerializedPayload").HasColumnType("NCLOB");

        modelBuilder.Entity<ActivityExecutionRecord>().Ignore(x => x.ActivityState);
        modelBuilder.Entity<ActivityExecutionRecord>().Ignore(x => x.Exception);
        modelBuilder.Entity<ActivityExecutionRecord>().Ignore(x => x.Payload);
        modelBuilder.Entity<ActivityExecutionRecord>().Ignore(x => x.Outputs);
        modelBuilder.Entity<ActivityExecutionRecord>().Ignore(x => x.Properties);
        modelBuilder.Entity<ActivityExecutionRecord>().Property<string>("SerializedActivityState").HasColumnType("NCLOB");
        modelBuilder.Entity<ActivityExecutionRecord>().Property<string>("SerializedException").HasColumnType("NCLOB");
        modelBuilder.Entity<ActivityExecutionRecord>().Property<string>("SerializedPayload").HasColumnType("NCLOB");
        modelBuilder.Entity<ActivityExecutionRecord>().Property<string>("SerializedOutputs").HasColumnType("NCLOB");
        modelBuilder.Entity<ActivityExecutionRecord>().Property<string>("SerializedProperties").HasColumnType("NCLOB");

        modelBuilder.Entity<StoredBookmark>().Ignore(x => x.Payload);
        modelBuilder.Entity<StoredBookmark>().Ignore(x => x.Metadata);
        modelBuilder.Entity<StoredBookmark>().Property<string>("SerializedPayload").HasColumnType("NCLOB");
        modelBuilder.Entity<StoredBookmark>().Property<string>("SerializedMetadata").HasColumnType("NCLOB");
        
        modelBuilder.Entity<StoredTrigger>().Ignore(x => x.Payload);
        modelBuilder.Entity<StoredTrigger>().Property<string>("SerializedPayload").HasColumnType("NCLOB");

        modelBuilder.Entity<WorkflowInboxMessage>().Ignore(x => x.Input);
        modelBuilder.Entity<WorkflowInboxMessage>().Ignore(x => x.BookmarkPayload);
        modelBuilder.Entity<WorkflowInboxMessage>().Property<string>("SerializedInput").HasColumnType("NCLOB");
        modelBuilder.Entity<WorkflowInboxMessage>().Property<string>("SerializedBookmarkPayload").HasColumnType("NCLOB");
        
        modelBuilder.Entity<SerializedKeyValuePair>().Property<string>("SerializedValue").HasColumnType("NCLOB");
    }
}
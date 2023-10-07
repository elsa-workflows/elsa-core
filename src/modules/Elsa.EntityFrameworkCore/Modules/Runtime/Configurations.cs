using Elsa.Workflows.Core;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <summary>
/// EF Core configuration for various sets of <see cref="DbContext"/>. 
/// </summary>
public class Configurations :
    IEntityTypeConfiguration<StoredTrigger>,
    IEntityTypeConfiguration<WorkflowExecutionLogRecord>,
    IEntityTypeConfiguration<ActivityExecutionRecord>,
    IEntityTypeConfiguration<StoredBookmark>,
    IEntityTypeConfiguration<WorkflowInboxMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StoredTrigger> builder)
    {
        builder.Ignore(x => x.Payload);
        builder.Property<string>("SerializedPayload");
        builder.HasIndex(x => x.WorkflowDefinitionId).HasDatabaseName($"IX_{nameof(StoredTrigger)}_{nameof(StoredTrigger.WorkflowDefinitionId)}");
        builder.HasIndex(x => x.WorkflowDefinitionVersionId).HasDatabaseName($"IX_{nameof(StoredTrigger)}_{nameof(StoredTrigger.WorkflowDefinitionVersionId)}");
        builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(StoredTrigger)}_{nameof(StoredTrigger.Name)}");
        builder.HasIndex(x => x.Hash).HasDatabaseName($"IX_{nameof(StoredTrigger)}_{nameof(StoredTrigger.Hash)}");
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<WorkflowExecutionLogRecord> builder)
    {
        builder.Ignore(x => x.ActivityState);
        builder.Ignore(x => x.Payload);
        builder.Property<string>("SerializedActivityState");
        builder.Property<string>("SerializedPayload");

        builder.HasIndex(x => x.Timestamp).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.Timestamp)}");
        builder.HasIndex(x => x.Sequence).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.Sequence)}");
        builder.HasIndex(x => new { x.Timestamp, x.Sequence }).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.Timestamp)}_{nameof(WorkflowExecutionLogRecord.Sequence)}");
        builder.HasIndex(x => x.ActivityInstanceId).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.ActivityInstanceId)}");
        builder.HasIndex(x => x.ParentActivityInstanceId).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.ParentActivityInstanceId)}");
        builder.HasIndex(x => x.ActivityId).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.ActivityId)}");
        builder.HasIndex(x => x.ActivityType).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.ActivityType)}");
        builder.HasIndex(x => x.ActivityTypeVersion).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.ActivityTypeVersion)}");
        builder.HasIndex(x => new {x.ActivityType, x.ActivityTypeVersion}).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.ActivityType)}_{nameof(WorkflowExecutionLogRecord.ActivityTypeVersion)}");
        builder.HasIndex(x => x.ActivityName).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.ActivityName)}");
        builder.HasIndex(x => x.EventName).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.EventName)}");
        builder.HasIndex(x => x.WorkflowDefinitionId).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.WorkflowDefinitionId)}");
        builder.HasIndex(x => x.WorkflowDefinitionVersionId).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.WorkflowDefinitionVersionId)}");
        builder.HasIndex(x => x.WorkflowInstanceId).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.WorkflowInstanceId)}");
        builder.HasIndex(x => x.WorkflowVersion).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.WorkflowVersion)}");
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ActivityExecutionRecord> builder)
    {
        builder.Ignore(x => x.ActivityState);
        builder.Ignore(x => x.Exception);
        builder.Ignore(x => x.Payload);
        builder.Ignore(x => x.Outputs);
        builder.Property<string>("SerializedActivityState");
        builder.Property<string>("SerializedException");
        builder.Property<string>("SerializedPayload");
        builder.Property<string>("SerializedOutputs");
        builder.Property(x => x.Status).HasConversion<string>();
        
        builder.HasIndex(x => x.WorkflowInstanceId).HasDatabaseName($"IX_{nameof(ActivityExecutionRecord)}_{nameof(ActivityExecutionRecord.WorkflowInstanceId)}");
        builder.HasIndex(x => x.ActivityId).HasDatabaseName($"IX_{nameof(ActivityExecutionRecord)}_{nameof(ActivityExecutionRecord.ActivityId)}");
        builder.HasIndex(x => x.ActivityType).HasDatabaseName($"IX_{nameof(ActivityExecutionRecord)}_{nameof(ActivityExecutionRecord.ActivityType)}");
        builder.HasIndex(x => x.ActivityTypeVersion).HasDatabaseName($"IX_{nameof(ActivityExecutionRecord)}_{nameof(ActivityExecutionRecord.ActivityTypeVersion)}");
        builder.HasIndex(x => new {x.ActivityType, x.ActivityTypeVersion}).HasDatabaseName($"IX_{nameof(ActivityExecutionRecord)}_{nameof(ActivityExecutionRecord.ActivityType)}_{nameof(ActivityExecutionRecord.ActivityTypeVersion)}");
        builder.HasIndex(x => x.ActivityName).HasDatabaseName($"IX_{nameof(ActivityExecutionRecord)}_{nameof(ActivityExecutionRecord.ActivityName)}");
        builder.HasIndex(x => x.StartedAt).HasDatabaseName($"IX_{nameof(ActivityExecutionRecord)}_{nameof(ActivityExecutionRecord.StartedAt)}");
        builder.HasIndex(x => x.HasBookmarks).HasDatabaseName($"IX_{nameof(ActivityExecutionRecord)}_{nameof(ActivityExecutionRecord.HasBookmarks)}");
        builder.HasIndex(x => x.Status).HasDatabaseName($"IX_{nameof(ActivityExecutionRecord)}_{nameof(ActivityExecutionRecord.Status)}");
        builder.HasIndex(x => x.CompletedAt).HasDatabaseName($"IX_{nameof(ActivityExecutionRecord)}_{nameof(ActivityExecutionRecord.CompletedAt)}");
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StoredBookmark> builder)
    {
        builder.Ignore(x => x.Payload);
        builder.Ignore(x => x.Metadata);
        builder.Property<string>("SerializedPayload");
        builder.Property<string>("SerializedMetadata");
        builder.HasKey(x => x.BookmarkId);
        builder.HasIndex(x => x.ActivityTypeName, $"IX_{nameof(StoredBookmark)}_{nameof(StoredBookmark.ActivityTypeName)}");
        builder.HasIndex(x => x.Hash, $"IX_{nameof(StoredBookmark)}_{nameof(StoredBookmark.Hash)}");
        builder.HasIndex(x => x.WorkflowInstanceId, $"IX_{nameof(StoredBookmark)}_{nameof(StoredBookmark.WorkflowInstanceId)}");
        builder.HasIndex(x => x.ActivityInstanceId, $"IX_{nameof(StoredBookmark)}_{nameof(StoredBookmark.ActivityInstanceId)}");
        builder.HasIndex(x => x.CreatedAt, $"IX_{nameof(StoredBookmark)}_{nameof(StoredBookmark.CreatedAt)}");
        builder.HasIndex(x => new { x.ActivityTypeName, x.Hash }, $"IX_{nameof(StoredBookmark)}_{nameof(StoredBookmark.ActivityTypeName)}_{nameof(StoredBookmark.Hash)}");
        builder.HasIndex(x => new { x.ActivityTypeName, x.Hash, x.WorkflowInstanceId }, $"IX_{nameof(StoredBookmark)}_{nameof(StoredBookmark.ActivityTypeName)}_{nameof(StoredBookmark.Hash)}_{nameof(StoredBookmark.WorkflowInstanceId)}");
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<WorkflowInboxMessage> builder)
    {
        builder.Ignore(x => x.Input);
        builder.Ignore(x => x.BookmarkPayload);
        builder.Property<string>("SerializedInput");
        builder.Property<string>("SerializedBookmarkPayload");
        builder.HasIndex(x => x.ActivityTypeName, $"IX_{nameof(WorkflowInboxMessage)}_{nameof(WorkflowInboxMessage.ActivityTypeName)}");
        builder.HasIndex(x => x.Hash, $"IX_{nameof(WorkflowInboxMessage)}_{nameof(WorkflowInboxMessage.Hash)}");
        builder.HasIndex(x => x.WorkflowInstanceId, $"IX_{nameof(WorkflowInboxMessage)}_{nameof(WorkflowInboxMessage.WorkflowInstanceId)}");
        builder.HasIndex(x => x.CorrelationId, $"IX_{nameof(WorkflowInboxMessage)}_{nameof(WorkflowInboxMessage.CorrelationId)}");
        builder.HasIndex(x => x.ActivityInstanceId, $"IX_{nameof(WorkflowInboxMessage)}_{nameof(WorkflowInboxMessage.ActivityInstanceId)}");
        builder.HasIndex(x => x.CreatedAt, $"IX_{nameof(WorkflowInboxMessage)}_{nameof(WorkflowInboxMessage.CreatedAt)}");
        builder.HasIndex(x => x.ExpiresAt, $"IX_{nameof(WorkflowInboxMessage)}_{nameof(WorkflowInboxMessage.ExpiresAt)}");
    }
}
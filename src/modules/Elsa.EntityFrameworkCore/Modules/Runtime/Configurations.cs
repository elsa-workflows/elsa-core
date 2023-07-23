using Elsa.Workflows.Core;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <summary>
/// EF Core configuration for various sets of <see cref="DbContext"/>. 
/// </summary>
public class Configurations :
    IEntityTypeConfiguration<WorkflowState>,
    IEntityTypeConfiguration<StoredTrigger>,
    IEntityTypeConfiguration<WorkflowExecutionLogRecord>,
    IEntityTypeConfiguration<ActivityExecutionRecord>,
    IEntityTypeConfiguration<StoredBookmark>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<WorkflowState> builder)
    {
        builder.Ignore(x => x.Bookmarks);
        builder.Ignore(x => x.Properties);
        builder.Ignore(x => x.CompletionCallbacks);
        builder.Ignore(x => x.ActivityExecutionContexts);
        builder.Ignore(x => x.Fault);
        builder.Ignore(x => x.Output);
        builder.Property<string>("Data");
        builder.Property(x => x.Status).HasConversion<EnumToStringConverter<WorkflowStatus>>();
        builder.Property(x => x.SubStatus).HasConversion<EnumToStringConverter<WorkflowSubStatus>>();
        builder.HasIndex(x => x.CorrelationId).HasDatabaseName($"IX_{nameof(WorkflowState)}_{nameof(WorkflowState.CorrelationId)}");
        builder.HasIndex(x=> x.DefinitionId).HasDatabaseName($"IX_{nameof(WorkflowState)}_{nameof(WorkflowState.DefinitionId)}");
        builder.HasIndex(x=> x.DefinitionVersionId).HasDatabaseName($"IX_{nameof(WorkflowState)}_{nameof(WorkflowState.DefinitionVersionId)}");
        builder.HasIndex(x => new { x.Status, x.SubStatus, x.DefinitionId, x.DefinitionVersion }).HasDatabaseName($"IX_{nameof(WorkflowState)}_{nameof(WorkflowState.Status)}_{nameof(WorkflowState.SubStatus)}_{nameof(WorkflowState.DefinitionId)}_{nameof(WorkflowState.DefinitionVersion)}");
        builder.HasIndex(x => new { x.Status, x.SubStatus }).HasDatabaseName($"IX_{nameof(WorkflowState)}_{nameof(WorkflowState.Status)}_{nameof(WorkflowState.SubStatus)}");
        builder.HasIndex(x => new { x.Status, x.DefinitionId}).HasDatabaseName($"IX_{nameof(WorkflowState)}_{nameof(WorkflowState.Status)}_{nameof(WorkflowState.DefinitionId)}");
        builder.HasIndex(x => new { x.Status, x.SubStatus }).HasDatabaseName($"IX_{nameof(WorkflowState)}_{nameof(WorkflowState.Status)}_{nameof(WorkflowState.SubStatus)}");
        builder.HasIndex(x => x.CreatedAt).HasDatabaseName($"IX_{nameof(WorkflowState)}_{nameof(WorkflowState.CreatedAt)}");
        builder.HasIndex(x => x.UpdatedAt).HasDatabaseName($"IX_{nameof(WorkflowState)}_{nameof(WorkflowState.UpdatedAt)}");
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StoredTrigger> builder)
    {
        builder.Ignore(x => x.Payload);
        builder.Property<string>("Data");
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
        builder.Property<string>("PayloadData");
        builder.Property<string>("ActivityData");

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
        builder.Property<string>("ActivityData");
        
        builder.HasIndex(x => x.WorkflowInstanceId).HasDatabaseName($"IX_{nameof(ActivityExecutionRecord)}_{nameof(ActivityExecutionRecord.WorkflowInstanceId)}");
        builder.HasIndex(x => x.ActivityId).HasDatabaseName($"IX_{nameof(ActivityExecutionRecord)}_{nameof(ActivityExecutionRecord.ActivityId)}");
        builder.HasIndex(x => x.ActivityType).HasDatabaseName($"IX_{nameof(ActivityExecutionRecord)}_{nameof(ActivityExecutionRecord.ActivityType)}");
        builder.HasIndex(x => x.ActivityTypeVersion).HasDatabaseName($"IX_{nameof(ActivityExecutionRecord)}_{nameof(ActivityExecutionRecord.ActivityTypeVersion)}");
        builder.HasIndex(x => new {x.ActivityType, x.ActivityTypeVersion}).HasDatabaseName($"IX_{nameof(ActivityExecutionRecord)}_{nameof(ActivityExecutionRecord.ActivityType)}_{nameof(ActivityExecutionRecord.ActivityTypeVersion)}");
        builder.HasIndex(x => x.ActivityName).HasDatabaseName($"IX_{nameof(ActivityExecutionRecord)}_{nameof(ActivityExecutionRecord.ActivityName)}");
        builder.HasIndex(x => x.StartedAt).HasDatabaseName($"IX_{nameof(ActivityExecutionRecord)}_{nameof(ActivityExecutionRecord.StartedAt)}");
        builder.HasIndex(x => x.HasBookmarks).HasDatabaseName($"IX_{nameof(ActivityExecutionRecord)}_{nameof(ActivityExecutionRecord.HasBookmarks)}");
        builder.HasIndex(x => x.CompletedAt).HasDatabaseName($"IX_{nameof(ActivityExecutionRecord)}_{nameof(ActivityExecutionRecord.CompletedAt)}");
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StoredBookmark> builder)
    {
        builder.Ignore(x => x.Payload);
        builder.Property<string>("Data");
        builder.HasKey(x => x.BookmarkId);
        builder.HasIndex(x => x.ActivityTypeName, $"IX_{nameof(StoredBookmark)}_{nameof(StoredBookmark.ActivityTypeName)}");
        builder.HasIndex(x => x.Hash, $"IX_{nameof(StoredBookmark)}_{nameof(StoredBookmark.Hash)}");
        builder.HasIndex(x => x.WorkflowInstanceId, $"IX_{nameof(StoredBookmark)}_{nameof(StoredBookmark.WorkflowInstanceId)}");
        builder.HasIndex(x => new { x.ActivityTypeName, x.Hash }, $"IX_{nameof(StoredBookmark)}_{nameof(StoredBookmark.ActivityTypeName)}_{nameof(StoredBookmark.Hash)}");
        builder.HasIndex(x => new { x.ActivityTypeName, x.Hash, x.WorkflowInstanceId }, $"IX_{nameof(StoredBookmark)}_{nameof(StoredBookmark.ActivityTypeName)}_{nameof(StoredBookmark.Hash)}_{nameof(StoredBookmark.WorkflowInstanceId)}");
    }
}
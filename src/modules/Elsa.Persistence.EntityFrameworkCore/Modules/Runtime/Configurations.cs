using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.Runtime
{
    public class Configurations :
        IEntityTypeConfiguration<WorkflowState>,
        IEntityTypeConfiguration<WorkflowTrigger>,
        IEntityTypeConfiguration<WorkflowExecutionLogRecord>,
        IEntityTypeConfiguration<StoredBookmark>
    {
        public void Configure(EntityTypeBuilder<WorkflowState> builder)
        {
            builder.Ignore(x => x.Bookmarks);
            builder.Ignore(x => x.Properties);
            builder.Ignore(x => x.ActivityOutput);
            builder.Ignore(x => x.CompletionCallbacks);
            builder.Ignore(x => x.PersistentVariables);
            builder.Ignore(x => x.ActivityExecutionContexts);
            builder.Property<string>("Data");
            builder.Property<DateTimeOffset>("CreatedAt");
            builder.Property<DateTimeOffset>("UpdatedAt");
            builder.Property(x => x.Status).HasConversion<EnumToStringConverter<WorkflowStatus>>();
            builder.Property(x => x.SubStatus).HasConversion<EnumToStringConverter<WorkflowSubStatus>>();
            builder.HasIndex(x => x.CorrelationId).HasDatabaseName($"IX_{nameof(WorkflowState)}_{nameof(WorkflowState.CorrelationId)}");
            builder.HasIndex(x=>x.DefinitionId).HasDatabaseName($"IX_{nameof(WorkflowState)}_{nameof(WorkflowState.DefinitionId)}");
            builder.HasIndex(x => new { x.Status, x.SubStatus, x.DefinitionId, x.DefinitionVersion }).HasDatabaseName($"IX_{nameof(WorkflowState)}_{nameof(WorkflowState.Status)}_{nameof(WorkflowState.SubStatus)}_{nameof(WorkflowState.DefinitionId)}_{nameof(WorkflowState.DefinitionVersion)}");
            builder.HasIndex(x => new { x.Status, x.SubStatus }).HasDatabaseName($"IX_{nameof(WorkflowState)}_{nameof(WorkflowState.Status)}_{nameof(WorkflowState.SubStatus)}");
            builder.HasIndex(x => new { x.Status, x.DefinitionId}).HasDatabaseName($"IX_{nameof(WorkflowState)}_{nameof(WorkflowState.Status)}_{nameof(WorkflowState.DefinitionId)}");
            builder.HasIndex(x => new { x.Status, x.SubStatus }).HasDatabaseName($"IX_{nameof(WorkflowState)}_{nameof(WorkflowState.Status)}_{nameof(WorkflowState.SubStatus)}");
            builder.HasIndex("CreatedAt").HasDatabaseName($"IX_{nameof(WorkflowState)}_CreatedAt");
            builder.HasIndex("UpdatedAt").HasDatabaseName($"IX_{nameof(WorkflowState)}_UpdatedAt");
        }

        public void Configure(EntityTypeBuilder<WorkflowTrigger> builder)
        {
            builder.HasIndex(x => x.WorkflowDefinitionId).HasDatabaseName($"IX_{nameof(WorkflowTrigger)}_{nameof(WorkflowTrigger.WorkflowDefinitionId)}");
            builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(WorkflowTrigger)}_{nameof(WorkflowTrigger.Name)}");
            builder.HasIndex(x => x.Hash).HasDatabaseName($"IX_{nameof(WorkflowTrigger)}_{nameof(WorkflowTrigger.Hash)}");
        }

        public void Configure(EntityTypeBuilder<WorkflowExecutionLogRecord> builder)
        {
            builder.Ignore(x => x.Payload);
            builder.Property<string>("PayloadData");

            builder.HasIndex(x => x.Timestamp).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.Timestamp)}");
            builder.HasIndex(x => x.ActivityInstanceId).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.ActivityInstanceId)}");
            builder.HasIndex(x => x.ParentActivityInstanceId).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.ParentActivityInstanceId)}");
            builder.HasIndex(x => x.ActivityId).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.ActivityId)}");
            builder.HasIndex(x => x.ActivityType).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.ActivityType)}");
            builder.HasIndex(x => x.EventName).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.EventName)}");
            builder.HasIndex(x => x.WorkflowDefinitionId).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.WorkflowDefinitionId)}");
            builder.HasIndex(x => x.WorkflowInstanceId).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.WorkflowInstanceId)}");
            builder.HasIndex(x => x.WorkflowVersion).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.WorkflowVersion)}");
        }

        public void Configure(EntityTypeBuilder<StoredBookmark> builder)
        {
            builder.HasKey(x => x.BookmarkId);
            builder.HasIndex(x => x.ActivityTypeName, $"IX_{nameof(StoredBookmark)}_{nameof(StoredBookmark.ActivityTypeName)}");
            builder.HasIndex(x => x.Hash, $"IX_{nameof(StoredBookmark)}_{nameof(StoredBookmark.Hash)}");
            builder.HasIndex(x => x.WorkflowInstanceId, $"IX_{nameof(StoredBookmark)}_{nameof(StoredBookmark.WorkflowInstanceId)}");
            builder.HasIndex(x => new { x.ActivityTypeName, x.Hash }, $"IX_{nameof(StoredBookmark)}_{nameof(StoredBookmark.ActivityTypeName)}_{nameof(StoredBookmark.Hash)}");
            builder.HasIndex(x => new { x.ActivityTypeName, x.Hash, x.WorkflowInstanceId }, $"IX_{nameof(StoredBookmark)}_{nameof(StoredBookmark.ActivityTypeName)}_{nameof(StoredBookmark.Hash)}_{nameof(StoredBookmark.WorkflowInstanceId)}");
        }
    }
}
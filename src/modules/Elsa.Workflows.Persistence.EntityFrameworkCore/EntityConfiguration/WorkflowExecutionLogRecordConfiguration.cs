using Elsa.Workflows.Management.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.EntityConfiguration
{
    public class WorkflowExecutionLogRecordConfiguration : IEntityTypeConfiguration<WorkflowExecutionLogRecord>
    {
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
    }
}
using Elsa.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EntityFrameworkCore.Configuration
{
    public class WorkflowExecutionLogRecordConfiguration : IEntityTypeConfiguration<WorkflowExecutionLogRecord>
    {
        public void Configure(EntityTypeBuilder<WorkflowExecutionLogRecord> builder)
        {
            builder.Ignore(x => x.Payload);
            builder.Property<string>("PayloadData");
            
            builder.HasIndex(x => x.Timestamp).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.Timestamp)}");
            builder.HasIndex(x => x.ActivityId).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.ActivityId)}");
            builder.HasIndex(x => x.ActivityType).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.ActivityType)}");
            builder.HasIndex(x => x.EventName).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.EventName)}");
            builder.HasIndex(x => x.WorkflowInstanceId).HasDatabaseName($"IX_{nameof(WorkflowExecutionLogRecord)}_{nameof(WorkflowExecutionLogRecord.WorkflowInstanceId)}");
        }
    }
}
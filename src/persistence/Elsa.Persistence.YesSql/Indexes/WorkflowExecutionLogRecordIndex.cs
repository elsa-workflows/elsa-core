using System;
using Elsa.Persistence.YesSql.Data;
using Elsa.Persistence.YesSql.Documents;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql.Indexes
{
    public class WorkflowExecutionLogRecordIndex : MapIndex
    {
        public string? TenantId { get; set; }
        public string RecordId { get; set; } = default!;
        public string WorkflowInstanceId { get; set; } = default!;
        public DateTime Timestamp { get; set; }
    }

    public class WorkflowExecutionLogRecordIndexProvider : IndexProvider<WorkflowExecutionLogRecordDocument>
    {
        public WorkflowExecutionLogRecordIndexProvider() => CollectionName = CollectionNames.WorkflowExecutionLog;

        public override void Describe(DescribeContext<WorkflowExecutionLogRecordDocument> context)
        {
            context.For<WorkflowExecutionLogRecordIndex>()
                .Map(
                    record => new WorkflowExecutionLogRecordIndex
                    {
                        RecordId = record.RecordId,
                        TenantId = record.TenantId,
                        WorkflowInstanceId = record.WorkflowInstanceId,
                        Timestamp = record.Timestamp.ToDateTimeUtc()
                    }
                );
        }
    }
}
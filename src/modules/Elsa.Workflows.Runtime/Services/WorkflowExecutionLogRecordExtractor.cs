using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class WorkflowExecutionLogRecordExtractor(IIdentityGenerator identityGenerator) : ILogRecordExtractor<WorkflowExecutionLogRecord>
{
    /// <inheritdoc />
    public IEnumerable<WorkflowExecutionLogRecord> ExtractLogRecords(WorkflowExecutionContext context)
    {
        return context.ExecutionLog.Select(x => new WorkflowExecutionLogRecord
        {
            Id = identityGenerator.GenerateId(),
            ActivityInstanceId = x.ActivityInstanceId,
            ParentActivityInstanceId = x.ParentActivityInstanceId,
            ActivityNodeId = x.NodeId,
            ActivityId = x.ActivityId,
            ActivityType = x.ActivityType,
            ActivityTypeVersion = x.ActivityTypeVersion,
            ActivityName = x.ActivityName,
            Message = x.Message,
            EventName = x.EventName,
            WorkflowDefinitionId = context.Workflow.Identity.DefinitionId,
            WorkflowDefinitionVersionId = context.Workflow.Identity.Id,
            WorkflowInstanceId = context.Id,
            WorkflowVersion = context.Workflow.Version,
            Source = x.Source,
            ActivityState = x.ActivityState,
            Payload = x.Payload,
            Timestamp = x.Timestamp,
            Sequence = x.Sequence
        });
    }
}
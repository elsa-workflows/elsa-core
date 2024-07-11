using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows;

namespace Elsa.ProtoActor.Mappers;

public class WorkflowSubStatusMapper
{
    public WorkflowSubStatus Map(ProtoWorkflowSubStatus subStatus)
    {
        return subStatus switch
        {
            ProtoWorkflowSubStatus.WorkflowSubStatusPending => WorkflowSubStatus.Pending,
            ProtoWorkflowSubStatus.WorkflowSubStatusFaulted => WorkflowSubStatus.Faulted,
            ProtoWorkflowSubStatus.WorkflowSubStatusFinished => WorkflowSubStatus.Finished,
            ProtoWorkflowSubStatus.WorkflowSubStatusCancelled => WorkflowSubStatus.Cancelled,
            ProtoWorkflowSubStatus.WorkflowSubStatusExecuting => WorkflowSubStatus.Executing,
            ProtoWorkflowSubStatus.WorkflowSubStatusSuspended => WorkflowSubStatus.Suspended,
            _ => throw new ArgumentOutOfRangeException(nameof(subStatus), subStatus, null)
        };
    }

    public ProtoWorkflowSubStatus Map(WorkflowSubStatus subStatus)
    {
        return subStatus switch
        {
            WorkflowSubStatus.Pending => ProtoWorkflowSubStatus.WorkflowSubStatusPending,
            WorkflowSubStatus.Faulted => ProtoWorkflowSubStatus.WorkflowSubStatusFaulted,
            WorkflowSubStatus.Finished => ProtoWorkflowSubStatus.WorkflowSubStatusFinished,
            WorkflowSubStatus.Cancelled => ProtoWorkflowSubStatus.WorkflowSubStatusCancelled,
            WorkflowSubStatus.Executing => ProtoWorkflowSubStatus.WorkflowSubStatusExecuting,
            WorkflowSubStatus.Suspended => ProtoWorkflowSubStatus.WorkflowSubStatusSuspended,
            _ => throw new ArgumentOutOfRangeException(nameof(subStatus), subStatus, null)
        };
    }
}
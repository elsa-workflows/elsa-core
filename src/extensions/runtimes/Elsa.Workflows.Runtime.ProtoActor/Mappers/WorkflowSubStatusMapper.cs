using ProtoWorkflowSubStatus = Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.WorkflowSubStatus;

namespace Elsa.Workflows.Runtime.ProtoActor.Mappers;

public class WorkflowSubStatusMapper
{
    public WorkflowSubStatus Map(ProtoWorkflowSubStatus subStatus)
    {
        return subStatus switch
        {
            ProtoWorkflowSubStatus.Pending => WorkflowSubStatus.Pending,
            ProtoWorkflowSubStatus.Faulted => WorkflowSubStatus.Faulted,
            ProtoWorkflowSubStatus.Finished => WorkflowSubStatus.Finished,
            ProtoWorkflowSubStatus.Cancelled => WorkflowSubStatus.Cancelled,
            ProtoWorkflowSubStatus.Executing => WorkflowSubStatus.Executing,
            ProtoWorkflowSubStatus.Suspended => WorkflowSubStatus.Suspended,
            _ => throw new ArgumentOutOfRangeException(nameof(subStatus), subStatus, null)
        };
    }

    public ProtoWorkflowSubStatus Map(WorkflowSubStatus subStatus)
    {
        return subStatus switch
        {
            WorkflowSubStatus.Pending => ProtoWorkflowSubStatus.Pending,
            WorkflowSubStatus.Faulted => ProtoWorkflowSubStatus.Faulted,
            WorkflowSubStatus.Finished => ProtoWorkflowSubStatus.Finished,
            WorkflowSubStatus.Cancelled => ProtoWorkflowSubStatus.Cancelled,
            WorkflowSubStatus.Executing => ProtoWorkflowSubStatus.Executing,
            WorkflowSubStatus.Suspended => ProtoWorkflowSubStatus.Suspended,
            _ => throw new ArgumentOutOfRangeException(nameof(subStatus), subStatus, null)
        };
    }
}
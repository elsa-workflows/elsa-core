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
            // Instance-column markers. The runner does not transition to them, but a snapshot that carries one must not throw.
            ProtoWorkflowSubStatus.Interrupted => WorkflowSubStatus.Interrupted,
            ProtoWorkflowSubStatus.Cancelling => WorkflowSubStatus.Cancelling,
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
            // Instance-column markers. The runner does not transition to them, but a snapshot that carries one must not throw.
            WorkflowSubStatus.Interrupted => ProtoWorkflowSubStatus.Interrupted,
            WorkflowSubStatus.Cancelling => ProtoWorkflowSubStatus.Cancelling,
            _ => throw new ArgumentOutOfRangeException(nameof(subStatus), subStatus, null)
        };
    }
}
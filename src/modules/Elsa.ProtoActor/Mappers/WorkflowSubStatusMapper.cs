using Elsa.Workflows.Core;
using ProtoWorkflowSubStatus = Elsa.ProtoActor.ProtoBuf.WorkflowSubStatus;

namespace Elsa.ProtoActor.Mappers;

internal class WorkflowSubStatusMapper
{
    public WorkflowSubStatus Map(ProtoWorkflowSubStatus subStatus) =>
        subStatus switch
        {
            ProtoWorkflowSubStatus.Faulted => WorkflowSubStatus.Faulted,
            ProtoWorkflowSubStatus.Finished => WorkflowSubStatus.Finished,
            ProtoWorkflowSubStatus.Cancelled => WorkflowSubStatus.Cancelled,
            ProtoWorkflowSubStatus.Executing => WorkflowSubStatus.Executing,
            ProtoWorkflowSubStatus.Suspended => WorkflowSubStatus.Suspended,
            _ => throw new ArgumentOutOfRangeException(nameof(subStatus), subStatus, null)
        };
    
    public ProtoWorkflowSubStatus Map(WorkflowSubStatus subStatus) =>
        subStatus switch
        {
            WorkflowSubStatus.Faulted => ProtoWorkflowSubStatus.Faulted,
            WorkflowSubStatus.Finished => ProtoWorkflowSubStatus.Finished,
            WorkflowSubStatus.Cancelled => ProtoWorkflowSubStatus.Cancelled,
            WorkflowSubStatus.Executing => ProtoWorkflowSubStatus.Executing,
            WorkflowSubStatus.Suspended => ProtoWorkflowSubStatus.Suspended,
            _ => throw new ArgumentOutOfRangeException(nameof(subStatus), subStatus, null)
        };
}
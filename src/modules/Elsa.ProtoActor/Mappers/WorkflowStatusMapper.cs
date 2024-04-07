using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows;

namespace Elsa.ProtoActor.Mappers;

internal class WorkflowStatusMapper
{
    public WorkflowStatus Map(ProtoWorkflowStatus status) =>
        status switch
        {
            ProtoWorkflowStatus.WorkflowStatusFinished => WorkflowStatus.Finished,
            ProtoWorkflowStatus.WorkflowStatusRunning => WorkflowStatus.Running,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    
    public ProtoWorkflowStatus Map(WorkflowStatus status) =>
        status switch
        {
            WorkflowStatus.Finished => ProtoWorkflowStatus.WorkflowStatusFinished,
            WorkflowStatus.Running => ProtoWorkflowStatus.WorkflowStatusRunning,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
}
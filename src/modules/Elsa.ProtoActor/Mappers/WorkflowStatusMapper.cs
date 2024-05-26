using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows;

namespace Elsa.ProtoActor.Mappers;

public class WorkflowStatusMapper
{
    public WorkflowStatus Map(ProtoWorkflowStatus status)
    {
        return status switch
        {
            ProtoWorkflowStatus.WorkflowStatusFinished => WorkflowStatus.Finished,
            ProtoWorkflowStatus.WorkflowStatusRunning => WorkflowStatus.Running,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }

    public ProtoWorkflowStatus Map(WorkflowStatus status)
    {
        return status switch
        {
            WorkflowStatus.Finished => ProtoWorkflowStatus.WorkflowStatusFinished,
            WorkflowStatus.Running => ProtoWorkflowStatus.WorkflowStatusRunning,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }
}
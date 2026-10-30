using ProtoWorkflowStatus = Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.WorkflowStatus;

namespace Elsa.Workflows.Runtime.ProtoActor.Mappers;

public class WorkflowStatusMapper
{
    public WorkflowStatus Map(ProtoWorkflowStatus status)
    {
        return status switch
        {
            ProtoWorkflowStatus.Finished => WorkflowStatus.Finished,
            ProtoWorkflowStatus.Running => WorkflowStatus.Running,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }

    public ProtoWorkflowStatus Map(WorkflowStatus status)
    {
        return status switch
        {
            WorkflowStatus.Finished => ProtoWorkflowStatus.Finished,
            WorkflowStatus.Running => ProtoWorkflowStatus.Running,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }
}
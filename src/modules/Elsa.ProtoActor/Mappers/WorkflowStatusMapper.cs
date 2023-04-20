using Elsa.Workflows.Core.Models;
using ProtoWorkflowStatus = Elsa.ProtoActor.Protos.WorkflowStatus;

namespace Elsa.ProtoActor.Mappers;

public class WorkflowStatusMapper
{
    public WorkflowStatus Map(ProtoWorkflowStatus status) =>
        status switch
        {
            ProtoWorkflowStatus.Finished => WorkflowStatus.Finished,
            ProtoWorkflowStatus.Running => WorkflowStatus.Running,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    
    public ProtoWorkflowStatus Map(WorkflowStatus status) =>
        status switch
        {
            WorkflowStatus.Finished => ProtoWorkflowStatus.Finished,
            WorkflowStatus.Running => ProtoWorkflowStatus.Running,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
}
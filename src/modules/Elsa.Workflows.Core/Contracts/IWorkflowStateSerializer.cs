using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Contracts;

public interface IWorkflowStateSerializer
{
    WorkflowState SerializeState(WorkflowExecutionContext workflowExecutionContext);
    void DeserializeState(WorkflowExecutionContext workflowExecutionContext, WorkflowState state);
}
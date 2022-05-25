using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Services;

public interface IWorkflowStateSerializer
{
    WorkflowState ReadState(WorkflowExecutionContext workflowExecutionContext);
    void WriteState(WorkflowExecutionContext workflowExecutionContext, WorkflowState state);
}
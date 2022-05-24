using Elsa.Models;
using Elsa.State;

namespace Elsa.Services;

public interface IWorkflowStateSerializer
{
    WorkflowState ReadState(WorkflowExecutionContext workflowExecutionContext);
    void WriteState(WorkflowExecutionContext workflowExecutionContext, WorkflowState state);
}
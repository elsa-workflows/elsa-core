using Elsa.Workflows;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.State;

namespace Elsa.Testing.Shared.EventArgs;

public class WorkflowStateCommittedEventArgs(WorkflowExecutionContext workflowExecutionContext, WorkflowState workflowState, WorkflowInstance workflowInstance) : System.EventArgs 
{
    public WorkflowExecutionContext WorkflowExecutionContext { get; } = workflowExecutionContext;
    public WorkflowState WorkflowState { get; } = workflowState;
    public WorkflowInstance WorkflowInstance { get; } = workflowInstance;
}
using Elsa.Workflows;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.State;

namespace Elsa.Testing.Shared;

public class WorkflowStateCommittedEventArgs(WorkflowExecutionContext workflowExecutionContext, WorkflowState workflowState, WorkflowInstance workflowInstance) : EventArgs 
{
    public WorkflowExecutionContext WorkflowExecutionContext { get; } = workflowExecutionContext;
    public WorkflowState WorkflowState { get; } = workflowState;
    public WorkflowInstance WorkflowInstance { get; } = workflowInstance;
}
using Elsa.Workflows.Activities;
using Elsa.Workflows.State;

namespace Elsa.Testing.Shared;

public class WorkflowFinishedEventArgs(Workflow workflow, WorkflowState workflowState) : EventArgs
{
    public Workflow Workflow { get; } = workflow;
    public WorkflowState WorkflowState { get; } = workflowState;
}
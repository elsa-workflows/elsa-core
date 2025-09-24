using Elsa.Workflows.Activities;
using Elsa.Workflows.State;

namespace Elsa.Testing.Shared.EventArgs;

public class WorkflowFinishedEventArgs(Workflow workflow, WorkflowState workflowState) : System.EventArgs
{
    public Workflow Workflow { get; } = workflow;
    public WorkflowState WorkflowState { get; } = workflowState;
}
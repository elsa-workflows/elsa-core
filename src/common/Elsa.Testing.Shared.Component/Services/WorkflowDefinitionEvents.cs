namespace Elsa.Testing.Shared.Services;

public class WorkflowDefinitionEvents : IWorkflowDefinitionEvents
{
    public event EventHandler<WorkflowDefinitionDeletedEventArgs>? WorkflowDefinitionDeleted;
    public void OnWorkflowDefinitionDeleted(WorkflowDefinitionDeletedEventArgs args) => WorkflowDefinitionDeleted?.Invoke(this, args);
}
namespace Elsa.Testing.Shared.Services;

public class WorkflowDefinitionEvents
{
    public event EventHandler<WorkflowDefinitionDeletedEventArgs>? WorkflowDefinitionDeleted;
    public void OnWorkflowDefinitionDeleted(WorkflowDefinitionDeletedEventArgs args) => WorkflowDefinitionDeleted?.Invoke(this, args);
}
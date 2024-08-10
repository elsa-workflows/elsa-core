namespace Elsa.Testing.Shared.Services;

public class WorkflowEvents
{
    public event EventHandler<WorkflowFinishedEventArgs>? WorkflowFinished;
    public event EventHandler<WorkflowInstanceSavedEventArgs>? WorkflowInstanceSaved;
    public void OnWorkflowFinished(WorkflowFinishedEventArgs args) => WorkflowFinished?.Invoke(this, args);
    public void OnWorkflowInstanceSaved(WorkflowInstanceSavedEventArgs args) => WorkflowInstanceSaved?.Invoke(this, args);
}
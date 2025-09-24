using Elsa.Testing.Shared.EventArgs;

namespace Elsa.Testing.Shared.Services;

public class WorkflowEvents
{
    public event EventHandler<WorkflowFinishedEventArgs>? WorkflowFinished;
    public event EventHandler<WorkflowInstanceSavedEventArgs>? WorkflowInstanceSaved;
    public event EventHandler<WorkflowStateCommittedEventArgs>? WorkflowStateCommitted;
    public event EventHandler<ActivityExecutedEventArgs>? ActivityExecuted;
    public event EventHandler<ActivityExecutedLogUpdatedEventArgs>? ActivityExecutedLogUpdated;
    public void OnWorkflowFinished(WorkflowFinishedEventArgs args) => WorkflowFinished?.Invoke(this, args);
    public void OnWorkflowInstanceSaved(WorkflowInstanceSavedEventArgs args) => WorkflowInstanceSaved?.Invoke(this, args);
    public void OnWorkflowStateCommitted(WorkflowStateCommittedEventArgs args) => WorkflowStateCommitted?.Invoke(this, args);
    public void OnActivityExecuted(ActivityExecutedEventArgs args) => ActivityExecuted?.Invoke(this, args);
    public void OnActivityExecutedLogUpdated(ActivityExecutedLogUpdatedEventArgs args) => ActivityExecutedLogUpdated?.Invoke(this, args);
}
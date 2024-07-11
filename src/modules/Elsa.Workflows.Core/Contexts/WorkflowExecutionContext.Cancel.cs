namespace Elsa.Workflows;

/// <summary>
/// Provides context to the currently executing workflow.
/// </summary>
public partial class WorkflowExecutionContext
{
    private ICollection<CancellationTokenSource> _cancellationTokenSources = new List<CancellationTokenSource>();
    private ICollection<CancellationTokenRegistration> _cancellationRegistrations = new List<CancellationTokenRegistration>();

    /// <summary>
    /// Cancels the workflow and all it's children.
    /// </summary>
    public void Cancel()
    {
        foreach (var source in _cancellationTokenSources)
            source.Cancel();
        
        _cancellationTokenSources.Clear();
    }
    
    private void CancelWorkflow()
    {
        Bookmarks.Clear();
        _completionCallbackEntries.Clear();

        if (Status != WorkflowStatus.Running && SubStatus != WorkflowSubStatus.Faulted)
            return;

        AddExecutionLogEntry("Workflow cancelled");
        
        TransitionTo(WorkflowSubStatus.Cancelled);
        
        foreach (var registration in _cancellationRegistrations)
            registration.Dispose();
    }
}
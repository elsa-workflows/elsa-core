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
    
    /// <summary>
    /// Whether the exception is this execution being cancelled through <see cref="Cancel"/>, as opposed to an operation that
    /// was cancelled for another reason (e.g. a timeout), an <see cref="OperationCanceledException"/> thrown without a
    /// cancelled token, or the caller's <see cref="CancellationToken"/> being cancelled (e.g. an aborted HTTP request),
    /// which still ends the workflow as cancelled with an incident.
    /// </summary>
    internal bool IsExecutionCancellation(Exception exception) =>
        exception is OperationCanceledException { CancellationToken.IsCancellationRequested: true }
        && ExecutionCancellationToken.IsCancellationRequested
        && !CancellationToken.IsCancellationRequested;

    private void CancelWorkflow()
    {
        Bookmarks.Clear();
        _completionCallbackEntries.Clear();

        if (!CanTransitionTo(WorkflowSubStatus.Cancelled))
            return;

        AddExecutionLogEntry("Workflow cancelled");
        
        TransitionTo(WorkflowSubStatus.Cancelled);
        
        foreach (var registration in _cancellationRegistrations)
            registration.Dispose();
    }
}
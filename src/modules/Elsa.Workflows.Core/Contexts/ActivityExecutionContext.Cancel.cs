using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Signals;

namespace Elsa.Workflows;

public partial class ActivityExecutionContext
{
    private readonly CancellationTokenRegistration _cancellationRegistration;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly INotificationSender _publisher;

    private void CancelActivity()
    {
        // If the activity is not running, do nothing.
        if (Status != ActivityStatus.Running && Status != ActivityStatus.Faulted)
            return;

        _ = Task.Run(async () => await CancelActivityAsync());
    }
    
    private bool CanCancelActivity()
    {
        return Status is not ActivityStatus.Canceled and not ActivityStatus.Completed;
    }

    private async Task CancelActivityAsync()
    {
        if(!CanCancelActivity())
            return;
        
        // Select all child contexts.
        var childContexts = WorkflowExecutionContext.ActivityExecutionContexts.Where(x => x.ParentActivityExecutionContext == this).ToList();

        foreach (var childContext in childContexts)
            childContext._cancellationTokenSource.Cancel();

        TransitionTo(ActivityStatus.Canceled);
        ClearBookmarks();
        ClearCompletionCallbacks();
        WorkflowExecutionContext.Bookmarks.RemoveWhere(x => x.ActivityNodeId == NodeId);

        // Add an execution log entry.
        AddExecutionLogEntry("Canceled", payload: JournalData);
        
        await _cancellationRegistration.DisposeAsync();
        await this.SendSignalAsync(new CancelSignal());
        
        // ReSharper disable once MethodSupportsCancellation
        await _publisher.SendAsync(new ActivityCancelled(this));
    }
}
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Signals;

namespace Elsa.Workflows;

public partial class ActivityExecutionContext
{
    private readonly INotificationSender _publisher;

    private bool CanCancelActivity()
    {
        return Status is not ActivityStatus.Canceled and not ActivityStatus.Completed;
    }

    /// <summary>
    /// Withdraws work that the engine has not invoked yet and that this activity's cancellation makes obsolete: the
    /// work item that would have started this activity, and the work items this activity scheduled for children whose
    /// execution contexts do not exist yet. Without this, a container could tear a branch down and still have an
    /// activity from that branch execute afterwards, side effects and all.
    /// </summary>
    /// <remarks>
    /// The work items are removed from the scheduler rather than left to be dequeued and discarded, because the
    /// scheduler is not write-only: containers such as <c>Flowchart</c> ask it whether they still have pending work,
    /// and a withdrawn item left in the queue would keep answering yes.
    /// </remarks>
    internal void WithdrawScheduledWork()
    {
        WorkflowExecutionContext.Scheduler.RemoveWhere(workItem => workItem.ExistingActivityExecutionContext == this || workItem.Owner == this);
    }

    private async Task CancelActivityAsync()
    {
        if(!CanCancelActivity())
            return;

        WithdrawScheduledWork();
        TransitionTo(ActivityStatus.Canceled);
        ClearBookmarks();
        ClearCompletionCallbacks();
        WorkflowExecutionContext.Bookmarks.RemoveWhere(x => x.ActivityNodeId == NodeId);
        AddExecutionLogEntry("Canceled");
        await this.SendSignalAsync(new CancelSignal());
        await CancelChildActivitiesAsync();
        
        // ReSharper disable once MethodSupportsCancellation
        await _publisher.SendAsync(new ActivityCancelled(this));
    }
    
    private async Task CancelChildActivitiesAsync()
    {
        var childContexts = WorkflowExecutionContext.ActivityExecutionContexts.Where(x => x.ParentActivityExecutionContext == this && x.CanCancelActivity()).ToList();

        foreach (var childContext in childContexts)
            await childContext.CancelActivityAsync();
    }
}
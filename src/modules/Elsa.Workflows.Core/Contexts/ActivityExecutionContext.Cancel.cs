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

    private async Task CancelActivityAsync()
    {
        if(!CanCancelActivity())
            return;
        
        TransitionTo(ActivityStatus.Canceled);
        ClearBookmarks();
        ClearCompletionCallbacks();
        WorkflowExecutionContext.Bookmarks.RemoveWhere(x => x.ActivityNodeId == NodeId);
        AddExecutionLogEntry("Canceled", payload: JournalData);
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
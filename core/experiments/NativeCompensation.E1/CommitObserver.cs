using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Notifications;

namespace NativeCompensation.E1;

// Deterministic injection only. DefaultCommitStateHandler has completed its real store saves;
// the activity awaiting CommitAsync has not received acknowledgement until this returns.
internal sealed class CommitObserver(Barriers barriers) : INotificationHandler<WorkflowStateCommitted>
{
    public async Task HandleAsync(WorkflowStateCommitted notification, CancellationToken cancellationToken)
    {
        if (!notification.WorkflowState.Properties.ContainsKey(Journal.PropertyKey))
            return;
        var journal = Journal.Read(notification.WorkflowState.Properties);
        Events.Write("native-commit", new
        {
            journal.Revision, journal.Phase, incidents = notification.WorkflowState.Incidents.Count,
            contexts = notification.WorkflowState.ActivityExecutionContexts.Select(x => new { x.Id, x.ParentContextId, x.FaultCount, x.IsExecuting, x.ScheduledActivityNodeId }),
            work = notification.WorkflowState.ScheduledActivities,
            callbacks = notification.WorkflowState.CompletionCallbacks
        });
        var armed = journal.Entries.LastOrDefault(x => x.Status == "Armed" && x.ForwardContextId != null);
        if (armed != null)
            await barriers.ReachAsync($"durable-unacknowledged-reserve-{armed.Slot}", notification.WorkflowExecutionContext);
        else if (journal.Entries.Count == 0 && notification.WorkflowState.ScheduledActivities.Count > 0)
            await barriers.ReachAsync("durable-unacknowledged-scope", notification.WorkflowExecutionContext);
    }
}

using Elsa.Extensions;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Extension methods for workflow dispatch outbox state stored on workflow state.
/// </summary>
public static class WorkflowDispatchOutboxStateExtensions
{
    /// <summary>
    /// The workflow property key used to store committed outbox item IDs.
    /// </summary>
    public const string PropertyKey = "Elsa.Workflows.Runtime.WorkflowDispatchOutbox";

    // Maps the instance ID of each waited-for child dispatched during the current burst to the outbox item that creates it.
    private static readonly object PendingChildDispatchesKey = new();

    /// <summary>
    /// Adds an outbox item ID to the workflow execution context's persisted property bag.
    /// </summary>
    public static void AddWorkflowDispatchOutboxItem(this WorkflowExecutionContext context, string outboxItemId)
    {
        var state = context.Properties.GetWorkflowDispatchOutboxState();

        if (!state.ItemIds.Contains(outboxItemId))
            state.ItemIds.Add(outboxItemId);

        context.Properties[PropertyKey] = state;
    }

    /// <summary>
    /// Removes a delivered outbox item ID from the persisted workflow state property bag.
    /// </summary>
    public static bool RemoveWorkflowDispatchOutboxItem(this WorkflowState workflowState, string outboxItemId)
    {
        return workflowState.Properties.RemoveWorkflowDispatchOutboxItem(outboxItemId);
    }

    /// <summary>
    /// Remembers, for the rest of the current burst, that the specified outbox item creates the specified child instance.
    /// </summary>
    internal static void AddPendingChildDispatch(this WorkflowExecutionContext context, string childInstanceId, string outboxItemId)
    {
        context.TransientProperties.GetOrAdd(PendingChildDispatchesKey, () => new Dictionary<string, string>())[childInstanceId] = outboxItemId;
    }

    /// <summary>
    /// Withdraws the outbox item that creates the specified child, when the current burst enqueued it: the item is
    /// removed from the workflow state and deleted from the outbox. The outbox cannot deliver an item before the parent
    /// commits it, so when the parent has not committed since the dispatch, the child is never created. When it has, for
    /// example under an activity commit strategy, the outbox may already have delivered the item, and withdrawing it
    /// only helps if it has not. Changes nothing when this burst enqueued no such item, for example because the
    /// transactional outbox is off or the child was dispatched in an earlier burst.
    /// </summary>
    internal static async Task WithdrawPendingChildDispatchAsync(this WorkflowExecutionContext context, string childInstanceId, CancellationToken cancellationToken)
    {
        if (!context.TransientProperties.TryGetValue(PendingChildDispatchesKey, out var value)
            || value is not Dictionary<string, string> pendingChildDispatches
            || !pendingChildDispatches.Remove(childInstanceId, out var outboxItemId))
            return;

        // Unclaim the item first: the outbox never delivers an item the owner's committed state does not reference, so an
        // item the parent has not committed stays undelivered even if deleting it below fails. The recurring sweep then
        // deletes it once the orphaned item retention elapses.
        context.Properties.RemoveWorkflowDispatchOutboxItem(outboxItemId);

        try
        {
            await context.ServiceProvider.GetRequiredService<IWorkflowDispatchOutboxStore>().DeleteAsync(outboxItemId, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            context.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(WorkflowDispatchOutbox))
                .LogWarning(e, "Failed to delete withdrawn workflow dispatch outbox item {OutboxItemId} for child workflow {WorkflowInstanceId}; unless the parent committed it before the withdrawal, it is not delivered and is deleted once the orphaned item retention elapses.", outboxItemId, childInstanceId);
        }
    }

    /// <summary>
    /// Returns true when the workflow state includes the specified committed outbox item ID.
    /// </summary>
    public static bool HasWorkflowDispatchOutboxItem(this WorkflowState? workflowState, string outboxItemId)
    {
        return workflowState?.Properties.GetWorkflowDispatchOutboxState().ItemIds.Contains(outboxItemId) == true;
    }

    /// <summary>
    /// Returns true when the workflow state includes committed outbox item IDs.
    /// </summary>
    public static bool HasWorkflowDispatchOutboxItems(this WorkflowState? workflowState)
    {
        return workflowState?.Properties.GetWorkflowDispatchOutboxState().ItemIds.Count > 0;
    }

    private static bool RemoveWorkflowDispatchOutboxItem(this IDictionary<string, object> properties, string outboxItemId)
    {
        var state = properties.GetWorkflowDispatchOutboxState();

        if (!state.ItemIds.Remove(outboxItemId))
            return false;

        if (state.ItemIds.Count == 0)
            properties.Remove(PropertyKey);
        else
            properties[PropertyKey] = state;

        return true;
    }

    private static WorkflowDispatchOutboxState GetWorkflowDispatchOutboxState(this IDictionary<string, object>? properties)
    {
        return properties?.TryGetValue<WorkflowDispatchOutboxState>(PropertyKey, out var state) == true ? state : new();
    }
}

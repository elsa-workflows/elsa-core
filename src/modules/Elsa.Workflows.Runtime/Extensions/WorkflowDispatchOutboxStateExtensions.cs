using Elsa.Extensions;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.State;

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

    private static WorkflowDispatchOutboxState GetWorkflowDispatchOutboxState(this IDictionary<string, object>? properties)
    {
        return properties?.TryGetValue<WorkflowDispatchOutboxState>(PropertyKey, out var state) == true ? state : new();
    }
}

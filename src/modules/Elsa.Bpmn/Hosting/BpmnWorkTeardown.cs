using Elsa.Extensions;
using Elsa.Workflows;

namespace Elsa.Bpmn.Hosting;

/// <summary>
/// Stops a unit of work and everything it in turn started.
/// </summary>
/// <remarks>
/// Both places that tear work down go through here: the interpreter's <c>CancelWorkSubtree</c> command, and a scope
/// terminalizing the unit of work whose fault it just claimed. The mechanism is the same in either case.
/// </remarks>
internal static class BpmnWorkTeardown
{
    /// <summary>The activity execution with the given id, or <c>null</c> when it has already gone.</summary>
    public static ActivityExecutionContext? FindContext(WorkflowExecutionContext workflowExecutionContext, string activityExecutionContextId) =>
        workflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => string.Equals(x.Id, activityExecutionContextId, StringComparison.Ordinal));

    /// <summary>
    /// Tears down a unit of work and everything underneath it.
    /// </summary>
    /// <remarks>
    /// Cancellation itself needs no BPMN-specific code: the public <c>CancelActivityAsync</c> extension walks the
    /// child subtree recursively, which is exactly what "and everything it in turn started" means, and it withdraws
    /// the scheduled-but-not-yet-invoked work items in that subtree so nothing from the destroyed branch runs
    /// afterwards. The only thing this method adds is the reason, recorded on the torn-down activity's journal
    /// before it goes, so the workflow's execution log says which BPMN element destroyed the branch and why.
    /// </remarks>
    public static async ValueTask CancelSubtreeAsync(ActivityExecutionContext childContext, string reason)
    {
        childContext.AddExecutionLogEntry("Torn down by BPMN", reason);
        await childContext.CancelActivityAsync();
    }
}

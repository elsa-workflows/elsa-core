using Elsa.Extensions;
using Elsa.Workflows;

namespace Elsa.Bpmn.Hosting;

/// <summary>
/// Stops a unit of work and everything it in turn started.
/// </summary>
/// <remarks>
/// Both places that tear work down go through here: the interpreter's <c>CancelWorkSubtree</c> command, and a scope
/// terminalizing the unit of work whose fault it just claimed. The mechanism is the same in either case, and so is the
/// one thing this host cannot do.
/// </remarks>
internal static class BpmnWorkTeardown
{
    /// <summary>The activity execution with the given id, or <c>null</c> when it has already gone.</summary>
    public static ActivityExecutionContext? FindContext(WorkflowExecutionContext workflowExecutionContext, string activityExecutionContextId) =>
        workflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => string.Equals(x.Id, activityExecutionContextId, StringComparison.Ordinal));

    /// <summary>
    /// Tears down a unit of work and everything underneath it, or refuses when it cannot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Cancellation itself needs no new code: the public <c>CancelActivityAsync</c> extension already walks the child
    /// subtree recursively, which is exactly what "and everything it in turn started" means.
    /// </para>
    /// <para>
    /// What it cannot do is withdraw work whose activity is scheduled but has not been invoked yet. Such work has no
    /// running execution to cancel, and <c>IActivityScheduler</c> offers no way to remove a queued work item, so the
    /// activity would run after BPMN destroyed the branch it belongs to — leaving two live branches where the model
    /// says one, with nothing in the resulting state to say so. Refusing loudly is the lesser of the two evils the
    /// library itself ranks: a fault now, rather than a silent divergence surfacing as a doubled payment weeks later.
    /// </para>
    /// </remarks>
    public static async ValueTask CancelSubtreeAsync(ActivityExecutionContext childContext, string reason)
    {
        var scheduler = childContext.WorkflowExecutionContext.Scheduler;
        var subtree = new[] { childContext }.Concat(childContext.GetDescendants());
        var queued = subtree.FirstOrDefault(context => scheduler.Any(item => item.ExistingActivityExecutionContext?.Id == context.Id));

        if (queued is not null)
        {
            throw new NotSupportedException(
                $"BPMN asked to tear down the work of activity '{childContext.Activity.Id}' ({reason}), but activity "
                + $"'{queued.Activity.Id}' inside that subtree is scheduled and has not started, and a scheduled work "
                + "item cannot be withdrawn. Running it anyway would leave a branch alive that BPMN destroyed.");
        }

        await childContext.CancelActivityAsync();
    }
}

using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.Signals;

namespace Elsa.Workflows.Core.Behaviors;

/// <summary>
/// Implements a "break" behavior that handles the <see cref="BreakSignal"/> signal.
/// </summary>
public class ScheduledChildCallbackBehavior : Behavior
{
    public ScheduledChildCallbackBehavior()
    {
        OnSignalReceived<ActivityCompleted>(OnChildActivityCompletedAsync);
    }

    private async ValueTask OnChildActivityCompletedAsync(ActivityCompleted signal, SignalContext context)
    {
        var activityExecutionContext = context.ActivityExecutionContext;
        var childActivityExecutionContext = context.SourceActivityExecutionContext;
        var childActivity = childActivityExecutionContext.Activity;
        var callbackEntry = activityExecutionContext.WorkflowExecutionContext.PopCompletionCallback(activityExecutionContext, childActivity);

        if (callbackEntry == null)
            return;

        await callbackEntry(activityExecutionContext, childActivityExecutionContext);
    }
}
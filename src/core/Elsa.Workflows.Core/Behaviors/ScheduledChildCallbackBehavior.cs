using Elsa.Models;
using Elsa.Services;
using Elsa.Signals;

namespace Elsa.Behaviors;

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
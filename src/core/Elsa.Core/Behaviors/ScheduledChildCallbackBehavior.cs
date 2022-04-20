using Elsa.Contracts;
using Elsa.Models;
using Elsa.Signals;

namespace Elsa.Behaviors;

/// <summary>
/// Implements a "break" behavior that handles the <see cref="BreakSignal"/> signal.
/// </summary>
public class ScheduledChildCallbackBehavior : IBehavior
{
    public async ValueTask HandleSignalAsync(object signal, SignalContext context)
    {
        if (signal is not ActivityCompleted)
            return;

        await OnChildActivityCompletedAsync(context);
    }

    public ValueTask ExecuteAsync(ActivityExecutionContext context) => ValueTask.CompletedTask;

    private async ValueTask OnChildActivityCompletedAsync(SignalContext context)
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
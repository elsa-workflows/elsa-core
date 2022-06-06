using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.Signals;

namespace Elsa.Workflows.Core.Behaviors;

/// <summary>
/// Implements a behavior that invokes "child completed" callbacks on parent activities.
/// </summary>
public class ScheduledChildCallbackBehavior : Behavior
{
    public ScheduledChildCallbackBehavior(IActivity owner) : base(owner)
    {
        OnSignalReceived<ActivityCompleted>(OnActivityCompletedAsync);
    }

    private async ValueTask OnActivityCompletedAsync(ActivityCompleted signal, SignalContext context)
    {
        var activityExecutionContext = context.ReceiverActivityExecutionContext;
        var childActivityExecutionContext = context.SenderActivityExecutionContext;
        var childActivity = childActivityExecutionContext.Activity;
        var callbackEntry = activityExecutionContext.WorkflowExecutionContext.PopCompletionCallback(activityExecutionContext, childActivity);

        if (callbackEntry == null)
            return;

        // Before invoking the parent activity, make sure its properties are evaluated.
        await activityExecutionContext.EvaluateInputPropertiesAsync();
        await callbackEntry(activityExecutionContext, childActivityExecutionContext);
    }
}
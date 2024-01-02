using Elsa.Extensions;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Signals;

namespace Elsa.Workflows.Behaviors;

/// <summary>
/// Implements a behavior that invokes "child completed" callbacks on parent activities.
/// </summary>
public class ScheduledChildCallbackBehavior : Behavior
{
    /// <inheritdoc />
    public ScheduledChildCallbackBehavior(IActivity owner) : base(owner)
    {
        OnSignalReceived<ActivityCompleted>(OnActivityCompletedAsync);
    }

    private async ValueTask OnActivityCompletedAsync(ActivityCompleted signal, SignalContext context)
    {
        var activityExecutionContext = context.ReceiverActivityExecutionContext;
        var childActivityExecutionContext = context.SenderActivityExecutionContext;
        var childActivityNode = childActivityExecutionContext.ActivityNode;
        var callbackEntry = activityExecutionContext.WorkflowExecutionContext.PopCompletionCallback(activityExecutionContext, childActivityNode);

        if (callbackEntry == null)
            return;

        // Before invoking the parent activity, make sure its properties are evaluated.
        if (!activityExecutionContext.GetHasEvaluatedProperties())
            await activityExecutionContext.EvaluateInputPropertiesAsync();

        if (callbackEntry.CompletionCallback != null)
        {
            var completedContext = new ActivityCompletedContext(activityExecutionContext, childActivityExecutionContext, signal.Result);
            var tag = callbackEntry.Tag;
            completedContext.TargetContext.Tag = tag;
            await callbackEntry.CompletionCallback(completedContext);
        }
    }
}
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Notifications;
using JetBrains.Annotations;
using ActivityCompleted = Elsa.Workflows.Signals.ActivityCompleted;

namespace Elsa.Workflows.Behaviors;

/// <summary>
/// Implements a behavior that invokes "child completed" callbacks on parent activities.
/// </summary>
[UsedImplicitly]
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

        if (callbackEntry?.CompletionCallback != null)
        {
            var completedContext = new ActivityCompletedContext(activityExecutionContext, childActivityExecutionContext, signal.Result);
            var tag = callbackEntry.Tag;
            completedContext.TargetContext.Tag = tag;
            
            var mediator = activityExecutionContext.GetRequiredService<IMediator>();
            var invokingActivityCallbackNotification = new InvokingActivityCallback(activityExecutionContext, childActivityExecutionContext);
            await mediator.SendAsync(invokingActivityCallbackNotification, context.CancellationToken);
            
            await callbackEntry.CompletionCallback(completedContext);
        }
    }
}
using Elsa.Expressions.Models;
using Elsa.Extensions;

namespace Elsa.Workflows.Runtime.Activities;

public abstract class EventBase : Trigger<object>;

public abstract class EventBase<TResult> : Trigger<TResult>
{
    protected abstract string GetEventName(ExpressionExecutionContext context);

    protected virtual ValueTask OnEventReceivedAsync(ActivityExecutionContext context, TResult? input)
    {
        OnEventReceived(context, input);
        return default;
    }

    protected virtual void OnEventReceived(ActivityExecutionContext context, TResult? input)
    {
    }

    protected override object GetTriggerPayload(TriggerIndexingContext context)
    {
        var eventName = GetEventName(context.ExpressionExecutionContext);
        return context.GetEventStimulus(eventName);
    }
    
    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var eventName = GetEventName(context.ExpressionExecutionContext);
        context.WaitForEvent(eventName, EventReceivedAsync);
        return default;
    }

    protected async ValueTask EventReceivedAsync(ActivityExecutionContext context)
    {
        var input = (TResult?)context.GetEventInput();
        Result.Set(context, input);
        await context.CompleteActivityAsync();
    }
}
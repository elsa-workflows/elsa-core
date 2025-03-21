using Elsa.Extensions;
using Elsa.Workflows;

namespace Elsa.Server.Web.Activities;

public class CustomEvent : Trigger<object>
{
    protected override object GetTriggerPayload(TriggerIndexingContext context)
    {
        return context.GetEventStimulus("MyEvent");
    }
    
    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        context.WaitForEvent("MyEvent", OnEventReceivedAsync);
        return default;
    }

    private async ValueTask OnEventReceivedAsync(ActivityExecutionContext context)
    {
        context.SetResult(context.GetEventInput());
        Console.WriteLine("Event received");
        await context.CompleteActivityAsync();
    }
}
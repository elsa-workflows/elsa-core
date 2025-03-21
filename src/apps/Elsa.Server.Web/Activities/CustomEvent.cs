using Elsa.Expressions.Models;
using Elsa.Workflows;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Server.Web.Activities;

public class CustomEvent : EventBase<object>
{
    protected override string GetEventName(ExpressionExecutionContext context)
    {
        return "MyEvent";
    }
    
    protected override void OnEventReceived(ActivityExecutionContext context, object? eventData)
    {
        Console.WriteLine("Event received with data: " + eventData);
    }
}
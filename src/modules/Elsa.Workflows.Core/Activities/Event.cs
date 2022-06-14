using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Activities;

[Activity("Elsa", "Primitives", "Wait for an event to be triggered.")]
public class Event : Trigger<object?>
{
    [JsonConstructor]
    public Event()
    {
    }

    public Event(string eventName) : this(new Literal<string>(eventName))
    {
    }

    public Event(Func<string> text) : this(new DelegateBlockReference<string>(text))
    {
    }

    public Event(Func<ExpressionExecutionContext, string?> text) : this(new DelegateBlockReference<string?>(text))
    {
    }

    public Event(Variable<string> variable) => EventName = new Input<string>(variable);
    public Event(Literal<string> literal) => EventName = new Input<string>(literal);
    public Event(DelegateBlockReference delegateBlockExpression) => EventName = new Input<string>(delegateBlockExpression);
    public Event(Input<string> eventName) => EventName = eventName;

    [Input] public Input<string> EventName { get; set; } = default!;

    protected override void Execute(ActivityExecutionContext context)
    {
        var eventName = context.Get(EventName)!;
        context.CreateBookmark(new EventBookmarkData(eventName));
    }
}
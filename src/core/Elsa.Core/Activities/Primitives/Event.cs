using Elsa.Attributes;
using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Activities.Primitives;

public class Event : Activity
{
    public Event()
    {
    }

    public Event(string eventName) : this(new Literal<string>(eventName))
    {
    }

    public Event(Func<string> text) : this(new DelegateReference<string>(text))
    {
    }

    public Event(Func<ExpressionExecutionContext, string?> text) : this(new DelegateReference<string?>(text))
    {
    }

    public Event(Variable<string> variable) => EventName = new Input<string>(variable);
    public Event(Literal<string> literal) => EventName = new Input<string>(literal);
    public Event(DelegateReference delegateExpression) => EventName = new Input<string>(delegateExpression);
    public Event(Input<string> eventName) => EventName = eventName;

    [Input] public Input<string> EventName { get; set; } = default!;
    [Output] public Output<object?>? Payload { get; set; }

    protected override void Execute(ActivityExecutionContext context)
    {
        var hasher = context.GetRequiredService<IHasher>();
        var eventName = context.Get(EventName)!;
        var hash = hasher.Hash(eventName);
        context.SetBookmark(hash);
    }
}
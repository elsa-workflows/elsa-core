using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Wait for an event to be triggered.
/// </summary>
[Activity("Elsa", "Primitives", "Wait for an event to be triggered.")]
public class Event : Trigger<object?>
{
    /// <inheritdoc />
    [JsonConstructor]
    public Event()
    {
    }

    /// <inheritdoc />
    public Event(string eventName) : this(new Literal<string>(eventName))
    {
    }

    /// <inheritdoc />
    public Event(Func<string> text) : this(new DelegateBlockReference<string>(text))
    {
    }

    /// <inheritdoc />
    public Event(Func<ExpressionExecutionContext, string?> text) : this(new DelegateBlockReference<string?>(text))
    {
    }

    /// <inheritdoc />
    public Event(Variable<string> variable) => EventName = new Input<string>(variable);

    /// <inheritdoc />
    public Event(Literal<string> literal) => EventName = new Input<string>(literal);

    /// <inheritdoc />
    public Event(DelegateBlockReference delegateBlockExpression) => EventName = new Input<string>(delegateBlockExpression);

    /// <inheritdoc />
    public Event(Input<string> eventName) => EventName = eventName;

    /// <summary>
    /// The name of the event to listen for.
    /// </summary>
    [Input(Description = "The name of the event to listen for.")] public Input<string> EventName { get; set; } = default!;

    /// <inheritdoc />
    protected override object GetTriggerPayload(TriggerIndexingContext context)
    {
        var eventName = EventName.Get(context.ExpressionExecutionContext);
        return new EventBookmarkPayload(eventName);
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var eventName = context.Get(EventName)!;
        
        if(!context.IsTriggerOfWorkflow())
        {
            context.CreateBookmark(new EventBookmarkPayload(eventName));
            return;
        }

        await context.CompleteActivityAsync();
    }
}
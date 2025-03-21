using System.Runtime.CompilerServices;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Activities;

/// <summary>
/// Wait for an event to be triggered.
/// </summary>
[Activity("Elsa", "Primitives", "Wait for an event to be published.")]
[UsedImplicitly]
public class Event : Trigger<object?>
{
    internal const string EventInputWorkflowInputKey = "__EventPayloadWorkflowInput";

    /// <inheritdoc />
    internal Event([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <inheritdoc />
    public Event(string eventName, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null)
        : this(new Literal<string>(eventName), source, line)
    {
    }

    /// <inheritdoc />
    public Event(Func<string> eventName, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null)
        : this(new Input<string>(Expression.DelegateExpression(eventName), new()), source, line)
    {
    }

    /// <inheritdoc />
    public Event(Func<ExpressionExecutionContext, string?> eventName, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null)
        : this(new Input<string>(Expression.DelegateExpression(eventName), new()), source, line)
    {
    }

    /// <inheritdoc />
    public Event(Variable<string> variable, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : this(source, line)
    {
        EventName = new(variable);
    }

    /// <inheritdoc />
    public Event(Literal<string> literal, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : this(source, line)
    {
        EventName = new(literal);
    }

    /// <inheritdoc />
    public Event(Input<string> eventName, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : this(source, line)
    {
        EventName = eventName;
    }

    /// <summary>
    /// The name of the event to listen for.
    /// </summary>
    [Input(Description = "The name of the event to listen for.")]
    public Input<string> EventName { get; set; } = null!;

    /// <inheritdoc />
    protected override object GetTriggerPayload(TriggerIndexingContext context)
    {
        var eventName = EventName.Get(context.ExpressionExecutionContext);
        return context.GetEventStimulus(eventName);
    }

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        var eventName = context.Get(EventName)!;
        context.WaitForEvent(eventName, CompleteInternalAsync);
    }

    private async ValueTask CompleteInternalAsync(ActivityExecutionContext context)
    {
        context.SetResult(context.GetEventInput());
        await context.CompleteActivityAsync();
    }
}
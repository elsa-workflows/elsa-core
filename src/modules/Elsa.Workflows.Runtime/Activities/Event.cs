using System.Runtime.CompilerServices;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Stimuli;
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
    internal Event([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    public Event(string eventName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
        : this(new Literal<string>(eventName), source, line)
    {
    }

    /// <inheritdoc />
    public Event(Func<string> eventName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
        : this(new Input<string>(Expression.DelegateExpression(eventName), new MemoryBlockReference()), source, line)
    {
    }

    /// <inheritdoc />
    public Event(Func<ExpressionExecutionContext, string?> eventName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
        : this(new Input<string>(Expression.DelegateExpression(eventName), new MemoryBlockReference()), source, line)
    {
    }

    /// <inheritdoc />
    public Event(Variable<string> variable, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line) =>
        EventName = new Input<string>(variable);

    /// <inheritdoc />
    public Event(Literal<string> literal, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line) =>
        EventName = new Input<string>(literal);

    /// <inheritdoc />
    public Event(Input<string> eventName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line) => EventName = eventName;

    /// <summary>
    /// The name of the event to listen for.
    /// </summary>
    [Input(Description = "The name of the event to listen for.")]
    public Input<string> EventName { get; set; } = default!;

    /// <inheritdoc />
    protected override object GetTriggerPayload(TriggerIndexingContext context)
    {
        var eventName = EventName.Get(context.ExpressionExecutionContext);
        return new EventStimulus(eventName);
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var eventName = context.Get(EventName)!;

        if (!context.IsTriggerOfWorkflow())
        {
            var options = new CreateBookmarkArgs
            {
                Stimulus = new EventStimulus(eventName),
                IncludeActivityInstanceId = false,
                Callback = CompleteInternalAsync
            };
            context.CreateBookmark(options);
            return;
        }

        await CompleteInternalAsync(context);
    }

    private async ValueTask CompleteInternalAsync(ActivityExecutionContext context)
    {
        if (context.TryGetWorkflowInput<object?>(EventInputWorkflowInputKey, out var input))
            context.SetResult(input);

        await context.CompleteActivityAsync();
    }
}
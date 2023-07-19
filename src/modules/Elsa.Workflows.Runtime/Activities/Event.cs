using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Bookmarks;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Activities;

/// <summary>
/// Wait for an event to be triggered.
/// </summary>
[Activity("Elsa", "Primitives", "Wait for an event to be published.")]
[PublicAPI]
public class Event : Trigger<object?>
{
    /// <inheritdoc />
    [JsonConstructor]
    public Event()
    {
    }
    
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
        : this(new DelegateBlockReference<string>(eventName), source, line)
    {
    }

    /// <inheritdoc />
    public Event(Func<ExpressionExecutionContext, string?> eventName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
        : this(new DelegateBlockReference<string?>(eventName), source, line)
    {
    }

    /// <inheritdoc />
    public Event(Variable<string> variable, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line) =>
        EventName = new Input<string>(variable);

    /// <inheritdoc />
    public Event(Literal<string> literal, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line) =>
        EventName = new Input<string>(literal);

    /// <inheritdoc />
    public Event(DelegateBlockReference delegateBlockExpression, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line) =>
        EventName = new Input<string>(delegateBlockExpression);

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
        return new EventBookmarkPayload(eventName);
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var eventName = context.Get(EventName)!;

        if (!context.IsTriggerOfWorkflow())
        {
            context.CreateBookmark(new EventBookmarkPayload(eventName));
            return;
        }

        await context.CompleteActivityAsync();
    }
}
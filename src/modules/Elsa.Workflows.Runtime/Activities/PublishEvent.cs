using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Contracts;
using JetBrains.Annotations;
using System.Runtime.CompilerServices;
using Elsa.Workflows.Core;

namespace Elsa.Workflows.Runtime.Activities;

/// <summary>
/// Faults the workflow.
/// </summary>
[Activity("Elsa", "Primitives", "Publishes an event.")]
[PublicAPI]
public class PublishEvent : Activity
{
    /// <inheritdoc />
    public PublishEvent([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <summary>
    /// The name of the event to publish.
    /// </summary>
    [Input(Description = "The name of the event to publish.")]
    public Input<string> EventName { get; set; } = default!;

    /// <summary>
    /// The correlation ID to scope the event to.
    /// </summary>
    [Input(Description = "The correlation ID to scope the event to.")]
    public Input<string?> CorrelationId { get; set; } = default!;

    /// <summary>
    /// Whether the event is local to the workflow.
    /// </summary>
    [Input(DisplayName = "Local event", Description = "Whether the event is local to the workflow. When checked, the event will be delivered to this workflow instance only.")]
    public Input<bool> IsLocalEvent { get; set; } = default!;

    /// <summary>
    /// The input to send as the event body.
    /// </summary>
    [Input(Description = "The input to send as the event body.")]
    public Input<IDictionary<string, object>?> Input { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var eventName = EventName.Get(context);
        var correlationId = CorrelationId.GetOrDefault(context);
        var isLocalEvent = IsLocalEvent.GetOrDefault(context);
        var workflowInstanceId = isLocalEvent ? context.WorkflowExecutionContext.Id : default;
        var input = Input.GetOrDefault(context);
        var publisher = context.GetRequiredService<IEventPublisher>();

        await publisher.DispatchAsync(eventName, correlationId, workflowInstanceId, null, input, context.CancellationToken);
        await context.CompleteActivityAsync();
    }
}
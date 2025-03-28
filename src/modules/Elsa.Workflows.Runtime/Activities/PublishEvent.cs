using Elsa.Extensions;
using JetBrains.Annotations;
using System.Runtime.CompilerServices;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.Activities;

/// <summary>
/// Faults the workflow.
/// </summary>
[Activity("Elsa", "Primitives", "Publishes an event.")]
[UsedImplicitly]
public class PublishEvent([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : Activity(source, line)
{
    /// <summary>
    /// The name of the event to publish.
    /// </summary>
    [Input(Description = "The name of the event to publish.")]
    public Input<string> EventName { get; set; } = null!;

    /// <summary>
    /// The correlation ID to scope the event to.
    /// </summary>
    [Input(Description = "The correlation ID to scope the event to.")]
    public Input<string?> CorrelationId { get; set; } = null!;

    /// <summary>
    /// Whether the event is local to the workflow.
    /// </summary>
    [Input(DisplayName = "Local event", Description = "Whether the event is local to the workflow. When checked, the event will be delivered to this workflow instance only.")]
    public Input<bool> IsLocalEvent { get; set; } = null!;

    [Input(Description = "Whether the event should be delivered synchronously or asynchronously.", DefaultValue = true)]
    public Input<bool> Asynchronous { get; set; } = new(true);

    /// <summary>
    /// The input to send as the event body.
    /// </summary>
    [Input(Description = "The payload to send as the event body.")]
    public Input<object> Payload { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var eventName = EventName.Get(context);
        var correlationId = CorrelationId.GetOrDefault(context);
        var isLocalEvent = IsLocalEvent.GetOrDefault(context);
        var asynchronous = Asynchronous.GetOrDefault(context, () => true);
        var workflowInstanceId = isLocalEvent ? context.WorkflowExecutionContext.Id : null;
        var payload = Payload.GetOrDefault(context);
        var publisher = context.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(eventName, correlationId, workflowInstanceId, null, payload, asynchronous, context.CancellationToken);
        await context.CompleteActivityAsync();
    }
}
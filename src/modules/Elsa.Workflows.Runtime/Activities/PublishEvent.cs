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

    /// <summary>
    /// The input to send as the event body.
    /// </summary>
    /// <remarks>
    /// The payload is persisted as part of the receiving workflow's state, and only types with a registered
    /// serialization alias survive that round trip as themselves. A payload whose type has no alias — an anonymous
    /// type, or any POCO that was never registered — is stored without a type discriminator and read back as a
    /// property bag whose keys carry the state serializer's camel-case naming policy, so a consumer looking for
    /// <c>Status</c> will find <c>status</c>. Register the type during startup with
    /// <c>AddTypeAlias&lt;TPayload&gt;()</c> to keep it intact, or pass a <c>Dictionary&lt;string, object&gt;</c>,
    /// whose keys are stored verbatim, when a property bag is what you intend.
    /// </remarks>
    [Input(Description = "The payload to send as the event body.")]
    public Input<object> Payload { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var eventName = EventName.Get(context);
        var correlationId = CorrelationId.GetOrDefault(context).NullIfWhiteSpace();
        var isLocalEvent = IsLocalEvent.GetOrDefault(context);
        var workflowInstanceId = isLocalEvent ? context.WorkflowExecutionContext.Id : null;
        var payload = Payload.GetOrDefault(context);
        var publisher = context.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(eventName, correlationId, workflowInstanceId, null, payload, true, context.CancellationToken);
        await context.CompleteActivityAsync();
    }
}
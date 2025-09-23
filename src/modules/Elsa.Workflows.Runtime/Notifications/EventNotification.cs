using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// A notification wraping an event that applications can subscribe to.
/// </summary>
/// <param name="EventName">The name of the event.</param>
/// <param name="CorrelationId">The correlation id of the event.</param>
/// <param name="WorkflowInstanceId">The ID of the workflow instance which published the event.</param>
/// <param name="ActivityInstanceId">The ID of the activity instance which published the event.</param>
/// <param name="Payload">The payload of the event.</param>
/// <param name="Asynchronous">Whether the event is requested to be published as asynchronous or not.</param>
public record EventNotification(string EventName, string? CorrelationId, string? WorkflowInstanceId, string? ActivityInstanceId, object? Payload, bool Asynchronous) : INotification;
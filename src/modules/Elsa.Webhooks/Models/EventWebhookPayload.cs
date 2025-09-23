namespace Elsa.Webhooks.Models;

/// <summary>
/// Stores payload information about the Event webhook event type.
/// </summary>
public record EventWebhookPayload(
    string EventName, 
    string? CorrelationId, 
    string? WorkflowInstanceId, 
    string? ActivityInstanceId, 
    object? Payload);
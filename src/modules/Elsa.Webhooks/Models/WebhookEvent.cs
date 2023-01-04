namespace Elsa.Webhooks.Models;

/// <summary>
/// A payload sent to a webhook endpoint.
/// </summary>
public record WebhookEvent(string EventType, object Payload, DateTimeOffset Timestamp);
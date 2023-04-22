namespace Elsa.Webhooks.Models;

/// <summary>
/// A payload sent to a webhook url.
/// </summary>
public record WebhookEvent(string EventType, object? Payload, DateTimeOffset Timestamp);
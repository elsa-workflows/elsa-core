namespace Elsa.Samples.Webhooks.ExternalApp.Models;

public record WebhookEvent<T>(string EventType, T Payload);
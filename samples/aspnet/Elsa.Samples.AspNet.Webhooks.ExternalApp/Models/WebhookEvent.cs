namespace Elsa.Samples.AspNet.Webhooks.ExternalApp.Models;

public record WebhookEvent<T>(string EventType, T Payload);
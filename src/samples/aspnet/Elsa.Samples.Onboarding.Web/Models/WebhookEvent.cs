namespace Elsa.Samples.Onboarding.Web.Models;

public record WebhookEvent(string EventType, RunTaskWebhook Payload, DateTimeOffset Timestamp);
namespace Elsa.Samples.AspNet.Onboarding.Web.Models;

public record WebhookEvent(string EventType, RunTaskWebhook Payload, DateTimeOffset Timestamp);
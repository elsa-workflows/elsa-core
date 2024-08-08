using Elsa.Mediator.Contracts;
using WebhooksCore;

namespace Elsa.Webhooks.Notifications;

public record WebhookEventReceived(WebhookEvent WebhookEvent, WebhookSource WebhookSource) : INotification;
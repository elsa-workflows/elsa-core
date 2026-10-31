using Elsa.Mediator.Contracts;
using WebhooksCore;

namespace Elsa.Http.Webhooks.Notifications;

public record WebhookEventReceived(WebhookEvent WebhookEvent, WebhookSource WebhookSource) : INotification;

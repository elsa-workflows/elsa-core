using Elsa.Webhooks.Models;
using MediatR;

namespace Elsa.Webhooks.Events
{
    public abstract class WebhookDefinitionNotification : INotification
    {
        public WebhookDefinitionNotification(WebhookDefinition webhookDefinition) => WebhookDefinition = webhookDefinition;
        public WebhookDefinition WebhookDefinition { get; }
    }
}
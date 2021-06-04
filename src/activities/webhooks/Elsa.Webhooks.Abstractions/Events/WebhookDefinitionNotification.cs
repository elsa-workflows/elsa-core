using Elsa.Webhooks.Abstractions.Models;
using MediatR;

namespace Elsa.Webhooks.Abstractions.Events
{
    public abstract class WebhookDefinitionNotification : INotification
    {
        public WebhookDefinitionNotification(WebhookDefinition webhookDefinition) => WebhookDefinition = webhookDefinition;
        public WebhookDefinition WebhookDefinition { get; }
    }
}
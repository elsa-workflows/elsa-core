using Elsa.Webhooks.Abstractions.Models;

namespace Elsa.Webhooks.Abstractions.Events
{
    public class WebhookDefinitionDeleted : WebhookDefinitionNotification
    {
        public WebhookDefinitionDeleted(WebhookDefinition webhookDefinition) : base(webhookDefinition)
        {
        }
    }
}
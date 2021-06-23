using Elsa.Webhooks.Models;

namespace Elsa.Webhooks.Events
{
    public class WebhookDefinitionDeleted : WebhookDefinitionNotification
    {
        public WebhookDefinitionDeleted(WebhookDefinition webhookDefinition) : base(webhookDefinition)
        {
        }
    }
}
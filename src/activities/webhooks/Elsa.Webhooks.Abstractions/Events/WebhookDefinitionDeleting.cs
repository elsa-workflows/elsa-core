using Elsa.Webhooks.Models;

namespace Elsa.Webhooks.Events
{
    public class WebhookDefinitionDeleting : WebhookDefinitionNotification
    {
        public WebhookDefinitionDeleting(WebhookDefinition webhookDefinition) : base(webhookDefinition)
        {
        }
    }
}
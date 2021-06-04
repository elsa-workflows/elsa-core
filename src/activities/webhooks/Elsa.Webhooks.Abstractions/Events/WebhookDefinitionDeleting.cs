using Elsa.Webhooks.Abstractions.Models;

namespace Elsa.Webhooks.Abstractions.Events
{
    public class WebhookDefinitionDeleting : WebhookDefinitionNotification
    {
        public WebhookDefinitionDeleting(WebhookDefinition webhookDefinition) : base(webhookDefinition)
        {
        }
    }
}
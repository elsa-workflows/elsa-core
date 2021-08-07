using Elsa.Webhooks.Models;

namespace Elsa.Webhooks.Events
{
    public class WebhookDefinitionSaving : WebhookDefinitionNotification
    {
        public WebhookDefinitionSaving(WebhookDefinition webhookDefinition) : base(webhookDefinition)
        {
        }
    }
}
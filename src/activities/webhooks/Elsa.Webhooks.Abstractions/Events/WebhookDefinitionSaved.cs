using Elsa.Webhooks.Models;

namespace Elsa.Webhooks.Events
{
    public class WebhookDefinitionSaved : WebhookDefinitionNotification
    {
        public WebhookDefinitionSaved(WebhookDefinition webhookDefinition) : base(webhookDefinition)
        {
        }
    }
}
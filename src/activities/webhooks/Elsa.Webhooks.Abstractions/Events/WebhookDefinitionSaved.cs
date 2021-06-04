using Elsa.Webhooks.Abstractions.Models;

namespace Elsa.Webhooks.Abstractions.Events
{
    public class WebhookDefinitionSaved : WebhookDefinitionNotification
    {
        public WebhookDefinitionSaved(WebhookDefinition webhookDefinition) : base(webhookDefinition)
        {
        }
    }
}
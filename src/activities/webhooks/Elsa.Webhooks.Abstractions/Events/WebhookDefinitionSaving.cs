using Elsa.Webhooks.Abstractions.Models;

namespace Elsa.Webhooks.Abstractions.Events
{
    public class WebhookDefinitionSaving : WebhookDefinitionNotification
    {
        public WebhookDefinitionSaving(WebhookDefinition webhookDefinition) : base(webhookDefinition)
        {
        }
    }
}
using System.Collections.Generic;
using Elsa.Webhooks.Models;
using MediatR;

namespace Elsa.Webhooks.Events
{
    public class ManyWebhookDefinitionsDeleting : INotification
    {
        public ManyWebhookDefinitionsDeleting(IEnumerable<WebhookDefinition> webhookDefinitions) => WebhookDefinitions = webhookDefinitions;
        public IEnumerable<WebhookDefinition> WebhookDefinitions { get; }
    }
}
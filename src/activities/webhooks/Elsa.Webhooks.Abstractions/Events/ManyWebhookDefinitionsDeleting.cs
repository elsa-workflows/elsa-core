using System.Collections.Generic;
using Elsa.Webhooks.Abstractions.Models;
using MediatR;

namespace Elsa.Webhooks.Abstractions.Events
{
    public class ManyWebhookDefinitionsDeleting : INotification
    {
        public ManyWebhookDefinitionsDeleting(IEnumerable<WebhookDefinition> webhookDefinitions) => WebhookDefinitions = webhookDefinitions;
        public IEnumerable<WebhookDefinition> WebhookDefinitions { get; }
    }
}
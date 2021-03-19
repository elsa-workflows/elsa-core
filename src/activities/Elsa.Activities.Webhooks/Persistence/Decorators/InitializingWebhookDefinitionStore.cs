using Elsa.Activities.Webhooks.Models;
using Elsa.Services;

namespace Elsa.Activities.Webhooks.Persistence.Decorators
{
    public class InitializingWebhookDefinitionStore : InitializingStoreBase<WebhookDefinition>, IWebhookDefinitionStore
    {
        public InitializingWebhookDefinitionStore(IWebhookDefinitionStore store, IIdGenerator idGenerator)
            : base(store, idGenerator)
        {
        }

        protected override WebhookDefinition Initialize(WebhookDefinition webhookDefinition)
        {
            if (string.IsNullOrWhiteSpace(webhookDefinition.Id))
                webhookDefinition.Id = IdGenerator.Generate();

            return webhookDefinition;
        }
    }
}
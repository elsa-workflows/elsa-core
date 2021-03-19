using Elsa.Activities.Webhooks.Models;
using Elsa.Persistence.InMemory;
using Elsa.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Activities.Webhooks.Persistence.InMemory
{
    public class InMemoryWebhookStore : InMemoryStore<WebhookDefinition>, IWebhookDefinitionStore
    {
        public InMemoryWebhookStore(IMemoryCache memoryCache, IIdGenerator idGenerator) : base(memoryCache, idGenerator)
        {
        }
    }
}

using Elsa.Persistence.InMemory;
using Elsa.Services;
using Elsa.Webhooks.Abstractions.Models;
using Elsa.Webhooks.Abstractions.Persistence;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Activities.Webhooks.Persistence.InMemory
{
    public class InMemoryWebhookDefinitionStore : InMemoryStore<WebhookDefinition>, IWebhookDefinitionStore
    {
        public InMemoryWebhookDefinitionStore(IMemoryCache memoryCache, IIdGenerator idGenerator) : base(memoryCache, idGenerator)
        {
        }
    }
}

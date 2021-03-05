using System;
using Elsa.Activities.Webhooks.Models;
using Elsa.Activities.Webhooks.Services;
using Elsa.Persistence.InMemory;
using Elsa.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Activities.Webhooks.Persistence
{
    public class InMemoryWebhookStore : InMemoryStore<Webhook>, IWebhookStore
    {
        public InMemoryWebhookStore(IMemoryCache memoryCache, IIdGenerator idGenerator) : base(memoryCache, idGenerator)
        {
        }
    }
}

using Elsa.Models;
using Elsa.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Persistence.InMemory;

public class InMemoryTriggerStore : InMemoryStore<Trigger>, ITriggerStore
{
    public InMemoryTriggerStore(IMemoryCache memoryCache, IIdGenerator idGenerator) : base(memoryCache, idGenerator)
    {
    }
}
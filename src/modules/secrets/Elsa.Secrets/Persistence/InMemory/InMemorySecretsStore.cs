using Elsa.Persistence.InMemory;
using Elsa.Secrets.Models;
using Elsa.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Secrets.Persistence.InMemory
{
    public class InMemorySecretsStore : InMemoryStore<Secret>, ISecretsStore
    {
        public InMemorySecretsStore(IMemoryCache memoryCache, IIdGenerator idGenerator) : base(memoryCache, idGenerator)
        {
        }
    }
}

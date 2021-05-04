using Elsa.Models;
using Elsa.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Persistence.InMemory
{
    public class InMemoryBookmarkStore : InMemoryStore<Bookmark>, IBookmarkStore
    {
        public InMemoryBookmarkStore(IMemoryCache memoryCache, IIdGenerator idGenerator) : base(memoryCache, idGenerator)
        {
        }
    }
}
using Elsa.Caching.Options;
using Elsa.Caching.Services;
using Elsa.Common.Multitenancy;
using Elsa.Common.Serialization;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Stores;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Stores;

public class CachingTriggerStoreTests
{
    [Fact]
    public async Task RequiredRemovalSignalEvictsCachedTriggerRows()
    {
        var filter = new TriggerFilter { Hash = "route" };
        var oldTrigger = new StoredTrigger { Id = "old-trigger", Hash = filter.Hash };
        IReadOnlyCollection<StoredTrigger> current = [oldTrigger];
        var store = Substitute.For<ITriggerStore>();
        store.FindManyAsync(filter).Returns(_ => ValueTask.FromResult<IEnumerable<StoredTrigger>>(current));

        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var options = Microsoft.Extensions.Options.Options.Create(new CachingOptions());
        var cache = new CacheManager(memoryCache, new ChangeTokenSignaler(new ChangeTokenSignalInvoker()), options);
        var cachedStore = new CachingTriggerStore(store, cache, new Hasher(SerializationTypeRegistry.CreateDefault()), Substitute.For<ITenantAccessor>());
        Assert.Same(oldTrigger, Assert.Single(await cachedStore.FindManyAsync(filter)));

        current = [];
        await cache.TriggerTokenAsync(CachingTriggerStore.RequiredRemovalCacheInvalidationTokenKey);

        Assert.Empty(await cachedStore.FindManyAsync(filter));
    }
}

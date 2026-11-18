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
    public async Task ReplaceEvictsTriggerRowsReadBeforeTheWriteCompletes()
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
        store.ReplaceAsync(Arg.Any<IEnumerable<StoredTrigger>>(), Arg.Any<IEnumerable<StoredTrigger>>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask(ReplaceAsync()));

        await cachedStore.ReplaceAsync([oldTrigger], []);

        Assert.Empty(await cachedStore.FindManyAsync(filter));

        async Task ReplaceAsync()
        {
            Assert.Same(oldTrigger, Assert.Single(await cachedStore.FindManyAsync(filter)));
            current = [];
        }
    }
}

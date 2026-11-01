using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Elsa.Workflows.Runtime.Stores;

namespace Elsa.Workflows.Runtime.UnitTests.Stores;

public class BookmarkQueueStoreFindManyPagedTests
{
    [Fact(DisplayName = "Memory queue store pages, orders and filters without a total")]
    public async Task MemoryQueueStore_FindManyPaged_OrdersFiltersAndPages()
    {
        var store = await CreateSeededQueueStoreAsync();
        var order = new BookmarkQueueItemOrder<string>(x => x.Id, OrderDirection.Descending);

        var firstPage = await store.FindManyAsync(new BookmarkQueueFilter { TenantAgnostic = true }, order, PageArgs.FromRange(0, 2));
        var secondPage = await store.FindManyAsync(new BookmarkQueueFilter { TenantAgnostic = true }, order, PageArgs.FromRange(2, 2));
        var olderThanCutoff = await store.FindManyAsync(new BookmarkQueueFilter { CreatedAtLessThan = DateTimeOffset.UnixEpoch.AddDays(1) }, order, PageArgs.All);

        Assert.Equal(["q-t2", "q-t1"], firstPage.Select(x => x.Id));
        Assert.Equal(["q-default", "q-a"], secondPage.Select(x => x.Id));
        Assert.Equal(["q-a"], olderThanCutoff.Select(x => x.Id));
    }

    [Fact(DisplayName = "Memory queue store scopes to the ambient tenant unless TenantAgnostic is set")]
    public async Task MemoryQueueStore_FindManyPaged_HonorsTenantAgnostic()
    {
        var store = await CreateSeededQueueStoreAsync();
        var order = new BookmarkQueueItemOrder<string>(x => x.Id, OrderDirection.Ascending);

        var scoped = await store.FindManyAsync(new BookmarkQueueFilter(), order, PageArgs.All);
        var agnostic = await store.FindManyAsync(new BookmarkQueueFilter { TenantAgnostic = true }, order, PageArgs.All);

        Assert.Equal(["q-a"], scoped.Select(x => x.Id));
        Assert.Equal(["q-a", "q-default", "q-t1", "q-t2"], agnostic.Select(x => x.Id));
    }

    [Fact(DisplayName = "Memory dead-letter store pages, orders and filters without a total")]
    public async Task MemoryDeadLetterStore_FindManyPaged_OrdersFiltersAndPages()
    {
        var store = new MemoryBookmarkQueueDeadLetterStore(new MemoryStore<BookmarkQueueDeadLetterItem>());

        foreach (var (id, deadLetteredAt) in new[] { ("d1", 1), ("d2", 2), ("d3", 3) })
            await store.AddAsync(new BookmarkQueueDeadLetterItem { Id = id, OriginalQueueItemId = $"q-{id}", DeadLetteredAt = DateTimeOffset.UnixEpoch.AddDays(deadLetteredAt) });

        var order = new BookmarkQueueDeadLetterItemOrder<DateTimeOffset>(x => x.DeadLetteredAt, OrderDirection.Ascending);

        var page = await store.FindManyAsync(new BookmarkQueueDeadLetterFilter(), order, PageArgs.FromRange(1, 1));
        var older = await store.FindManyAsync(new BookmarkQueueDeadLetterFilter { DeadLetteredAtLessThan = DateTimeOffset.UnixEpoch.AddDays(3) }, order, PageArgs.All);

        Assert.Equal(["d2"], page.Select(x => x.Id));
        Assert.Equal(["d1", "d2"], older.Select(x => x.Id));
    }

    [Fact(DisplayName = "A store implementing only the original members serves the paged FindManyAsync through PageAsync")]
    public async Task CustomStore_FindManyPaged_FallsBackToPageAsync()
    {
        var inner = new MemoryBookmarkQueueStore(new MemoryStore<BookmarkQueueItem>());
        await inner.AddAsync(new BookmarkQueueItem { Id = "a", CreatedAt = DateTimeOffset.UnixEpoch });
        await inner.AddAsync(new BookmarkQueueItem { Id = "b", CreatedAt = DateTimeOffset.UnixEpoch });
        await inner.AddAsync(new BookmarkQueueItem { Id = "c", CreatedAt = DateTimeOffset.UnixEpoch });
        IBookmarkQueueStore store = new PageOnlyBookmarkQueueStore(inner);

        var items = await store.FindManyAsync(new BookmarkQueueFilter(), new BookmarkQueueItemOrder<string>(x => x.Id, OrderDirection.Ascending), PageArgs.FromRange(1, 2));

        Assert.Equal(["b", "c"], items.Select(x => x.Id));
    }

    private static async Task<MemoryBookmarkQueueStore> CreateSeededQueueStoreAsync()
    {
        var store = new MemoryBookmarkQueueStore(new MemoryStore<BookmarkQueueItem>(), new TestTenantAccessor("tenant-a"));

        await store.AddAsync(new BookmarkQueueItem { Id = "q-a", TenantId = "tenant-a", CreatedAt = DateTimeOffset.UnixEpoch });
        await store.AddAsync(new BookmarkQueueItem { Id = "q-t1", TenantId = "t1", CreatedAt = DateTimeOffset.UnixEpoch.AddDays(2) });
        await store.AddAsync(new BookmarkQueueItem { Id = "q-t2", TenantId = "t2", CreatedAt = DateTimeOffset.UnixEpoch.AddDays(2) });
        await store.AddAsync(new BookmarkQueueItem { Id = "q-default", TenantId = "", CreatedAt = DateTimeOffset.UnixEpoch.AddDays(2) });
        return store;
    }

    private sealed class PageOnlyBookmarkQueueStore(IBookmarkQueueStore inner) : IBookmarkQueueStore
    {
        public Task SaveAsync(BookmarkQueueItem record, CancellationToken cancellationToken = default) => inner.SaveAsync(record, cancellationToken);
        public Task AddAsync(BookmarkQueueItem record, CancellationToken cancellationToken = default) => inner.AddAsync(record, cancellationToken);
        public Task<BookmarkQueueItem?> FindAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default) => inner.FindAsync(filter, cancellationToken);
        public Task<IEnumerable<BookmarkQueueItem>> FindManyAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default) => inner.FindManyAsync(filter, cancellationToken);
        public Task<Page<BookmarkQueueItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default) => inner.PageAsync(pageArgs, orderBy, cancellationToken);
        public Task<Page<BookmarkQueueItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueFilter filter, BookmarkQueueItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default) => inner.PageAsync(pageArgs, filter, orderBy, cancellationToken);
        public Task<long> DeleteAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default) => inner.DeleteAsync(filter, cancellationToken);
    }
}

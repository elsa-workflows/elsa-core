using Elsa.Common;
using Elsa.Common.RecurringTasks;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Elsa.Workflows.Runtime.Stores;
using Elsa.Workflows.Runtime.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class BookmarkQueueMultitenantPurgerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private readonly DateTimeOffset _now = new(2026, 5, 20, 10, 0, 0, TimeSpan.Zero);
    private readonly DefaultTenantAccessor _accessor = new();
    private readonly ITenantService _tenantService = Substitute.For<ITenantService>();
    private readonly ISystemClock _clock = Substitute.For<ISystemClock>();
    private readonly IIdentityGenerator _identityGenerator = Substitute.For<IIdentityGenerator>();
    private readonly MemoryBookmarkQueueStore _queueStore;
    private readonly MemoryBookmarkQueueDeadLetterStore _deadLetterStore;
    private readonly BookmarkQueueDeadLetterManager _manager;

    public BookmarkQueueMultitenantPurgerTests()
    {
        _identityGenerator.GenerateId().Returns(_ => Guid.NewGuid().ToString());
        _clock.UtcNow.Returns(_now);
        _queueStore = new(new MemoryStore<BookmarkQueueItem>(), _accessor);
        _deadLetterStore = new(new MemoryStore<BookmarkQueueDeadLetterItem>());
        _manager = new(_deadLetterStore, _queueStore, Substitute.For<IBookmarkQueueSignaler>(), _clock, _identityGenerator, NullLogger<BookmarkQueueDeadLetterManager>.Instance);

        foreach (var tenantId in new[] { "tenant-a", "tenant-b" })
            _tenantService.FindAsync(tenantId, Arg.Any<CancellationToken>()).Returns(new Tenant { Id = tenantId, Name = tenantId });
    }

    [Fact(DisplayName = "One pass purges the expired items of several tenants and keeps fresh ones")]
    public async Task PurgeAsync_PurgesExpiredItemsOfAllTenants()
    {
        await _queueStore.AddAsync(NewQueueItem("a-expired", "tenant-a", _now.AddMinutes(-2)));
        await _queueStore.AddAsync(NewQueueItem("b-expired", "tenant-b", _now.AddMinutes(-2)));
        await _queueStore.AddAsync(NewQueueItem("default-expired", null, _now.AddMinutes(-2)));
        await _queueStore.AddAsync(NewQueueItem("agnostic-expired", Tenant.AgnosticTenantId, _now.AddMinutes(-2)));
        await _queueStore.AddAsync(NewQueueItem("a-fresh", "tenant-a", _now));
        var purger = CreatePurger(new BookmarkQueuePurgeOptions { Ttl = TimeSpan.FromMinutes(1), DeadLetterTtl = TimeSpan.FromDays(7) });

        await purger.PurgeAsync().WaitAsync(Timeout);

        var remaining = await _queueStore.FindManyAsync(new BookmarkQueueFilter { TenantAgnostic = true });
        Assert.Equal(["a-fresh"], remaining.Select(x => x.Id));
    }

    [Fact(DisplayName = "Dead letters of expired items keep their origin tenant")]
    public async Task PurgeAsync_DeadLettersKeepOriginTenant()
    {
        await _queueStore.AddAsync(NewQueueItem("a-expired", "tenant-a", _now.AddMinutes(-2)));
        await _queueStore.AddAsync(NewQueueItem("b-expired", "tenant-b", _now.AddMinutes(-2)));
        var purger = CreatePurger(new BookmarkQueuePurgeOptions { Ttl = TimeSpan.FromMinutes(1), DeadLetterTtl = TimeSpan.FromDays(7) });

        await purger.PurgeAsync().WaitAsync(Timeout);

        var deadLetters = await _deadLetterStore.FindManyAsync(new BookmarkQueueDeadLetterFilter());
        Assert.Equal(
            [("a-expired", "tenant-a"), ("b-expired", "tenant-b")],
            deadLetters.OrderBy(x => x.OriginalQueueItemId).Select(x => (x.OriginalQueueItemId, x.TenantId)));
    }

    [Fact(DisplayName = "Expired dead letters are purged across tenants")]
    public async Task PurgeAsync_PurgesExpiredDeadLettersOfAllTenants()
    {
        await _deadLetterStore.AddAsync(NewDeadLetter("a-old", "tenant-a", _now.AddDays(-8)));
        await _deadLetterStore.AddAsync(NewDeadLetter("b-old", "tenant-b", _now.AddDays(-8)));
        await _deadLetterStore.AddAsync(NewDeadLetter("default-old", null, _now.AddDays(-8)));
        await _deadLetterStore.AddAsync(NewDeadLetter("a-fresh", "tenant-a", _now.AddDays(-1)));
        var purger = CreatePurger(new BookmarkQueuePurgeOptions { DeadLetterTtl = TimeSpan.FromDays(7) });

        await purger.PurgeAsync().WaitAsync(Timeout);

        var remaining = await _deadLetterStore.FindManyAsync(new BookmarkQueueDeadLetterFilter());
        Assert.Equal(["a-fresh"], remaining.Select(x => x.Id));
    }

    [Fact(DisplayName = "Expired items and dead letters of an unknown tenant are purged in one pass and not read again")]
    public async Task PurgeAsync_WhenTenantIsUnknown_PurgesUnderSyntheticTenant()
    {
        await _queueStore.AddAsync(NewQueueItem("ghost-expired", "ghost", _now.AddMinutes(-2)));
        await _deadLetterStore.AddAsync(NewDeadLetter("ghost-old", "ghost", _now.AddDays(-8)));
        var recordingDeadLetterStore = new RecordingDeadLetterStore(_deadLetterStore, _accessor);
        var pagedQueueItemIds = new List<string>();
        var queueStore = new DelegatingDeleteQueueStore(_queueStore, filter => _queueStore.DeleteAsync(filter), items => pagedQueueItemIds.AddRange(items.Select(x => x.Id)));
        var purger = CreatePurger(new BookmarkQueuePurgeOptions { Ttl = TimeSpan.FromMinutes(1), DeadLetterTtl = TimeSpan.FromDays(7) }, queueStore, recordingDeadLetterStore);

        await Task.Run(() => purger.PurgeAsync()).WaitAsync(Timeout);

        Assert.Empty(await _queueStore.FindManyAsync(new BookmarkQueueFilter { TenantAgnostic = true }));
        Assert.DoesNotContain(await _deadLetterStore.FindManyAsync(new BookmarkQueueDeadLetterFilter()), x => x.Id == "ghost-old");
        Assert.Equal(["ghost"], recordingDeadLetterStore.AddedTenantIds);
        Assert.Equal(["ghost-expired"], pagedQueueItemIds);
        Assert.Equal(["ghost-old"], recordingDeadLetterStore.PagedIds);

        pagedQueueItemIds.Clear();
        recordingDeadLetterStore.PagedIds.Clear();

        await Task.Run(() => purger.PurgeAsync()).WaitAsync(Timeout);

        Assert.Empty(pagedQueueItemIds);
        Assert.Empty(recordingDeadLetterStore.PagedIds);
    }

    [Fact(DisplayName = "Expired items of unknown and known tenants are all purged across batches")]
    public async Task PurgeAsync_PurgesUnknownTenantItemsAcrossBatches()
    {
        for (var i = 0; i < 3; i++)
            await _queueStore.AddAsync(NewQueueItem($"ghost-{i}", "ghost", _now.AddMinutes(-10 + i)));
        await _queueStore.AddAsync(NewQueueItem("a-expired", "tenant-a", _now.AddMinutes(-2)));
        var purger = CreatePurger(new BookmarkQueuePurgeOptions { Ttl = TimeSpan.FromMinutes(1), DeadLetterTtl = TimeSpan.FromDays(7), BatchSize = 2 });

        await Task.Run(() => purger.PurgeAsync()).WaitAsync(Timeout);

        Assert.Empty(await _queueStore.FindManyAsync(new BookmarkQueueFilter { TenantAgnostic = true }));
    }

    [Fact(DisplayName = "Dead letters are written under the tenant context of their item")]
    public async Task PurgeAsync_WritesDeadLettersUnderItemTenantContext()
    {
        await _queueStore.AddAsync(NewQueueItem("a-expired", "tenant-a", _now.AddMinutes(-2)));
        await _queueStore.AddAsync(NewQueueItem("b-expired", "tenant-b", _now.AddMinutes(-2)));
        var recordingDeadLetterStore = new RecordingDeadLetterStore(_deadLetterStore, _accessor);
        var purger = CreatePurger(new BookmarkQueuePurgeOptions { Ttl = TimeSpan.FromMinutes(1), DeadLetterTtl = TimeSpan.FromDays(7) }, deadLetterStore: recordingDeadLetterStore);

        await purger.PurgeAsync().WaitAsync(Timeout);

        Assert.Equal(["tenant-a", "tenant-b"], recordingDeadLetterStore.AddedTenantIds.Order());
    }

    [Fact(DisplayName = "A cancelled purge propagates the cancellation instead of isolating it per tenant")]
    public async Task PurgeAsync_WhenCancelled_Propagates()
    {
        using var cts = new CancellationTokenSource();
        await _queueStore.AddAsync(NewQueueItem("a-expired", "tenant-a", _now.AddMinutes(-2)));
        var queueStore = new DelegatingDeleteQueueStore(_queueStore, _ =>
        {
            cts.Cancel();
            throw new OperationCanceledException(cts.Token);
        });
        var purger = CreatePurger(new BookmarkQueuePurgeOptions { Ttl = TimeSpan.FromMinutes(1), DeadLetterTtl = TimeSpan.FromDays(7) }, queueStore);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => purger.PurgeAsync(cts.Token)).WaitAsync(Timeout);
    }

    [Fact(DisplayName = "The container resolves the multitenant purger and trigger task")]
    public async Task Container_ResolvesMultitenantConstructors()
    {
        var signaler = Substitute.For<IBookmarkQueueSignaler>();
        var signaledTenantIds = new List<string?>();
        signaler
            .When(x => x.TriggerAsync(Arg.Any<CancellationToken>()))
            .Do(_ => signaledTenantIds.Add(_accessor.Tenant?.Id));
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IBookmarkQueueStore>(_queueStore);
        services.AddSingleton<IBookmarkQueueDeadLetterStore>(_deadLetterStore);
        services.AddSingleton<IBookmarkQueueDeadLetterManager>(_manager);
        services.AddSingleton(_clock);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new BookmarkQueuePurgeOptions { Ttl = TimeSpan.FromMinutes(1), DeadLetterTtl = TimeSpan.FromDays(7) }));
        services.AddSingleton(_tenantService);
        services.AddSingleton<ITenantAccessor>(_accessor);
        services.AddSingleton(Substitute.For<IBookmarkQueueWorker>());
        services.AddSingleton(signaler);
        services.AddScoped<IBookmarkQueuePurger, DefaultBookmarkQueuePurger>();
        services.AddRecurringTask<TriggerBookmarkQueueRecurringTask>();
        await _queueStore.AddAsync(NewQueueItem("a-expired", "tenant-a", _now.AddMinutes(-2)));
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        await scope.ServiceProvider.GetServices<IRecurringTask>().OfType<TriggerBookmarkQueueRecurringTask>().Single().ExecuteAsync(CancellationToken.None).WaitAsync(Timeout);
        await scope.ServiceProvider.GetRequiredService<IBookmarkQueuePurger>().PurgeAsync().WaitAsync(Timeout);

        Assert.Empty(await _queueStore.FindManyAsync(new BookmarkQueueFilter { TenantAgnostic = true }));
        Assert.Equal(["tenant-a"], signaledTenantIds);
    }

    [Fact(DisplayName = "A tenant group that fails does not stop the other tenants or the dead-letter pass")]
    public async Task PurgeAsync_WhenOneTenantFails_PurgesOtherTenantsAndDeadLetters()
    {
        await _queueStore.AddAsync(NewQueueItem("a-expired", "tenant-a", _now.AddMinutes(-3)));
        await _queueStore.AddAsync(NewQueueItem("b-expired", "tenant-b", _now.AddMinutes(-2)));
        await _deadLetterStore.AddAsync(NewDeadLetter("b-old", "tenant-b", _now.AddDays(-8)));
        var queueStore = new DelegatingDeleteQueueStore(_queueStore, async filter =>
        {
            if (_accessor.TenantId == "tenant-a")
                throw new InvalidOperationException("delete failed");

            return await _queueStore.DeleteAsync(filter);
        });
        var purger = CreatePurger(new BookmarkQueuePurgeOptions { Ttl = TimeSpan.FromMinutes(1), DeadLetterTtl = TimeSpan.FromDays(7) }, queueStore);

        await purger.PurgeAsync().WaitAsync(Timeout);

        var remaining = await _queueStore.FindManyAsync(new BookmarkQueueFilter { TenantAgnostic = true });
        Assert.Equal(["a-expired"], remaining.Select(x => x.Id));
        Assert.DoesNotContain(await _deadLetterStore.FindManyAsync(new BookmarkQueueDeadLetterFilter()), x => x.Id == "b-old");
    }

    [Fact(DisplayName = "Expired items behind a full batch of known-tenant items that cannot be deleted are still purged")]
    public async Task PurgeAsync_SkipsPastItemsThatCouldNotBeDeleted()
    {
        for (var i = 0; i < 3; i++)
            await _queueStore.AddAsync(NewQueueItem($"stuck-{i}", "tenant-a", _now.AddMinutes(-10 + i)));
        await _queueStore.AddAsync(NewQueueItem("a-expired", "tenant-a", _now.AddMinutes(-2)));
        var queueStore = new DelegatingDeleteQueueStore(_queueStore, async filter =>
        {
            var ids = filter.Ids!.Where(x => !x.StartsWith("stuck")).ToList();
            return ids.Count == 0 ? 0 : await _queueStore.DeleteAsync(new BookmarkQueueFilter { Ids = ids });
        });
        var purger = CreatePurger(new BookmarkQueuePurgeOptions { Ttl = TimeSpan.FromMinutes(1), DeadLetterTtl = TimeSpan.FromDays(7), BatchSize = 2 }, queueStore);

        await Task.Run(() => purger.PurgeAsync()).WaitAsync(Timeout);

        var remaining = await _queueStore.FindManyAsync(new BookmarkQueueFilter { TenantAgnostic = true });
        Assert.Equal(["stuck-0", "stuck-1", "stuck-2"], remaining.Select(x => x.Id).Order());
    }

    private DefaultBookmarkQueuePurger CreatePurger(BookmarkQueuePurgeOptions options, IBookmarkQueueStore? queueStore = null, IBookmarkQueueDeadLetterStore? deadLetterStore = null)
    {
        return new(
            queueStore ?? _queueStore,
            deadLetterStore ?? _deadLetterStore,
            deadLetterStore is null ? _manager : new BookmarkQueueDeadLetterManager(deadLetterStore, queueStore ?? _queueStore, Substitute.For<IBookmarkQueueSignaler>(), _clock, _identityGenerator, NullLogger<BookmarkQueueDeadLetterManager>.Instance),
            _clock,
            Microsoft.Extensions.Options.Options.Create(options),
            NullLogger<DefaultBookmarkQueuePurger>.Instance,
            _tenantService,
            _accessor);
    }

    private static BookmarkQueueItem NewQueueItem(string id, string? tenantId, DateTimeOffset createdAt)
    {
        return new()
        {
            Id = id,
            TenantId = tenantId,
            WorkflowInstanceId = "workflow-instance",
            BookmarkId = "bookmark",
            StimulusHash = "hash",
            ActivityTypeName = "activity",
            CreatedAt = createdAt
        };
    }

    private static BookmarkQueueDeadLetterItem NewDeadLetter(string id, string? tenantId, DateTimeOffset deadLetteredAt)
    {
        return new()
        {
            Id = id,
            TenantId = tenantId,
            OriginalQueueItemId = $"queue-{id}",
            OriginalCreatedAt = deadLetteredAt.AddMinutes(-1),
            DeadLetteredAt = deadLetteredAt,
            Reason = "Expired"
        };
    }

    private sealed class RecordingDeadLetterStore(IBookmarkQueueDeadLetterStore inner, ITenantAccessor accessor) : IBookmarkQueueDeadLetterStore
    {
        public List<string?> AddedTenantIds { get; } = [];
        public List<string> PagedIds { get; } = [];

        public Task SaveAsync(BookmarkQueueDeadLetterItem record, CancellationToken cancellationToken = default) => inner.SaveAsync(record, cancellationToken);

        public Task AddAsync(BookmarkQueueDeadLetterItem record, CancellationToken cancellationToken = default)
        {
            AddedTenantIds.Add(accessor.TenantId);
            return inner.AddAsync(record, cancellationToken);
        }

        public Task<BookmarkQueueDeadLetterItem?> TryMarkReplayedAsync(string id, string queueItemId, DateTimeOffset replayedAt, CancellationToken cancellationToken = default) => inner.TryMarkReplayedAsync(id, queueItemId, replayedAt, cancellationToken);

        public Task<BookmarkQueueDeadLetterItem?> FindAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default) => inner.FindAsync(filter, cancellationToken);

        public Task<IEnumerable<BookmarkQueueDeadLetterItem>> FindManyAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default) => inner.FindManyAsync(filter, cancellationToken);

        public Task<Page<BookmarkQueueDeadLetterItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueDeadLetterItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default) => inner.PageAsync(pageArgs, orderBy, cancellationToken);

        public async Task<Page<BookmarkQueueDeadLetterItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueDeadLetterFilter filter, BookmarkQueueDeadLetterItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default)
        {
            var page = await inner.PageAsync(pageArgs, filter, orderBy, cancellationToken);
            PagedIds.AddRange(page.Items.Select(x => x.Id));
            return page;
        }

        public Task<long> DeleteAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default) => inner.DeleteAsync(filter, cancellationToken);
    }

    private sealed class DelegatingDeleteQueueStore(IBookmarkQueueStore inner, Func<BookmarkQueueFilter, Task<long>> delete, Action<IEnumerable<BookmarkQueueItem>>? onPaged = null) : IBookmarkQueueStore
    {
        public Task SaveAsync(BookmarkQueueItem record, CancellationToken cancellationToken = default) => inner.SaveAsync(record, cancellationToken);

        public Task AddAsync(BookmarkQueueItem record, CancellationToken cancellationToken = default) => inner.AddAsync(record, cancellationToken);

        public Task<BookmarkQueueItem?> FindAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default) => inner.FindAsync(filter, cancellationToken);

        public Task<IEnumerable<BookmarkQueueItem>> FindManyAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default) => inner.FindManyAsync(filter, cancellationToken);

        public Task<Page<BookmarkQueueItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default) => inner.PageAsync(pageArgs, orderBy, cancellationToken);

        public async Task<Page<BookmarkQueueItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueFilter filter, BookmarkQueueItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default)
        {
            var page = await inner.PageAsync(pageArgs, filter, orderBy, cancellationToken);
            onPaged?.Invoke(page.Items);
            return page;
        }

        public Task<long> DeleteAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default) => delete(filter);
    }
}

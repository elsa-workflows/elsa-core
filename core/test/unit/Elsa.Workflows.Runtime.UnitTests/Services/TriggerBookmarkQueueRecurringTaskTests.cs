using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Stores;
using Elsa.Workflows.Runtime.Tasks;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class TriggerBookmarkQueueRecurringTaskTests
{
    private readonly DefaultTenantAccessor _accessor = new();
    private readonly ITenantService _tenantService = Substitute.For<ITenantService>();
    private readonly IBookmarkQueueSignaler _signaler = Substitute.For<IBookmarkQueueSignaler>();
    private readonly List<string?> _signaledTenantIds = [];

    public TriggerBookmarkQueueRecurringTaskTests()
    {
        _signaler
            .When(x => x.TriggerAsync(Arg.Any<CancellationToken>()))
            .Do(_ => _signaledTenantIds.Add(_accessor.Tenant?.Id));

        foreach (var tenantId in new[] { "tenant-a", "tenant-b", Tenant.DefaultTenantId })
            _tenantService.FindAsync(tenantId, Arg.Any<CancellationToken>()).Returns(new Tenant { Id = tenantId, Name = tenantId });
    }

    [Fact(DisplayName = "ExecuteAsync signals a tenant with queue items under its tenant context and skips one without")]
    public async Task ExecuteAsync_SignalsOnlyTenantsWithItems()
    {
        var store = new MemoryBookmarkQueueStore(new MemoryStore<BookmarkQueueItem>());
        await store.AddAsync(new BookmarkQueueItem { Id = "1", TenantId = "tenant-a" });
        var task = CreateTask(store);

        await task.ExecuteAsync(CancellationToken.None);

        Assert.Equal(["tenant-a"], _signaledTenantIds);
    }

    [Fact(DisplayName = "ExecuteAsync signals the default tenant once for items without a tenant id or with the agnostic id")]
    public async Task ExecuteAsync_MapsUnassignedAndAgnosticItemsToDefaultTenant()
    {
        var store = new MemoryBookmarkQueueStore(new MemoryStore<BookmarkQueueItem>());
        await store.AddAsync(new BookmarkQueueItem { Id = "1", TenantId = null });
        await store.AddAsync(new BookmarkQueueItem { Id = "2", TenantId = Tenant.AgnosticTenantId });
        var task = CreateTask(store);

        await task.ExecuteAsync(CancellationToken.None);

        Assert.Equal([Tenant.DefaultTenantId], _signaledTenantIds);
    }

    [Fact(DisplayName = "ExecuteAsync falls back to all active tenants when the store cannot list tenant ids")]
    public async Task ExecuteAsync_WhenStoreReturnsNull_SignalsAllActiveTenants()
    {
        var store = Substitute.For<IBookmarkQueueStore>();
        store.ListTenantIdsAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyCollection<string>?)null);
        _tenantService.ListAsync(Arg.Any<CancellationToken>()).Returns(
        [
            new Tenant { Id = "tenant-a", Name = "tenant-a" },
            new Tenant { Id = "tenant-b", Name = "tenant-b" }
        ]);
        var task = CreateTask(store);

        await task.ExecuteAsync(CancellationToken.None);

        Assert.Equal(["tenant-a", "tenant-b"], _signaledTenantIds);
    }

    [Fact(DisplayName = "ExecuteAsync skips a tenant id that cannot be found")]
    public async Task ExecuteAsync_SkipsUnknownTenant()
    {
        var store = new MemoryBookmarkQueueStore(new MemoryStore<BookmarkQueueItem>());
        await store.AddAsync(new BookmarkQueueItem { Id = "1", TenantId = "ghost" });
        await store.AddAsync(new BookmarkQueueItem { Id = "2", TenantId = "tenant-b" });
        var task = CreateTask(store);

        await task.ExecuteAsync(CancellationToken.None);

        Assert.Equal(["tenant-b"], _signaledTenantIds);
    }

    [Fact(DisplayName = "ExecuteAsync without multitenancy signals once and does not query the store")]
    public async Task ExecuteAsync_WithoutMultitenancy_SignalsOnce()
    {
        var store = Substitute.For<IBookmarkQueueStore>();
        var task = new TriggerBookmarkQueueRecurringTask(Substitute.For<IBookmarkQueueWorker>(), _signaler, null, store, null);

        await task.ExecuteAsync(CancellationToken.None);

        await _signaler.Received(1).TriggerAsync(Arg.Any<CancellationToken>());
        await store.DidNotReceive().ListTenantIdsAsync(Arg.Any<CancellationToken>());
    }

    private TriggerBookmarkQueueRecurringTask CreateTask(IBookmarkQueueStore store) =>
        new(Substitute.For<IBookmarkQueueWorker>(), _signaler, _accessor, store, _tenantService);
}

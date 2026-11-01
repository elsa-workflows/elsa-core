using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Stores;
using Elsa.Workflows.Runtime.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class TriggerBookmarkQueueRecurringTaskTests
{
    private readonly DefaultTenantAccessor _accessor = new();
    private readonly ITenantService _tenantService = Substitute.For<ITenantService>();
    private readonly IBookmarkQueueSignaler _signaler = Substitute.For<IBookmarkQueueSignaler>();
    private readonly List<string?> _signaledTenantIds = [];
    private readonly ITenantScopeFactory _scopeFactory = Substitute.For<ITenantScopeFactory>();
    private readonly Dictionary<string, IBookmarkQueueStore> _ownStores = new();
    private readonly List<string> _scopedTenantIds = [];
    private readonly HashSet<string> _failingTenantIds = [];
    private IBookmarkQueueStore? _defaultScopeStore;

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

    [Fact(DisplayName = "ExecuteAsync signals an own-database tenant whose own store has items and skips one without")]
    public async Task ExecuteAsync_OwnDatabaseTenant_SignalsOnlyWithItems()
    {
        var withItems = OwnDatabaseTenant("tenant-with");
        var without = OwnDatabaseTenant("tenant-without");
        UseOwnDatabaseTenants(withItems, without);
        await _ownStores[withItems.Id].AddAsync(new BookmarkQueueItem { Id = "1", TenantId = withItems.Id });

        await CreateScopedTask(SharedStore()).ExecuteAsync(CancellationToken.None);

        Assert.Equal(["tenant-with"], _signaledTenantIds);
        Assert.Equal(["tenant-with", "tenant-without"], _scopedTenantIds);
    }

    [Fact(DisplayName = "ExecuteAsync signals an own-database tenant whose store cannot list tenant ids")]
    public async Task ExecuteAsync_OwnDatabaseTenantStoreReturnsNull_Signals()
    {
        var tenant = OwnDatabaseTenant("tenant-own");
        UseOwnDatabaseTenants(tenant);
        var ownStore = Substitute.For<IBookmarkQueueStore>();
        ownStore.ListTenantIdsAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyCollection<string>?)null);
        _ownStores[tenant.Id] = ownStore;

        await CreateScopedTask(SharedStore()).ExecuteAsync(CancellationToken.None);

        Assert.Equal(["tenant-own"], _signaledTenantIds);
    }

    [Fact(DisplayName = "ExecuteAsync does not signal or scope a tenant the shared pass already signaled")]
    public async Task ExecuteAsync_TenantAlreadySignaledBySharedPass_NotSignaledTwice()
    {
        var tenant = OwnDatabaseTenant("tenant-own");
        UseOwnDatabaseTenants(tenant);
        var shared = SharedStore();
        await shared.AddAsync(new BookmarkQueueItem { Id = "1", TenantId = tenant.Id });
        await _ownStores[tenant.Id].AddAsync(new BookmarkQueueItem { Id = "2", TenantId = tenant.Id });

        await CreateScopedTask(shared).ExecuteAsync(CancellationToken.None);

        Assert.Equal(["tenant-own"], _signaledTenantIds);
        Assert.Empty(_scopedTenantIds);
    }

    [Fact(DisplayName = "ExecuteAsync reads the shared store through the default tenant scope under an own-database ambient tenant")]
    public async Task ExecuteAsync_AmbientOwnDatabaseTenant_UsesDefaultScopeForSharedPass()
    {
        var tenant = OwnDatabaseTenant("tenant-own");
        UseOwnDatabaseTenants(tenant);
        var defaultScopeStore = new MemoryBookmarkQueueStore(new MemoryStore<BookmarkQueueItem>());
        await defaultScopeStore.AddAsync(new BookmarkQueueItem { Id = "1", TenantId = "tenant-a" });
        _defaultScopeStore = defaultScopeStore;

        using (_accessor.PushContext(tenant))
            await CreateScopedTask(SharedStore()).ExecuteAsync(CancellationToken.None);

        Assert.Equal(["tenant-a"], _signaledTenantIds);
        Assert.Equal([Tenant.DefaultTenantId, "tenant-own"], _scopedTenantIds);
    }

    [Fact(DisplayName = "ExecuteAsync isolates a failing own-database tenant")]
    public async Task ExecuteAsync_FailingOwnDatabaseTenant_OthersStillSignaled()
    {
        var failing = OwnDatabaseTenant("tenant-failing");
        var healthy = OwnDatabaseTenant("tenant-healthy");
        UseOwnDatabaseTenants(failing, healthy);
        _failingTenantIds.Add(failing.Id);
        await _ownStores[healthy.Id].AddAsync(new BookmarkQueueItem { Id = "1", TenantId = healthy.Id });
        var shared = SharedStore();
        await shared.AddAsync(new BookmarkQueueItem { Id = "2", TenantId = "tenant-a" });

        await CreateScopedTask(shared).ExecuteAsync(CancellationToken.None);

        Assert.Equal(["tenant-a", "tenant-healthy"], _signaledTenantIds);
    }

    private static MemoryBookmarkQueueStore SharedStore() => new(new MemoryStore<BookmarkQueueItem>());

    private static Tenant OwnDatabaseTenant(string id) => new()
    {
        Id = id,
        Name = id,
        Configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Sqlite"] = $"Data Source={id}.db" }).Build()
    };

    private void UseOwnDatabaseTenants(params Tenant[] tenants)
    {
        foreach (var tenant in tenants)
        {
            _tenantService.FindAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
            _ownStores[tenant.Id] = SharedStore();
        }

        _tenantService.ListAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IEnumerable<Tenant>>(tenants));
        _scopeFactory.CreateScope(Arg.Any<Tenant?>()).Returns(ci =>
        {
            var tenant = ci.Arg<Tenant?>()!;
            _scopedTenantIds.Add(tenant.Id);

            if (_failingTenantIds.Contains(tenant.Id))
                throw new InvalidOperationException("boom");

            var store = ReferenceEquals(tenant, Tenant.Default) ? _defaultScopeStore! : _ownStores[tenant.Id];
            var services = new ServiceCollection().AddSingleton(store).BuildServiceProvider();
            return new TenantScope(services.CreateScope(), _accessor, tenant);
        });
    }

    private TriggerBookmarkQueueRecurringTask CreateScopedTask(IBookmarkQueueStore store) =>
        new(Substitute.For<IBookmarkQueueWorker>(), _signaler, _accessor, store, _tenantService, null, _scopeFactory);

    private TriggerBookmarkQueueRecurringTask CreateTask(IBookmarkQueueStore store) =>
        new(Substitute.For<IBookmarkQueueWorker>(), _signaler, _accessor, store, _tenantService);
}

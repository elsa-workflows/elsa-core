using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Services;
using Elsa.Workflows.Runtime.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class OwnDatabaseTenantBookmarkQueuePurgerTests
{
    private const string SharedKey = "shared";
    private const string DefaultScopeKey = "default-scope";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private readonly DateTimeOffset _now = new(2026, 5, 20, 10, 0, 0, TimeSpan.Zero);
    private readonly DefaultTenantAccessor _accessor = new();
    private readonly ITenantService _tenantService = Substitute.For<ITenantService>();
    private readonly ITenantScopeFactory _scopeFactory = Substitute.For<ITenantScopeFactory>();
    private readonly ISystemClock _clock = Substitute.For<ISystemClock>();
    private readonly IIdentityGenerator _identityGenerator = Substitute.For<IIdentityGenerator>();
    private readonly Dictionary<string, Database> _databases = new();
    private readonly List<string> _scopedTenantIds = [];
    private readonly HashSet<string> _failingTenantIds = [];
    private Action<string>? _onScope;

    public OwnDatabaseTenantBookmarkQueuePurgerTests()
    {
        _identityGenerator.GenerateId().Returns(_ => Guid.NewGuid().ToString());
        _clock.UtcNow.Returns(_now);
        _databases[SharedKey] = CreateDatabase();
        _tenantService.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ci => Task.FromResult<Tenant?>(new Tenant { Id = ci.ArgAt<string>(0), Name = ci.ArgAt<string>(0) }));
        _scopeFactory.CreateScope(Arg.Any<Tenant?>()).Returns(ci =>
        {
            var tenant = ci.Arg<Tenant?>()!;
            _scopedTenantIds.Add(tenant.Id);
            _onScope?.Invoke(tenant.Id);

            if (_failingTenantIds.Contains(tenant.Id))
                throw new InvalidOperationException("boom");

            var database = _databases[ReferenceEquals(tenant, Tenant.Default) ? DefaultScopeKey : tenant.Id];
            var services = new ServiceCollection()
                .AddSingleton<IBookmarkQueueStore>(database.QueueStore)
                .AddSingleton<IBookmarkQueueDeadLetterStore>(database.DeadLetterStore)
                .AddSingleton<IBookmarkQueueDeadLetterManager>(database.Manager)
                .BuildServiceProvider();
            return new TenantScope(services.CreateScope(), _accessor, tenant);
        });
    }

    [Fact(DisplayName = "A tenant with its own database is purged in its own scope with tenant-scoped reads")]
    public async Task OwnDatabaseTenant_PurgedInOwnScope()
    {
        var tenant = OwnDatabaseTenant("tenant-own");
        UseTenants(tenant);
        await AddQueueItemAsync(SharedKey, "shared-expired", "t1");
        await AddQueueItemAsync(tenant.Id, "own-expired", tenant.Id);
        await AddQueueItemAsync(tenant.Id, "other-expired", "other");
        await AddDeadLetterAsync(tenant.Id, "own-old", tenant.Id);

        await CreatePurger().PurgeAsync().WaitAsync(Timeout);

        Assert.Equal(["tenant-own"], _scopedTenantIds);
        Assert.Empty(await QueueIdsAsync(SharedKey));
        Assert.Equal(["other-expired"], await QueueIdsAsync(tenant.Id));
        var deadLetters = (await _databases[tenant.Id].DeadLetterStore.FindManyAsync(new BookmarkQueueDeadLetterFilter { TenantAgnostic = true })).ToList();
        Assert.Contains(deadLetters, x => x.OriginalQueueItemId == "own-expired" && x.TenantId == tenant.Id);
        Assert.DoesNotContain(deadLetters, x => x.Id == "own-old");
    }

    [Fact(DisplayName = "Tenants sharing the default database never get a scope")]
    public async Task SharedDatabaseTenants_NoScope()
    {
        UseTenants(new Tenant { Id = "t1", Name = "t1" }, new Tenant { Id = "t2", Name = "t2" });
        await AddQueueItemAsync(SharedKey, "shared-expired", "t1");

        await CreatePurger().PurgeAsync().WaitAsync(Timeout);

        Assert.Empty(await QueueIdsAsync(SharedKey));
        _scopeFactory.DidNotReceiveWithAnyArgs().CreateScope(default);
    }

    [Fact(DisplayName = "A default tenant with its own database is purged in its own scope")]
    public async Task DefaultTenantWithOwnDatabase_PurgedInOwnScope()
    {
        var defaultTenant = OwnDatabaseTenant(Tenant.DefaultTenantId);
        UseTenants(defaultTenant);
        await AddQueueItemAsync(SharedKey, "shared-expired", "t1");
        await AddQueueItemAsync(defaultTenant.Id, "own-expired", Tenant.DefaultTenantId);

        await CreatePurger().PurgeAsync().WaitAsync(Timeout);

        Assert.Equal([Tenant.DefaultTenantId], _scopedTenantIds);
        Assert.Empty(await QueueIdsAsync(SharedKey));
        Assert.Empty(await QueueIdsAsync(defaultTenant.Id));
    }

    [Fact(DisplayName = "Under an own-database ambient tenant the shared pass uses the default tenant scope")]
    public async Task AmbientOwnDatabaseTenant_SharedPassUsesDefaultScope()
    {
        var tenant = OwnDatabaseTenant("tenant-own");
        UseTenants(tenant);
        _databases[DefaultScopeKey] = CreateDatabase();
        await AddQueueItemAsync(DefaultScopeKey, "default-expired", "t1");
        await AddQueueItemAsync(SharedKey, "ambient-store-expired", "t1");
        await AddQueueItemAsync(tenant.Id, "own-expired", tenant.Id);

        using (_accessor.PushContext(tenant))
            await CreatePurger().PurgeAsync().WaitAsync(Timeout);

        Assert.Equal([Tenant.DefaultTenantId, "tenant-own"], _scopedTenantIds);
        Assert.Empty(await QueueIdsAsync(DefaultScopeKey));
        Assert.Empty(await QueueIdsAsync(tenant.Id));
        Assert.Equal(["ambient-store-expired"], await QueueIdsAsync(SharedKey));
    }

    [Fact(DisplayName = "A failing tenant does not stop other tenants or the shared pass")]
    public async Task FailingTenant_OthersStillPurged()
    {
        var failing = OwnDatabaseTenant("tenant-failing");
        var healthy = OwnDatabaseTenant("tenant-healthy");
        UseTenants(failing, healthy);
        _failingTenantIds.Add(failing.Id);
        await AddQueueItemAsync(SharedKey, "shared-expired", "t1");
        await AddQueueItemAsync(failing.Id, "failing-expired", failing.Id);
        await AddQueueItemAsync(healthy.Id, "healthy-expired", healthy.Id);

        await CreatePurger().PurgeAsync().WaitAsync(Timeout);

        Assert.Empty(await QueueIdsAsync(SharedKey));
        Assert.Empty(await QueueIdsAsync(healthy.Id));
        Assert.Equal(["failing-expired"], await QueueIdsAsync(failing.Id));
    }

    [Fact(DisplayName = "A failing tenant listing still runs the shared pass")]
    public async Task FailingTenantListing_SharedPassStillRuns()
    {
        _tenantService.ListAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException<IEnumerable<Tenant>>(new InvalidOperationException("boom")));
        await AddQueueItemAsync(SharedKey, "shared-expired", "t1");

        await CreatePurger().PurgeAsync().WaitAsync(Timeout);

        Assert.Empty(await QueueIdsAsync(SharedKey));
    }

    [Fact(DisplayName = "A cancelled purge is not swallowed per tenant")]
    public async Task Cancellation_Propagates()
    {
        var tenant = OwnDatabaseTenant("tenant-own");
        UseTenants(tenant);
        using var cts = new CancellationTokenSource();
        _onScope = _ =>
        {
            cts.Cancel();
            throw new OperationCanceledException(cts.Token);
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreatePurger().PurgeAsync(cts.Token)).WaitAsync(Timeout);
    }

    [Fact(DisplayName = "The obsolete constructor purges the executing tenant only and never creates a scope")]
    public async Task ObsoleteConstructor_SingleTenant()
    {
        var tenant = OwnDatabaseTenant("tenant-own");
        UseTenants(tenant);
        await AddQueueItemAsync(SharedKey, "default-expired", Tenant.DefaultTenantId);
        await AddQueueItemAsync(SharedKey, "t1-expired", "t1");
        var shared = _databases[SharedKey];

#pragma warning disable CS0618
        var purger = new DefaultBookmarkQueuePurger(shared.QueueStore, shared.DeadLetterStore, shared.Manager, _clock, Microsoft.Extensions.Options.Options.Create(Options()), NullLogger<DefaultBookmarkQueuePurger>.Instance);
#pragma warning restore CS0618

        await purger.PurgeAsync().WaitAsync(Timeout);

        Assert.Equal(["t1-expired"], await QueueIdsAsync(SharedKey));
        _scopeFactory.DidNotReceiveWithAnyArgs().CreateScope(default);
    }

    private static BookmarkQueuePurgeOptions Options() => new() { Ttl = TimeSpan.FromMinutes(1), DeadLetterTtl = TimeSpan.FromDays(7) };

    private static Tenant OwnDatabaseTenant(string id) => new()
    {
        Id = id,
        Name = id,
        Configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Sqlite"] = $"Data Source={id}.db" }).Build()
    };

    private DefaultBookmarkQueuePurger CreatePurger()
    {
        var shared = _databases[SharedKey];
        return new(shared.QueueStore, shared.DeadLetterStore, shared.Manager, _clock, Microsoft.Extensions.Options.Options.Create(Options()), NullLogger<DefaultBookmarkQueuePurger>.Instance, _tenantService, _accessor, _scopeFactory);
    }

    private Database CreateDatabase()
    {
        var queueStore = new MemoryBookmarkQueueStore(new MemoryStore<BookmarkQueueItem>(), _accessor);
        var deadLetterStore = new MemoryBookmarkQueueDeadLetterStore(new MemoryStore<BookmarkQueueDeadLetterItem>());
        var manager = new BookmarkQueueDeadLetterManager(deadLetterStore, queueStore, Substitute.For<IBookmarkQueueSignaler>(), _clock, _identityGenerator, NullLogger<BookmarkQueueDeadLetterManager>.Instance);
        return new(queueStore, deadLetterStore, manager);
    }

    private void UseTenants(params Tenant[] tenants)
    {
        foreach (var tenant in tenants.Where(x => x.HasOwnDatabase()))
            _databases[tenant.Id] = CreateDatabase();

        _tenantService.ListAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IEnumerable<Tenant>>(tenants));
    }

    private Task AddQueueItemAsync(string database, string id, string tenantId) =>
        _databases[database].QueueStore.AddAsync(new BookmarkQueueItem
        {
            Id = id,
            TenantId = tenantId,
            WorkflowInstanceId = "workflow-instance",
            BookmarkId = "bookmark",
            StimulusHash = "hash",
            ActivityTypeName = "activity",
            CreatedAt = _now.AddMinutes(-2)
        });

    private Task AddDeadLetterAsync(string database, string id, string tenantId) =>
        _databases[database].DeadLetterStore.AddAsync(new BookmarkQueueDeadLetterItem
        {
            Id = id,
            TenantId = tenantId,
            OriginalQueueItemId = $"queue-{id}",
            OriginalCreatedAt = _now.AddDays(-9),
            DeadLetteredAt = _now.AddDays(-8),
            Reason = "Expired"
        });

    private async Task<List<string>> QueueIdsAsync(string database) =>
        (await _databases[database].QueueStore.FindManyAsync(new BookmarkQueueFilter { TenantAgnostic = true })).Select(x => x.Id).Order().ToList();

    private sealed record Database(MemoryBookmarkQueueStore QueueStore, MemoryBookmarkQueueDeadLetterStore DeadLetterStore, BookmarkQueueDeadLetterManager Manager);
}

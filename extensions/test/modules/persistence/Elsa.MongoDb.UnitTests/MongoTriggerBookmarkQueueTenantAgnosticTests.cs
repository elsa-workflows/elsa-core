using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.MongoDb.Common;
using Elsa.Persistence.MongoDb.Modules.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Elsa.MongoDb.UnitTests;

public sealed class MongoTriggerBookmarkQueueTenantAgnosticTests : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder().WithImage("mongo:7.0.24").Build();
    private readonly TestTenantAccessor _tenantAccessor = new();
    private MongoClient? _client;
    private MongoTriggerStore _triggerStore = null!;
    private MongoBookmarkQueueStore _bookmarkQueueStore = null!;

    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();

            _client = new MongoClient(_container.GetConnectionString());
            var database = _client.GetDatabase($"elsa-trigger-queue-tenants-{Guid.NewGuid():N}");
            _triggerStore = new MongoTriggerStore(new MongoDbStore<StoredTrigger>(
                database.GetCollection<StoredTrigger>("triggers"),
                _tenantAccessor));
            _bookmarkQueueStore = new MongoBookmarkQueueStore(new MongoDbStore<BookmarkQueueItem>(
                database.GetCollection<BookmarkQueueItem>("bookmark_queue"),
                _tenantAccessor));
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        try
        {
            _client?.Dispose();
        }
        finally
        {
            await _container.DisposeAsync();
        }
    }

    [Fact]
    public async Task TriggerStore_PagedFindMany_TenantAgnostic_ReturnsBothTenants()
    {
        // Arrange
        using (var tenantA = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" }))
            await _triggerStore.SaveAsync(Trigger("trigger-a"));

        using (var tenantB = _tenantAccessor.PushContext(new Tenant { Id = "tenant-b" }))
            await _triggerStore.SaveAsync(Trigger("trigger-b"));

        var pageArgs = PageArgs.FromPage(0, 10);
        var order = new StoredTriggerOrder<string>(x => x.Id, OrderDirection.Ascending);

        // Act
        Page<StoredTrigger> scoped;
        Page<StoredTrigger> agnostic;
        using (var tenantB = _tenantAccessor.PushContext(new Tenant { Id = "tenant-b" }))
        {
            scoped = await _triggerStore.FindManyAsync(new TriggerFilter(), pageArgs, order);
            agnostic = await _triggerStore.FindManyAsync(new TriggerFilter { TenantAgnostic = true }, pageArgs, order);
        }

        // Assert
        Assert.Equal(["trigger-b"], scoped.Items.Select(x => x.Id));
        Assert.Equal(1, scoped.TotalCount);
        Assert.Equal(["trigger-a", "trigger-b"], agnostic.Items.Select(x => x.Id));
        Assert.Equal(2, agnostic.TotalCount);
    }

    [Fact]
    public async Task BookmarkQueueStore_FindManyAndPage_TenantAgnostic_ReturnsBothTenants()
    {
        // Arrange
        using (var tenantA = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" }))
            await _bookmarkQueueStore.SaveAsync(QueueItem("queue-a"));

        using (var tenantB = _tenantAccessor.PushContext(new Tenant { Id = "tenant-b" }))
            await _bookmarkQueueStore.SaveAsync(QueueItem("queue-b"));

        var pageArgs = PageArgs.FromPage(0, 10);
        var order = new BookmarkQueueItemOrder<string>(x => x.Id, OrderDirection.Ascending);

        // Act
        IEnumerable<BookmarkQueueItem> scopedMany;
        IEnumerable<BookmarkQueueItem> agnosticMany;
        Page<BookmarkQueueItem> scopedPage;
        Page<BookmarkQueueItem> agnosticPage;
        using (var tenantB = _tenantAccessor.PushContext(new Tenant { Id = "tenant-b" }))
        {
            scopedMany = await _bookmarkQueueStore.FindManyAsync(new BookmarkQueueFilter());
            agnosticMany = await _bookmarkQueueStore.FindManyAsync(new BookmarkQueueFilter { TenantAgnostic = true });
            scopedPage = await _bookmarkQueueStore.PageAsync(pageArgs, new BookmarkQueueFilter(), order);
            agnosticPage = await _bookmarkQueueStore.PageAsync(pageArgs, new BookmarkQueueFilter { TenantAgnostic = true }, order);
        }

        // Assert
        Assert.Equal(["queue-b"], scopedMany.Select(x => x.Id));
        Assert.Equal(["queue-a", "queue-b"], agnosticMany.Select(x => x.Id).OrderBy(x => x));
        Assert.Equal(["queue-b"], scopedPage.Items.Select(x => x.Id));
        Assert.Equal(1, scopedPage.TotalCount);
        Assert.Equal(["queue-a", "queue-b"], agnosticPage.Items.Select(x => x.Id));
        Assert.Equal(2, agnosticPage.TotalCount);
    }

    private static StoredTrigger Trigger(string id) =>
        new()
        {
            Id = id,
            WorkflowDefinitionId = "definition-1",
            WorkflowDefinitionVersionId = "definition-1:1",
            ActivityId = "activity-1",
            Name = id
        };

    private static BookmarkQueueItem QueueItem(string id) =>
        new()
        {
            Id = id,
            WorkflowInstanceId = "instance-1",
            CreatedAt = DateTimeOffset.UtcNow
        };

    private sealed class TestTenantAccessor : ITenantAccessor
    {
        public string TenantId => Tenant?.Id ?? Tenant.DefaultTenantId;
        public Tenant? Tenant { get; private set; }

        public IDisposable PushContext(Tenant? tenant)
        {
            var previousTenant = Tenant;
            Tenant = tenant;
            return new Restore(() => Tenant = previousTenant);
        }

        private sealed class Restore(Action restore) : IDisposable
        {
            public void Dispose() => restore();
        }
    }
}

using Dapper;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.Dapper.Modules.Runtime.Records;
using Elsa.Persistence.Dapper.Modules.Runtime.Stores;
using Elsa.Persistence.Dapper.Services;
using Elsa.Workflows;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Microsoft.Data.Sqlite;
using NSubstitute;

namespace Elsa.Persistence.Dapper.UnitTests;

public sealed class DapperBookmarkQueueStoreTenantAgnosticTests : IDisposable
{
    private static readonly (string Id, string? TenantId, string CreatedAt)[] Items =
    [
        ("q-t1", "t1", "2026-01-02T00:00:00+00:00"),
        ("q-t2", "t2", "2026-01-02T00:00:00+00:00"),
        ("q-empty", "", "2026-01-01T00:00:00+00:00"),
        ("q-null", null, "2026-01-01T00:00:00+00:00")
    ];

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"elsa-dapper-bookmark-queue-{Guid.NewGuid():N}.db");

    [Fact(DisplayName = "PageAsync with TenantAgnostic returns the items of every tenant")]
    public async Task PageAsync_TenantAgnostic_ReturnsItemsOfEveryTenant()
    {
        var (store, tenantScope) = CreateSeededStoreForTenant1();
        using var _ = tenantScope;
        var order = new BookmarkQueueItemOrder<string>(x => x.Id, OrderDirection.Ascending);

        var page = await store.PageAsync(PageArgs.FromPage(0, 10), new BookmarkQueueFilter { TenantAgnostic = true }, order);

        Assert.Equal(["q-empty", "q-null", "q-t1", "q-t2"], page.Items.Select(x => x.Id));
        Assert.Equal(4, page.TotalCount);
    }

    [Fact(DisplayName = "PageAsync without TenantAgnostic returns only the ambient tenant's items")]
    public async Task PageAsync_Scoped_ReturnsOnlyAmbientTenantItems()
    {
        var (store, tenantScope) = CreateSeededStoreForTenant1();
        using var _ = tenantScope;
        var order = new BookmarkQueueItemOrder<string>(x => x.Id, OrderDirection.Ascending);

        var page = await store.PageAsync(PageArgs.FromPage(0, 10), new BookmarkQueueFilter(), order);

        Assert.Equal(["q-t1"], page.Items.Select(x => x.Id));
        Assert.Equal(1, page.TotalCount);
    }

    [Fact(DisplayName = "Paged FindManyAsync with TenantAgnostic returns the requested page of every tenant")]
    public async Task FindManyAsync_Paged_TenantAgnostic_ReturnsRequestedPage()
    {
        var (store, tenantScope) = CreateSeededStoreForTenant1();
        using var _ = tenantScope;
        var order = new BookmarkQueueItemOrder<string>(x => x.Id, OrderDirection.Ascending);

        var items = await store.FindManyAsync(new BookmarkQueueFilter { TenantAgnostic = true }, order, PageArgs.FromRange(1, 2));

        Assert.Equal(["q-null", "q-t1"], items.Select(x => x.Id));
    }

    [Fact(DisplayName = "Paged FindManyAsync without TenantAgnostic applies the ambient tenant and the filter")]
    public async Task FindManyAsync_Paged_Scoped_ReturnsOnlyAmbientTenantItems()
    {
        var (store, tenantScope) = CreateSeededStoreForTenant1();
        using var _ = tenantScope;
        var order = new BookmarkQueueItemOrder<string>(x => x.Id, OrderDirection.Ascending);

        var scoped = await store.FindManyAsync(new BookmarkQueueFilter(), order, PageArgs.FromRange(0, 10));
        var filtered = await store.FindManyAsync(new BookmarkQueueFilter { CreatedAtLessThan = DateTimeOffset.Parse("2026-01-01T12:00:00+00:00") }, order, PageArgs.FromRange(0, 10));

        Assert.Equal(["q-t1"], scoped.Select(x => x.Id));
        Assert.Empty(filtered);
    }

    [Fact(DisplayName = "FindManyAsync follows TenantAgnostic")]
    public async Task FindManyAsync_FollowsTenantAgnostic()
    {
        var (store, tenantScope) = CreateSeededStoreForTenant1();
        using var _ = tenantScope;

        var agnostic = await store.FindManyAsync(new BookmarkQueueFilter { TenantAgnostic = true });
        var scoped = await store.FindManyAsync(new BookmarkQueueFilter());

        Assert.Equal(["q-empty", "q-null", "q-t1", "q-t2"], agnostic.Select(x => x.Id).Order());
        Assert.Equal(["q-t1"], scoped.Select(x => x.Id));
    }

    [Fact(DisplayName = "ListTenantIdsAsync returns the distinct normalized tenant ids")]
    public async Task ListTenantIdsAsync_ReturnsDistinctNormalizedTenantIds()
    {
        var (store, tenantScope) = CreateSeededStoreForTenant1();
        using var _ = tenantScope;

        var tenantIds = await store.ListTenantIdsAsync();

        Assert.NotNull(tenantIds);
        Assert.Equal(["", "t1", "t2"], tenantIds.Order());
    }

    [Fact(DisplayName = "CreatedAtLessThan returns only the items created before the cutoff")]
    public async Task FindManyAsync_CreatedAtLessThan_ReturnsOnlyOlderItems()
    {
        var (store, tenantScope) = CreateSeededStoreForTenant1();
        using var _ = tenantScope;

        var older = await store.FindManyAsync(new BookmarkQueueFilter
        {
            CreatedAtLessThan = DateTimeOffset.Parse("2026-01-01T12:00:00+00:00"),
            TenantAgnostic = true
        });

        Assert.Equal(["q-empty", "q-null"], older.Select(x => x.Id).Order());
    }

    [Fact(DisplayName = "DeleteAsync with TenantAgnostic stays tenant-scoped")]
    public async Task DeleteAsync_TenantAgnostic_StaysTenantScoped()
    {
        var (store, tenantScope) = CreateSeededStoreForTenant1();
        using var _ = tenantScope;

        var deleted = await store.DeleteAsync(new BookmarkQueueFilter { Ids = ["q-t1", "q-t2"], TenantAgnostic = true });

        Assert.Equal(1, deleted);
        var remaining = await store.FindManyAsync(new BookmarkQueueFilter { TenantAgnostic = true });
        Assert.Equal(["q-empty", "q-null", "q-t2"], remaining.Select(x => x.Id).Order());
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(_databasePath))
            File.Delete(_databasePath);
    }

    private (DapperBookmarkQueueStore Store, IDisposable TenantScope) CreateSeededStoreForTenant1()
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = _databasePath, Pooling = false }.ToString();

        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();
            connection.Execute("""
                              create table BookmarkQueueItems (
                                  Id text not null primary key,
                                  TenantId text null,
                                  WorkflowInstanceId text null,
                                  BookmarkId text null,
                                  StimulusHash text null,
                                  ActivityInstanceId text null,
                                  ActivityTypeName text null,
                                  SerializedOptions text null,
                                  CreatedAt text not null
                              );
                              """);

            foreach (var (id, tenantId, createdAt) in Items)
            {
                connection.Execute(
                    """
                    insert into BookmarkQueueItems (Id, TenantId, WorkflowInstanceId, CreatedAt)
                    values (@id, @tenantId, 'instance-1', @createdAt)
                    """,
                    new { id, tenantId, createdAt });
            }
        }

        var tenantAccessor = new RoleTestTenantAccessor();
        var store = new DapperBookmarkQueueStore(
            new Store<BookmarkQueueItemRecord>(new SqliteDbConnectionProvider(connectionString), tenantAccessor, "BookmarkQueueItems"),
            Substitute.For<IPayloadSerializer>());
        return (store, tenantAccessor.PushContext(new Tenant { Id = "t1" }));
    }
}

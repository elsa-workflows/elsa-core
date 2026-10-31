using Dapper;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.Dapper.Modules.Runtime.Records;
using Elsa.Persistence.Dapper.Modules.Runtime.Stores;
using Elsa.Persistence.Dapper.Services;
using Elsa.Workflows;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Data.Sqlite;
using NSubstitute;

namespace Elsa.Persistence.Dapper.UnitTests;

public sealed class DapperBookmarkStoreTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"elsa-dapper-bookmarks-{Guid.NewGuid():N}.db");
    private readonly DapperBookmarkStore _store;
    private readonly TestTenantAccessor _tenantAccessor = new();

    public DapperBookmarkStoreTests()
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = _databasePath, Pooling = false }.ToString();
        var connectionProvider = new SqliteDbConnectionProvider(connectionString);

        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        connection.Execute("""
                          create table Bookmarks (
                              Id text not null primary key,
                              TenantId text null,
                              ActivityTypeName text not null,
                              Hash text not null,
                              WorkflowInstanceId text not null,
                              CorrelationId text null,
                              ActivityInstanceId text null,
                              SerializedPayload text null,
                              SerializedMetadata text null,
                              CreatedAt text not null
                          );
                          insert into Bookmarks (Id, TenantId, ActivityTypeName, Hash, WorkflowInstanceId, ActivityInstanceId, CreatedAt)
                          values ('a1', 'tenant-a', 'Test', 'hash-a1', 'workflow-a', 'activity-a1', '2026-01-01T00:00:00+00:00');
                          insert into Bookmarks (Id, TenantId, ActivityTypeName, Hash, WorkflowInstanceId, ActivityInstanceId, CreatedAt)
                          values ('a2', 'tenant-a', 'Test', 'hash-a2', 'workflow-other', 'activity-a2', '2026-01-02T00:00:00+00:00');
                          insert into Bookmarks (Id, TenantId, ActivityTypeName, Hash, WorkflowInstanceId, ActivityInstanceId, CreatedAt)
                          values ('a3', 'tenant-a', 'Test', 'hash-a3', 'workflow-a', 'activity-a3', '2026-01-03T00:00:00+00:00');
                          insert into Bookmarks (Id, TenantId, ActivityTypeName, Hash, WorkflowInstanceId, ActivityInstanceId, CreatedAt)
                          values ('b1', 'tenant-b', 'Test', 'hash-b1', 'workflow-b', 'activity-b1', '2026-01-04T00:00:00+00:00');
                          insert into Bookmarks (Id, TenantId, ActivityTypeName, Hash, WorkflowInstanceId, ActivityInstanceId, CreatedAt)
                          values ('b2', 'tenant-b', 'Test', 'hash-b2', 'workflow-b', 'activity-b2', '2026-01-05T00:00:00+00:00');
                          """);

        var store = new Store<StoredBookmarkRecord>(connectionProvider, _tenantAccessor, "Bookmarks");
        _store = new DapperBookmarkStore(store, Substitute.For<IPayloadSerializer>());
    }

    [Fact]
    public async Task FindAsync_WithBookmarkId_ReturnsOnlyRequestedBookmark()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });

        var bookmark = await _store.FindAsync(new BookmarkFilter { BookmarkId = "a2" });

        Assert.NotNull(bookmark);
        Assert.Equal("a2", bookmark.Id);
    }

    [Fact]
    public async Task FindManyAsync_WithBookmarkIds_ReturnsOnlyRequestedIdsForCurrentTenant()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });

        var bookmarks = await _store.FindManyAsync(new BookmarkFilter { BookmarkIds = ["a1", "b1"] });

        Assert.Equal(["a1"], bookmarks.Select(x => x.Id));
    }

    [Fact]
    public async Task FindManyAsync_WithBookmarkIds_ComposesWithOtherFilters()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });

        var bookmarks = await _store.FindManyAsync(new BookmarkFilter
        {
            BookmarkIds = ["a1", "a2"],
            WorkflowInstanceId = "workflow-a"
        });

        Assert.Equal(["a1"], bookmarks.Select(x => x.Id));
    }

    [Fact]
    public async Task FindManyAsync_WithUnknownOrEmptyBookmarkIds_ReturnsNoRows()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });

        var unknown = await _store.FindManyAsync(new BookmarkFilter { BookmarkIds = ["missing"] });
        var empty = await _store.FindManyAsync(new BookmarkFilter { BookmarkIds = [] });

        Assert.Empty(unknown);
        Assert.Empty(empty);
    }

    [Fact]
    public async Task FindManyAsync_WithoutBookmarkIds_PreservesUnfilteredBehavior()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });

        var bookmarks = await _store.FindManyAsync(new BookmarkFilter());

        Assert.Equal(["a1", "a2", "a3"], bookmarks.Select(x => x.Id).Order());
    }

    [Fact]
    public async Task FindManyAsync_WithBookmarkIds_PaginatesMatchingRowsAndReportsTotalCount()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        var filter = new BookmarkFilter { BookmarkIds = ["a1", "a2"] };

        var firstPage = await _store.FindManyAsync(filter, PageArgs.FromRange(0, 1));
        var secondPage = await _store.FindManyAsync(filter, PageArgs.FromRange(1, 1));

        Assert.Equal(["a1"], firstPage.Items.Select(x => x.Id));
        Assert.Equal(2, firstPage.TotalCount);
        Assert.Equal(["a2"], secondPage.Items.Select(x => x.Id));
        Assert.Equal(2, secondPage.TotalCount);
    }

    [Fact]
    public async Task DeleteAsync_WithBookmarkIds_DeletesOnlyRequestedRows()
    {
        using (var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" }))
        {
            var deleted = await _store.DeleteAsync(new BookmarkFilter { BookmarkIds = ["a1", "b1"] });

            Assert.Equal(1, deleted);
        }

        using var remainingTenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        var remainingTenantBookmarks = await _store.FindManyAsync(new BookmarkFilter { TenantAgnostic = true });

        Assert.Equal(["a2", "a3", "b1", "b2"], remainingTenantBookmarks.Select(x => x.Id).Order());
    }

    [Fact]
    public async Task FindManyAsync_WithTenantAgnosticBookmarkIds_ReturnsMatchingRowsAcrossTenants()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });

        var bookmarks = await _store.FindManyAsync(new BookmarkFilter
        {
            BookmarkIds = ["a1", "b1"],
            TenantAgnostic = true
        });

        Assert.Equal(["a1", "b1"], bookmarks.Select(x => x.Id).Order());
    }

    public void Dispose()
    {
        File.Delete(_databasePath);
    }

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

using Dapper;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.Dapper.Modules.Management.Records;
using Elsa.Persistence.Dapper.Modules.Management.Stores;
using Elsa.Persistence.Dapper.Services;
using Elsa.Workflows;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime;
using Microsoft.Data.Sqlite;
using NSubstitute;

namespace Elsa.Persistence.Dapper.UnitTests;

public sealed class DapperWorkflowInstanceStoreTenantAgnosticTests : IDisposable
{
    private static readonly Dictionary<string, string> TenantById = new()
    {
        ["i-t1"] = "t1",
        ["i-t2"] = "t2",
        ["i-empty"] = "",
        ["i-star"] = "*"
    };

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"elsa-dapper-instances-{Guid.NewGuid():N}.db");

    [Theory]
    [InlineData(true, new[] { "i-empty", "i-star", "i-t1", "i-t2" })]
    [InlineData(false, new[] { "i-star", "i-t1" })]
    public async Task ReadsFollowTenantAgnostic(bool tenantAgnostic, string[] expectedIds)
    {
        var (store, tenantScope) = CreateSeededStoreForTenant1();
        using var _ = tenantScope;

        var filter = new WorkflowInstanceFilter { TenantAgnostic = tenantAgnostic };
        var summaries = await store.EnumerateSummariesAsync(filter, 1).ToListAsync();

        Assert.Equal(expectedIds, summaries.Select(x => x.Id).Order());
        Assert.All(summaries, x => Assert.Equal(TenantById[x.Id], x.TenantId));
        Assert.Equal(expectedIds.Length, await store.CountAsync(filter));
    }

    [Theory]
    [InlineData(true, new[] { "i-empty", "i-star", "i-t1", "i-t2" })]
    [InlineData(false, new[] { "i-star", "i-t1" })]
    public async Task FindMethodsFollowTenantAgnostic(bool tenantAgnostic, string[] expectedIds)
    {
        var (store, tenantScope) = CreateSeededStoreForTenant1();
        using var _ = tenantScope;

        var filter = new WorkflowInstanceFilter { TenantAgnostic = tenantAgnostic };
        var instances = await store.FindManyAsync(filter);
        var ids = await store.FindManyIdsAsync(filter);
        var foundEmpty = await store.FindAsync(new WorkflowInstanceFilter { Id = "i-empty", TenantAgnostic = tenantAgnostic });
        var foundOther = await store.FindAsync(new WorkflowInstanceFilter { Id = "i-t2", TenantAgnostic = tenantAgnostic });

        Assert.Equal(expectedIds, instances.Select(x => x.Id).Order());
        Assert.Equal(expectedIds, ids.Order());
        Assert.Equal(tenantAgnostic, foundEmpty != null);
        Assert.Equal(tenantAgnostic, foundOther != null);
    }

    [Fact]
    public async Task DeleteWithTenantAgnostic_StaysTenantScoped()
    {
        var (store, tenantScope) = CreateSeededStoreForTenant1();
        using var _ = tenantScope;

        var deleted = await store.DeleteAsync(new WorkflowInstanceFilter { Ids = TenantById.Keys.ToList(), TenantAgnostic = true });

        Assert.Equal(1, deleted);
        var remaining = await store.FindManyIdsAsync(new WorkflowInstanceFilter { TenantAgnostic = true });
        Assert.Equal(["i-empty", "i-star", "i-t2"], remaining.Order());
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(_databasePath))
            File.Delete(_databasePath);
    }

    private (DapperWorkflowInstanceStore Store, IDisposable TenantScope) CreateSeededStoreForTenant1()
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = _databasePath, Pooling = false }.ToString();

        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();
            connection.Execute("""
                              create table WorkflowInstances (
                                  Id text not null primary key,
                                  TenantId text null,
                                  DefinitionId text not null,
                                  DefinitionVersionId text not null,
                                  Version integer not null,
                                  WorkflowState text not null,
                                  Status text not null,
                                  SubStatus text not null,
                                  IsExecuting integer not null,
                                  CorrelationId text null,
                                  Name text null,
                                  IncidentCount integer not null,
                                  IsSystem integer not null,
                                  CreatedAt text not null,
                                  UpdatedAt text not null,
                                  FinishedAt text null
                              );
                              """);

            foreach (var (id, tenantId) in TenantById)
            {
                connection.Execute(
                    """
                    insert into WorkflowInstances (Id, TenantId, DefinitionId, DefinitionVersionId, Version, WorkflowState, Status, SubStatus, IsExecuting, IncidentCount, IsSystem, CreatedAt, UpdatedAt)
                    values (@id, @tenantId, 'definition-1', 'definition-1:1', 1, '{}', 'Running', 'Interrupted', 0, 0, 0, '2026-01-01T00:00:00+00:00', '2026-01-01T00:00:00+00:00')
                    """,
                    new { id, tenantId });
            }
        }

        var tenantAccessor = new RoleTestTenantAccessor();
        var store = new DapperWorkflowInstanceStore(
            new Store<WorkflowInstanceRecord>(new SqliteDbConnectionProvider(connectionString), tenantAccessor, "WorkflowInstances"),
            Substitute.For<IWorkflowStateSerializer>());
        return (store, tenantAccessor.PushContext(new Tenant { Id = "t1" }));
    }
}

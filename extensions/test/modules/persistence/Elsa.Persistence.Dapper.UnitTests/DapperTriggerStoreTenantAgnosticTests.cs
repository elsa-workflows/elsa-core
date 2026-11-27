using Dapper;
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

/// <summary>
/// The Dapper trigger store keeps one shared row per unscheduled trigger of a tenant-agnostic workflow definition, so it must find rows of
/// every tenant on request and delete a shared row under the same authority as EF Core.
/// </summary>
public sealed class DapperTriggerStoreTenantAgnosticTests : IDisposable
{
    private static readonly (string Id, string? TenantId)[] Rows = [("trigger-shared", Tenant.AgnosticTenantId), ("trigger-t1", "t1"), ("trigger-t2", "t2")];
    private static readonly string[] AllIds = Rows.Select(x => x.Id).ToArray();

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"elsa-dapper-triggers-{Guid.NewGuid():N}.db");
    private readonly RoleTestTenantAccessor _tenantAccessor = new();
    private readonly DapperTriggerStore _store;

    public DapperTriggerStoreTenantAgnosticTests()
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = _databasePath, Pooling = false }.ToString();

        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();
            connection.Execute("""
                               create table Triggers (
                                   Id text not null primary key,
                                   TenantId text null,
                                   WorkflowDefinitionId text not null,
                                   WorkflowDefinitionVersionId text not null,
                                   Name text not null,
                                   ActivityId text not null,
                                   Hash text null,
                                   SerializedPayload text null
                               );
                               """);

            foreach (var (id, tenantId) in Rows)
            {
                connection.Execute(
                    """
                    insert into Triggers (Id, TenantId, WorkflowDefinitionId, WorkflowDefinitionVersionId, Name, ActivityId, Hash)
                    values (@id, @tenantId, 'definition', 'version', 'Elsa.Event', 'activity', 'hash')
                    """,
                    new { id, tenantId });
            }
        }

        _store = new(new Store<StoredTriggerRecord>(new SqliteDbConnectionProvider(connectionString), _tenantAccessor, "Triggers"), Substitute.For<IPayloadSerializer>());
    }

    [Fact(DisplayName = "FindManyAsync returns the rows of every tenant only for a tenant-agnostic filter")]
    public async Task FindManyAsync_FollowsTenantAgnostic()
    {
        using var _ = _tenantAccessor.PushContext(new() { Id = "t1" });

        var scoped = await _store.FindManyAsync(new TriggerFilter { Hash = "hash" });
        var agnostic = await _store.FindManyAsync(new TriggerFilter { Hash = "hash", TenantAgnostic = true });

        Assert.Equal(["trigger-shared", "trigger-t1"], scoped.Select(x => x.Id).Order());
        Assert.Equal(AllIds, agnostic.Select(x => x.Id).Order());
    }

    [Fact(DisplayName = "A tenant deletes a shared row only inside a shared write scope for triggers, and never another tenant's row")]
    public async Task DeleteManyAsync_DeletesSharedRowOnlyWithGrant()
    {
        using var _ = _tenantAccessor.PushContext(new() { Id = "t1" });

        Assert.Equal(1, await _store.DeleteManyAsync(new TriggerFilter { Ids = AllIds }));
        Assert.Equal(["trigger-shared", "trigger-t2"], await RemainingIdsAsync());

        using (SharedTenantWriteScope.Enter<StoredTrigger>())
            Assert.Equal(1, await _store.DeleteManyAsync(new TriggerFilter { Ids = AllIds, TenantAgnostic = true }));

        Assert.Equal(["trigger-t2"], await RemainingIdsAsync());
    }

    [Fact(DisplayName = "A \"*\" tenant context alone does not delete a shared row")]
    public async Task DeleteManyAsync_AgnosticContextWithoutGrant_DeletesNothing()
    {
        using var _ = _tenantAccessor.PushContext(new() { Id = Tenant.AgnosticTenantId });

        Assert.Equal(0, await _store.DeleteManyAsync(new TriggerFilter { Ids = AllIds }));

        using (SharedTenantWriteScope.Enter<StoredTrigger>())
            await _store.ReplaceAsync([new StoredTrigger { Id = "trigger-shared" }], []);

        Assert.Equal(["trigger-t1", "trigger-t2"], await RemainingIdsAsync());
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(_databasePath))
            File.Delete(_databasePath);
    }

    private async Task<IEnumerable<string>> RemainingIdsAsync() =>
        (await _store.FindManyAsync(new TriggerFilter { TenantAgnostic = true })).Select(x => x.Id).Order();
}

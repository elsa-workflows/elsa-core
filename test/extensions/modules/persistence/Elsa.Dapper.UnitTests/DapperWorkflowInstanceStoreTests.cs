using Dapper;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.Dapper.Modules.Management.Records;
using Elsa.Persistence.Dapper.Modules.Management.Stores;
using Elsa.Persistence.Dapper.Services;
using Elsa.Workflows;
using Microsoft.Data.Sqlite;
using NSubstitute;

namespace Elsa.Dapper.UnitTests;

public sealed class DapperWorkflowInstanceStoreTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"elsa-dapper-instances-{Guid.NewGuid():N}.db");
    private readonly string _connectionString;
    private readonly DapperWorkflowInstanceStore _store;
    private readonly TestTenantAccessor _tenantAccessor = new();

    public DapperWorkflowInstanceStoreTests()
    {
        _connectionString = new SqliteConnectionStringBuilder { DataSource = _databasePath, Pooling = false }.ToString();
        var connectionProvider = new SqliteDbConnectionProvider(_connectionString);

        using var connection = new SqliteConnection(_connectionString);
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
                               UpdatedAt text null,
                               FinishedAt text null
                           );
                           """);

        var store = new Store<WorkflowInstanceRecord>(connectionProvider, _tenantAccessor, "WorkflowInstances");
        _store = new DapperWorkflowInstanceStore(store, Substitute.For<IWorkflowStateSerializer>());
    }

    [Fact(DisplayName = "TryMarkInterruptedAsync marks a Running instance as Running+Interrupted")]
    public async Task TryMarkInterruptedAsync_MarksRunningInstance()
    {
        InsertInterruptible("running-1", WorkflowStatus.Running, WorkflowSubStatus.Executing, isExecuting: true);

        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        var marked = await _store.TryMarkInterruptedAsync("running-1");

        Assert.True(marked);
        Assert.Equal(("Running", "Interrupted", 0), ReadMarkers("running-1"));
    }

    [Fact(DisplayName = "TryMarkInterruptedAsync refuses Finished/Cancelled")]
    public async Task TryMarkInterruptedAsync_DoesNotOverwriteCancelledInstanceByDefault()
    {
        InsertInterruptible("cancelled-1", WorkflowStatus.Finished, WorkflowSubStatus.Cancelled, isExecuting: false);

        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        var marked = await _store.TryMarkInterruptedAsync("cancelled-1");

        Assert.False(marked);
        Assert.Equal(("Finished", "Cancelled", 0), ReadMarkers("cancelled-1"));
    }

    [Fact(DisplayName = "TryMarkInterruptedAsync refuses Finished/Cancelled even when allowFinishedCancelled is true")]
    public async Task TryMarkInterruptedAsync_RefusesCancelledWhenAllowed()
    {
        InsertInterruptible("cancelled-1", WorkflowStatus.Finished, WorkflowSubStatus.Cancelled, isExecuting: false);

        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        var marked = await _store.TryMarkInterruptedAsync("cancelled-1", allowFinishedCancelled: true);

        Assert.False(marked);
        Assert.Equal(("Finished", "Cancelled", 0), ReadMarkers("cancelled-1"));
    }

    [Fact(DisplayName = "TryMarkInterruptedAsync refuses Finished/Finished")]
    public async Task TryMarkInterruptedAsync_RefusesFinishedInstance()
    {
        InsertInterruptible("finished-1", WorkflowStatus.Finished, WorkflowSubStatus.Finished, isExecuting: false);

        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        var marked = await _store.TryMarkInterruptedAsync("finished-1");

        Assert.False(marked);
        Assert.Equal(("Finished", "Finished", 0), ReadMarkers("finished-1"));
    }

    [Fact(DisplayName = "TryMarkInterruptedAsync still refuses Finished/Finished when allowFinishedCancelled is true")]
    public async Task TryMarkInterruptedAsync_RefusesFinishedEvenWhenCancelledAllowed()
    {
        InsertInterruptible("finished-1", WorkflowStatus.Finished, WorkflowSubStatus.Finished, isExecuting: false);

        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        var marked = await _store.TryMarkInterruptedAsync("finished-1", allowFinishedCancelled: true);

        Assert.False(marked);
        Assert.Equal(("Finished", "Finished", 0), ReadMarkers("finished-1"));
    }

    [Fact(DisplayName = "TryMarkInterruptedAsync refuses Finished/Faulted")]
    public async Task TryMarkInterruptedAsync_RefusesFaultedInstance()
    {
        InsertInterruptible("faulted-1", WorkflowStatus.Finished, WorkflowSubStatus.Faulted, isExecuting: false);

        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        var marked = await _store.TryMarkInterruptedAsync("faulted-1");

        Assert.False(marked);
        Assert.Equal(("Finished", "Faulted", 0), ReadMarkers("faulted-1"));
    }

    [Fact(DisplayName = "TryMarkInterruptedAsync still refuses Finished/Faulted when allowFinishedCancelled is true")]
    public async Task TryMarkInterruptedAsync_RefusesFaultedEvenWhenCancelledAllowed()
    {
        InsertInterruptible("faulted-1", WorkflowStatus.Finished, WorkflowSubStatus.Faulted, isExecuting: false);

        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        var marked = await _store.TryMarkInterruptedAsync("faulted-1", allowFinishedCancelled: true);

        Assert.False(marked);
        Assert.Equal(("Finished", "Faulted", 0), ReadMarkers("faulted-1"));
    }

    [Fact(DisplayName = "TryMarkInterruptedAsync returns false when the instance is missing")]
    public async Task TryMarkInterruptedAsync_ReturnsFalseWhenMissing()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        var marked = await _store.TryMarkInterruptedAsync("missing-1");

        Assert.False(marked);
    }

    [Fact(DisplayName = "TryMarkInterruptedAsync does not mark a Running row that belongs to a different tenant")]
    public async Task TryMarkInterruptedAsync_DoesNotMarkRunningInstanceOfAnotherTenant()
    {
        InsertInterruptible("running-other", WorkflowStatus.Running, WorkflowSubStatus.Executing, isExecuting: true, tenantId: "tenant-b");

        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        var marked = await _store.TryMarkInterruptedAsync("running-other");

        Assert.False(marked);
        Assert.Equal(("Running", "Executing", 1), ReadMarkers("running-other"));
    }

    public void Dispose()
    {
        File.Delete(_databasePath);
    }

    private void InsertInterruptible(string id, WorkflowStatus status, WorkflowSubStatus subStatus, bool isExecuting, string tenantId = "tenant-a")
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        Insert(connection, id, DateTimeOffset.UtcNow, isExecuting, status.ToString(), subStatus.ToString(), tenantId);
    }

    private (string Status, string SubStatus, long IsExecuting) ReadMarkers(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.QuerySingle<(string, string, long)>(
            "select Status, SubStatus, IsExecuting from WorkflowInstances where Id = @Id",
            new { Id = id });
    }

    private static void Insert(SqliteConnection connection, string id, DateTimeOffset updatedAt, bool isExecuting, string status = "Running", string subStatus = "Executing", string tenantId = "tenant-a")
    {
        connection.Execute(
            """
            insert into WorkflowInstances
                (Id, TenantId, DefinitionId, DefinitionVersionId, Version, WorkflowState, Status, SubStatus, IsExecuting, IncidentCount, IsSystem, CreatedAt, UpdatedAt)
            values
                (@Id, @TenantId, 'definition-1', 'definition-1:1', 1, '{}', @Status, @SubStatus, @IsExecuting, 0, 0, @CreatedAt, @UpdatedAt);
            """,
            new
            {
                Id = id,
                TenantId = tenantId,
                Status = status,
                SubStatus = subStatus,
                IsExecuting = isExecuting,
                CreatedAt = updatedAt,
                UpdatedAt = updatedAt
            });
    }

    private sealed class TestTenantAccessor : ITenantAccessor
    {
        public string TenantId => Tenant?.Id ?? Elsa.Common.Multitenancy.Tenant.DefaultTenantId;
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

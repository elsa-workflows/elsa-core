using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.MongoDb.Common;
using Elsa.Persistence.MongoDb.Modules.Management;
using Elsa.Workflows;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.State;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NSubstitute;
using Testcontainers.MongoDb;

namespace Elsa.MongoDb.UnitTests;

public sealed class MongoWorkflowInstanceStoreTenantIsolationTests : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder().WithImage("mongo:7.0.24").Build();
    private readonly TestTenantAccessor _tenantAccessor = new();
    private MongoClient? _client;
    private MongoWorkflowInstanceStore _store = null!;

    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();

            _client = new MongoClient(_container.GetConnectionString());
            var database = _client.GetDatabase($"elsa-instance-tenants-{Guid.NewGuid():N}");
            var collection = database.GetCollection<WorkflowInstance>("workflow_instances");
            var mongoDbStore = new MongoDbStore<WorkflowInstance>(collection, _tenantAccessor);
            _store = new MongoWorkflowInstanceStore(mongoDbStore, Substitute.For<ILogger<MongoWorkflowInstanceStore>>());
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
    public async Task TenantB_CannotMarkOrSummarize_TenantARunningRow()
    {
        // Arrange
        using (var tenantA = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" }))
            await _store.AddAsync(Instance("running-a", WorkflowStatus.Running, WorkflowSubStatus.Executing, isExecuting: true));

        using (var tenantB = _tenantAccessor.PushContext(new Tenant { Id = "tenant-b" }))
            await _store.AddAsync(Instance("running-b", WorkflowStatus.Running, WorkflowSubStatus.Executing, isExecuting: true));

        // Act
        bool marked;
        Page<WorkflowInstanceSummary> summaries;
        using (var tenantB = _tenantAccessor.PushContext(new Tenant { Id = "tenant-b" }))
        {
            marked = await _store.TryMarkInterruptedAsync("running-a");
            summaries = await _store.SummarizeManyAsync(
                new WorkflowInstanceFilter(),
                PageArgs.FromPage(0, 10),
                new WorkflowInstanceOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending));
        }

        // Assert
        Assert.False(marked);
        Assert.Equal(["running-b"], summaries.Items.Select(x => x.Id));

        using var verifyTenantA = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        var stored = await _store.FindAsync(new WorkflowInstanceFilter { Id = "running-a" });
        Assert.NotNull(stored);
        Assert.Equal(WorkflowStatus.Running, stored.Status);
        Assert.Equal(WorkflowSubStatus.Executing, stored.SubStatus);
        Assert.True(stored.IsExecuting);
    }

    [Fact]
    public async Task SameTenant_CanMarkAndSummarize_OwnRunningRow()
    {
        // Arrange
        using (var tenantA = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" }))
            await _store.AddAsync(Instance("running-a", WorkflowStatus.Running, WorkflowSubStatus.Executing, isExecuting: true));

        // Act
        bool marked;
        Page<WorkflowInstanceSummary> summaries;
        using (var tenantA = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" }))
        {
            marked = await _store.TryMarkInterruptedAsync("running-a");
            summaries = await _store.SummarizeManyAsync(
                new WorkflowInstanceFilter(),
                PageArgs.FromPage(0, 10),
                new WorkflowInstanceOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending));
        }

        // Assert
        Assert.True(marked);
        var summary = Assert.Single(summaries.Items);
        Assert.Equal("running-a", summary.Id);
        Assert.Equal(WorkflowStatus.Running, summary.Status);
        Assert.Equal(WorkflowSubStatus.Interrupted, summary.SubStatus);

        using var verifyTenantA = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        var stored = await _store.FindAsync(new WorkflowInstanceFilter { Id = "running-a" });
        Assert.NotNull(stored);
        Assert.Equal(WorkflowStatus.Running, stored.Status);
        Assert.Equal(WorkflowSubStatus.Interrupted, stored.SubStatus);
        Assert.False(stored.IsExecuting);
    }

    private static WorkflowInstance Instance(string id, WorkflowStatus status, WorkflowSubStatus subStatus, bool isExecuting)
    {
        var now = DateTimeOffset.UtcNow;
        return new WorkflowInstance
        {
            Id = id,
            DefinitionId = "definition-1",
            DefinitionVersionId = "definition-1:1",
            Version = 1,
            WorkflowState = new WorkflowState
            {
                Id = id,
                DefinitionId = "definition-1",
                DefinitionVersionId = "definition-1:1",
                Status = status,
                SubStatus = subStatus,
                IsExecuting = isExecuting,
                CreatedAt = now,
                UpdatedAt = now
            },
            Status = status,
            SubStatus = subStatus,
            IsExecuting = isExecuting,
            CreatedAt = now,
            UpdatedAt = now
        };
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

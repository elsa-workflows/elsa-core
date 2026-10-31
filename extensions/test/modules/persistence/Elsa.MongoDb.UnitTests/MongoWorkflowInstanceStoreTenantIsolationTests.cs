using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.MongoDb.Common;
using Elsa.Persistence.MongoDb.Modules.Management;
using Elsa.Workflows;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime;
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

    [Theory]
    [InlineData(true, new[] { "i-default", "i-star", "i-t1", "i-t2" })]
    [InlineData(false, new[] { "i-star", "i-t1" })]
    public async Task ReadsFollowTenantAgnostic(bool tenantAgnostic, string[] expectedIds)
    {
        // Arrange
        var tenantById = await SeedTenantInstancesAsync();

        // Act
        using var tenant1 = _tenantAccessor.PushContext(new Tenant { Id = "t1" });
        var filter = new WorkflowInstanceFilter { TenantAgnostic = tenantAgnostic };
        var summaries = await _store.EnumerateSummariesAsync(filter, 1).ToListAsync();
        var count = await _store.CountAsync(filter);

        // Assert
        Assert.Equal(expectedIds, summaries.Select(x => x.Id).Order());
        Assert.All(summaries, x => Assert.Equal(tenantById[x.Id], x.TenantId));
        Assert.Equal(expectedIds.Length, count);
    }

    [Theory]
    [InlineData(true, new[] { "i-default", "i-star", "i-t1", "i-t2" })]
    [InlineData(false, new[] { "i-star", "i-t1" })]
    public async Task FindMethodsFollowTenantAgnostic(bool tenantAgnostic, string[] expectedIds)
    {
        // Arrange
        await SeedTenantInstancesAsync();

        // Act
        using var tenant1 = _tenantAccessor.PushContext(new Tenant { Id = "t1" });
        var filter = new WorkflowInstanceFilter { TenantAgnostic = tenantAgnostic };
        var instances = await _store.FindManyAsync(filter);
        var ids = await _store.FindManyIdsAsync(filter);
        var foundDefault = await _store.FindAsync(new WorkflowInstanceFilter { Id = "i-default", TenantAgnostic = tenantAgnostic });
        var foundOther = await _store.FindAsync(new WorkflowInstanceFilter { Id = "i-t2", TenantAgnostic = tenantAgnostic });

        // Assert
        Assert.Equal(expectedIds, instances.Select(x => x.Id).Order());
        Assert.Equal(expectedIds, ids.Order());
        Assert.Equal(tenantAgnostic, foundDefault != null);
        Assert.Equal(tenantAgnostic, foundOther != null);
    }

    [Fact]
    public async Task DeleteWithTenantAgnostic_StaysTenantScoped()
    {
        // Arrange
        await SeedTenantInstancesAsync();

        // Act
        long deleted;
        using (var tenant1 = _tenantAccessor.PushContext(new Tenant { Id = "t1" }))
            deleted = await _store.DeleteAsync(new WorkflowInstanceFilter { TenantAgnostic = true });

        // Assert
        Assert.Equal(1, deleted);
        using var verify = _tenantAccessor.PushContext(new Tenant { Id = "t1" });
        var remaining = await _store.FindManyIdsAsync(new WorkflowInstanceFilter { TenantAgnostic = true });
        Assert.Equal(["i-default", "i-star", "i-t2"], remaining.Order());
    }

    private async Task<Dictionary<string, string?>> SeedTenantInstancesAsync()
    {
        var tenantById = new Dictionary<string, string?> { ["i-t1"] = "t1", ["i-t2"] = "t2", ["i-default"] = null, ["i-star"] = Tenant.AgnosticTenantId };

        foreach (var (id, tenantId) in tenantById)
        {
            using var scope = _tenantAccessor.PushContext(tenantId == null ? null : new Tenant { Id = tenantId });
            await _store.AddAsync(Instance(id, WorkflowStatus.Running, WorkflowSubStatus.Interrupted, isExecuting: false));
        }

        return tenantById;
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

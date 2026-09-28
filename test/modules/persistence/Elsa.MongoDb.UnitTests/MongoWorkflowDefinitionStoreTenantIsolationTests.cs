using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.MongoDb.Common;
using Elsa.Persistence.MongoDb.Modules.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Elsa.MongoDb.UnitTests;

public sealed class MongoWorkflowDefinitionStoreTenantIsolationTests : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder().WithImage("mongo:7.0.24").Build();
    private readonly TestTenantAccessor _tenantAccessor = new();
    private MongoClient? _client;
    private MongoWorkflowDefinitionStore _store = null!;

    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();

            _client = new MongoClient(_container.GetConnectionString());
            var database = _client.GetDatabase($"elsa-definition-tenants-{Guid.NewGuid():N}");
            var collection = database.GetCollection<WorkflowDefinition>("workflow_definitions");
            var mongoDbStore = new MongoDbStore<WorkflowDefinition>(collection, _tenantAccessor);
            _store = new MongoWorkflowDefinitionStore(mongoDbStore);
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
    public async Task TenantB_PagedSummaries_NeitherContainNorCount_TenantADefinitions()
    {
        // Arrange
        using (var tenantA = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" }))
            await _store.SaveAsync(Definition("def-a", "id-a"));

        using (var tenantB = _tenantAccessor.PushContext(new Tenant { Id = "tenant-b" }))
            await _store.SaveAsync(Definition("def-b", "id-b"));

        // Act
        Page<WorkflowDefinitionSummary> unordered;
        Page<WorkflowDefinitionSummary> ordered;
        using (var tenantB = _tenantAccessor.PushContext(new Tenant { Id = "tenant-b" }))
        {
            var filter = new WorkflowDefinitionFilter();
            var pageArgs = PageArgs.FromPage(0, 10);
            unordered = await _store.FindSummariesAsync(filter, pageArgs);
            ordered = await _store.FindSummariesAsync(
                filter,
                new WorkflowDefinitionOrder<string>(x => x.Id, OrderDirection.Ascending),
                pageArgs);
        }

        // Assert
        Assert.Equal(["id-b"], unordered.Items.Select(x => x.Id));
        Assert.Equal(1, unordered.TotalCount);
        Assert.Equal(["id-b"], ordered.Items.Select(x => x.Id));
        Assert.Equal(1, ordered.TotalCount);
    }

    [Fact]
    public async Task TenantAgnostic_PagedSummaries_ReturnRowsFromBothTenants()
    {
        // Arrange
        using (var tenantA = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" }))
            await _store.SaveAsync(Definition("def-a", "id-a"));

        using (var tenantB = _tenantAccessor.PushContext(new Tenant { Id = "tenant-b" }))
            await _store.SaveAsync(Definition("def-b", "id-b"));

        // Act
        Page<WorkflowDefinitionSummary> unordered;
        Page<WorkflowDefinitionSummary> ordered;
        using (var tenantB = _tenantAccessor.PushContext(new Tenant { Id = "tenant-b" }))
        {
            var filter = new WorkflowDefinitionFilter { TenantAgnostic = true };
            var pageArgs = PageArgs.FromPage(0, 10);
            unordered = await _store.FindSummariesAsync(filter, pageArgs);
            ordered = await _store.FindSummariesAsync(
                filter,
                new WorkflowDefinitionOrder<string>(x => x.Id, OrderDirection.Ascending),
                pageArgs);
        }

        // Assert
        Assert.Equal(["id-a", "id-b"], unordered.Items.Select(x => x.Id).OrderBy(x => x));
        Assert.Equal(2, unordered.TotalCount);
        Assert.Equal(["id-a", "id-b"], ordered.Items.Select(x => x.Id));
        Assert.Equal(2, ordered.TotalCount);
    }

    private static WorkflowDefinition Definition(string definitionId, string id) =>
        new()
        {
            Id = id,
            DefinitionId = definitionId,
            Name = definitionId,
            Version = 1,
            IsLatest = true,
            MaterializerName = "Json"
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

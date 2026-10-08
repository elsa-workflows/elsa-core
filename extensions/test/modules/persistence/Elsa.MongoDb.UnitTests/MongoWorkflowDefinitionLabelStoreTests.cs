using Elsa.Common.Multitenancy;
using Elsa.Labels.Services;
using Elsa.Persistence.MongoDb.Common;
using Elsa.Persistence.MongoDb.Modules.Labels;
using Elsa.Workflows.Management.Filters;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using WorkflowDefinitionLabel = Elsa.Labels.Entities.WorkflowDefinitionLabel;

namespace Elsa.MongoDb.UnitTests;

public sealed class MongoWorkflowDefinitionLabelStoreTests : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder().WithImage("mongo:7.0.24").Build();
    private readonly TestTenantAccessor _tenantAccessor = new();
    private MongoClient? _client;
    private MongoWorkflowDefinitionLabelStore _store = null!;

    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();

            _client = new MongoClient(_container.GetConnectionString());
            var database = _client.GetDatabase($"elsa-labels-{Guid.NewGuid():N}");
            var collection = database.GetCollection<WorkflowDefinitionLabel>("workflow_definition_labels");
            var mongoDbStore = new MongoDbStore<WorkflowDefinitionLabel>(collection, _tenantAccessor);
            _store = new MongoWorkflowDefinitionLabelStore(mongoDbStore);
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
    public async Task FindByLabelIdsAsync_ReturnsAnyMatchingVersionForCurrentTenant()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        await SaveAsync(
            Record("association-a1", "definition-a", "version-a1", "label-a"),
            Record("association-a2", "definition-a", "version-a2", "label-b"),
            Record("association-a3", "definition-b", "version-b1", "label-c"));

        using var otherTenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-b" });
        await SaveAsync(Record("association-b1", "definition-b", "version-b1", "label-a"));

        using var queryTenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        var results = await _store.FindByLabelIdsAsync(["label-a", "label-b"]);

        Assert.Equal(["version-a1", "version-a2"], results.Select(x => x.WorkflowDefinitionVersionId).OrderBy(x => x));
        Assert.All(results, result => Assert.Equal("tenant-a", result.TenantId));
    }

    [Fact]
    public async Task FindByLabelIdsAsync_ReturnsNoRowsForEmptyOrUnknownLabelIds()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        await SaveAsync(Record("association-a1", "definition-a", "version-a1", "label-a"));

        Assert.Empty(await _store.FindByLabelIdsAsync([]));
        Assert.Empty(await _store.FindByLabelIdsAsync(["missing-label"]));
    }

    [Fact]
    public async Task WorkflowDefinitionLabelFilterProvider_AppliesAnyMatchingVersionIds()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        await SaveAsync(
            Record("association-a1", "definition-a", "version-a1", "label-a"),
            Record("association-a2", "definition-a", "version-a2", "label-b"));

        var filter = new WorkflowDefinitionFilter { LabelIds = ["label-b", "missing-label"] };
        var provider = new WorkflowDefinitionLabelFilterProvider(_store);

        await provider.ApplyAsync(filter);

        Assert.Equal(["version-a2"], filter.Ids);
    }

    private async Task SaveAsync(params WorkflowDefinitionLabel[] records)
    {
        await _store.SaveManyAsync(records);
    }

    private static WorkflowDefinitionLabel Record(string id, string workflowDefinitionId, string workflowDefinitionVersionId, string labelId) => new()
    {
        Id = id,
        WorkflowDefinitionId = workflowDefinitionId,
        WorkflowDefinitionVersionId = workflowDefinitionVersionId,
        LabelId = labelId
    };

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

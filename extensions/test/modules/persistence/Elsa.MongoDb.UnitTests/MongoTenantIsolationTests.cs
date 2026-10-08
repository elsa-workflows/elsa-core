using Elsa.Common.Entities;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.MongoDb.Common;
using Elsa.Workflows.Management.Entities;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Elsa.MongoDb.UnitTests;

public sealed class MongoTenantIsolationTests : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder().WithImage("mongo:7.0.24").Build();
    private readonly TestTenantAccessor _tenantAccessor = new();
    private MongoClient? _client;
    private MongoDbStore<WorkflowDefinition> _store = null!;

    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();

            _client = new MongoClient(_container.GetConnectionString());
            var database = _client.GetDatabase($"elsa-tenants-{Guid.NewGuid():N}");
            var collection = database.GetCollection<WorkflowDefinition>("workflow_definitions");
            _store = new MongoDbStore<WorkflowDefinition>(collection, _tenantAccessor);
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
    public async Task SaveManyAsync_PreservesExplicitTenantIds_AndStampsOnlyNullTenantIds()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        await SaveManyAsync(
        [
            Definition("agnostic", "agnostic", Tenant.AgnosticTenantId),
            Definition("other", "other", "tenant-b"),
            Definition("ambient", "ambient", null)
        ]);
        await _store.SaveAsync(Definition("single", "single", Tenant.AgnosticTenantId));

        var documents = (await _store.ListAsync(tenantAgnostic: true)).OrderBy(x => x.Id).ToList();

        Assert.Equal(
            [
                ("agnostic", Tenant.AgnosticTenantId),
                ("ambient", "tenant-a"),
                ("other", "tenant-b"),
                ("single", Tenant.AgnosticTenantId)
            ],
            documents.Select(x => (x.Id, x.TenantId)));
    }

    [Fact]
    public async Task ListAsync_ForTenant_IncludesOwnAndAgnosticDocuments_ButNotOtherTenants()
    {
        using (var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" }))
        {
            await SaveManyAsync(
            [
                Definition("agnostic", "agnostic", Tenant.AgnosticTenantId),
                Definition("a", "a", "tenant-a"),
                Definition("b", "b", "tenant-b")
            ]);
        }

        using var queryTenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        var documents = (await _store.ListAsync()).OrderBy(x => x.Id).ToList();

        Assert.Equal(["a", "agnostic"], documents.Select(x => x.Id));
    }

    [Fact]
    public async Task TenantScopedDelete_OnlyDeletesCurrentTenant_WhenCustomKeyIsShared()
    {
        using (var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" }))
        {
            await SaveManyAsync(
            [
                Definition("agnostic", "shared", Tenant.AgnosticTenantId),
                Definition("a", "shared", "tenant-a"),
                Definition("b", "shared", "tenant-b")
            ]);
        }

        using var deleteTenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        var deleted = await _store.DeleteWhereAsync(
            x => x.DefinitionId == "shared",
            nameof(WorkflowDefinition.DefinitionId),
            tenantAgnostic: false);

        Assert.Equal(1, deleted);
        var remaining = (await _store.ListAsync(tenantAgnostic: true)).OrderBy(x => x.Id).ToList();
        Assert.Equal([("agnostic", Tenant.AgnosticTenantId), ("b", "tenant-b")], remaining.Select(x => (x.Id, x.TenantId)));
    }

    [Fact]
    public async Task AmbientNullDelete_LeavesAgnosticDocuments_WhileExplicitAgnosticTenantCanManageThem()
    {
        using (var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" }))
        {
            await SaveManyAsync(
            [
                Definition("agnostic", "agnostic", Tenant.AgnosticTenantId),
                Definition("tenant", "tenant", "tenant-a")
            ]);
        }

        using (var defaultScope = _tenantAccessor.PushContext(null))
            await _store.SaveAsync(Definition("legacy", "legacy", null));

        using (var defaultScope = _tenantAccessor.PushContext(null))
        {
            var deleted = await _store.DeleteWhereAsync(
                x => x.Id == "agnostic" || x.Id == "tenant",
                nameof(WorkflowDefinition.Id),
                tenantAgnostic: false);

            Assert.Equal(0, deleted);

            deleted = await _store.DeleteWhereAsync(
                x => x.Id == "legacy",
                nameof(WorkflowDefinition.Id),
                tenantAgnostic: false);

            Assert.Equal(1, deleted);
        }

        using (var agnosticScope = _tenantAccessor.PushContext(new Tenant { Id = Tenant.AgnosticTenantId }))
        {
            var deleted = await _store.DeleteWhereAsync(
                x => x.Id == "agnostic",
                nameof(WorkflowDefinition.Id),
                tenantAgnostic: false);

            Assert.Equal(1, deleted);
        }

        var remaining = (await _store.ListAsync(tenantAgnostic: true)).ToList();
        Assert.Equal(["tenant"], remaining.Select(x => x.Id));
    }

    private static WorkflowDefinition Definition(string id, string definitionId, string? tenantId) => new()
    {
        Id = id,
        DefinitionId = definitionId,
        TenantId = tenantId
    };

    private Task SaveManyAsync(params WorkflowDefinition[] definitions) =>
        _store.SaveManyAsync(definitions, primaryKey: nameof(Entity.Id));

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

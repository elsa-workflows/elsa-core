using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Labels.Entities;
using Elsa.Persistence.MongoDb.Common;
using Elsa.Persistence.MongoDb.Modules.Labels;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Elsa.MongoDb.UnitTests;

public sealed class MongoLabelStoreTenantIsolationTests : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder().WithImage("mongo:7.0.24").Build();
    private readonly TestTenantAccessor _tenantAccessor = new();
    private MongoClient? _client;
    private MongoLabelStore _store = null!;

    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();

            _client = new MongoClient(_container.GetConnectionString());
            var database = _client.GetDatabase($"elsa-label-tenants-{Guid.NewGuid():N}");
            _store = new MongoLabelStore(
                new MongoDbStore<Label>(database.GetCollection<Label>("labels"), _tenantAccessor),
                new MongoDbStore<WorkflowDefinitionLabel>(database.GetCollection<WorkflowDefinitionLabel>("workflow_definition_labels"), _tenantAccessor));
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
    public async Task UnpagedList_ReturnsOnlyAmbientTenantLabels()
    {
        // Arrange
        using (var tenantA = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" }))
            await _store.SaveAsync(Label("label-a", "Alpha"));

        using (var tenantB = _tenantAccessor.PushContext(new Tenant { Id = "tenant-b" }))
            await _store.SaveAsync(Label("label-b", "Beta"));

        // Act
        Page<Label> page;
        using (var tenantB = _tenantAccessor.PushContext(new Tenant { Id = "tenant-b" }))
            page = await _store.ListAsync();

        // Assert
        Assert.Equal(["label-b"], page.Items.Select(x => x.Id));
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task PagedList_NeitherContainsNorCounts_OtherTenantLabels()
    {
        // Arrange
        using (var tenantA = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" }))
            await _store.SaveAsync(Label("label-a", "Alpha"));

        using (var tenantB = _tenantAccessor.PushContext(new Tenant { Id = "tenant-b" }))
            await _store.SaveAsync(Label("label-b", "Beta"));

        // Act
        Page<Label> page;
        using (var tenantB = _tenantAccessor.PushContext(new Tenant { Id = "tenant-b" }))
            page = await _store.ListAsync(PageArgs.FromPage(0, 10));

        // Assert
        Assert.Equal(["label-b"], page.Items.Select(x => x.Id));
        Assert.Equal(1, page.TotalCount);
    }

    private static Label Label(string id, string name) =>
        new()
        {
            Id = id,
            Name = name
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

using Elsa.Common;
using Elsa.Persistence.VNext.Builders;
using Elsa.Persistence.VNext.Contracts;
using Elsa.Persistence.VNext.Document;
using Elsa.Persistence.VNext.Extensions;
using Elsa.Persistence.VNext.Extensions.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Persistence.VNext.UnitTests;

public class PersistenceVNextIntegrationTests
{
    [Fact]
    public void SchemaCatalog_ComposesRegisteredManifests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPersistenceVNext();
        services.AddSingleton<IPersistenceSchemaProvider, OrdersSchemaProvider>();
        services.AddSingleton<IPersistenceSchemaProvider, CustomersSchemaProvider>();

        using var serviceProvider = services.BuildServiceProvider();
        var schema = serviceProvider.GetRequiredService<IPersistenceSchemaCatalog>().DescribeSchema();

        Assert.Equal("Elsa", schema.Name);
        Assert.Contains(schema.StorageUnits, x => x.Name == "Orders");
        Assert.Contains(schema.StorageUnits, x => x.Name == "Customers");
    }

    [Fact]
    public async Task StartupTask_MaterializesRegisteredDocumentStoresAndRecordsStatus()
    {
        var store = new RecordingDocumentStore();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPersistenceVNext();
        services.AddSingleton<IPersistenceSchemaProvider, OrdersSchemaProvider>();
        services.AddSingleton<IDocumentStore>(store);

        using var serviceProvider = services.BuildServiceProvider();
        var startupTask = Assert.Single(serviceProvider.GetServices<IStartupTask>());

        await startupTask.ExecuteAsync(CancellationToken.None);

        var status = serviceProvider.GetRequiredService<IPersistenceVNextStatus>().Snapshot;
        Assert.True(store.WasMaterialized);
        Assert.True(status.Succeeded);
        Assert.Contains("Orders", status.StorageUnits);
        Assert.Contains(typeof(RecordingDocumentStore).FullName!, status.DocumentStoreTypes);
    }

    [Fact]
    public async Task StartupTask_RecordsRecoveryHintsWhenMaterializationFails()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPersistenceVNext();
        services.AddSingleton<IPersistenceSchemaProvider, OrdersSchemaProvider>();
        services.AddSingleton<IDocumentStore, FailingDocumentStore>();

        using var serviceProvider = services.BuildServiceProvider();
        var startupTask = Assert.Single(serviceProvider.GetServices<IStartupTask>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => startupTask.ExecuteAsync(CancellationToken.None));

        var status = serviceProvider.GetRequiredService<IPersistenceVNextStatus>().Snapshot;
        Assert.False(status.Succeeded);
        Assert.Equal("provider unavailable", status.ErrorMessage);
        Assert.Contains(status.RecoveryHints, x => x.Contains("connect to its database"));
        Assert.Contains(status.RecoveryHints, x => x.Contains("materialization lock strategy"));
    }

    private class OrdersSchemaProvider : IPersistenceSchemaProvider
    {
        public PersistenceSchema DescribeSchema()
        {
            return new PersistenceSchemaBuilder("Orders")
                .StorageUnit("Orders", storage => storage
                    .RequiredField("Id", PersistenceColumnType.String, 450)
                    .RequiredField("Status", PersistenceColumnType.String, 50)
                    .Key("PK_Orders", "Id")
                    .Index("IX_Orders_Status", "Status"))
                .Build();
        }
    }

    private class CustomersSchemaProvider : IPersistenceSchemaProvider
    {
        public PersistenceSchema DescribeSchema()
        {
            return new PersistenceSchemaBuilder("Customers")
                .StorageUnit("Customers", storage => storage
                    .RequiredField("Id", PersistenceColumnType.String, 450)
                    .RequiredField("Name", PersistenceColumnType.String, 200)
                    .Key("PK_Customers", "Id")
                    .Index("IX_Customers_Name", "Name"))
                .Build();
        }
    }

    private class RecordingDocumentStore : IDocumentStore
    {
        public bool WasMaterialized { get; private set; }

        public Task MaterializeAsync(CancellationToken cancellationToken = default)
        {
            WasMaterialized = true;
            return Task.CompletedTask;
        }

        public Task<StoredDocument> SaveAsync(SaveDocumentRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StoredDocument?> LoadAsync(string storageUnit, string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(string storageUnit, string id, long? expectedVersion = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<StoredDocument>> QueryAsync(DocumentQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private class FailingDocumentStore : IDocumentStore
    {
        public Task MaterializeAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException("provider unavailable");
        public Task<StoredDocument> SaveAsync(SaveDocumentRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StoredDocument?> LoadAsync(string storageUnit, string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(string storageUnit, string id, long? expectedVersion = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<StoredDocument>> QueryAsync(DocumentQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}

using Elsa.Common;
using Elsa.Persistence.VNext.Builders;
using Elsa.Persistence.VNext.Contracts;
using Elsa.Persistence.VNext.Document;
using Elsa.Persistence.VNext.Extensions;
using Elsa.Persistence.VNext.Extensions.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Elsa.Persistence.VNext.UnitTests;

public class PersistenceVNextIntegrationTests
{
    [Test]
    public async Task SchemaCatalog_ComposesRegisteredManifests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPersistenceVNext();
        services.AddSingleton<IPersistenceSchemaProvider, OrdersSchemaProvider>();
        services.AddSingleton<IPersistenceSchemaProvider, CustomersSchemaProvider>();

        using var serviceProvider = services.BuildServiceProvider();
        var schema = serviceProvider.GetRequiredService<IPersistenceSchemaCatalog>().DescribeSchema();

        await Assert.That(schema.Name).IsEqualTo("Elsa");
        await Assert.That(schema.StorageUnits).Contains(x => x.Name == "Orders");
        await Assert.That(schema.StorageUnits).Contains(x => x.Name == "Customers");
    }

    [Test]
    public async Task StartupTask_MaterializesRegisteredDocumentStoresAndRecordsStatus()
    {
        var store = new RecordingDocumentStore();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPersistenceVNext();
        services.AddSingleton<IPersistenceSchemaProvider, OrdersSchemaProvider>();
        services.AddSingleton<IDocumentStore>(store);

        using var serviceProvider = services.BuildServiceProvider();
        var startupTask = await Assert.That(serviceProvider.GetServices<IStartupTask>()).HasSingleItem();

        await startupTask.ExecuteAsync(CancellationToken.None);

        var status = serviceProvider.GetRequiredService<IPersistenceVNextStatus>().Snapshot;
        await Assert.That(store.WasMaterialized).IsTrue();
        await Assert.That(status.Succeeded).IsTrue();
        await Assert.That(status.StorageUnits).Contains("Orders");
        await Assert.That(status.DocumentStoreTypes).Contains(typeof(RecordingDocumentStore).FullName!);
    }

    [Test]
    public async Task StartupTask_RecordsRecoveryHintsWhenMaterializationFails()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPersistenceVNext();
        services.AddSingleton<IPersistenceSchemaProvider, OrdersSchemaProvider>();
        services.AddSingleton<IDocumentStore, FailingDocumentStore>();

        using var serviceProvider = services.BuildServiceProvider();
        var startupTask = await Assert.That(serviceProvider.GetServices<IStartupTask>()).HasSingleItem();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => startupTask.ExecuteAsync(CancellationToken.None));

        var status = serviceProvider.GetRequiredService<IPersistenceVNextStatus>().Snapshot;
        await Assert.That(status.Succeeded).IsFalse();
        await Assert.That(status.ErrorMessage).IsEqualTo("provider unavailable");
        await Assert.That(status.RecoveryHints).Contains(x => x.Contains("connect to its database"));
        await Assert.That(status.RecoveryHints).Contains(x => x.Contains("materialization lock strategy"));
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

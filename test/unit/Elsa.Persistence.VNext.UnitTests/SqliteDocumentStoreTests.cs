using Elsa.Persistence.VNext.Builders;
using Elsa.Persistence.VNext.Document;
using Elsa.Persistence.VNext.Sqlite;
using Microsoft.Data.Sqlite;
using System.Threading.Tasks;

namespace Elsa.Persistence.VNext.UnitTests;

public class SqliteDocumentStoreTests : IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly SqliteDocumentStore _store;

    public SqliteDocumentStoreTests()
    {
        _store = new SqliteDocumentStore(_connection, CreateSchema());
    }

    [Test]
    public async Task SaveAndLoadAsync_PersistsDocumentEnvelope()
    {
        await ActivateAsync();

        var saved = await SaveAsync("order-1", """{"number":"1001","status":"Open","customerId":"customer-1"}""", "Open", "customer-1");
        var loaded = await _store.LoadAsync("Orders", "order-1");

        await Assert.That(loaded).IsNotNull();
        await Assert.That(loaded.StorageUnit).IsEqualTo("Orders");
        await Assert.That(loaded.Id).IsEqualTo("order-1");
        await Assert.That(loaded.Content).IsEqualTo(saved.Content);
        await Assert.That(loaded.Version).IsEqualTo(1);
        await Assert.That(loaded.CreatedAt <= loaded.UpdatedAt).IsTrue();
    }

    [Test]
    public async Task SaveAsync_ReplacesIndexValuesInSameDocumentVersion()
    {
        await ActivateAsync();

        var first = await SaveAsync("order-1", """{"status":"Open","customerId":"customer-1"}""", "Open", "customer-1");
        await SaveAsync("order-1", """{"status":"Closed","customerId":"customer-2"}""", "Closed", "customer-2", first.Version);

        var openOrders = await QueryByStatusAsync("Open");
        var closedOrders = await QueryByStatusAsync("Closed");
        var customerOrders = await QueryByCustomerAndStatusAsync("customer-2", "Closed");

        await Assert.That(openOrders).IsEmpty();
        var closedOrder = await Assert.That(closedOrders).HasSingleItem();
        await Assert.That(closedOrder.Version).IsEqualTo(2);
        await Assert.That(customerOrders).HasSingleItem();
    }

    [Test]
    public async Task QueryAsync_ReturnsDocumentsByDeclaredIndex()
    {
        await ActivateAsync();
        await SaveAsync("order-1", """{"status":"Open","customerId":"customer-1"}""", "Open", "customer-1");
        await SaveAsync("order-2", """{"status":"Closed","customerId":"customer-1"}""", "Closed", "customer-1");
        await SaveAsync("order-3", """{"status":"Open","customerId":"customer-2"}""", "Open", "customer-2");

        var results = await QueryByCustomerAndStatusAsync("customer-1", "Closed");

        var result = await Assert.That(results).HasSingleItem();
        await Assert.That(result.Id).IsEqualTo("order-2");
    }

    [Test]
    public async Task QueryAsync_RejectsUndeclaredIndex()
    {
        await ActivateAsync();
        await SaveAsync("order-1", """{"status":"Open","customerId":"customer-1","priority":"High"}""", "Open", "customer-1");

        var query = new DocumentQuery("Orders", new Dictionary<string, string?> { ["Priority"] = "High" });
        await Assert.ThrowsExactlyAsync<DocumentQueryNotIndexedException>(() => _store.QueryAsync(query));
    }

    [Test]
    public async Task SaveAsync_RejectsStaleVersion()
    {
        await ActivateAsync();
        await SaveAsync("order-1", """{"status":"Open","customerId":"customer-1"}""", "Open", "customer-1");
        await SaveAsync("order-1", """{"status":"Closed","customerId":"customer-1"}""", "Closed", "customer-1", expectedVersion: 1);

        await Assert.ThrowsExactlyAsync<DocumentStoreConcurrencyException>(() =>
            SaveAsync("order-1", """{"status":"Cancelled","customerId":"customer-1"}""", "Cancelled", "customer-1", expectedVersion: 1));
    }

    [Test]
    public async Task DeleteAsync_RemovesDocumentAndIndexRows()
    {
        await ActivateAsync();
        var saved = await SaveAsync("order-1", """{"status":"Open","customerId":"customer-1"}""", "Open", "customer-1");

        var deleted = await _store.DeleteAsync("Orders", "order-1", saved.Version);
        var loaded = await _store.LoadAsync("Orders", "order-1");
        var results = await QueryByStatusAsync("Open");

        await Assert.That(deleted).IsTrue();
        await Assert.That(loaded).IsNull();
        await Assert.That(results).IsEmpty();
    }

    private async Task ActivateAsync()
    {
        await _store.MaterializeAsync();
    }

    private Task<StoredDocument> SaveAsync(string id, string content, string status, string customerId, long? expectedVersion = null)
    {
        var request = new SaveDocumentRequest(
            "Orders",
            id,
            content,
            new Dictionary<string, string?>
            {
                ["Status"] = status,
                ["CustomerId"] = customerId
            },
            expectedVersion);

        return _store.SaveAsync(request);
    }

    private Task<IReadOnlyList<StoredDocument>> QueryByStatusAsync(string status)
    {
        return _store.QueryAsync(new DocumentQuery("Orders", new Dictionary<string, string?> { ["Status"] = status }));
    }

    private Task<IReadOnlyList<StoredDocument>> QueryByCustomerAndStatusAsync(string customerId, string status)
    {
        return _store.QueryAsync(new DocumentQuery("Orders", new Dictionary<string, string?>
        {
            ["CustomerId"] = customerId,
            ["Status"] = status
        }));
    }

    private static PersistenceSchema CreateSchema()
    {
        return new PersistenceSchemaBuilder("Orders")
            .StorageUnit("Orders", storage => storage
                .RequiredField("Id", PersistenceColumnType.String, 450)
                .RequiredField("Status", PersistenceColumnType.String, 50)
                .RequiredField("CustomerId", PersistenceColumnType.String, 450)
                .Key("PK_Orders", "Id")
                .Index("IX_Orders_Status", "Status")
                .Index("IX_Orders_CustomerId_Status", ["CustomerId", "Status"]))
            .Build();
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}

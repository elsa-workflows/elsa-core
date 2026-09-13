using Elsa.Persistence.VNext.Document;
using Elsa.Persistence.VNext.Runtime;
using Elsa.Persistence.VNext.Runtime.Models;
using Elsa.Persistence.VNext.Runtime.Services;
using Elsa.Persistence.VNext.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace Elsa.Persistence.VNext.UnitTests;

public class RuntimeEntityTests : IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly SqliteDocumentStore _store;
    private readonly RuntimeEntityManager _manager;

    public RuntimeEntityTests()
    {
        _store = new SqliteDocumentStore(_connection, new RuntimeEntityPersistenceSchemaProvider().DescribeSchema());
        _manager = new RuntimeEntityManager(_store, new RuntimeEntityDefinitionValidator(Microsoft.Extensions.Options.Options.Create(new RuntimeEntityOptions())));
    }

    [Test]
    public async Task SchemaProvider_UsesFixedStorageUnitsAndRuntimeIndexSlots()
    {
        var schema = new RuntimeEntityPersistenceSchemaProvider().DescribeSchema();

        await Assert.That(schema.StorageUnits).Contains(x => x.Name == RuntimeEntityPersistenceSchemaProvider.DefinitionsStorageUnit);
        var instances = await Assert.That(schema.StorageUnits).HasSingleItem(x => x.Name == RuntimeEntityPersistenceSchemaProvider.InstancesStorageUnit);
        await Assert.That(instances.Indexes).Contains(x => x.Name == "IX_RuntimeEntityInstances_Index1");
        await Assert.That(instances.Indexes).Contains(x => x.Name == "IX_RuntimeEntityInstances_Index4");
    }

    [Test]
    public async Task RuntimeEntityManager_PublishesPersistsQueriesAndAuditsRuntimeEntities()
    {
        await ActivateAsync();
        await _manager.SaveDraftAsync(CreateCustomerDefinition());
        var published = await _manager.PublishAsync("Customer");

        await _manager.SaveInstanceAsync(new RuntimeEntityInstance
        {
            Id = "customer-1",
            DefinitionName = "Customer",
            Data =
            {
                ["email"] = "one@example.com",
                ["tier"] = "Gold"
            }
        });
        await _manager.SaveInstanceAsync(new RuntimeEntityInstance
        {
            Id = "customer-2",
            DefinitionName = "Customer",
            Data =
            {
                ["email"] = "two@example.com",
                ["tier"] = "Silver"
            }
        });

        var goldCustomers = await _manager.QueryInstancesAsync("Customer", "tier", "Gold");
        var loaded = await _manager.GetInstanceAsync("Customer", "customer-1");
        var deleted = await _manager.DeleteInstanceAsync("Customer", "customer-2");
        var deletedCustomer = await _manager.GetInstanceAsync("Customer", "customer-2");
        var audit = await _manager.ListAuditAsync("customer:customer-1");

        await Assert.That(published.Status).IsEqualTo(RuntimeEntityDefinitionStatus.Published);
        var customer = await Assert.That(goldCustomers).HasSingleItem();
        await Assert.That(customer.Id).IsEqualTo("customer-1");
        await Assert.That(loaded).IsNotNull();
        await Assert.That(loaded!.Data["email"]!.ToString()).IsEqualTo("one@example.com");
        await Assert.That(deleted).IsTrue();
        await Assert.That(deletedCustomer).IsNull();
        await Assert.That(audit).Contains(x => x.Action == "Created");
    }

    [Test]
    public async Task RuntimeEntityManager_RejectsQueriesForUndeclaredIndexes()
    {
        await ActivateAsync();
        await _manager.SaveDraftAsync(CreateCustomerDefinition());
        await _manager.PublishAsync("Customer");

        await Assert.ThrowsExactlyAsync<DocumentQueryNotIndexedException>(() =>
            _manager.QueryInstancesAsync("Customer", "email", "one@example.com"));
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    private async Task ActivateAsync()
    {
        await _connection.OpenAsync();
        await _store.MaterializeAsync();
    }

    private static RuntimeEntityDefinition CreateCustomerDefinition()
    {
        return new RuntimeEntityDefinition
        {
            Name = "Customer",
            Fields =
            {
                new("email", RuntimeEntityFieldType.String, IsRequired: true),
                new("tier", RuntimeEntityFieldType.String)
            },
            Indexes =
            {
                new("IX_Customer_Tier", "tier")
            }
        };
    }
}

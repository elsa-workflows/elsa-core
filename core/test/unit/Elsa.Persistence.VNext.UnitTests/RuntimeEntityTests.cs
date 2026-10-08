using Elsa.Persistence.VNext.Document;
using Elsa.Persistence.VNext.Runtime;
using Elsa.Persistence.VNext.Runtime.Models;
using Elsa.Persistence.VNext.Runtime.Services;
using Elsa.Persistence.VNext.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

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

    [Fact]
    public void SchemaProvider_UsesFixedStorageUnitsAndRuntimeIndexSlots()
    {
        var schema = new RuntimeEntityPersistenceSchemaProvider().DescribeSchema();

        Assert.Contains(schema.StorageUnits, x => x.Name == RuntimeEntityPersistenceSchemaProvider.DefinitionsStorageUnit);
        var instances = Assert.Single(schema.StorageUnits, x => x.Name == RuntimeEntityPersistenceSchemaProvider.InstancesStorageUnit);
        Assert.Contains(instances.Indexes, x => x.Name == "IX_RuntimeEntityInstances_Index1");
        Assert.Contains(instances.Indexes, x => x.Name == "IX_RuntimeEntityInstances_Index4");
    }

    [Fact]
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

        Assert.Equal(RuntimeEntityDefinitionStatus.Published, published.Status);
        var customer = Assert.Single(goldCustomers);
        Assert.Equal("customer-1", customer.Id);
        Assert.NotNull(loaded);
        Assert.Equal("one@example.com", loaded!.Data["email"]!.ToString());
        Assert.True(deleted);
        Assert.Null(deletedCustomer);
        Assert.Contains(audit, x => x.Action == "Created");
    }

    [Fact]
    public async Task RuntimeEntityManager_RejectsQueriesForUndeclaredIndexes()
    {
        await ActivateAsync();
        await _manager.SaveDraftAsync(CreateCustomerDefinition());
        await _manager.PublishAsync("Customer");

        await Assert.ThrowsAsync<DocumentQueryNotIndexedException>(() =>
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

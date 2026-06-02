using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;
using Elsa.ModularPersistence.Queries;
using Elsa.ModularPersistence.Runtime;

namespace Elsa.ModularPersistence.UnitTests.Runtime;

public class RuntimeEntityDataServiceTests
{
    private readonly InMemoryRuntimeStorageDefinitionStore _definitionStore = new();
    private readonly InMemoryDocumentStoreFactoryRegistry _storeFactoryRegistry = new();
    private readonly RuntimeEntityDataService _service;
    private readonly RuntimeStorageOperationContext _context = new("alice", ["read:customers", "write:customers"]);

    public RuntimeEntityDataServiceTests()
    {
        _service = new RuntimeEntityDataService(_definitionStore, _storeFactoryRegistry);
    }

    [Fact]
    public async Task CreateGetQueryUpdateAndDeleteUseRuntimeDefinitionManifest()
    {
        await _definitionStore.SaveAsync(CreateDefinition(RuntimeStorageDefinitionState.Published));

        var created = await _service.CreateAsync("customers", new RuntimeEntitySaveRequest("customer-1", """{"Id":"customer-1","Email":"a@example.com"}"""), _context);
        var loaded = await _service.GetAsync("customers", "customer-1", null, _context);
        var queried = await _service.QueryAsync(
            "customers",
            new RuntimeEntityQueryRequest(
                [
                    new RuntimeEntityQueryFilter("Email", DocumentQueryFilterOperator.Equals, [DocumentQueryValue.String("a@example.com")])
                ]),
            _context);
        var updated = await _service.UpdateAsync("customers", new RuntimeEntitySaveRequest("customer-1", """{"Id":"customer-1","Email":"b@example.com"}""", ExpectedVersion: created.Version), _context);
        var deleted = await _service.DeleteAsync("customers", "customer-1", null, _context);

        Assert.Equal(1, created.Version);
        Assert.Equal("customer-1", loaded?.Id);
        Assert.Equal("customer-1", Assert.Single(queried).Id);
        Assert.Equal(2, updated.Version);
        Assert.True(deleted);
        Assert.Null(await _service.GetAsync("customers", "customer-1", null, _context));
    }

    [Fact]
    public async Task OperationsRejectUnpublishedDefinitions()
    {
        await _definitionStore.SaveAsync(CreateDefinition(RuntimeStorageDefinitionState.Draft));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.GetAsync("customers", "customer-1", null, _context));

        Assert.Contains("not published", exception.Message);
    }

    [Fact]
    public async Task OperationsEnforceDefinitionPermissions()
    {
        await _definitionStore.SaveAsync(CreateDefinition(RuntimeStorageDefinitionState.Published));
        var context = new RuntimeStorageOperationContext("bob", ["read:other"]);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await _service.GetAsync("customers", "customer-1", null, context));

        Assert.Contains("read:customers", exception.Message);
    }

    [Fact]
    public async Task QueryRequiresIndexedFields()
    {
        await _definitionStore.SaveAsync(CreateDefinition(RuntimeStorageDefinitionState.Published));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.QueryAsync(
            "customers",
            new RuntimeEntityQueryRequest(
                [
                    new RuntimeEntityQueryFilter("Name", DocumentQueryFilterOperator.Equals, [DocumentQueryValue.String("Alice")])
                ]),
            _context));

        Assert.Contains("not indexed", exception.Message);
    }

    private static RuntimeStorageDefinition CreateDefinition(RuntimeStorageDefinitionState state) =>
        new(
            "customers",
            "runtime.customers",
            "Customers",
            [
                new RuntimeStorageFieldDefinition("Id", StorageFieldType.String, true),
                new RuntimeStorageFieldDefinition("Email", StorageFieldType.String, true),
                new RuntimeStorageFieldDefinition("Name", StorageFieldType.String)
            ],
            [
                new RuntimeStorageIndexDefinition("IX_Customers_Email", ["Email"])
            ],
            ["read:customers", "write:customers"])
        {
            State = state
        };

    private sealed class InMemoryDocumentStoreFactoryRegistry : IRuntimeEntityDocumentStoreFactoryRegistry
    {
        private readonly InMemoryDocumentStore _store = new();

        public IDocumentStore CreateStore(StorageManifestDescriptor manifest, string? providerName) => _store;
    }

    private sealed class InMemoryDocumentStore : IDocumentStore
    {
        private readonly Dictionary<DocumentKey, DocumentEnvelope> _documents = new();

        public ValueTask<IDocumentSession> OpenSessionAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IDocumentSession>(new Session(_documents));

        private sealed class Session(Dictionary<DocumentKey, DocumentEnvelope> documents) : IDocumentSession
        {
            public ValueTask<DocumentEnvelope?> LoadAsync(DocumentKey key, CancellationToken cancellationToken = default) =>
                ValueTask.FromResult(documents.GetValueOrDefault(key));

            public ValueTask<DocumentSaveResult> SaveAsync(DocumentEnvelope document, ExpectedDocumentVersion expectedVersion = default, CancellationToken cancellationToken = default)
            {
                documents.TryGetValue(document.Key, out var existing);
                ValidateConcurrency(document.Key, existing?.Version, expectedVersion);
                documents[document.Key] = document;
                return ValueTask.FromResult(new DocumentSaveResult(document.Key, document.Version));
            }

            public ValueTask<IReadOnlyCollection<DocumentEnvelope>> QueryAsync(DocumentQuery query, CancellationToken cancellationToken = default)
            {
                var results = documents.Values
                    .Where(x => x.Type == query.DocumentType)
                    .Where(document => query.Filters.All(filter => Matches(document, filter)))
                    .ToArray();
                return ValueTask.FromResult<IReadOnlyCollection<DocumentEnvelope>>(results);
            }

            public ValueTask DeleteAsync(DocumentKey key, ExpectedDocumentVersion expectedVersion = default, CancellationToken cancellationToken = default)
            {
                documents.TryGetValue(key, out var existing);
                ValidateConcurrency(key, existing?.Version, expectedVersion);
                documents.Remove(key);
                return ValueTask.CompletedTask;
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;

            private static bool Matches(DocumentEnvelope document, DocumentQueryFilter filter)
            {
                if (filter.Operator != DocumentQueryFilterOperator.Equals)
                    throw new NotSupportedException();

                var json = System.Text.Json.JsonDocument.Parse(document.Data);
                var value = json.RootElement.GetProperty(filter.FieldName).GetString();
                return string.Equals(value, filter.Values.Single().TextValue, StringComparison.Ordinal);
            }

            private static void ValidateConcurrency(DocumentKey key, long? currentVersion, ExpectedDocumentVersion expectedVersion)
            {
                if (expectedVersion.Kind == ExpectedDocumentVersionKind.New && currentVersion is not null)
                    throw new DocumentConcurrencyException(key, "Document already exists.");

                if (expectedVersion.Kind == ExpectedDocumentVersionKind.Exact && currentVersion != expectedVersion.Version)
                    throw new DocumentConcurrencyException(key, "Document version did not match.");
            }
        }
    }
}

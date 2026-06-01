using BenchmarkDotNet.Attributes;
using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;
using Elsa.ModularPersistence.Queries;
using Elsa.ModularPersistence.Runtime;

namespace Elsa.Workflows.PerformanceTests;

[Config(typeof(Config))]
public class RuntimeEntityDataBenchmark
{
    private readonly RuntimeStorageOperationContext _context = RuntimeStorageOperationContext.System;
    private InMemoryRuntimeStorageDefinitionStore _definitionStore = null!;
    private RuntimeEntityDataService _service = null!;
    private int _counter;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _definitionStore = new InMemoryRuntimeStorageDefinitionStore();
        await _definitionStore.SaveAsync(new RuntimeStorageDefinition(
            "customers",
            "runtime.customers",
            "Customers",
            [
                new RuntimeStorageFieldDefinition("Id", StorageFieldType.String, true),
                new RuntimeStorageFieldDefinition("Email", StorageFieldType.String, true),
                new RuntimeStorageFieldDefinition("Priority", StorageFieldType.Int32)
            ],
            [
                new RuntimeStorageIndexDefinition("IX_Customers_Email", ["Email"]),
                new RuntimeStorageIndexDefinition("IX_Customers_Priority", ["Priority"])
            ],
            ["read:customers"])
        {
            State = RuntimeStorageDefinitionState.Published
        });
        _service = new RuntimeEntityDataService(_definitionStore, new InMemoryDocumentStoreFactoryRegistry());

        for (var i = 0; i < 1000; i++)
            await _service.CreateAsync("customers", new RuntimeEntitySaveRequest($"seed-{i}", CreatePayload($"seed-{i}", $"seed-{i}@example.com", i)), _context);
    }

    [Benchmark]
    public async ValueTask<RuntimeEntityRecord> Create()
    {
        var index = Interlocked.Increment(ref _counter);
        return await _service.CreateAsync("customers", new RuntimeEntitySaveRequest($"created-{index}", CreatePayload($"created-{index}", $"created-{index}@example.com", index)), _context);
    }

    [Benchmark]
    public async ValueTask<RuntimeEntityRecord?> Read() =>
        await _service.GetAsync("customers", "seed-500", null, _context);

    [Benchmark]
    public async ValueTask<IReadOnlyCollection<RuntimeEntityRecord>> QueryByIndexedField() =>
        await _service.QueryAsync(
            "customers",
            new RuntimeEntityQueryRequest(
                [
                    new RuntimeEntityQueryFilter("Email", DocumentQueryFilterOperator.Equals, [DocumentQueryValue.String("seed-500@example.com")])
                ]),
            _context);

    [Benchmark]
    public async ValueTask<RuntimeEntityRecord> Update()
    {
        var id = $"update-{Interlocked.Increment(ref _counter)}";
        var created = await _service.CreateAsync("customers", new RuntimeEntitySaveRequest(id, CreatePayload(id, $"{id}@example.com", 1)), _context);
        return await _service.UpdateAsync("customers", new RuntimeEntitySaveRequest(id, CreatePayload(id, $"{id}@example.com", 2), ExpectedVersion: created.Version), _context);
    }

    private static string CreatePayload(string id, string email, int priority) =>
        $$"""{"Id":"{{id}}","Email":"{{email}}","Priority":{{priority}}}""";

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
                if (expectedVersion.Kind == ExpectedDocumentVersionKind.New && existing is not null)
                    throw new DocumentConcurrencyException(document.Key, "Document already exists.");
                if (expectedVersion.Kind == ExpectedDocumentVersionKind.Exact && existing?.Version != expectedVersion.Version)
                    throw new DocumentConcurrencyException(document.Key, "Document version mismatch.");

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
                documents.Remove(key);
                return ValueTask.CompletedTask;
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;

            private static bool Matches(DocumentEnvelope document, DocumentQueryFilter filter)
            {
                var json = System.Text.Json.JsonDocument.Parse(document.Data);
                var property = json.RootElement.GetProperty(filter.FieldName);
                return filter.Operator switch
                {
                    DocumentQueryFilterOperator.Equals when filter.Values.Single().Type == StorageFieldType.String =>
                        string.Equals(property.GetString(), filter.Values.Single().TextValue, StringComparison.Ordinal),
                    DocumentQueryFilterOperator.Equals when filter.Values.Single().Type == StorageFieldType.Int32 =>
                        property.GetInt32() == filter.Values.Single().NumberValue,
                    _ => throw new NotSupportedException("The benchmark in-memory store only supports equality filters.")
                };
            }
        }
    }
}

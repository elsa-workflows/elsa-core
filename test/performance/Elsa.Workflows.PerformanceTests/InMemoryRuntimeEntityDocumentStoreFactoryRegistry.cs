using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;
using Elsa.ModularPersistence.Queries;
using Elsa.ModularPersistence.Runtime;

namespace Elsa.Workflows.PerformanceTests;

internal sealed class InMemoryRuntimeEntityDocumentStoreFactoryRegistry : IRuntimeEntityDocumentStoreFactoryRegistry
{
    private readonly InMemoryDocumentStore _store = new();

    public IDocumentStore CreateStore(StorageManifestDescriptor manifest, string? providerName) => _store;

    private sealed class InMemoryDocumentStore : IDocumentStore
    {
        private readonly Dictionary<DocumentKey, DocumentEnvelope> _documents = new();
        private readonly Lock _syncRoot = new();

        public ValueTask<IDocumentSession> OpenSessionAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IDocumentSession>(new Session(_documents, _syncRoot));
    }

    private sealed class Session(Dictionary<DocumentKey, DocumentEnvelope> documents, Lock syncRoot) : IDocumentSession
    {
        public ValueTask<DocumentEnvelope?> LoadAsync(DocumentKey key, CancellationToken cancellationToken = default)
        {
            lock (syncRoot)
                return ValueTask.FromResult(documents.GetValueOrDefault(key));
        }

        public ValueTask<DocumentSaveResult> SaveAsync(DocumentEnvelope document, ExpectedDocumentVersion expectedVersion = default, CancellationToken cancellationToken = default)
        {
            lock (syncRoot)
            {
                documents.TryGetValue(document.Key, out var existing);
                if (expectedVersion.Kind == ExpectedDocumentVersionKind.New && existing is not null)
                    throw new DocumentConcurrencyException(document.Key, "Document already exists.");
                if (expectedVersion.Kind == ExpectedDocumentVersionKind.Exact && existing?.Version != expectedVersion.Version)
                    throw new DocumentConcurrencyException(document.Key, "Document version mismatch.");

                documents[document.Key] = document;
                return ValueTask.FromResult(new DocumentSaveResult(document.Key, document.Version));
            }
        }

        public ValueTask<IReadOnlyCollection<DocumentEnvelope>> QueryAsync(DocumentQuery query, CancellationToken cancellationToken = default)
        {
            lock (syncRoot)
            {
                var results = documents.Values
                    .Where(x => x.Type == query.DocumentType)
                    .Where(document => query.Filters.All(filter => Matches(document, filter)))
                    .Skip(query.Page?.Offset ?? 0)
                    .Take(query.Page?.Limit ?? int.MaxValue)
                    .ToArray();

                return ValueTask.FromResult<IReadOnlyCollection<DocumentEnvelope>>(results);
            }
        }

        public ValueTask DeleteAsync(DocumentKey key, ExpectedDocumentVersion expectedVersion = default, CancellationToken cancellationToken = default)
        {
            lock (syncRoot)
            {
                documents.TryGetValue(key, out var existing);
                if (expectedVersion.Kind == ExpectedDocumentVersionKind.Exact && existing?.Version != expectedVersion.Version)
                    throw new DocumentConcurrencyException(key, "Document version mismatch.");

                documents.Remove(key);
                return ValueTask.CompletedTask;
            }
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private static bool Matches(DocumentEnvelope document, DocumentQueryFilter filter)
        {
            using var json = System.Text.Json.JsonDocument.Parse(document.Data);
            if (!json.RootElement.TryGetProperty(filter.FieldName, out var property))
                return filter.Operator == DocumentQueryFilterOperator.IsNull;

            var queryValue = filter.Values.SingleOrDefault();
            return filter.Operator switch
            {
                DocumentQueryFilterOperator.Equals => Equals(property, queryValue),
                DocumentQueryFilterOperator.In => filter.Values.Any(value => Equals(property, value)),
                DocumentQueryFilterOperator.IsNotNull => true,
                DocumentQueryFilterOperator.IsNull => false,
                _ => throw new NotSupportedException("The benchmark in-memory store supports equality, in, and null filters.")
            };
        }

        private static bool Equals(System.Text.Json.JsonElement property, DocumentQueryValue? value)
        {
            if (value == null)
                return property.ValueKind == System.Text.Json.JsonValueKind.Null;

            return value.Type switch
            {
                StorageFieldType.String or StorageFieldType.Guid or StorageFieldType.Json or StorageFieldType.Binary =>
                    string.Equals(property.GetString(), value.TextValue, StringComparison.Ordinal),
                StorageFieldType.Int32 => property.GetInt32() == value.NumberValue,
                StorageFieldType.Int64 => property.GetInt64() == value.NumberValue,
                StorageFieldType.Boolean => property.GetBoolean() == value.BooleanValue,
                StorageFieldType.DateTimeOffset => property.GetDateTimeOffset() == value.DateTimeValue,
                _ => throw new NotSupportedException($"The benchmark in-memory store does not support {value.Type} values.")
            };
        }
    }
}

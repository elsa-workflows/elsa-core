using System.Text.Json;
using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;
using Elsa.ModularPersistence.Queries;

namespace Elsa.Workflows.Management.Persistence.ModularPersistence.UnitTests.Testing;

internal sealed class InMemoryDocumentStore : IDocumentStore
{
    private readonly Dictionary<DocumentKey, DocumentEnvelope> _documents = new();

    public IReadOnlyCollection<DocumentEnvelope> Documents => _documents.Values.ToArray();

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
                .Where(x => query.TenantId is null || string.Equals(x.TenantId, query.TenantId, StringComparison.Ordinal))
                .Where(document => query.Filters.All(filter => Matches(document, filter)))
                .ToArray();

            foreach (var sort in query.Sorts.Reverse())
                results = Sort(results, sort).ToArray();

            if (query.Page != null)
                results = results.Skip(query.Page.Offset).Take(query.Page.Limit).ToArray();

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

        private static IEnumerable<DocumentEnvelope> Sort(IEnumerable<DocumentEnvelope> documents, DocumentQuerySort sort) =>
            sort.SortOrder == StorageIndexSortOrder.Descending
                ? documents.OrderByDescending(x => GetComparableValue(x, sort.FieldName))
                : documents.OrderBy(x => GetComparableValue(x, sort.FieldName));

        private static bool Matches(DocumentEnvelope document, DocumentQueryFilter filter)
        {
            using var json = JsonDocument.Parse(document.Data);
            var value = TryGetProperty(json.RootElement, filter.FieldName, out var property) ? property : default;

            return filter.Operator switch
            {
                DocumentQueryFilterOperator.Equals => Compare(value, filter.Values.Single()) == 0,
                DocumentQueryFilterOperator.NotEquals => Compare(value, filter.Values.Single()) != 0,
                DocumentQueryFilterOperator.In => filter.Values.Any(queryValue => Compare(value, queryValue) == 0),
                DocumentQueryFilterOperator.GreaterThan => Compare(value, filter.Values.Single()) > 0,
                DocumentQueryFilterOperator.GreaterThanOrEqual => Compare(value, filter.Values.Single()) >= 0,
                DocumentQueryFilterOperator.LessThan => Compare(value, filter.Values.Single()) < 0,
                DocumentQueryFilterOperator.LessThanOrEqual => Compare(value, filter.Values.Single()) <= 0,
                DocumentQueryFilterOperator.Between => Compare(value, filter.Values[0]) >= 0 && Compare(value, filter.Values[1]) < 0,
                DocumentQueryFilterOperator.StartsWith => GetString(value)?.StartsWith(filter.Values.Single().TextValue!, StringComparison.Ordinal) == true,
                DocumentQueryFilterOperator.IsNull => value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null,
                DocumentQueryFilterOperator.IsNotNull => value.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null,
                _ => throw new NotSupportedException($"Query operator '{filter.Operator}' is not supported by the test store.")
            };
        }

        private static IComparable? GetComparableValue(DocumentEnvelope document, string fieldName)
        {
            using var json = JsonDocument.Parse(document.Data);
            if (!TryGetProperty(json.RootElement, fieldName, out var value))
                return null;

            return value.ValueKind switch
            {
                JsonValueKind.String when DateTimeOffset.TryParse(value.GetString(), out var dateTime) => dateTime,
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.GetDecimal(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => null
            };
        }

        private static int Compare(JsonElement value, DocumentQueryValue queryValue)
        {
            if (value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                return queryValue.TextValue == null && queryValue.NumberValue == null && queryValue.BooleanValue == null && queryValue.DateTimeValue == null ? 0 : -1;

            return queryValue.Type switch
            {
                StorageFieldType.String => string.Compare(GetString(value), queryValue.TextValue, StringComparison.Ordinal),
                StorageFieldType.Int32 or StorageFieldType.Int64 or StorageFieldType.Decimal => value.GetDecimal().CompareTo(queryValue.NumberValue.GetValueOrDefault()),
                StorageFieldType.Boolean => value.GetBoolean().CompareTo(queryValue.BooleanValue.GetValueOrDefault()),
                StorageFieldType.DateTimeOffset => DateTimeOffset.Parse(value.GetString()!).CompareTo(queryValue.DateTimeValue.GetValueOrDefault()),
                _ => throw new NotSupportedException($"Query value type '{queryValue.Type}' is not supported by the test store.")
            };
        }

        private static string? GetString(JsonElement value) =>
            value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();

        private static bool TryGetProperty(JsonElement root, string fieldName, out JsonElement property)
        {
            if (root.TryGetProperty(fieldName, out property))
                return true;

            property = default;
            return false;
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

using System.Data;
using System.Globalization;
using Elsa.Persistence.VNext.Document;
using Microsoft.Data.Sqlite;

namespace Elsa.Persistence.VNext.Sqlite;

public class SqliteDocumentStore : IDocumentStore
{
    private const string DocumentsTable = "ElsaDocuments";
    private const string IndexValuesTable = "ElsaDocumentIndexValues";
    private readonly SqliteConnection _connection;
    private readonly DocumentDatabasePlan _plan;

    public SqliteDocumentStore(SqliteConnection connection, PersistenceSchema schema) : this(connection, new DocumentDatabasePlanner().Plan(schema))
    {
    }

    public SqliteDocumentStore(SqliteConnection connection, DocumentDatabasePlan plan)
    {
        _connection = connection;
        _plan = plan;
    }

    public async Task MaterializeAsync(CancellationToken cancellationToken = default)
    {
        await OpenAsync(cancellationToken);

        await using var transaction = (SqliteTransaction)await _connection.BeginTransactionAsync(cancellationToken);
        foreach (var statement in CreateMaterializationStatements())
            await ExecuteAsync(transaction, statement, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<StoredDocument> SaveAsync(SaveDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(request.StorageUnit);
        ValidateSaveRequest(collection, request);
        await OpenAsync(cancellationToken);

        await using var transaction = (SqliteTransaction)await _connection.BeginTransactionAsync(cancellationToken);
        var existing = await LoadAsync(transaction, request.StorageUnit, request.Id, cancellationToken);
        ValidateExpectedVersion(request.StorageUnit, request.Id, request.ExpectedVersion, existing?.Version);

        var now = DateTimeOffset.UtcNow;
        var createdAt = existing?.CreatedAt ?? now;
        var version = existing is null ? 1 : existing.Version + 1;
        var saved = new StoredDocument(request.StorageUnit, request.Id, request.Content, version, createdAt, now);

        await UpsertDocumentAsync(transaction, saved, cancellationToken);
        await ReplaceIndexValuesAsync(transaction, collection, request, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return saved;
    }

    public async Task<StoredDocument?> LoadAsync(string storageUnit, string id, CancellationToken cancellationToken = default)
    {
        _ = GetCollection(storageUnit);
        await OpenAsync(cancellationToken);
        return await LoadAsync(null, storageUnit, id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(string storageUnit, string id, long? expectedVersion = null, CancellationToken cancellationToken = default)
    {
        _ = GetCollection(storageUnit);
        await OpenAsync(cancellationToken);

        await using var transaction = (SqliteTransaction)await _connection.BeginTransactionAsync(cancellationToken);
        var existing = await LoadAsync(transaction, storageUnit, id, cancellationToken);
        if (existing is null)
        {
            ValidateExpectedVersion(storageUnit, id, expectedVersion, null);
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        ValidateExpectedVersion(storageUnit, id, expectedVersion, existing.Version);
        await DeleteIndexValuesAsync(transaction, storageUnit, id, cancellationToken);
        await DeleteDocumentAsync(transaction, storageUnit, id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<StoredDocument>> QueryAsync(DocumentQuery query, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(query.StorageUnit);
        var index = FindMatchingIndex(collection, query);
        await OpenAsync(cancellationToken);

        await using var transaction = (SqliteTransaction)await _connection.BeginTransactionAsync(cancellationToken);
        var documentIds = await QueryDocumentIdsAsync(transaction, query, index, cancellationToken);
        var documents = new List<StoredDocument>();

        foreach (var documentId in documentIds)
        {
            var document = await LoadAsync(transaction, query.StorageUnit, documentId, cancellationToken);
            if (document is not null)
                documents.Add(document);
        }

        await transaction.CommitAsync(cancellationToken);
        return documents;
    }

    private static IReadOnlyList<string> CreateMaterializationStatements()
    {
        return [
            $"""
            CREATE TABLE IF NOT EXISTS "{DocumentsTable}" (
                "StorageUnit" TEXT NOT NULL,
                "Id" TEXT NOT NULL,
                "Content" TEXT NOT NULL,
                "Version" INTEGER NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL,
                CONSTRAINT "PK_ElsaDocuments" PRIMARY KEY ("StorageUnit", "Id")
            );
            """,
            $"""
            CREATE TABLE IF NOT EXISTS "{IndexValuesTable}" (
                "StorageUnit" TEXT NOT NULL,
                "IndexName" TEXT NOT NULL,
                "FieldName" TEXT NOT NULL,
                "FieldValue" TEXT,
                "DocumentId" TEXT NOT NULL
            );
            """,
            $"""
            CREATE INDEX IF NOT EXISTS "IX_ElsaDocumentIndexValues_Lookup"
            ON "{IndexValuesTable}" ("StorageUnit", "IndexName", "FieldName", "FieldValue", "DocumentId");
            """
        ];
    }

    private DocumentCollection GetCollection(string storageUnit)
    {
        return _plan.Collections.SingleOrDefault(x => string.Equals(x.Name, storageUnit, StringComparison.Ordinal))
            ?? throw new DocumentStoreValidationException($"Storage unit '{storageUnit}' is not declared in the persistence schema.");
    }

    private static DocumentIndex FindMatchingIndex(DocumentCollection collection, DocumentQuery query)
    {
        var filterFields = query.Filters.Keys.Order(StringComparer.Ordinal).ToArray();
        var index = collection.Indexes.SingleOrDefault(x => x.Fields.Order(StringComparer.Ordinal).SequenceEqual(filterFields, StringComparer.Ordinal));

        if (index is null)
            throw new DocumentQueryNotIndexedException(query.StorageUnit, filterFields);

        return index;
    }

    private static void ValidateSaveRequest(DocumentCollection collection, SaveDocumentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
            throw new DocumentStoreValidationException("Document ID is required.");

        var missingFields = collection.Indexes
            .SelectMany(x => x.Fields)
            .Distinct(StringComparer.Ordinal)
            .Where(field => !request.IndexValues.ContainsKey(field))
            .Order(StringComparer.Ordinal)
            .ToList();

        if (missingFields.Count > 0)
            throw new DocumentStoreValidationException($"Storage unit '{collection.Name}' requires index values for fields '{string.Join(", ", missingFields)}'.");
    }

    private static void ValidateExpectedVersion(string storageUnit, string documentId, long? expectedVersion, long? actualVersion)
    {
        if (expectedVersion is null)
            return;

        if (expectedVersion != (actualVersion ?? 0))
            throw new DocumentStoreConcurrencyException(storageUnit, documentId, expectedVersion, actualVersion);
    }

    private async Task OpenAsync(CancellationToken cancellationToken)
    {
        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync(cancellationToken);
    }

    private async Task<StoredDocument?> LoadAsync(SqliteTransaction? transaction, string storageUnit, string id, CancellationToken cancellationToken)
    {
        await using var command = _connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
                              SELECT "StorageUnit", "Id", "Content", "Version", "CreatedAt", "UpdatedAt"
                              FROM "{DocumentsTable}"
                              WHERE "StorageUnit" = $storageUnit AND "Id" = $id;
                              """;
        command.Parameters.AddWithValue("$storageUnit", storageUnit);
        command.Parameters.AddWithValue("$id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return ReadDocument(reader);
    }

    private static StoredDocument ReadDocument(SqliteDataReader reader)
    {
        return new StoredDocument(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt64(3),
            DateTimeOffset.Parse(reader.GetString(4), CultureInfo.InvariantCulture),
            DateTimeOffset.Parse(reader.GetString(5), CultureInfo.InvariantCulture));
    }

    private async Task UpsertDocumentAsync(SqliteTransaction transaction, StoredDocument document, CancellationToken cancellationToken)
    {
        await using var command = _connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
                              INSERT INTO "{DocumentsTable}" ("StorageUnit", "Id", "Content", "Version", "CreatedAt", "UpdatedAt")
                              VALUES ($storageUnit, $id, $content, $version, $createdAt, $updatedAt)
                              ON CONFLICT("StorageUnit", "Id") DO UPDATE SET
                                  "Content" = excluded."Content",
                                  "Version" = excluded."Version",
                                  "UpdatedAt" = excluded."UpdatedAt";
                              """;
        command.Parameters.AddWithValue("$storageUnit", document.StorageUnit);
        command.Parameters.AddWithValue("$id", document.Id);
        command.Parameters.AddWithValue("$content", document.Content);
        command.Parameters.AddWithValue("$version", document.Version);
        command.Parameters.AddWithValue("$createdAt", document.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$updatedAt", document.UpdatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task ReplaceIndexValuesAsync(SqliteTransaction transaction, DocumentCollection collection, SaveDocumentRequest request, CancellationToken cancellationToken)
    {
        await DeleteIndexValuesAsync(transaction, request.StorageUnit, request.Id, cancellationToken);

        foreach (var index in collection.Indexes)
        {
            foreach (var field in index.Fields)
                await InsertIndexValueAsync(transaction, request.StorageUnit, index.Name, field, request.IndexValues[field], request.Id, cancellationToken);
        }
    }

    private async Task InsertIndexValueAsync(SqliteTransaction transaction, string storageUnit, string indexName, string fieldName, string? fieldValue, string documentId, CancellationToken cancellationToken)
    {
        await using var command = _connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
                              INSERT INTO "{IndexValuesTable}" ("StorageUnit", "IndexName", "FieldName", "FieldValue", "DocumentId")
                              VALUES ($storageUnit, $indexName, $fieldName, $fieldValue, $documentId);
                              """;
        command.Parameters.AddWithValue("$storageUnit", storageUnit);
        command.Parameters.AddWithValue("$indexName", indexName);
        command.Parameters.AddWithValue("$fieldName", fieldName);
        command.Parameters.AddWithValue("$fieldValue", (object?)fieldValue ?? DBNull.Value);
        command.Parameters.AddWithValue("$documentId", documentId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<string>> QueryDocumentIdsAsync(SqliteTransaction transaction, DocumentQuery query, DocumentIndex index, CancellationToken cancellationToken)
    {
        var clauses = new List<string>();
        var command = _connection.CreateCommand();
        command.Transaction = transaction;
        command.Parameters.AddWithValue("$storageUnit", query.StorageUnit);
        command.Parameters.AddWithValue("$indexName", index.Name);

        var parameterIndex = 0;
        foreach (var filter in query.Filters.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            var fieldParameter = $"$field{parameterIndex}";
            var valueParameter = $"$value{parameterIndex}";
            clauses.Add($"""("FieldName" = {fieldParameter} AND "FieldValue" IS {valueParameter})""");
            command.Parameters.AddWithValue(fieldParameter, filter.Key);
            command.Parameters.AddWithValue(valueParameter, (object?)filter.Value ?? DBNull.Value);
            parameterIndex++;
        }

        command.CommandText = $"""
                              SELECT "DocumentId"
                              FROM "{IndexValuesTable}"
                              WHERE "StorageUnit" = $storageUnit
                                AND "IndexName" = $indexName
                                AND ({string.Join(" OR ", clauses)})
                              GROUP BY "DocumentId"
                              HAVING COUNT(DISTINCT "FieldName") = $fieldCount
                              ORDER BY "DocumentId";
                              """;
        command.Parameters.AddWithValue("$fieldCount", query.Filters.Count);

        var documentIds = new List<string>();
        await using (command)
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                documentIds.Add(reader.GetString(0));
        }

        return documentIds;
    }

    private async Task DeleteIndexValuesAsync(SqliteTransaction transaction, string storageUnit, string documentId, CancellationToken cancellationToken)
    {
        await using var command = _connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""DELETE FROM "{IndexValuesTable}" WHERE "StorageUnit" = $storageUnit AND "DocumentId" = $documentId;""";
        command.Parameters.AddWithValue("$storageUnit", storageUnit);
        command.Parameters.AddWithValue("$documentId", documentId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task DeleteDocumentAsync(SqliteTransaction transaction, string storageUnit, string documentId, CancellationToken cancellationToken)
    {
        await using var command = _connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""DELETE FROM "{DocumentsTable}" WHERE "StorageUnit" = $storageUnit AND "Id" = $documentId;""";
        command.Parameters.AddWithValue("$storageUnit", storageUnit);
        command.Parameters.AddWithValue("$documentId", documentId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task ExecuteAsync(SqliteTransaction transaction, string sql, CancellationToken cancellationToken)
    {
        await using var command = _connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

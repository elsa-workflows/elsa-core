using System.Data;
using System.Data.Common;
using System.Globalization;
using Elsa.Persistence.VNext.Document;

namespace Elsa.Persistence.VNext.Relational.Documents;

public class RelationalDocumentStore : IDocumentStore
{
    private readonly DbConnection _connection;
    private readonly DocumentDatabasePlan _plan;
    private readonly RelationalDocumentStoreDialect _dialect;

    public RelationalDocumentStore(DbConnection connection, PersistenceSchema schema, RelationalDocumentStoreDialect dialect) : this(connection, new DocumentDatabasePlanner().Plan(schema), dialect)
    {
    }

    public RelationalDocumentStore(DbConnection connection, DocumentDatabasePlan plan, RelationalDocumentStoreDialect dialect)
    {
        _connection = connection;
        _plan = plan;
        _dialect = dialect;
    }

    public async Task MaterializeAsync(CancellationToken cancellationToken = default)
    {
        await OpenAsync(cancellationToken);

        await using var transaction = await _connection.BeginTransactionAsync(cancellationToken);
        foreach (var statement in _dialect.CreateMaterializationLockStatements())
            await ExecuteAsync(transaction, statement, cancellationToken);

        foreach (var statement in _dialect.CreateMaterializationStatements())
            await ExecuteAsync(transaction, statement, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<StoredDocument> SaveAsync(SaveDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(request.StorageUnit);
        ValidateSaveRequest(collection, request);
        await OpenAsync(cancellationToken);

        await using var transaction = await _connection.BeginTransactionAsync(cancellationToken);
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

        await using var transaction = await _connection.BeginTransactionAsync(cancellationToken);
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
        var index = _dialect.FindMatchingIndex(collection, query);
        await OpenAsync(cancellationToken);

        await using var transaction = await _connection.BeginTransactionAsync(cancellationToken);
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

    private DocumentCollection GetCollection(string storageUnit)
    {
        return _plan.Collections.SingleOrDefault(x => string.Equals(x.Name, storageUnit, StringComparison.Ordinal))
            ?? throw new DocumentStoreValidationException($"Storage unit '{storageUnit}' is not declared in the persistence schema.");
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

    private async Task<StoredDocument?> LoadAsync(DbTransaction? transaction, string storageUnit, string id, CancellationToken cancellationToken)
    {
        await using var command = _connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = _dialect.RenderSelectDocumentSql();
        AddParameter(command, "storageUnit", storageUnit);
        AddParameter(command, "id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return ReadDocument(reader);
    }

    private static StoredDocument ReadDocument(DbDataReader reader)
    {
        return new StoredDocument(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt64(3),
            DateTimeOffset.Parse(reader.GetString(4), CultureInfo.InvariantCulture),
            DateTimeOffset.Parse(reader.GetString(5), CultureInfo.InvariantCulture));
    }

    private async Task UpsertDocumentAsync(DbTransaction transaction, StoredDocument document, CancellationToken cancellationToken)
    {
        await using var command = _connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = _dialect.RenderUpsertDocumentSql();
        AddParameter(command, "storageUnit", document.StorageUnit);
        AddParameter(command, "id", document.Id);
        AddParameter(command, "content", document.Content);
        AddParameter(command, "version", document.Version);
        AddParameter(command, "createdAt", _dialect.ConvertTimestamp(document.CreatedAt));
        AddParameter(command, "updatedAt", _dialect.ConvertTimestamp(document.UpdatedAt));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task ReplaceIndexValuesAsync(DbTransaction transaction, DocumentCollection collection, SaveDocumentRequest request, CancellationToken cancellationToken)
    {
        await DeleteIndexValuesAsync(transaction, request.StorageUnit, request.Id, cancellationToken);

        foreach (var index in collection.Indexes)
        {
            foreach (var field in index.Fields)
                await InsertIndexValueAsync(transaction, request.StorageUnit, index.Name, field, request.IndexValues[field], request.Id, cancellationToken);
        }
    }

    private async Task InsertIndexValueAsync(DbTransaction transaction, string storageUnit, string indexName, string fieldName, string? fieldValue, string documentId, CancellationToken cancellationToken)
    {
        await using var command = _connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = _dialect.RenderInsertIndexValueSql();
        AddParameter(command, "storageUnit", storageUnit);
        AddParameter(command, "indexName", indexName);
        AddParameter(command, "fieldName", fieldName);
        AddParameter(command, "fieldValue", _dialect.ConvertStoredValue(fieldValue));
        AddParameter(command, "documentId", documentId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<string>> QueryDocumentIdsAsync(DbTransaction transaction, DocumentQuery query, DocumentIndex index, CancellationToken cancellationToken)
    {
        var command = _connection.CreateCommand();
        command.Transaction = transaction;
        AddParameter(command, "storageUnit", query.StorageUnit);
        AddParameter(command, "indexName", index.Name);

        var clauses = new List<string>();
        var parameterIndex = 0;
        foreach (var filter in query.Filters.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            var fieldName = $"field{parameterIndex}";
            var fieldValue = $"value{parameterIndex}";
            clauses.Add(_dialect.RenderFieldFilter(_dialect.Parameter(fieldName), _dialect.Parameter(fieldValue)));
            AddParameter(command, fieldName, filter.Key);
            AddParameter(command, fieldValue, _dialect.ConvertStoredValue(filter.Value));
            parameterIndex++;
        }

        command.CommandText = _dialect.RenderQueryDocumentIdsSql(clauses);
        AddParameter(command, "fieldCount", query.Filters.Count);

        var documentIds = new List<string>();
        await using (command)
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                documentIds.Add(reader.GetString(0));
        }

        return documentIds;
    }

    private async Task DeleteIndexValuesAsync(DbTransaction transaction, string storageUnit, string documentId, CancellationToken cancellationToken)
    {
        await using var command = _connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = _dialect.RenderDeleteIndexValuesSql();
        AddParameter(command, "storageUnit", storageUnit);
        AddParameter(command, "documentId", documentId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task DeleteDocumentAsync(DbTransaction transaction, string storageUnit, string documentId, CancellationToken cancellationToken)
    {
        await using var command = _connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = _dialect.RenderDeleteDocumentSql();
        AddParameter(command, "storageUnit", storageUnit);
        AddParameter(command, "documentId", documentId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task ExecuteAsync(DbTransaction transaction, string sql, CancellationToken cancellationToken)
    {
        await using var command = _connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = _dialect.Parameter(name);
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}

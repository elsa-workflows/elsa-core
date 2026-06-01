using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;
using Elsa.ModularPersistence.Queries;
using Elsa.ModularPersistence.Relational.Contracts;

namespace Elsa.ModularPersistence.Sqlite.Services;

/// <summary>
/// Executes SQLite document operations.
/// </summary>
public sealed class SqliteDocumentSession(IRelationalConnectionFactory connectionFactory, StorageManifestDescriptor manifest) : IDocumentSession
{
    private static readonly DocumentQueryCapabilities QueryCapabilities = DocumentQueryCapabilities.Portable;

    public async ValueTask<DocumentEnvelope?> LoadAsync(DocumentKey key, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Type, TenantId, Version, CreatedAt, UpdatedAt, Data, Metadata
            FROM ModularPersistenceDocuments
            WHERE Id = @Id AND Type = @Type AND TenantId = @TenantId;
            """;
        AddKeyParameters(command, key);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return ReadDocument(reader);
    }

    public async ValueTask<DocumentSaveResult> SaveAsync(DocumentEnvelope document, ExpectedDocumentVersion expectedVersion = default, CancellationToken cancellationToken = default)
    {
        var storageUnit = GetStorageUnit(document.Type);
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var currentVersion = await ReadCurrentVersionAsync(connection, transaction, document.Key, cancellationToken);
        ValidateSaveConcurrency(document.Key, currentVersion, expectedVersion);

        if (currentVersion is null)
            await InsertDocumentAsync(connection, transaction, document, cancellationToken);
        else
            await UpdateDocumentAsync(connection, transaction, document, expectedVersion, cancellationToken);

        await DeleteIndexEntriesAsync(connection, transaction, document.Key, cancellationToken);
        await InsertIndexEntriesAsync(connection, transaction, storageUnit, document, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new DocumentSaveResult(document.Key, document.Version);
    }

    public async ValueTask<IReadOnlyCollection<DocumentEnvelope>> QueryAsync(DocumentQuery query, CancellationToken cancellationToken = default)
    {
        var planner = new DocumentQueryPlanner();
        var plan = planner.Plan(manifest, query, QueryCapabilities);
        if (!plan.IsExecutable)
            throw new DocumentQueryException(plan, "Document query failed planning.");

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        ConfigureQueryCommand(command, plan.StorageUnit!, query);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var documents = new List<DocumentEnvelope>();
        while (await reader.ReadAsync(cancellationToken))
            documents.Add(ReadDocument(reader));

        return documents;
    }

    public async ValueTask DeleteAsync(DocumentKey key, ExpectedDocumentVersion expectedVersion = default, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var currentVersion = await ReadCurrentVersionAsync(connection, transaction, key, cancellationToken);
        ValidateDeleteConcurrency(key, currentVersion, expectedVersion);

        await DeleteIndexEntriesAsync(connection, transaction, key, cancellationToken);
        await DeleteDocumentAsync(connection, transaction, key, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private StorageUnitDescriptor GetStorageUnit(string documentType) =>
        manifest.StorageUnits.SingleOrDefault(x => x.Name == documentType)
        ?? throw new InvalidOperationException($"No storage unit named '{documentType}' is declared by manifest '{manifest.SchemaName}'.");

    private static void ValidateSaveConcurrency(DocumentKey key, long? currentVersion, ExpectedDocumentVersion expectedVersion)
    {
        switch (expectedVersion.Kind)
        {
            case ExpectedDocumentVersionKind.Any:
                return;
            case ExpectedDocumentVersionKind.New when currentVersion is null:
                return;
            case ExpectedDocumentVersionKind.New:
                throw new DocumentConcurrencyException(key, $"Document '{key.Id}' already exists.");
            case ExpectedDocumentVersionKind.Exact when currentVersion == expectedVersion.Version:
                return;
            case ExpectedDocumentVersionKind.Exact:
                throw new DocumentConcurrencyException(key, $"Document '{key.Id}' version did not match expected version {expectedVersion.Version}.");
            default:
                throw new ArgumentOutOfRangeException(nameof(expectedVersion), expectedVersion.Kind, "Unknown expected document version kind.");
        }
    }

    private static void ValidateDeleteConcurrency(DocumentKey key, long? currentVersion, ExpectedDocumentVersion expectedVersion)
    {
        switch (expectedVersion.Kind)
        {
            case ExpectedDocumentVersionKind.Any:
                return;
            case ExpectedDocumentVersionKind.New:
                throw new DocumentConcurrencyException(key, "Delete cannot require a new document.");
            case ExpectedDocumentVersionKind.Exact when currentVersion == expectedVersion.Version:
                return;
            case ExpectedDocumentVersionKind.Exact:
                throw new DocumentConcurrencyException(key, $"Document '{key.Id}' version did not match expected version {expectedVersion.Version}.");
            default:
                throw new ArgumentOutOfRangeException(nameof(expectedVersion), expectedVersion.Kind, "Unknown expected document version kind.");
        }
    }

    private static async ValueTask<long?> ReadCurrentVersionAsync(DbConnection connection, DbTransaction transaction, DocumentKey key, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT Version
            FROM ModularPersistenceDocuments
            WHERE Id = @Id AND Type = @Type AND TenantId = @TenantId;
            """;
        AddKeyParameters(command, key);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null ? null : Convert.ToInt64(result);
    }

    private static void ConfigureQueryCommand(DbCommand command, StorageUnitDescriptor storageUnit, DocumentQuery query)
    {
        var whereClauses = new List<string>
        {
            "d.Type = @DocumentType"
        };
        AddParameter(command, "@DocumentType", query.DocumentType);

        for (var i = 0; i < query.Filters.Count; i++)
        {
            var filter = query.Filters[i];
            var field = storageUnit.Fields.Single(x => x.Name == filter.FieldName);
            whereClauses.Add(BuildFilterExistsClause(command, filter, field, i));
        }

        var orderBy = query.Sorts.Count == 0
            ? ""
            : $"{Environment.NewLine}ORDER BY {string.Join(", ", query.Sorts.Select((sort, index) => BuildSortExpression(command, storageUnit, sort, index)))}";

        var paging = "";
        if (query.Page is not null)
        {
            AddParameter(command, "@Limit", query.Page.Limit);
            AddParameter(command, "@Offset", query.Page.Offset);
            paging = $"{Environment.NewLine}LIMIT @Limit OFFSET @Offset";
        }

        command.CommandText = $"""
            SELECT d.Id, d.Type, d.TenantId, d.Version, d.CreatedAt, d.UpdatedAt, d.Data, d.Metadata
            FROM ModularPersistenceDocuments d
            WHERE {string.Join($"{Environment.NewLine}  AND ", whereClauses)}
            {orderBy}{paging};
            """;
    }

    private static string BuildFilterExistsClause(DbCommand command, DocumentQueryFilter filter, StorageFieldDescriptor field, int filterIndex)
    {
        var alias = $"i{filterIndex}";
        var indexParameter = $"@Filter{filterIndex}IndexName";
        var fieldParameter = $"@Filter{filterIndex}FieldName";
        AddParameter(command, indexParameter, filter.IndexName);
        AddParameter(command, fieldParameter, filter.FieldName);

        return $"""
            EXISTS (
                SELECT 1
                FROM ModularPersistenceDocumentIndexes {alias}
                WHERE {alias}.DocumentId = d.Id
                  AND {alias}.DocumentType = d.Type
                  AND {alias}.TenantId = d.TenantId
                  AND {alias}.IndexName = {indexParameter}
                  AND {alias}.FieldName = {fieldParameter}
                  AND {BuildFilterCondition(command, alias, filter, field, filterIndex)}
            )
            """;
    }

    private static string BuildFilterCondition(DbCommand command, string alias, DocumentQueryFilter filter, StorageFieldDescriptor field, int filterIndex)
    {
        var column = GetIndexedColumn(field.Type);
        return filter.Operator switch
        {
            DocumentQueryFilterOperator.Equals => $"{column.For(alias)} = {AddQueryValueParameter(command, filter, field, filterIndex, 0)}",
            DocumentQueryFilterOperator.NotEquals => $"{column.For(alias)} <> {AddQueryValueParameter(command, filter, field, filterIndex, 0)}",
            DocumentQueryFilterOperator.In => $"{column.For(alias)} IN ({string.Join(", ", filter.Values.Select((_, valueIndex) => AddQueryValueParameter(command, filter, field, filterIndex, valueIndex)))})",
            DocumentQueryFilterOperator.GreaterThan => $"{column.For(alias)} > {AddQueryValueParameter(command, filter, field, filterIndex, 0)}",
            DocumentQueryFilterOperator.GreaterThanOrEqual => $"{column.For(alias)} >= {AddQueryValueParameter(command, filter, field, filterIndex, 0)}",
            DocumentQueryFilterOperator.LessThan => $"{column.For(alias)} < {AddQueryValueParameter(command, filter, field, filterIndex, 0)}",
            DocumentQueryFilterOperator.LessThanOrEqual => $"{column.For(alias)} <= {AddQueryValueParameter(command, filter, field, filterIndex, 0)}",
            DocumentQueryFilterOperator.Between => $"{column.For(alias)} >= {AddQueryValueParameter(command, filter, field, filterIndex, 0)} AND {column.For(alias)} <= {AddQueryValueParameter(command, filter, field, filterIndex, 1)}",
            DocumentQueryFilterOperator.IsNull => $"{alias}.NullValue = 1",
            DocumentQueryFilterOperator.IsNotNull => $"{alias}.NullValue = 0",
            DocumentQueryFilterOperator.StartsWith => throw new NotSupportedException("SQLite document queries do not support starts-with in the portable MVP."),
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter.Operator, "Unknown query filter operator.")
        };
    }

    private static string BuildSortExpression(DbCommand command, StorageUnitDescriptor storageUnit, DocumentQuerySort sort, int sortIndex)
    {
        var field = storageUnit.Fields.Single(x => x.Name == sort.FieldName);
        var column = GetIndexedColumn(field.Type);
        var indexParameter = $"@Sort{sortIndex}IndexName";
        var fieldParameter = $"@Sort{sortIndex}FieldName";
        AddParameter(command, indexParameter, sort.IndexName);
        AddParameter(command, fieldParameter, sort.FieldName);

        var direction = sort.SortOrder == StorageIndexSortOrder.Descending ? "DESC" : "ASC";
        return $"""
            (
                SELECT {column.For("s" + sortIndex)}
                FROM ModularPersistenceDocumentIndexes s{sortIndex}
                WHERE s{sortIndex}.DocumentId = d.Id
                  AND s{sortIndex}.DocumentType = d.Type
                  AND s{sortIndex}.TenantId = d.TenantId
                  AND s{sortIndex}.IndexName = {indexParameter}
                  AND s{sortIndex}.FieldName = {fieldParameter}
                LIMIT 1
            ) {direction}
            """;
    }

    private static string AddQueryValueParameter(DbCommand command, DocumentQueryFilter filter, StorageFieldDescriptor field, int filterIndex, int valueIndex)
    {
        var parameterName = $"@Filter{filterIndex}Value{valueIndex}";
        AddParameter(command, parameterName, ConvertQueryValue(filter.Values[valueIndex], field.Type));
        return parameterName;
    }

    private static object? ConvertQueryValue(DocumentQueryValue value, StorageFieldType fieldType) =>
        fieldType switch
        {
            StorageFieldType.String or StorageFieldType.Guid or StorageFieldType.Json or StorageFieldType.Binary => value.TextValue,
            StorageFieldType.Int32 or StorageFieldType.Int64 or StorageFieldType.Decimal => value.NumberValue is null ? null : decimal.ToDouble(value.NumberValue.Value),
            StorageFieldType.Boolean => value.BooleanValue is true ? 1 : 0,
            StorageFieldType.DateTimeOffset => value.DateTimeValue?.ToString("O"),
            _ => throw new ArgumentOutOfRangeException(nameof(fieldType), fieldType, "Unknown storage field type.")
        };

    private static IndexedColumn GetIndexedColumn(StorageFieldType fieldType) =>
        fieldType switch
        {
            StorageFieldType.String or StorageFieldType.Guid or StorageFieldType.Json or StorageFieldType.Binary => new IndexedColumn("StringValue"),
            StorageFieldType.Int32 or StorageFieldType.Int64 or StorageFieldType.Decimal => new IndexedColumn("NumberValue"),
            StorageFieldType.Boolean => new IndexedColumn("BooleanValue"),
            StorageFieldType.DateTimeOffset => new IndexedColumn("DateTimeValue"),
            _ => throw new ArgumentOutOfRangeException(nameof(fieldType), fieldType, "Unknown storage field type.")
        };

    private static async ValueTask InsertDocumentAsync(DbConnection connection, DbTransaction transaction, DocumentEnvelope document, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO ModularPersistenceDocuments (Id, Type, TenantId, Version, CreatedAt, UpdatedAt, Data, Metadata)
            VALUES (@Id, @Type, @TenantId, @Version, @CreatedAt, @UpdatedAt, @Data, @Metadata);
            """;
        AddDocumentParameters(command, document);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async ValueTask UpdateDocumentAsync(DbConnection connection, DbTransaction transaction, DocumentEnvelope document, ExpectedDocumentVersion expectedVersion, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE ModularPersistenceDocuments
            SET Version = @Version, CreatedAt = @CreatedAt, UpdatedAt = @UpdatedAt, Data = @Data, Metadata = @Metadata
            WHERE Id = @Id AND Type = @Type AND TenantId = @TenantId
              AND (@ExpectedVersion IS NULL OR Version = @ExpectedVersion);
            """;
        AddDocumentParameters(command, document);
        AddParameter(command, "@ExpectedVersion", expectedVersion.Kind == ExpectedDocumentVersionKind.Exact ? expectedVersion.Version : null);
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            throw new DocumentConcurrencyException(document.Key, $"Document '{document.Id}' version did not match expected version {expectedVersion.Version}.");
    }

    private static async ValueTask DeleteDocumentAsync(DbConnection connection, DbTransaction transaction, DocumentKey key, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DELETE FROM ModularPersistenceDocuments
            WHERE Id = @Id AND Type = @Type AND TenantId = @TenantId;
            """;
        AddKeyParameters(command, key);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async ValueTask DeleteIndexEntriesAsync(DbConnection connection, DbTransaction transaction, DocumentKey key, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DELETE FROM ModularPersistenceDocumentIndexes
            WHERE DocumentId = @Id AND DocumentType = @Type AND TenantId = @TenantId;
            """;
        AddKeyParameters(command, key);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async ValueTask InsertIndexEntriesAsync(DbConnection connection, DbTransaction transaction, StorageUnitDescriptor storageUnit, DocumentEnvelope document, CancellationToken cancellationToken)
    {
        using var json = JsonDocument.Parse(document.Data);
        foreach (var index in storageUnit.Indexes)
        {
            foreach (var indexField in index.Fields)
            {
                var field = storageUnit.Fields.Single(x => x.Name == indexField.FieldName);
                var value = TryReadJsonValue(json.RootElement, field);
                await InsertIndexEntryAsync(connection, transaction, document.Key, index.Name, field.Name, value, cancellationToken);
            }
        }
    }

    private static IndexedValue TryReadJsonValue(JsonElement root, StorageFieldDescriptor field)
    {
        if (!root.TryGetProperty(field.Name, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return IndexedValue.Null;

        return field.Type switch
        {
            StorageFieldType.String or StorageFieldType.Guid or StorageFieldType.Json or StorageFieldType.Binary => IndexedValue.String(value.ToString()),
            StorageFieldType.Boolean => value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False
                ? IndexedValue.Boolean(value.GetBoolean())
                : IndexedValue.String(value.ToString()),
            StorageFieldType.Int32 or StorageFieldType.Int64 or StorageFieldType.Decimal => value.TryGetDouble(out var number)
                ? IndexedValue.Number(number)
                : IndexedValue.String(value.ToString()),
            StorageFieldType.DateTimeOffset => value.ValueKind == JsonValueKind.String && value.TryGetDateTimeOffset(out var dateTime)
                ? IndexedValue.DateTime(dateTime)
                : IndexedValue.String(value.ToString()),
            _ => IndexedValue.String(value.ToString())
        };
    }

    private static async ValueTask InsertIndexEntryAsync(
        DbConnection connection,
        DbTransaction transaction,
        DocumentKey key,
        string indexName,
        string fieldName,
        IndexedValue value,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO ModularPersistenceDocumentIndexes (
                DocumentId,
                DocumentType,
                TenantId,
                IndexName,
                FieldName,
                StringValue,
                NumberValue,
                BooleanValue,
                DateTimeValue,
                NullValue)
            VALUES (
                @DocumentId,
                @DocumentType,
                @TenantId,
                @IndexName,
                @FieldName,
                @StringValue,
                @NumberValue,
                @BooleanValue,
                @DateTimeValue,
                @NullValue);
            """;
        AddParameter(command, "@DocumentId", key.Id);
        AddParameter(command, "@DocumentType", key.Type);
        AddParameter(command, "@TenantId", NormalizeTenantId(key.TenantId));
        AddParameter(command, "@IndexName", indexName);
        AddParameter(command, "@FieldName", fieldName);
        AddParameter(command, "@StringValue", value.StringValue);
        AddParameter(command, "@NumberValue", value.NumberValue);
        AddParameter(command, "@BooleanValue", value.BooleanValue);
        AddParameter(command, "@DateTimeValue", value.DateTimeValue);
        AddParameter(command, "@NullValue", value.IsNull ? 1 : 0);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddDocumentParameters(DbCommand command, DocumentEnvelope document)
    {
        AddKeyParameters(command, document.Key);
        AddParameter(command, "@Version", document.Version);
        AddParameter(command, "@CreatedAt", document.CreatedAt.ToString("O"));
        AddParameter(command, "@UpdatedAt", document.UpdatedAt.ToString("O"));
        AddParameter(command, "@Data", document.Data);
        AddParameter(command, "@Metadata", document.Metadata.Count == 0 ? null : JsonSerializer.Serialize(document.Metadata));
    }

    private static void AddKeyParameters(DbCommand command, DocumentKey key)
    {
        AddParameter(command, "@Id", key.Id);
        AddParameter(command, "@Type", key.Type);
        AddParameter(command, "@TenantId", NormalizeTenantId(key.TenantId));
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static string NormalizeTenantId(string? tenantId) => tenantId ?? "";

    private static IReadOnlyDictionary<string, string>? DeserializeMetadata(string? metadataJson) =>
        string.IsNullOrWhiteSpace(metadataJson)
            ? null
            : JsonSerializer.Deserialize<IReadOnlyDictionary<string, string>>(metadataJson);

    private static DocumentEnvelope ReadDocument(DbDataReader reader)
    {
        var tenantId = reader.GetString(2);
        var metadataJson = reader.IsDBNull(7) ? null : reader.GetString(7);

        return new DocumentEnvelope(
            reader.GetString(0),
            reader.GetString(1),
            string.IsNullOrEmpty(tenantId) ? null : tenantId,
            reader.GetInt64(3),
            DateTimeOffset.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            DateTimeOffset.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            reader.GetString(6),
            DeserializeMetadata(metadataJson));
    }

    private readonly record struct IndexedColumn(string Name)
    {
        public string For(string alias) => $"{alias}.{Name}";
    }

    private sealed record IndexedValue(string? StringValue, double? NumberValue, int? BooleanValue, string? DateTimeValue, bool IsNull)
    {
        public static IndexedValue Null { get; } = new(null, null, null, null, true);

        public static IndexedValue String(string? value) => string.IsNullOrEmpty(value) ? Null : new(value, null, null, null, false);

        public static IndexedValue Number(double value) => new(null, value, null, null, false);

        public static IndexedValue Boolean(bool value) => new(null, null, value ? 1 : 0, null, false);

        public static IndexedValue DateTime(DateTimeOffset value) => new(null, null, null, value.ToString("O"), false);
    }
}

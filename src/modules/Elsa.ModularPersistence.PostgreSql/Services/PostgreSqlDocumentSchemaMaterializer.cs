using System.Data.Common;
using Elsa.ModularPersistence.Contracts;
using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.PostgreSql.Options;
using Elsa.ModularPersistence.Relational.Contracts;

namespace Elsa.ModularPersistence.PostgreSql.Services;

/// <summary>
/// Materializes the portable document schema into PostgreSQL.
/// </summary>
public sealed class PostgreSqlDocumentSchemaMaterializer(IRelationalConnectionFactory connectionFactory, PostgreSqlModularPersistenceOptions? options = null) : IStorageManifestMaterializer
{
    public const string ProviderNameValue = "PostgreSQL";

    private readonly PostgreSqlModularPersistenceOptions _options = options ?? new PostgreSqlModularPersistenceOptions();

    public string ProviderName => ProviderNameValue;

    public bool CanMaterialize(StorageManifestDescriptor manifest) => true;

    public async ValueTask MaterializeAsync(StorageManifestDescriptor manifest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await AcquireSchemaLockAsync(connection, transaction, cancellationToken);
        await ExecuteAsync(connection, transaction, CreateDocumentsTableSql, cancellationToken);
        await ExecuteAsync(connection, transaction, CreateDocumentIndexesTableSql, cancellationToken);
        await ExecuteAsync(connection, transaction, CreateSchemaHistoryTableSql, cancellationToken);
        await ExecuteAsync(connection, transaction, CreateDocumentIndexesByStringValueSql, cancellationToken);
        await ExecuteAsync(connection, transaction, CreateDocumentIndexesByNumberValueSql, cancellationToken);
        await ExecuteAsync(connection, transaction, CreateDocumentIndexesByDateTimeValueSql, cancellationToken);
        if (_options.UseOptimizedJsonbIndexes)
        {
            foreach (var sql in BuildOptimizedJsonbIndexSql(manifest))
                await ExecuteAsync(connection, transaction, sql, cancellationToken);
        }

        await RecordManifestVersionAsync(connection, transaction, manifest, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private const string CreateDocumentsTableSql = """
        CREATE TABLE IF NOT EXISTS ModularPersistenceDocuments (
            Id VARCHAR(450) NOT NULL,
            Type VARCHAR(450) NOT NULL,
            TenantId VARCHAR(450) NOT NULL DEFAULT '',
            Version BIGINT NOT NULL,
            CreatedAt VARCHAR(64) NOT NULL,
            UpdatedAt VARCHAR(64) NOT NULL,
            Data JSONB NOT NULL,
            Metadata JSONB NULL,
            CONSTRAINT PK_ModularPersistenceDocuments PRIMARY KEY (Id, Type, TenantId)
        );
        """;

    private const string CreateDocumentIndexesTableSql = """
        CREATE TABLE IF NOT EXISTS ModularPersistenceDocumentIndexes (
            DocumentId VARCHAR(450) NOT NULL,
            DocumentType VARCHAR(450) NOT NULL,
            TenantId VARCHAR(450) NOT NULL DEFAULT '',
            IndexName VARCHAR(450) NOT NULL,
            FieldName VARCHAR(450) NOT NULL,
            StringValue VARCHAR(450) NULL,
            NumberValue DOUBLE PRECISION NULL,
            BooleanValue BOOLEAN NULL,
            DateTimeValue VARCHAR(64) NULL,
            NullValue BOOLEAN NOT NULL DEFAULT FALSE,
            CONSTRAINT PK_ModularPersistenceDocumentIndexes PRIMARY KEY (DocumentId, DocumentType, TenantId, IndexName, FieldName)
        );
        """;

    private const string CreateSchemaHistoryTableSql = """
        CREATE TABLE IF NOT EXISTS ModularPersistenceSchemaHistory (
            SchemaName VARCHAR(450) NOT NULL,
            Version VARCHAR(64) NOT NULL,
            AppliedAt VARCHAR(64) NOT NULL,
            CONSTRAINT PK_ModularPersistenceSchemaHistory PRIMARY KEY (SchemaName, Version)
        );
        """;

    private const string CreateDocumentIndexesByStringValueSql = """
        CREATE INDEX IF NOT EXISTS IX_ModularPersistenceDocumentIndexes_StringValue
        ON ModularPersistenceDocumentIndexes (DocumentType, TenantId, IndexName, FieldName, StringValue);
        """;

    private const string CreateDocumentIndexesByNumberValueSql = """
        CREATE INDEX IF NOT EXISTS IX_ModularPersistenceDocumentIndexes_NumberValue
        ON ModularPersistenceDocumentIndexes (DocumentType, TenantId, IndexName, FieldName, NumberValue);
        """;

    private const string CreateDocumentIndexesByDateTimeValueSql = """
        CREATE INDEX IF NOT EXISTS IX_ModularPersistenceDocumentIndexes_DateTimeValue
        ON ModularPersistenceDocumentIndexes (DocumentType, TenantId, IndexName, FieldName, DateTimeValue);
        """;

    private async ValueTask AcquireSchemaLockAsync(DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT pg_advisory_xact_lock(@LockKey);";
        AddParameter(command, "@LockKey", _options.SchemaLockKey);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static IEnumerable<string> BuildOptimizedJsonbIndexSql(StorageManifestDescriptor manifest)
    {
        foreach (var storageUnit in manifest.StorageUnits)
        {
            foreach (var index in storageUnit.Indexes)
            {
                if (index.PhysicalizationIntent != PhysicalizationIntent.OptimizedIndexes)
                    continue;

                foreach (var indexField in index.Fields)
                {
                    var field = storageUnit.Fields.Single(x => x.Name == indexField.FieldName);
                    var name = SanitizeIdentifier($"IX_ModularPersistenceDocuments_Jsonb_{index.Name}_{field.Name}");
                    var expression = GetJsonbExpression(field);
                    var predicate = GetJsonbPredicate(storageUnit, field);

                    yield return $"""
                        CREATE INDEX IF NOT EXISTS {name}
                        ON ModularPersistenceDocuments ({expression})
                        WHERE {predicate};
                        """;
                }
            }
        }
    }

    private static string GetJsonbExpression(StorageFieldDescriptor field) =>
        field.Type switch
        {
            StorageFieldType.String or StorageFieldType.Guid or StorageFieldType.Json or StorageFieldType.Binary => $"((Data ->> '{EscapeSqlLiteral(field.Name)}'))",
            StorageFieldType.Int32 or StorageFieldType.Int64 or StorageFieldType.Decimal => $"(((Data ->> '{EscapeSqlLiteral(field.Name)}')::double precision))",
            StorageFieldType.Boolean => $"(((Data ->> '{EscapeSqlLiteral(field.Name)}')::boolean))",
            StorageFieldType.DateTimeOffset => $"((Data ->> '{EscapeSqlLiteral(field.Name)}'))",
            _ => throw new ArgumentOutOfRangeException(nameof(field), field.Type, "Unknown storage field type.")
        };

    private static string GetJsonbPredicate(StorageUnitDescriptor storageUnit, StorageFieldDescriptor field)
    {
        var fieldName = EscapeSqlLiteral(field.Name);
        var typeName = EscapeSqlLiteral(storageUnit.Name);
        var typePredicate = $"Type = '{typeName}' AND Data ? '{fieldName}'";

        return field.Type switch
        {
            StorageFieldType.Int32 or StorageFieldType.Int64 or StorageFieldType.Decimal => $"{typePredicate} AND jsonb_typeof(Data -> '{fieldName}') = 'number'",
            StorageFieldType.Boolean => $"{typePredicate} AND jsonb_typeof(Data -> '{fieldName}') = 'boolean'",
            _ => typePredicate
        };
    }

    private static string SanitizeIdentifier(string value)
    {
        var chars = value.Select(x => char.IsLetterOrDigit(x) ? x : '_').ToArray();
        return new string(chars);
    }

    private static string EscapeSqlLiteral(string value) => value.Replace("'", "''", StringComparison.Ordinal);

    private static async ValueTask RecordManifestVersionAsync(DbConnection connection, DbTransaction transaction, StorageManifestDescriptor manifest, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO ModularPersistenceSchemaHistory (SchemaName, Version, AppliedAt)
            VALUES (@SchemaName, @Version, @AppliedAt)
            ON CONFLICT (SchemaName, Version) DO NOTHING;
            """;
        AddParameter(command, "@SchemaName", manifest.SchemaName);
        AddParameter(command, "@Version", manifest.Version.ToString());
        AddParameter(command, "@AppliedAt", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async ValueTask ExecuteAsync(DbConnection connection, DbTransaction transaction, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}

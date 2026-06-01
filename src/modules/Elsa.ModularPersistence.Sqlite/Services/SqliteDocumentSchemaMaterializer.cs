using System.Data.Common;
using Elsa.ModularPersistence.Contracts;
using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Relational.Contracts;

namespace Elsa.ModularPersistence.Sqlite.Services;

/// <summary>
/// Materializes the portable document schema into SQLite.
/// </summary>
public sealed class SqliteDocumentSchemaMaterializer(IRelationalConnectionFactory connectionFactory) : IStorageManifestMaterializer
{
    public bool CanMaterialize(StorageManifestDescriptor manifest) => true;

    public async ValueTask MaterializeAsync(StorageManifestDescriptor manifest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await ExecuteAsync(connection, transaction, CreateDocumentsTableSql, cancellationToken);
        await ExecuteAsync(connection, transaction, CreateDocumentIndexesTableSql, cancellationToken);
        await ExecuteAsync(connection, transaction, CreateSchemaHistoryTableSql, cancellationToken);
        await ExecuteAsync(connection, transaction, CreateDocumentIndexesByStringValueSql, cancellationToken);
        await ExecuteAsync(connection, transaction, CreateDocumentIndexesByNumberValueSql, cancellationToken);
        await ExecuteAsync(connection, transaction, CreateDocumentIndexesByDateTimeValueSql, cancellationToken);
        await RecordManifestVersionAsync(connection, transaction, manifest, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private const string CreateDocumentsTableSql = """
        CREATE TABLE IF NOT EXISTS ModularPersistenceDocuments (
            Id TEXT NOT NULL,
            Type TEXT NOT NULL,
            TenantId TEXT NOT NULL DEFAULT '',
            Version INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL,
            Data TEXT NOT NULL,
            Metadata TEXT NULL,
            PRIMARY KEY (Id, Type, TenantId)
        );
        """;

    private const string CreateDocumentIndexesTableSql = """
        CREATE TABLE IF NOT EXISTS ModularPersistenceDocumentIndexes (
            DocumentId TEXT NOT NULL,
            DocumentType TEXT NOT NULL,
            TenantId TEXT NOT NULL DEFAULT '',
            IndexName TEXT NOT NULL,
            FieldName TEXT NOT NULL,
            StringValue TEXT NULL,
            NumberValue REAL NULL,
            BooleanValue INTEGER NULL,
            DateTimeValue TEXT NULL,
            NullValue INTEGER NOT NULL DEFAULT 0,
            PRIMARY KEY (DocumentId, DocumentType, TenantId, IndexName, FieldName)
        );
        """;

    private const string CreateSchemaHistoryTableSql = """
        CREATE TABLE IF NOT EXISTS ModularPersistenceSchemaHistory (
            SchemaName TEXT NOT NULL,
            Version TEXT NOT NULL,
            AppliedAt TEXT NOT NULL,
            PRIMARY KEY (SchemaName, Version)
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

    private static async ValueTask RecordManifestVersionAsync(DbConnection connection, DbTransaction transaction, StorageManifestDescriptor manifest, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT OR IGNORE INTO ModularPersistenceSchemaHistory (SchemaName, Version, AppliedAt)
            VALUES (@SchemaName, @Version, @AppliedAt);
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

using System.Data.Common;
using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Relational.Contracts;

namespace Elsa.ModularPersistence.PostgreSql.Services;

/// <summary>
/// Materializes the portable document schema into PostgreSQL.
/// </summary>
public sealed class PostgreSqlDocumentSchemaMaterializer(IRelationalConnectionFactory connectionFactory)
{
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

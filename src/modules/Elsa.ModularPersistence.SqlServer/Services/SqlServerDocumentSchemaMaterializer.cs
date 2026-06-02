using System.Data.Common;
using Elsa.ModularPersistence.Contracts;
using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Relational.Contracts;
using Elsa.ModularPersistence.SqlServer.Options;

namespace Elsa.ModularPersistence.SqlServer.Services;

/// <summary>
/// Materializes the portable document schema into SQL Server.
/// </summary>
public sealed class SqlServerDocumentSchemaMaterializer(IRelationalConnectionFactory connectionFactory, SqlServerModularPersistenceOptions? options = null) : IStorageManifestMaterializer
{
    public const string ProviderNameValue = "SQL Server";

    private readonly SqlServerModularPersistenceOptions _options = options ?? new SqlServerModularPersistenceOptions();

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
        if (_options.UseOptimizedIndexes)
        {
            foreach (var sql in BuildOptimizedIndexSql(manifest))
                await ExecuteAsync(connection, transaction, sql, cancellationToken);
        }

        await RecordManifestVersionAsync(connection, transaction, manifest, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private const string CreateDocumentsTableSql = """
        IF OBJECT_ID(N'dbo.ModularPersistenceDocuments', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.ModularPersistenceDocuments (
                Id NVARCHAR(450) NOT NULL,
                Type NVARCHAR(450) NOT NULL,
                TenantId NVARCHAR(450) NOT NULL CONSTRAINT DF_ModularPersistenceDocuments_TenantId DEFAULT N'',
                Version BIGINT NOT NULL,
                CreatedAt NVARCHAR(64) NOT NULL,
                UpdatedAt NVARCHAR(64) NOT NULL,
                Data NVARCHAR(MAX) NOT NULL,
                Metadata NVARCHAR(MAX) NULL,
                CONSTRAINT PK_ModularPersistenceDocuments PRIMARY KEY (Id, Type, TenantId)
            );
        END;
        """;

    private const string CreateDocumentIndexesTableSql = """
        IF OBJECT_ID(N'dbo.ModularPersistenceDocumentIndexes', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.ModularPersistenceDocumentIndexes (
                DocumentId NVARCHAR(450) NOT NULL,
                DocumentType NVARCHAR(450) NOT NULL,
                TenantId NVARCHAR(450) NOT NULL CONSTRAINT DF_ModularPersistenceDocumentIndexes_TenantId DEFAULT N'',
                IndexName NVARCHAR(450) NOT NULL,
                FieldName NVARCHAR(450) NOT NULL,
                StringValue NVARCHAR(450) NULL,
                NumberValue FLOAT NULL,
                BooleanValue BIT NULL,
                DateTimeValue NVARCHAR(64) NULL,
                NullValue BIT NOT NULL CONSTRAINT DF_ModularPersistenceDocumentIndexes_NullValue DEFAULT 0,
                CONSTRAINT PK_ModularPersistenceDocumentIndexes PRIMARY KEY (DocumentId, DocumentType, TenantId, IndexName, FieldName)
            );
        END;
        """;

    private const string CreateSchemaHistoryTableSql = """
        IF OBJECT_ID(N'dbo.ModularPersistenceSchemaHistory', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.ModularPersistenceSchemaHistory (
                SchemaName NVARCHAR(450) NOT NULL,
                Version NVARCHAR(64) NOT NULL,
                AppliedAt NVARCHAR(64) NOT NULL,
                CONSTRAINT PK_ModularPersistenceSchemaHistory PRIMARY KEY (SchemaName, Version)
            );
        END;
        """;

    private const string CreateDocumentIndexesByStringValueSql = """
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ModularPersistenceDocumentIndexes_StringValue' AND object_id = OBJECT_ID(N'dbo.ModularPersistenceDocumentIndexes'))
        BEGIN
            CREATE INDEX IX_ModularPersistenceDocumentIndexes_StringValue
            ON dbo.ModularPersistenceDocumentIndexes (DocumentType, TenantId, IndexName, FieldName, StringValue);
        END;
        """;

    private const string CreateDocumentIndexesByNumberValueSql = """
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ModularPersistenceDocumentIndexes_NumberValue' AND object_id = OBJECT_ID(N'dbo.ModularPersistenceDocumentIndexes'))
        BEGIN
            CREATE INDEX IX_ModularPersistenceDocumentIndexes_NumberValue
            ON dbo.ModularPersistenceDocumentIndexes (DocumentType, TenantId, IndexName, FieldName, NumberValue);
        END;
        """;

    private const string CreateDocumentIndexesByDateTimeValueSql = """
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ModularPersistenceDocumentIndexes_DateTimeValue' AND object_id = OBJECT_ID(N'dbo.ModularPersistenceDocumentIndexes'))
        BEGIN
            CREATE INDEX IX_ModularPersistenceDocumentIndexes_DateTimeValue
            ON dbo.ModularPersistenceDocumentIndexes (DocumentType, TenantId, IndexName, FieldName, DateTimeValue);
        END;
        """;

    private async ValueTask AcquireSchemaLockAsync(DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DECLARE @LockResult int;
            EXEC @LockResult = sp_getapplock
                @Resource = N'Elsa.ModularPersistence.Schema',
                @LockMode = N'Exclusive',
                @LockOwner = N'Transaction',
                @LockTimeout = @LockTimeout;

            IF @LockResult < 0
                THROW 51000, 'Timed out waiting for the Elsa modular persistence schema lock.', 1;
            """;
        AddParameter(command, "@LockTimeout", Convert.ToInt32(_options.SchemaLockTimeout.TotalMilliseconds));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static IEnumerable<string> BuildOptimizedIndexSql(StorageManifestDescriptor manifest)
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
                    var valueColumn = GetIndexedColumn(field.Type);
                    var name = SanitizeIdentifier($"IX_ModularPersistenceDocumentIndexes_Optimized_{index.Name}_{field.Name}_{valueColumn}");
                    var indexName = EscapeSqlLiteral(index.Name);
                    var fieldName = EscapeSqlLiteral(field.Name);

                    yield return $"""
                        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'{name}' AND object_id = OBJECT_ID(N'dbo.ModularPersistenceDocumentIndexes'))
                        BEGIN
                            CREATE INDEX {name}
                            ON dbo.ModularPersistenceDocumentIndexes (DocumentType, TenantId, {valueColumn})
                            WHERE IndexName = N'{indexName}' AND FieldName = N'{fieldName}' AND NullValue = 0;
                        END;
                        """;
                }
            }
        }
    }

    private static string GetIndexedColumn(StorageFieldType fieldType) =>
        fieldType switch
        {
            StorageFieldType.String or StorageFieldType.Guid or StorageFieldType.Json or StorageFieldType.Binary => "StringValue",
            StorageFieldType.Int32 or StorageFieldType.Int64 or StorageFieldType.Decimal => "NumberValue",
            StorageFieldType.Boolean => "BooleanValue",
            StorageFieldType.DateTimeOffset => "DateTimeValue",
            _ => throw new ArgumentOutOfRangeException(nameof(fieldType), fieldType, "Unknown storage field type.")
        };

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
            IF NOT EXISTS (
                SELECT 1
                FROM dbo.ModularPersistenceSchemaHistory
                WHERE SchemaName = @SchemaName AND Version = @Version
            )
            BEGIN
                INSERT INTO dbo.ModularPersistenceSchemaHistory (SchemaName, Version, AppliedAt)
                VALUES (@SchemaName, @Version, @AppliedAt);
            END;
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

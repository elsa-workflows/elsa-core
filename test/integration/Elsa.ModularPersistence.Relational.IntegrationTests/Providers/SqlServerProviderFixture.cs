using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;
using Elsa.ModularPersistence.SqlServer.Options;
using Elsa.ModularPersistence.SqlServer.Services;
using Microsoft.Data.SqlClient;

namespace Elsa.ModularPersistence.Relational.IntegrationTests.Providers;

public sealed class SqlServerProviderFixture : RelationalProviderFixtureBase
{
    private const string ConnectionStringEnvironmentVariable = "ELSA_MODULAR_PERSISTENCE_SQLSERVER_CONNECTION_STRING";
    private readonly string? _connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);

    public override string ProviderName => "SQL Server";

    public override bool IsAvailable => !string.IsNullOrWhiteSpace(_connectionString);

    public override async ValueTask ResetAsync()
    {
        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            IF OBJECT_ID(N'dbo.ModularPersistenceDocumentIndexes', N'U') IS NOT NULL DROP TABLE dbo.ModularPersistenceDocumentIndexes;
            IF OBJECT_ID(N'dbo.ModularPersistenceDocuments', N'U') IS NOT NULL DROP TABLE dbo.ModularPersistenceDocuments;
            IF OBJECT_ID(N'dbo.ModularPersistenceSchemaHistory', N'U') IS NOT NULL DROP TABLE dbo.ModularPersistenceSchemaHistory;
            """;
        await command.ExecuteNonQueryAsync();
    }

    public override async ValueTask MaterializeAsync(StorageManifestDescriptor manifest)
    {
        var materializer = new SqlServerDocumentSchemaMaterializer(CreateConnectionFactory());
        await materializer.MaterializeAsync(manifest);
    }

    public override IDocumentStore CreateStore(StorageManifestDescriptor manifest) =>
        new SqlServerDocumentStore(CreateConnectionFactory(), manifest);

    public override async ValueTask<bool> TableExistsAsync(string tableName)
    {
        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE name = @Name";
        AddParameter(command, "@Name", tableName);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }

    public override async ValueTask<bool> IndexExistsAsync(string indexName)
    {
        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM sys.indexes
            WHERE name = @Name AND object_id = OBJECT_ID(N'dbo.ModularPersistenceDocumentIndexes');
            """;
        AddParameter(command, "@Name", indexName);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }

    public override async ValueTask<int> CountIndexRowsAsync(DocumentKey key)
    {
        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM dbo.ModularPersistenceDocumentIndexes
            WHERE DocumentId = @Id AND DocumentType = @Type AND TenantId = @TenantId;
            """;
        AddKeyParameters(command, key);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public override async ValueTask<IReadOnlyCollection<(string SchemaName, string Version)>> ReadSchemaHistoryAsync()
    {
        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT SchemaName, Version FROM dbo.ModularPersistenceSchemaHistory ORDER BY SchemaName, Version";
        await using var reader = await command.ExecuteReaderAsync();
        var entries = new List<(string SchemaName, string Version)>();

        while (await reader.ReadAsync())
            entries.Add((reader.GetString(0), reader.GetString(1)));

        return entries;
    }

    private SqlServerModularPersistenceConnectionFactory CreateConnectionFactory() =>
        new(new SqlServerModularPersistenceOptions { ConnectionString = GetConnectionString() });

    private string GetConnectionString()
    {
        if (!string.IsNullOrWhiteSpace(_connectionString))
            return _connectionString;

        throw new InvalidOperationException($"Set {ConnectionStringEnvironmentVariable} to run SQL Server modular persistence contract tests.");
    }
}

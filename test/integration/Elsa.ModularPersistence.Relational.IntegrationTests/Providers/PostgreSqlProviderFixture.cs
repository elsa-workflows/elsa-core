using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;
using Elsa.ModularPersistence.PostgreSql.Options;
using Elsa.ModularPersistence.PostgreSql.Services;
using Npgsql;

namespace Elsa.ModularPersistence.Relational.IntegrationTests.Providers;

public sealed class PostgreSqlProviderFixture : RelationalProviderFixtureBase
{
    private const string ConnectionStringEnvironmentVariable = "ELSA_MODULAR_PERSISTENCE_POSTGRESQL_CONNECTION_STRING";
    private readonly string? _connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);

    public override string ProviderName => "PostgreSQL";

    public override bool IsAvailable => !string.IsNullOrWhiteSpace(_connectionString);

    public override async ValueTask ResetAsync()
    {
        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DROP TABLE IF EXISTS ModularPersistenceDocumentIndexes;
            DROP TABLE IF EXISTS ModularPersistenceDocuments;
            DROP TABLE IF EXISTS ModularPersistenceSchemaHistory;
            """;
        await command.ExecuteNonQueryAsync();
    }

    public override async ValueTask MaterializeAsync(StorageManifestDescriptor manifest)
    {
        var materializer = new PostgreSqlDocumentSchemaMaterializer(CreateConnectionFactory());
        await materializer.MaterializeAsync(manifest);
    }

    public override IDocumentStore CreateStore(StorageManifestDescriptor manifest) =>
        new PostgreSqlDocumentStore(CreateConnectionFactory(), manifest);

    public override async ValueTask<bool> TableExistsAsync(string tableName)
    {
        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = current_schema() AND table_name = lower(@Name);
            """;
        AddParameter(command, "@Name", tableName);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }

    public override async ValueTask<bool> IndexExistsAsync(string indexName)
    {
        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM pg_indexes
            WHERE schemaname = current_schema() AND indexname = lower(@Name);
            """;
        AddParameter(command, "@Name", indexName);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }

    public override async ValueTask<int> CountIndexRowsAsync(DocumentKey key)
    {
        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM ModularPersistenceDocumentIndexes
            WHERE DocumentId = @Id AND DocumentType = @Type AND TenantId = @TenantId;
            """;
        AddKeyParameters(command, key);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public override async ValueTask<IReadOnlyCollection<(string SchemaName, string Version)>> ReadSchemaHistoryAsync()
    {
        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT SchemaName, Version FROM ModularPersistenceSchemaHistory ORDER BY SchemaName, Version";
        await using var reader = await command.ExecuteReaderAsync();
        var entries = new List<(string SchemaName, string Version)>();

        while (await reader.ReadAsync())
            entries.Add((reader.GetString(0), reader.GetString(1)));

        return entries;
    }

    private PostgreSqlModularPersistenceConnectionFactory CreateConnectionFactory() =>
        new(new PostgreSqlModularPersistenceOptions { ConnectionString = GetConnectionString() });

    private string GetConnectionString()
    {
        if (!string.IsNullOrWhiteSpace(_connectionString))
            return _connectionString;

        throw new InvalidOperationException($"Set {ConnectionStringEnvironmentVariable} to run PostgreSQL modular persistence contract tests.");
    }
}

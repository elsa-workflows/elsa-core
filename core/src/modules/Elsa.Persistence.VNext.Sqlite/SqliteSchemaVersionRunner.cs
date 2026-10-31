using Elsa.Persistence.VNext.Contracts;
using Elsa.Persistence.VNext.Relational;
using Microsoft.Data.Sqlite;

namespace Elsa.Persistence.VNext.Sqlite;

public class SqliteSchemaVersionRunner : IPersistenceSchemaMigrator<SqliteConnection>
{
    private const string HistoryTable = "ElsaSchemaVersions";
    private readonly RelationalSchemaPlanner _planner;
    private readonly SqliteSchemaSqlRenderer _renderer;

    public SqliteSchemaVersionRunner() : this(new RelationalSchemaPlanner(new SqliteTypeMapper()), new SqliteSchemaSqlRenderer())
    {
    }

    public SqliteSchemaVersionRunner(RelationalSchemaPlanner planner, SqliteSchemaSqlRenderer renderer)
    {
        _planner = planner;
        _renderer = renderer;
    }

    public async Task<PersistenceSchemaMigrationResult> MigrateAsync(SqliteConnection connection, PersistenceSchema schema, CancellationToken cancellationToken = default)
    {
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await EnsureHistoryTableAsync(connection, transaction, cancellationToken);

        var currentVersion = await GetCurrentVersionAsync(connection, transaction, schema.Name, cancellationToken);
        if (currentVersion >= schema.Version)
        {
            await transaction.CommitAsync(cancellationToken);
            return new PersistenceSchemaMigrationResult(schema.Name, currentVersion, schema.Version, false, []);
        }

        var statements = _renderer.Render(_planner.Plan(schema));
        foreach (var statement in statements)
            await ExecuteAsync(connection, transaction, statement, cancellationToken);

        await RecordVersionAsync(connection, transaction, schema, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new PersistenceSchemaMigrationResult(schema.Name, currentVersion, schema.Version, true, statements);
    }

    private static Task EnsureHistoryTableAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = $"""
                           CREATE TABLE IF NOT EXISTS "{HistoryTable}" (
                               "SchemaName" TEXT NOT NULL,
                               "Version" INTEGER NOT NULL,
                               "AppliedAt" TEXT NOT NULL,
                               CONSTRAINT "PK_ElsaSchemaVersions" PRIMARY KEY ("SchemaName", "Version")
                           );
                           """;

        return ExecuteAsync(connection, transaction, sql, cancellationToken);
    }

    private static async Task<int> GetCurrentVersionAsync(SqliteConnection connection, SqliteTransaction transaction, string schemaName, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""SELECT COALESCE(MAX("Version"), 0) FROM "{HistoryTable}" WHERE "SchemaName" = $schemaName;""";
        command.Parameters.AddWithValue("$schemaName", schemaName);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private static async Task RecordVersionAsync(SqliteConnection connection, SqliteTransaction transaction, PersistenceSchema schema, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""INSERT INTO "{HistoryTable}" ("SchemaName", "Version", "AppliedAt") VALUES ($schemaName, $version, $appliedAt);""";
        command.Parameters.AddWithValue("$schemaName", schema.Name);
        command.Parameters.AddWithValue("$version", schema.Version);
        command.Parameters.AddWithValue("$appliedAt", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ExecuteAsync(SqliteConnection connection, SqliteTransaction transaction, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

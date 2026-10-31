using System.Reflection;
using Elsa.Persistence.EFCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using MySqlSecrets = Elsa.Secrets.Persistence.EFCore.MySql.Migrations.Secrets;
using OracleSecrets = Elsa.Secrets.Persistence.EFCore.Oracle.Migrations.Secrets;
using PostgreSqlConnections = Elsa.Connections.Credentials.Persistence.EFCore.PostgreSql.Migrations.Connections;
using PostgreSqlSecrets = Elsa.Secrets.Persistence.EFCore.PostgreSql.Migrations.Secrets;
using SqlServerSecrets = Elsa.Secrets.Persistence.EFCore.SqlServer.Migrations.Secrets;
using SqliteConnections = Elsa.Connections.Credentials.Persistence.EFCore.Sqlite.Migrations.Connections;
using SqliteSecrets = Elsa.Secrets.Persistence.EFCore.Sqlite.Migrations.Secrets;

namespace Elsa.Secrets.UnitTests;

/// <summary>
/// Secrets and Connections migrations must apply schema-bearing operations to the configured schema (#8807).
/// </summary>
public class SecretMigrationCustomSchemaTests
{
    private const string CustomSchema = "custom_schema";
    private const string DefaultSchema = "Elsa";

    public static TheoryData<string, string, Func<IElsaDbContextSchema, Migration>> SecretsMigrations => new()
    {
        { "SqlServer/SecretTenancy", "Microsoft.EntityFrameworkCore.SqlServer", schema => new SqlServerSecrets.SecretTenancy(schema) },
        { "SqlServer/ManagedSecretOwnership", "Microsoft.EntityFrameworkCore.SqlServer", schema => new SqlServerSecrets.ManagedSecretOwnership(schema) },
        { "PostgreSql/SecretTenancy", "Npgsql.EntityFrameworkCore.PostgreSQL", schema => new PostgreSqlSecrets.SecretTenancy(schema) },
        { "PostgreSql/ManagedSecretOwnership", "Npgsql.EntityFrameworkCore.PostgreSQL", schema => new PostgreSqlSecrets.ManagedSecretOwnership(schema) },
        { "MySql/SecretTenancy", "Pomelo.EntityFrameworkCore.MySql", schema => new MySqlSecrets.SecretTenancy(schema) },
        { "MySql/ManagedSecretOwnership", "Pomelo.EntityFrameworkCore.MySql", schema => new MySqlSecrets.ManagedSecretOwnership(schema) },
        { "Oracle/SecretTenancy", "Oracle.EntityFrameworkCore", schema => new OracleSecrets.SecretTenancy(schema) },
        { "Oracle/ManagedSecretOwnership", "Oracle.EntityFrameworkCore", schema => new OracleSecrets.ManagedSecretOwnership(schema) },
        { "Sqlite/SecretTenancy", "Microsoft.EntityFrameworkCore.Sqlite", schema => new SqliteSecrets.SecretTenancy(schema) },
        { "Sqlite/ManagedSecretOwnership", "Microsoft.EntityFrameworkCore.Sqlite", schema => new SqliteSecrets.ManagedSecretOwnership(schema) },
    };

    public static TheoryData<string, string, Func<IElsaDbContextSchema, Migration>> ConnectionsMigrations => new()
    {
        { "PostgreSql/Initial", "Npgsql.EntityFrameworkCore.PostgreSQL", schema => new PostgreSqlConnections.Initial(schema) },
        { "PostgreSql/WorkflowCredentialUseGrants", "Npgsql.EntityFrameworkCore.PostgreSQL", schema => new PostgreSqlConnections.WorkflowCredentialUseGrants(schema) },
        { "PostgreSql/DueCredentialLifecycleCandidates", "Npgsql.EntityFrameworkCore.PostgreSQL", schema => new PostgreSqlConnections.DueCredentialLifecycleCandidates(schema) },
        { "Sqlite/Initial", "Microsoft.EntityFrameworkCore.Sqlite", schema => new SqliteConnections.Initial(schema) },
        { "Sqlite/GenerationCleanupLedger", "Microsoft.EntityFrameworkCore.Sqlite", schema => new SqliteConnections.GenerationCleanupLedger(schema) },
        { "Sqlite/WorkflowCredentialBindings", "Microsoft.EntityFrameworkCore.Sqlite", schema => new SqliteConnections.WorkflowCredentialBindings(schema) },
        { "Sqlite/ConnectionOffboardingOperations", "Microsoft.EntityFrameworkCore.Sqlite", schema => new SqliteConnections.ConnectionOffboardingOperations(schema) },
        { "Sqlite/WorkflowCredentialUseGrants", "Microsoft.EntityFrameworkCore.Sqlite", schema => new SqliteConnections.WorkflowCredentialUseGrants(schema) },
        { "Sqlite/DueCredentialLifecycleCandidates", "Microsoft.EntityFrameworkCore.Sqlite", schema => new SqliteConnections.DueCredentialLifecycleCandidates(schema) },
    };

    [Theory]
    [MemberData(nameof(SecretsMigrations))]
    public void Secrets_UpAndDown_UseConfiguredSchema(string name, string activeProvider, Func<IElsaDbContextSchema, Migration> create)
    {
        AssertMigrationUsesConfiguredSchema(name, activeProvider, create);
    }

    [Theory]
    [MemberData(nameof(ConnectionsMigrations))]
    public void Connections_UpAndDown_UseConfiguredSchema(string name, string activeProvider, Func<IElsaDbContextSchema, Migration> create)
    {
        AssertMigrationUsesConfiguredSchema(name, activeProvider, create);
    }

    private static void AssertMigrationUsesConfiguredSchema(string name, string activeProvider, Func<IElsaDbContextSchema, Migration> create)
    {
        var migration = create(new TestSchema(CustomSchema));
        migration.ActiveProvider = activeProvider;

        var upOperations = Apply(migration, "Up");
        var downOperations = Apply(migration, "Down");

        Assert.NotEmpty(upOperations);
        AssertOperationsUseCustomSchema(name, "Up", upOperations);
        AssertOperationsUseCustomSchema(name, "Down", downOperations);
    }

    private static IReadOnlyList<MigrationOperation> Apply(Migration migration, string methodName)
    {
        var builder = new MigrationBuilder(migration.ActiveProvider);
        var method = typeof(Migration).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Migration.{methodName} was not found.");

        try
        {
            method.Invoke(migration, [builder]);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is NotSupportedException)
        {
            // Irreversible migrations throw from Down and produce no operations.
        }

        return builder.Operations;
    }

    private static void AssertOperationsUseCustomSchema(string name, string direction, IReadOnlyList<MigrationOperation> operations)
    {
        var schemaValues = operations
            .SelectMany(operation => GetSchemaValues(operation).Select(value => (Operation: operation, value.Property, value.Schema)))
            .ToList();

        if (direction == "Up")
            Assert.False(schemaValues.Count == 0, $"{name} {direction} produced no schema-bearing operations.");

        foreach (var (operation, property, schema) in schemaValues)
        {
            Assert.False(
                string.Equals(schema, DefaultSchema, StringComparison.Ordinal),
                $"{name} {direction} {operation.GetType().Name}.{property} used the hard-coded {DefaultSchema} schema.");
            Assert.Equal(CustomSchema, schema);
        }
    }

    private static IEnumerable<(string Property, string? Schema)> GetSchemaValues(MigrationOperation operation)
    {
        if (operation is EnsureSchemaOperation ensureSchema)
        {
            yield return ("Name", ensureSchema.Name);
            yield break;
        }

        if (operation is DropSchemaOperation dropSchema)
        {
            yield return ("Name", dropSchema.Name);
            yield break;
        }

        foreach (var property in operation.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.PropertyType != typeof(string) || property.GetIndexParameters().Length != 0)
                continue;
            if (!property.Name.Contains("Schema", StringComparison.Ordinal))
                continue;

            yield return (property.Name, property.GetValue(operation) as string);
        }
    }

    private sealed record TestSchema(string Schema) : IElsaDbContextSchema;
}

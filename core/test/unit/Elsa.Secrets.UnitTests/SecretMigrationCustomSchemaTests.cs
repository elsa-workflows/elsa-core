using System.Reflection;
using Elsa.Connections.Credentials.Persistence.EFCore;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.UnitTests;
using Elsa.Secrets.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Elsa.Secrets.UnitTests;

/// <summary>
/// Secrets and Connections migrations must go through <see cref="DbSchemaAwareMigrationAssembly"/>
/// and apply schema-bearing operations to the configured schema (#8807).
/// </summary>
public class SecretMigrationCustomSchemaTests
{
    private const string CustomSchema = "CustomSchema";

    public static TheoryData<string> SecretsProviders =>
    [
        "SqlServer",
        "PostgreSql",
        "Oracle",
        "Sqlite"
        // MySQL is covered by Elsa.Persistence.EFCore.MySql.UnitTests; Pomelo cannot load on net10.
    ];

    public static TheoryData<string> ConnectionsProviders =>
    [
        "PostgreSql",
        "Sqlite"
    ];

    [Theory]
    [MemberData(nameof(SecretsProviders))]
    public void Secrets_MigrationScripts_UseConfiguredSchema(string providerName)
    {
        var provider = GetSecretsProvider(providerName);
        AssertProviderScripts<SecretsElsaDbContext>(provider);
    }

    [Theory]
    [MemberData(nameof(ConnectionsProviders))]
    public void Connections_MigrationScripts_UseConfiguredSchema(string providerName)
    {
        var provider = GetConnectionsProvider(providerName);
        AssertProviderScripts<ConnectionsElsaDbContext>(provider);
    }

    private static void AssertProviderScripts<TDbContext>(ProviderDefinition provider)
        where TDbContext : DbContext
    {
        var contextOptions = new ElsaDbContextOptions { SchemaName = CustomSchema };
        Action<DbContextOptionsBuilder<TDbContext>> configure = builder =>
            provider.Configure(builder, provider.MigrationsAssembly, contextOptions);

        var upScript = MigrationScriptGenerator.Generate(
            configure,
            Migration.InitialDatabase,
            provider.LastMigration,
            MigrationsSqlGenerationOptions.Default);

        AssertScriptUsesCustomSchema(upScript, requireCustomSchema: provider.SqlIncludesSchema);
        AssertUsesConfiguredSchema(MigrationScriptGenerator.CreateMigration(configure, provider.LastMigration).UpOperations);

        if (provider.LastReversibleMigration is null)
        {
            return;
        }

        var downScript = MigrationScriptGenerator.Generate(
            configure,
            provider.LastReversibleMigration,
            Migration.InitialDatabase,
            MigrationsSqlGenerationOptions.Default);

        AssertScriptUsesCustomSchema(downScript, requireCustomSchema: provider.SqlIncludesSchema);
        AssertUsesConfiguredSchema(MigrationScriptGenerator.CreateMigration(configure, provider.LastReversibleMigration).DownOperations);
    }

    private static void AssertScriptUsesCustomSchema(string script, bool requireCustomSchema)
    {
        Assert.False(string.IsNullOrWhiteSpace(script));
        Assert.DoesNotContain("Elsa", script, StringComparison.OrdinalIgnoreCase);
        if (requireCustomSchema)
        {
            Assert.Contains(CustomSchema, script, StringComparison.Ordinal);
        }
    }

    private static void AssertUsesConfiguredSchema(IEnumerable<MigrationOperation> operations)
    {
        var schemas = operations
            .Where(HasSchema)
            .Select(GetSchema)
            .ToList();

        Assert.NotEmpty(schemas);
        Assert.All(schemas, schema => Assert.Equal(CustomSchema, schema));
    }

    private static bool HasSchema(MigrationOperation operation) =>
        operation is EnsureSchemaOperation || operation.GetType().GetProperty("Schema") is not null;

    private static string? GetSchema(MigrationOperation operation)
    {
        if (operation is EnsureSchemaOperation ensureSchema)
        {
            return ensureSchema.Name;
        }

        return operation.GetType().GetProperty("Schema")?.GetValue(operation) as string;
    }

    private static ProviderDefinition GetSecretsProvider(string providerName) => providerName switch
    {
        "SqlServer" => new(
            typeof(Elsa.Secrets.Persistence.EFCore.SqlServer.Migrations.Secrets.ManagedSecretOwnership).Assembly,
            "20260923164247_ManagedSecretOwnership",
            "20260914120000_SecretDefaultTenantUniqueness",
            true,
            (builder, assembly, options) => builder.UseElsaSqlServer(assembly, "Server=unused", options)),
        "PostgreSql" => new(
            typeof(Elsa.Secrets.Persistence.EFCore.PostgreSql.Migrations.Secrets.ManagedSecretOwnership).Assembly,
            "20260923164247_ManagedSecretOwnership",
            "20260914120000_SecretDefaultTenantUniqueness",
            true,
            (builder, assembly, options) => builder.UseElsaPostgreSql(assembly, "Host=unused", options)),
        "Oracle" => new(
            typeof(Elsa.Secrets.Persistence.EFCore.Oracle.Migrations.Secrets.ManagedSecretOwnership).Assembly,
            "20260923164246_ManagedSecretOwnership",
            "20260914120000_SecretDefaultTenantUniqueness",
            true,
            (builder, assembly, options) => builder.UseElsaOracle(assembly, "Data Source=unused", options)),
        "Sqlite" => new(
            typeof(Elsa.Secrets.Persistence.EFCore.Sqlite.Migrations.Secrets.ManagedSecretOwnership).Assembly,
            "20260923164123_ManagedSecretOwnership",
            "20260914120000_SecretDefaultTenantUniqueness",
            false,
            (builder, assembly, options) => builder.UseElsaSqlite(assembly, "Data Source=:memory:", options)),
        _ => throw new ArgumentOutOfRangeException(nameof(providerName), providerName, null)
    };

    private static ProviderDefinition GetConnectionsProvider(string providerName) => providerName switch
    {
        "PostgreSql" => new(
            typeof(Elsa.Connections.Credentials.Persistence.EFCore.PostgreSql.Migrations.Connections.DueCredentialLifecycleCandidates).Assembly,
            "20260924150000_DueCredentialLifecycleCandidates",
            null,
            true,
            (builder, assembly, options) => builder.UseElsaPostgreSql(assembly, "Host=unused", options)),
        "Sqlite" => new(
            typeof(Elsa.Connections.Credentials.Persistence.EFCore.Sqlite.Migrations.Connections.DueCredentialLifecycleCandidates).Assembly,
            "20260924150000_DueCredentialLifecycleCandidates",
            null,
            false,
            (builder, assembly, options) => builder.UseElsaSqlite(assembly, "Data Source=:memory:", options)),
        _ => throw new ArgumentOutOfRangeException(nameof(providerName), providerName, null)
    };

    private sealed record ProviderDefinition(
        Assembly MigrationsAssembly,
        string LastMigration,
        string? LastReversibleMigration,
        bool SqlIncludesSchema,
        Action<DbContextOptionsBuilder, Assembly, ElsaDbContextOptions> Configure);
}

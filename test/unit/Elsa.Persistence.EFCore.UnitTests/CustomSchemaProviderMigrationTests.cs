using System.Reflection;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Elsa.Persistence.EFCore.UnitTests;

public class CustomSchemaProviderMigrationTests
{
    private const string CustomSchema = "CustomSchema";

    public static TheoryData<string> Providers
    {
        get
        {
            return
            [
                "SqlServer",
                "PostgreSql",
                "Oracle",
                "Sqlite"
            ];
        }
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public void FullMigrationScripts_UseConfiguredSchema(string providerName)
    {
        var provider = GetProvider(providerName);
        var contextOptions = new ElsaDbContextOptions { SchemaName = CustomSchema };
        Action<DbContextOptionsBuilder<IdentityElsaDbContext>> configureIdentity = builder => provider.Configure(builder, provider.MigrationsAssembly, contextOptions);

        var identityScript = MigrationScriptGenerator.Generate(
            configureIdentity,
            Migration.InitialDatabase,
            provider.IdentityMigration,
            MigrationsSqlGenerationOptions.Default);

        Assert.DoesNotContain("Elsa", identityScript, StringComparison.OrdinalIgnoreCase);

        AssertUsesConfiguredSchema(MigrationScriptGenerator.CreateMigration(configureIdentity, provider.IdentityMigration));
    }

    private static void AssertUsesConfiguredSchema(Migration migration)
    {
        var schemas = migration.UpOperations
            .Concat(migration.DownOperations)
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
            return ensureSchema.Name;

        return operation.GetType().GetProperty("Schema")?.GetValue(operation) as string;
    }

    private static ProviderDefinition GetProvider(string providerName) => providerName switch
    {
        "SqlServer" => new(
            typeof(Elsa.Persistence.EFCore.SqlServer.Migrations.Identity.PerTenantIdentityUniqueness).Assembly,
            "20260823231421_PerTenantIdentityUniqueness",
            (builder, assembly, options) => builder.UseElsaSqlServer(assembly, "Server=unused", options)),
        "PostgreSql" => new(
            typeof(Elsa.Persistence.EFCore.PostgreSql.Migrations.Identity.PerTenantIdentityUniqueness).Assembly,
            "20260823231437_PerTenantIdentityUniqueness",
            (builder, assembly, options) => builder.UseElsaPostgreSql(assembly, "Host=unused", options)),
        "Oracle" => new(
            typeof(Elsa.Persistence.EFCore.Oracle.Migrations.Identity.PerTenantIdentityUniqueness).Assembly,
            "20260823231459_PerTenantIdentityUniqueness",
            (builder, assembly, options) => builder.UseElsaOracle(assembly, "Data Source=unused", options)),
        "Sqlite" => new(
            typeof(Elsa.Persistence.EFCore.Sqlite.Migrations.Identity.PerTenantIdentityUniqueness).Assembly,
            "20260823231406_PerTenantIdentityUniqueness",
            (builder, assembly, options) => builder.UseElsaSqlite(assembly, "Data Source=:memory:", options)),
        _ => throw new ArgumentOutOfRangeException(nameof(providerName), providerName, null)
    };

    private sealed record ProviderDefinition(
        Assembly MigrationsAssembly,
        string IdentityMigration,
        Action<DbContextOptionsBuilder, Assembly, ElsaDbContextOptions> Configure);
}

using System.Reflection;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Identity;
using Elsa.Persistence.EFCore.Modules.Management;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
#if NET9_0
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
#endif

namespace Elsa.Persistence.EFCore.UnitTests;

public class CustomSchemaProviderMigrationTests
{
    private const string CustomSchema = "CustomSchema";

    public static TheoryData<string> Providers
    {
        get
        {
            var providers = new TheoryData<string>
            {
                "SqlServer",
                "PostgreSql",
                "Oracle",
                "Sqlite"
            };
#if NET9_0
            providers.Add("MySql");
#endif
            return providers;
        }
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public void FullMigrationScripts_UseConfiguredSchema(string providerName)
    {
        var provider = GetProvider(providerName);
        var contextOptions = new ElsaDbContextOptions { SchemaName = CustomSchema };
        Action<DbContextOptionsBuilder<ManagementElsaDbContext>> configureManagement = builder => provider.Configure(builder, provider.MigrationsAssembly, contextOptions);
        Action<DbContextOptionsBuilder<IdentityElsaDbContext>> configureIdentity = builder => provider.Configure(builder, provider.MigrationsAssembly, contextOptions);

        var managementScript = MigrationScriptGenerator.Generate(
            configureManagement,
            Migration.InitialDatabase,
            provider.ManagementMigration,
            MigrationsSqlGenerationOptions.Default);
        var identityScript = MigrationScriptGenerator.Generate(
            configureIdentity,
            Migration.InitialDatabase,
            provider.IdentityMigration,
            MigrationsSqlGenerationOptions.Default);

        Assert.DoesNotContain("Elsa", managementScript, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Elsa", identityScript, StringComparison.OrdinalIgnoreCase);

        AssertUsesConfiguredSchema(MigrationScriptGenerator.CreateMigration(configureManagement, provider.ManagementMigration));
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
            typeof(Elsa.Persistence.EFCore.SqlServer.Migrations.Management.WorkflowDefinitionRegistryGeneration).Assembly,
            "20260924222035_WorkflowDefinitionRegistryGeneration",
            "20260823231421_PerTenantIdentityUniqueness",
            (builder, assembly, options) => builder.UseElsaSqlServer(assembly, "Server=unused", options)),
        "PostgreSql" => new(
            typeof(Elsa.Persistence.EFCore.PostgreSql.Migrations.Management.WorkflowDefinitionRegistryGeneration).Assembly,
            "20260924222104_WorkflowDefinitionRegistryGeneration",
            "20260823231437_PerTenantIdentityUniqueness",
            (builder, assembly, options) => builder.UseElsaPostgreSql(assembly, "Host=unused", options)),
        "Oracle" => new(
            typeof(Elsa.Persistence.EFCore.Oracle.Migrations.Management.WorkflowDefinitionRegistryGeneration).Assembly,
            "20260924222221_WorkflowDefinitionRegistryGeneration",
            "20260823231459_PerTenantIdentityUniqueness",
            (builder, assembly, options) => builder.UseElsaOracle(assembly, "Data Source=unused", options)),
        "Sqlite" => new(
            typeof(Elsa.Persistence.EFCore.Sqlite.Migrations.Management.WorkflowDefinitionRegistryGeneration).Assembly,
            "20260924222051_WorkflowDefinitionRegistryGeneration",
            "20260823231406_PerTenantIdentityUniqueness",
            (builder, assembly, options) => builder.UseElsaSqlite(assembly, "Data Source=:memory:", options)),
#if NET9_0
        "MySql" => new(
            typeof(Elsa.Persistence.EFCore.MySql.Migrations.Management.WorkflowDefinitionRegistryGeneration).Assembly,
            "20260924222151_WorkflowDefinitionRegistryGeneration",
            "20260823231449_PerTenantIdentityUniqueness",
            (builder, assembly, options) => builder.UseElsaMySql(assembly, "Server=unused", options, ServerVersion.Parse("9.0.0"))),
#endif
        _ => throw new ArgumentOutOfRangeException(nameof(providerName), providerName, null)
    };

    private sealed record ProviderDefinition(
        Assembly MigrationsAssembly,
        string ManagementMigration,
        string IdentityMigration,
        Action<DbContextOptionsBuilder, Assembly, ElsaDbContextOptions> Configure);
}
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.UnitTests;
using Elsa.Secrets.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Elsa.Persistence.EFCore.MySql.UnitTests;

/// <summary>
/// MySQL Secrets migrations must go through <c>DbSchemaAwareMigrationAssembly</c> (#8807).
/// Pomelo does not target net10, so this lives with the MySQL unit tests.
/// </summary>
public class SecretMigrationCustomSchemaTests
{
    private const string CustomSchema = "CustomSchema";
    private const string LastMigration = "20260923164244_ManagedSecretOwnership";
    private const string LastReversibleMigration = "20260914120000_SecretDefaultTenantUniqueness";

    [Fact]
    public void Secrets_MigrationScripts_UseConfiguredSchema()
    {
        var assembly = typeof(Elsa.Secrets.Persistence.EFCore.MySql.Migrations.Secrets.ManagedSecretOwnership).Assembly;
        var contextOptions = new ElsaDbContextOptions { SchemaName = CustomSchema };
        Action<DbContextOptionsBuilder<SecretsElsaDbContext>> configure = builder =>
            builder.UseElsaMySql(assembly, "Server=unused", contextOptions, ServerVersion.Parse("9.0.0"));

        var upScript = MigrationScriptGenerator.Generate(
            configure,
            Migration.InitialDatabase,
            LastMigration,
            MigrationsSqlGenerationOptions.Default);
        var downScript = MigrationScriptGenerator.Generate(
            configure,
            LastReversibleMigration,
            Migration.InitialDatabase,
            MigrationsSqlGenerationOptions.Default);

        Assert.False(string.IsNullOrWhiteSpace(upScript));
        Assert.False(string.IsNullOrWhiteSpace(downScript));
        Assert.DoesNotContain("Elsa", upScript, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Elsa", downScript, StringComparison.OrdinalIgnoreCase);

        AssertUsesConfiguredSchema(MigrationScriptGenerator.CreateMigration(configure, LastMigration).UpOperations);
        AssertUsesConfiguredSchema(MigrationScriptGenerator.CreateMigration(configure, LastReversibleMigration).DownOperations);
    }

    private static void AssertUsesConfiguredSchema(IEnumerable<MigrationOperation> operations)
    {
        var schemas = operations
            .Where(operation => operation is EnsureSchemaOperation || operation.GetType().GetProperty("Schema") is not null)
            .Select(operation => operation is EnsureSchemaOperation ensure
                ? ensure.Name
                : operation.GetType().GetProperty("Schema")?.GetValue(operation) as string)
            .ToList();

        Assert.NotEmpty(schemas);
        Assert.All(schemas, schema => Assert.Equal(CustomSchema, schema));
    }
}

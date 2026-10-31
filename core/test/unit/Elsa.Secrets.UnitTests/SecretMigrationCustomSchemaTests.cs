using Elsa.Persistence.EFCore;
using Elsa.Secrets.Persistence.EFCore.Sqlite.Migrations.Secrets;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Elsa.Secrets.UnitTests;

/// <summary>
/// Later Secrets and Connections migrations must use the configured schema instead of a hard-coded "Elsa" (#8807).
/// </summary>
public class SecretMigrationCustomSchemaTests
{
    private const string CustomSchema = "custom_schema";

    public static TheoryData<Func<IElsaDbContextSchema, Migration>> SqliteMigrations => new()
    {
        schema => new SecretTenancy(schema),
        schema => new ManagedSecretOwnership(schema)
    };

    [Theory]
    [MemberData(nameof(SqliteMigrations))]
    public void UpOperations_UseConfiguredSchema(Func<IElsaDbContextSchema, Migration> create)
    {
        var migration = create(new TestSchema(CustomSchema));
        migration.ActiveProvider = "Microsoft.EntityFrameworkCore.Sqlite";

        var operations = migration.UpOperations;

        Assert.NotEmpty(operations);
        Assert.All(operations, operation => Assert.Equal(CustomSchema, GetSchema(operation)));
    }

    [Theory]
    [InlineData("Elsa.Secrets.Persistence.EFCore.Sqlite", "Secrets", "*_SecretTenancy.cs")]
    [InlineData("Elsa.Secrets.Persistence.EFCore.Sqlite", "Secrets", "*_ManagedSecretOwnership.cs")]
    [InlineData("Elsa.Secrets.Persistence.EFCore.SqlServer", "Secrets", "*_SecretTenancy.cs")]
    [InlineData("Elsa.Secrets.Persistence.EFCore.SqlServer", "Secrets", "*_ManagedSecretOwnership.cs")]
    [InlineData("Elsa.Secrets.Persistence.EFCore.PostgreSql", "Secrets", "*_SecretTenancy.cs")]
    [InlineData("Elsa.Secrets.Persistence.EFCore.PostgreSql", "Secrets", "*_ManagedSecretOwnership.cs")]
    [InlineData("Elsa.Secrets.Persistence.EFCore.MySql", "Secrets", "*_SecretTenancy.cs")]
    [InlineData("Elsa.Secrets.Persistence.EFCore.MySql", "Secrets", "*_ManagedSecretOwnership.cs")]
    [InlineData("Elsa.Secrets.Persistence.EFCore.Oracle", "Secrets", "*_SecretTenancy.cs")]
    [InlineData("Elsa.Secrets.Persistence.EFCore.Oracle", "Secrets", "*_ManagedSecretOwnership.cs")]
    [InlineData("Elsa.Connections.Credentials.Persistence.EFCore.PostgreSql", "Connections", "*_Initial.cs")]
    public void Migration_TakesSchemaAndHasNoHardCodedElsaSchema(string project, string folder, string pattern)
    {
        var dir = Path.Combine(FindRepoRoot(), "core", "src", "modules", project, "Migrations", folder);
        var source = File.ReadAllText(Directory.GetFiles(dir, pattern).Single(f => !f.EndsWith(".Designer.cs", StringComparison.Ordinal)));

        Assert.Contains("(IElsaDbContextSchema schema)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Elsa\"", source, StringComparison.Ordinal);
    }

    private static string? GetSchema(MigrationOperation operation) =>
        (string?)operation.GetType().GetProperty("Schema")?.GetValue(operation)
        ?? throw new InvalidOperationException($"{operation.GetType().Name} has no Schema property.");

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "core", "src", "modules")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private sealed record TestSchema(string Schema) : IElsaDbContextSchema;
}

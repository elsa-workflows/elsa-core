using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Alterations;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EFCore.UnitTests;

public class CustomSchemaMigrationTests
{
    private const string CustomSchema = "CustomElsa";

    [Theory]
    [InlineData("AlterationPlans", "IX_AlterationPlan_Status")]
    [InlineData("AlterationJobs", "IX_AlterationJob_Status")]
    public void GenerateScript_CustomSchema_RebuildsIndexesAroundAlteredColumns(string table, string index)
    {
        var migrationsAssembly = typeof(Elsa.Persistence.EFCore.SqlServer.Migrations.Alterations.V3_1).Assembly;
        var contextOptions = new ElsaDbContextOptions { SchemaName = CustomSchema };

        var script = MigrationScriptGenerator.Generate<AlterationsElsaDbContext>(
            builder => builder.UseElsaSqlServer(migrationsAssembly, "Server=unused", contextOptions),
            fromMigration: "20231015122155_Initial",
            toMigration: "20240329200626_V3_1",
            MigrationsSqlGenerationOptions.Default);

        var dropIndex = $"DROP INDEX [{index}] ON [{CustomSchema}].[{table}];";
        var alterColumn = $"ALTER TABLE [{CustomSchema}].[{table}] ALTER COLUMN [Status] nvarchar(450) NOT NULL;";
        var createIndex = $"CREATE INDEX [{index}] ON [{CustomSchema}].[{table}] ([Status]);";

        AssertStatementOrder(script, dropIndex, alterColumn, createIndex);
    }

    private static void AssertStatementOrder(string script, params string[] statements)
    {
        var previousIndex = -1;

        foreach (var statement in statements)
        {
            var currentIndex = script.IndexOf(statement, StringComparison.Ordinal);
            Assert.True(currentIndex >= 0, $"Expected migration script to contain:\n{statement}\n\nActual script:\n{script}");
            Assert.True(currentIndex > previousIndex, $"Expected statements in this order:\n{string.Join('\n', statements)}\n\nActual script:\n{script}");
            previousIndex = currentIndex;
        }
    }
}
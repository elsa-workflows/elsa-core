namespace Elsa.Persistence.EFCore.UnitTests;

/// <summary>
/// MySQL <c>V3_6</c> must create the three-column stored-trigger unique index described by its designer
/// and by the current snapshot. Four <c>varchar(255)</c> utf8mb4 columns exceed InnoDB's 3,072-byte
/// limit. The follow-up repair is idempotent so databases that already applied the four-column
/// <c>V3_6</c> are brought in line without rewriting applied history out from under them.
/// </summary>
public class MySqlStoredTriggerUniqueIndexMigrationTests
{
    private const string ThreeColumnIndexName = "IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId";
    private const string FourColumnIndexName = "IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId_TenantId";
    private const string FourColumnIndexNameTruncated = "IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId_Ten";

    [Fact]
    public void V3_6_CreatesThreeColumnUniqueIndexMatchingDesigner()
    {
        var up = ReadRuntimeMigration("20251204150235_V3_6.cs");
        var designer = ReadRuntimeMigration("20251204150235_V3_6.Designer.cs");

        AssertThreeColumnUniqueIndex(up);
        AssertThreeColumnUniqueIndex(designer);
        Assert.DoesNotContain(FourColumnIndexName, up, StringComparison.Ordinal);
        Assert.DoesNotContain(FourColumnIndexNameTruncated, up, StringComparison.Ordinal);
        Assert.Contains("IN (DATABASE()", up, StringComparison.Ordinal);
        Assert.Contains("DROP INDEX `IX_WorkflowExecutionLogRecord_ActivityNodeId`", up, StringComparison.Ordinal);
        Assert.Contains("DROP INDEX `IX_ActivityExecutionRecord_ActivityNodeId`", up, StringComparison.Ordinal);
    }

    [Fact]
    public void Snapshot_DescribesThreeColumnUniqueIndex()
    {
        var snapshot = ReadRuntimeMigration("RuntimeElsaDbContextModelSnapshot.cs");

        AssertThreeColumnUniqueIndex(snapshot);
        Assert.DoesNotContain(FourColumnIndexName, snapshot, StringComparison.Ordinal);
    }

    [Fact]
    public void FollowUp_DropsFourColumnIndexAndCreatesThreeColumnIndexIfMissing()
    {
        var migration = ReadRuntimeMigration("20261016120000_StoredTriggerUniqueIndex.cs");
        var designer = ReadRuntimeMigration("20261016120000_StoredTriggerUniqueIndex.Designer.cs");

        Assert.Contains(FourColumnIndexName, migration, StringComparison.Ordinal);
        Assert.Contains(FourColumnIndexNameTruncated, migration, StringComparison.Ordinal);
        Assert.Contains("DROP INDEX `{indexName}` ON `Triggers`", migration, StringComparison.Ordinal);
        Assert.Contains("CREATE UNIQUE INDEX `{indexName}` ON `Triggers` ({columns})", migration, StringComparison.Ordinal);
        Assert.Contains("`WorkflowDefinitionId`, `Hash`, `ActivityId`", migration, StringComparison.Ordinal);
        Assert.Contains("IF(@exist = 0", migration, StringComparison.Ordinal);
        Assert.Contains("IF(@exist > 0", migration, StringComparison.Ordinal);
        Assert.Contains("DropIndexIfPresent(migrationBuilder, FourColumnIndexName)", migration, StringComparison.Ordinal);
        Assert.Contains("DropIndexIfPresent(migrationBuilder, FourColumnIndexNameTruncated)", migration, StringComparison.Ordinal);
        Assert.Contains("CreateUniqueIndexIfMissing(", migration, StringComparison.Ordinal);
        Assert.Contains("ThreeColumnIndexName", migration, StringComparison.Ordinal);
        AssertThreeColumnUniqueIndex(designer);
        Assert.DoesNotContain(FourColumnIndexName, designer, StringComparison.Ordinal);
    }

    private static void AssertThreeColumnUniqueIndex(string source)
    {
        Assert.Contains(ThreeColumnIndexName, source, StringComparison.Ordinal);
        Assert.Contains("\"WorkflowDefinitionId\", \"Hash\", \"ActivityId\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("\"WorkflowDefinitionId\", \"Hash\", \"ActivityId\", \"TenantId\"", source, StringComparison.Ordinal);
    }

    private static string ReadRuntimeMigration(string fileName)
    {
        var repoRoot = FindRepoRoot();
        var path = Path.Combine(repoRoot, "core", "src", "modules", "Elsa.Persistence.EFCore.MySql", "Migrations", "Runtime", fileName);
        Assert.True(File.Exists(path), $"Expected migration file at {path}");
        return File.ReadAllText(path);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Elsa.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find Elsa.sln from the test output directory.");
    }
}

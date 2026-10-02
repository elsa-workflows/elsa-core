using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Persistence.Dapper.Contracts;
using Elsa.Persistence.Dapper.Dialects;
using Elsa.Persistence.Dapper.Extensions;
using Elsa.Persistence.Dapper.Models;

namespace Elsa.Persistence.Dapper.UnitTests;

/// <summary>
/// Captured from the query builder on this branch before QuoteIdentifier was wired through
/// ParameterizedQueryBuilderExtensions. SQLite and SQL Server (the only non-PG dialects
/// in this repo; MySQL/Oracle use the same unquoted SqlDialectBase path) must stay
/// byte-identical to these strings.
/// </summary>
public sealed class NonPgQuerySqlSnapshotTests
{
    [Fact(DisplayName = "SQLite query-builder SQL is byte-identical to the pre-hook capture")]
    public void Sqlite_QueryBuilderSql_IsByteIdenticalToPreHookCapture() =>
        AssertSnapshots(new SqliteDialect(), SqliteExpected);

    [Fact(DisplayName = "SQL Server query-builder SQL is byte-identical to the pre-hook capture")]
    public void SqlServer_QueryBuilderSql_IsByteIdenticalToPreHookCapture() =>
        AssertSnapshots(new SqlServerDialect(), SqlServerExpected);

    [Fact(DisplayName = "Default QuoteIdentifier is a no-op on non-PG dialects")]
    public void NonPg_QuoteIdentifier_ReturnsInputUnchanged()
    {
        ISqlDialect sqlite = new SqliteDialect();
        ISqlDialect sqlServer = new SqlServerDialect();
        Assert.Equal("CreatedAt", sqlite.QuoteIdentifier("CreatedAt"));
        Assert.Equal("CreatedAt", sqlServer.QuoteIdentifier("CreatedAt"));
        Assert.Same("CreatedAt", sqlite.QuoteIdentifier("CreatedAt"));
        Assert.Equal("1", sqlite.BooleanLiteral(true));
        Assert.Equal("0", sqlServer.BooleanLiteral(false));
    }

    private static void AssertSnapshots(ISqlDialect dialect, IReadOnlyDictionary<string, string> expected)
    {
        foreach (var (label, sql) in BuildQueries(dialect))
        {
            Assert.True(expected.ContainsKey(label), $"missing expected snapshot for {label}");
            Assert.Equal(expected[label], sql);
        }

        Assert.Equal(expected.Count, BuildQueries(dialect).Count());
    }

    internal static IEnumerable<(string Label, string Sql)> BuildQueries(ISqlDialect dialect)
    {
        yield return ("from-and", new ParameterizedQuery(dialect)
            .From("WorkflowDefinitions", "Id", "Name", "Version")
            .Is("DefinitionId", "def-1")
            .IsNot("Name", "skip")
            .In("Id", new object[] { "a", "b" })
            .IsNull("Description")
            .Sql.ToString());

        yield return ("order-asc", new ParameterizedQuery(dialect)
            .From("WorkflowInstances")
            .OrderBy("CreatedAt", OrderDirection.Ascending)
            .Sql.ToString());

        yield return ("order-desc", new ParameterizedQuery(dialect)
            .From("WorkflowInstances")
            .OrderBy("CreatedAt", OrderDirection.Descending)
            .Sql.ToString());

        yield return ("order-multi", new ParameterizedQuery(dialect)
            .From("WorkflowInstances")
            .OrderBy(
                new OrderField("Name", OrderDirection.Ascending),
                new OrderField("CreatedAt", OrderDirection.Descending))
            .Sql.ToString());

        yield return ("version-draft", new ParameterizedQuery(dialect)
            .From("WorkflowDefinitions")
            .Is(VersionOptions.Draft)
            .Sql.ToString());

        yield return ("version-latest", new ParameterizedQuery(dialect)
            .From("WorkflowDefinitions")
            .Is(VersionOptions.Latest)
            .Sql.ToString());

        yield return ("version-published", new ParameterizedQuery(dialect)
            .From("WorkflowDefinitions")
            .Is(VersionOptions.Published)
            .Sql.ToString());

        yield return ("version-latest-or-published", new ParameterizedQuery(dialect)
            .From("WorkflowDefinitions")
            .Is(VersionOptions.LatestOrPublished)
            .Sql.ToString());

        yield return ("version-latest-and-published", new ParameterizedQuery(dialect)
            .From("WorkflowDefinitions")
            .Is(VersionOptions.LatestAndPublished)
            .Sql.ToString());

        yield return ("version-specific", new ParameterizedQuery(dialect)
            .From("WorkflowDefinitions")
            .Is(VersionOptions.SpecificVersion(3))
            .Sql.ToString());

        yield return ("definition-search", new ParameterizedQuery(dialect)
            .From("WorkflowDefinitions")
            .WorkflowDefinitionSearchTerm("alpha")
            .Sql.ToString());

        yield return ("instance-search", new ParameterizedQuery(dialect)
            .From("WorkflowInstances")
            .AndWorkflowInstanceSearchTerm("bravo")
            .Sql.ToString());

        yield return ("starts-with", new ParameterizedQuery(dialect)
            .From("WorkflowInstances")
            .StartsWith("Name", true, "pre")
            .Sql.ToString());

        var inner = new ParameterizedQuery(dialect)
            .From("WorkflowInstances", "Id")
            .Is("DefinitionId", "def-1")
            .OrderBy(new OrderField("CreatedAt", OrderDirection.Descending))
            .Page(PageArgs.FromRange(0, 2));
        yield return ("paged-delete", new ParameterizedQuery(dialect)
            .Delete("WorkflowInstances", "Id", inner)
            .Sql.ToString());

        yield return ("count", new ParameterizedQuery(dialect)
            .Count("WorkflowDefinitions")
            .Sql.ToString());

        yield return ("count-distinct", new ParameterizedQuery(dialect)
            .Count("distinct DefinitionId", "WorkflowDefinitions")
            .Sql.ToString());

        yield return ("upsert", new ParameterizedQuery(dialect)
            .Upsert("WorkflowInstances", "Id", new SnapshotRecord { Id = "i1", Name = "n", Status = "Finished" })
            .Sql.ToString());
    }

    private static readonly IReadOnlyDictionary<string, string> SharedExpected = new Dictionary<string, string>
    {
        ["from-and"] = Join(
            "select Id, Name, Version from WorkflowDefinitions where 1=1",
            "and DefinitionId = @DefinitionId",
            "and not Name = @Name",
            "and Id in (@Id0, @Id1)",
            "and Description is null"),
        ["order-asc"] = Join(
            "select * from WorkflowInstances where 1=1",
            "order by CreatedAt asc"),
        ["order-desc"] = Join(
            "select * from WorkflowInstances where 1=1",
            "order by CreatedAt desc"),
        ["order-multi"] = Join(
            "select * from WorkflowInstances where 1=1",
            "order by Name asc,CreatedAt desc"),
        ["version-draft"] = Join(
            "select * from WorkflowDefinitions where 1=1",
            "and IsPublished = 0"),
        ["version-latest"] = Join(
            "select * from WorkflowDefinitions where 1=1",
            "and IsLatest = 1"),
        ["version-published"] = Join(
            "select * from WorkflowDefinitions where 1=1",
            "and IsPublished = 1"),
        ["version-latest-or-published"] = Join(
            "select * from WorkflowDefinitions where 1=1",
            "and (IsLatest = 1 or IsPublished = 1)"),
        ["version-latest-and-published"] = Join(
            "select * from WorkflowDefinitions where 1=1",
            "and IsLatest = 1 and IsPublished = 1"),
        ["version-specific"] = Join(
            "select * from WorkflowDefinitions where 1=1",
            "and Version = @Version"),
        ["definition-search"] = Join(
            "select * from WorkflowDefinitions where 1=1",
            "and (Name like @SearchTermLike or Description like @SearchTermLike or Id like @SearchTerm or DefinitionId like @SearchTerm)"),
        // ID → Id: also fixes instance search on case-sensitive SQL Server
        // collations, where the column is Id. This is the only intentional non-PG SQL change.
        ["instance-search"] = Join(
            "select * from WorkflowInstances where 1=1",
            "and (Name like @SearchTermLike or Id like @SearchTerm or DefinitionId like @SearchTerm or DefinitionVersionId like @SearchTerm or CorrelationId like @SearchTerm)"),
        ["starts-with"] = Join(
            "select * from WorkflowInstances where 1=1",
            "and Name like @SearchTermLike"),
        ["count"] = Join("select COUNT(*) from WorkflowDefinitions where 1=1"),
        ["count-distinct"] = Join("select COUNT(distinct DefinitionId) from WorkflowDefinitions where 1=1"),
    };

    private static readonly IReadOnlyDictionary<string, string> SqliteExpected = Merge(SharedExpected, new Dictionary<string, string>
    {
        ["paged-delete"] = Join(
            "delete from WorkflowInstances where 1=1",
            "and Id in (",
            "select Id from WorkflowInstances where 1=1",
            "and DefinitionId = @DefinitionId",
            "order by CreatedAt desc",
            "limit 2",
            "offset 0",
            "",
            "",
            ")"),
        ["upsert"] = Join("INSERT OR REPLACE INTO WorkflowInstances (Id, Name, Status) VALUES (@Id, @Name, @Status);"),
    });

    private static readonly IReadOnlyDictionary<string, string> SqlServerExpected = Merge(SharedExpected, new Dictionary<string, string>
    {
        ["paged-delete"] = Join(
            "delete from WorkflowInstances where 1=1",
            "and Id in (",
            "select Id from WorkflowInstances where 1=1",
            "and DefinitionId = @DefinitionId",
            "order by CreatedAt desc",
            "offset 0 rows",
            "fetch next 2 rows only",
            "",
            "",
            ")"),
        ["upsert"] = "\n                      MERGE INTO WorkflowInstances WITH (HOLDLOCK) AS Target\n                      USING (VALUES (@Id, @Name, @Status))\n                      AS Source (Id, Name, Status)\n                      ON Target.Id = Source.Id\n                      WHEN MATCHED THEN \n                      UPDATE SET Name = Source.Name, Status = Source.Status\n                      WHEN NOT MATCHED THEN\n                      INSERT (Id, Name, Status)\n                      VALUES (Source.Id, Source.Name, Source.Status);" + Environment.NewLine,
    });

    private static string Join(params string[] lines) =>
        string.Join(Environment.NewLine, lines) + Environment.NewLine;

    private static IReadOnlyDictionary<string, string> Merge(
        IReadOnlyDictionary<string, string> left,
        IReadOnlyDictionary<string, string> right)
    {
        var merged = new Dictionary<string, string>(left);
        foreach (var (key, value) in right)
            merged[key] = value;
        return merged;
    }

    public sealed class SnapshotRecord
    {
        public string Id { get; init; } = null!;
        public string Name { get; init; } = null!;
        public string Status { get; init; } = null!;
    }
}

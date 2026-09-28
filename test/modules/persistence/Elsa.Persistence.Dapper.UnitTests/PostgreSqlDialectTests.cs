using Elsa.Common.Entities;
using Elsa.Persistence.Dapper.Contracts;
using Elsa.Persistence.Dapper.Dialects;

namespace Elsa.Persistence.Dapper.UnitTests;

/// <summary>
/// SQL-shape tests for the PostgreSQL dialect. No database required.
/// </summary>
public sealed class PostgreSqlDialectTests
{
    private readonly ISqlDialect _dialect = new PostgreSqlDialect();

    [Fact(DisplayName = "From quotes the FluentMigrator mixed-case table name")]
    public void From_QuotesTableAndStar()
    {
        Assert.Equal("select * from \"ActivityExecutionRecords\" where 1=1", _dialect.From("ActivityExecutionRecords"));
    }

    [Fact(DisplayName = "From quotes each selected field")]
    public void From_QuotesFields()
    {
        Assert.Equal(
            "select \"Id\", \"Status\" from \"WorkflowInstances\" where 1=1",
            _dialect.From("WorkflowInstances", "Id", "Status"));
    }

    [Fact(DisplayName = "And / Count / Delete quote identifiers and leave parameter names unquoted")]
    public void Filters_QuoteIdentifiersOnly()
    {
        Assert.Equal("and \"Id\" = @Id", _dialect.And("Id"));
        Assert.Equal("and \"Id\" in (@Id0, @Id1)", _dialect.And("Id", ["@Id0", "@Id1"]));
        Assert.Equal("and \"CompletedAt\" is null", _dialect.IsNull("CompletedAt"));
        Assert.Equal("select COUNT(*) from \"Bookmarks\" where 1=1", _dialect.Count("Bookmarks"));
        Assert.Equal("delete from \"Bookmarks\" where 1=1", _dialect.Delete("Bookmarks"));
        Assert.Equal("order by \"CreatedAt\" desc", _dialect.OrderBy("CreatedAt", OrderDirection.Descending));
    }

    [Fact(DisplayName = "Upsert includes the primary key and quotes table, PK, and columns")]
    public void Upsert_IncludesPrimaryKeyAndQuotes()
    {
        var sql = _dialect.Upsert("ActivityExecutionRecords", "Id", ["Status", "AggregateFaultCount"]);

        Assert.Contains("insert into \"ActivityExecutionRecords\" (\"Id\", \"Status\", \"AggregateFaultCount\")", sql, StringComparison.Ordinal);
        Assert.Contains("values (@Id, @Status, @AggregateFaultCount)", sql, StringComparison.Ordinal);
        Assert.Contains("on conflict(\"Id\")", sql, StringComparison.Ordinal);
        Assert.Contains("\"Status\" = @Status", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("insert into ActivityExecutionRecords", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Quote_IsIdempotentAndEscapesEmbeddedQuotes()
    {
        Assert.Equal("*", PostgreSqlDialect.Quote("*"));
        Assert.Equal("\"Id\"", PostgreSqlDialect.Quote("Id"));
        Assert.Equal("\"Id\"", PostgreSqlDialect.Quote("\"Id\""));
        Assert.Equal("\"foo\"\"bar\"", PostgreSqlDialect.Quote("foo\"bar"));
    }
}

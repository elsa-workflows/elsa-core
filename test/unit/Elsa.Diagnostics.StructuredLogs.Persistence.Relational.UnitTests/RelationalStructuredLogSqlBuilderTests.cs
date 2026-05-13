using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Contracts;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Services;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests;

public class RelationalStructuredLogSqlBuilderTests
{
    private readonly RelationalStructuredLogSqlBuilder _builder = new(new FakeDialect());

    [Fact]
    public void BuildInsert_UsesDialectQuotingAndParameters()
    {
        var sql = _builder.BuildInsert();

        Assert.StartsWith("INSERT INTO [StructuredLogEvents]", sql, StringComparison.Ordinal);
        Assert.Contains("[TraceId]", sql, StringComparison.Ordinal);
        Assert.Contains("@TraceId", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildQuery_AddsReusableFilterPredicates()
    {
        var query = _builder.BuildQuery(new()
        {
            MinimumLevel = StructuredLogLevel.Warning,
            Levels = [StructuredLogLevel.Warning, StructuredLogLevel.Error],
            CategoryPrefix = "Elsa.Workflow",
            SourceId = "source-a",
            WorkflowDefinitionId = "definition-a",
            WorkflowInstanceId = "instance-a",
            CorrelationId = "correlation-a",
            TraceId = "trace-a",
            From = DateTimeOffset.UtcNow.AddMinutes(-5),
            To = DateTimeOffset.UtcNow,
            Take = 42
        });

        Assert.Contains("[Level] >=", query.Sql, StringComparison.Ordinal);
        Assert.Contains("[Level] IN (@Level0, @Level1)", query.Sql, StringComparison.Ordinal);
        Assert.Contains("[Category] LIKE @Category", query.Sql, StringComparison.Ordinal);
        Assert.Contains("[SourceId] = @SourceId", query.Sql, StringComparison.Ordinal);
        Assert.Contains("[WorkflowDefinitionId] = @WorkflowDefinitionId", query.Sql, StringComparison.Ordinal);
        Assert.Contains("[WorkflowInstanceId] = @WorkflowInstanceId", query.Sql, StringComparison.Ordinal);
        Assert.Contains("[CorrelationId] = @CorrelationId", query.Sql, StringComparison.Ordinal);
        Assert.Contains("[TraceId] = @TraceId", query.Sql, StringComparison.Ordinal);
        Assert.Contains("FETCH 42", query.Sql, StringComparison.Ordinal);
        Assert.Equal("Elsa.Workflow%", query.Parameters["Category"]);
    }

    private class FakeDialect : IRelationalStructuredLogDialect
    {
        public string ProviderName => "Fake";
        public string ParameterPrefix => "@";
        public string QuoteIdentifier(string identifier) => $"[{identifier}]";
        public string ApplyLimit(string sql, int limit) => $"{sql} FETCH {limit}";
    }
}

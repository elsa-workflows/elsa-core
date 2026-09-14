using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Options;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Contracts;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Services;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests;

public class RelationalStructuredLogSqlBuilderTests
{
    private readonly RelationalStructuredLogSqlBuilder _builder = CreateBuilder();

    [Test]
    public async Task BuildInsert_UsesDialectQuotingAndParameters()
    {
        var sql = _builder.BuildInsert();

        await Assert.That(sql).StartsWith("INSERT INTO [StructuredLogEvents]").WithComparison(StringComparison.Ordinal);
        await Assert.That(sql).Contains("[TraceId]").WithComparison(StringComparison.Ordinal);
        await Assert.That(sql).Contains("@TraceId").WithComparison(StringComparison.Ordinal);
    }

    [Test]
    public async Task BuildQuery_AddsReusableFilterPredicates()
    {
        var query = _builder.BuildQuery(new()
        {
            MinimumLevel = StructuredLogLevel.Warning,
            Levels = [StructuredLogLevel.Warning, StructuredLogLevel.Error],
            CategoryPrefix = "Elsa.Workflow",
            SourceId = "source-a",
            WorkflowDefinitionId = "definition-a",
            WorkflowInstanceId = "instance-a",
            TenantId = "tenant-a",
            CorrelationId = "correlation-a",
            TraceId = "trace-a",
            SpanId = "span-a",
            Text = "needle",
            From = DateTimeOffset.UtcNow.AddMinutes(-5),
            To = DateTimeOffset.UtcNow,
            Take = 42
        });

        await Assert.That(query.Sql).Contains("[Level] >=").WithComparison(StringComparison.Ordinal);
        await Assert.That(query.Sql).Contains("[Level] IN (@Level0, @Level1)").WithComparison(StringComparison.Ordinal);
        await Assert.That(query.Sql).Contains("[Category] LIKE @Category").WithComparison(StringComparison.Ordinal);
        await Assert.That(query.Sql).Contains("[SourceId] = @SourceId").WithComparison(StringComparison.Ordinal);
        await Assert.That(query.Sql).Contains("[WorkflowDefinitionId] = @WorkflowDefinitionId").WithComparison(StringComparison.Ordinal);
        await Assert.That(query.Sql).Contains("[WorkflowInstanceId] = @WorkflowInstanceId").WithComparison(StringComparison.Ordinal);
        await Assert.That(query.Sql).Contains("[TenantId] = @TenantId").WithComparison(StringComparison.Ordinal);
        await Assert.That(query.Sql).Contains("[CorrelationId] = @CorrelationId").WithComparison(StringComparison.Ordinal);
        await Assert.That(query.Sql).Contains("[TraceId] = @TraceId").WithComparison(StringComparison.Ordinal);
        await Assert.That(query.Sql).Contains("[SpanId] = @SpanId").WithComparison(StringComparison.Ordinal);
        foreach (var textColumn in new[] { "Message", "MessageTemplate", "Category", "ExceptionJson", "ScopesJson", "PropertiesJson" })
            await Assert.That(query.Sql).Contains($"[{textColumn}] LIKE @Text").WithComparison(StringComparison.Ordinal);
        await Assert.That(query.Sql).Contains("[Timestamp] >= @TimestampFrom").WithComparison(StringComparison.Ordinal);
        await Assert.That(query.Sql).Contains("[Timestamp] <= @TimestampTo").WithComparison(StringComparison.Ordinal);
        await Assert.That(query.Sql).Contains("FETCH 42").WithComparison(StringComparison.Ordinal);
        await Assert.That(query.Parameters["Category"]).IsEqualTo("Elsa.Workflow%");
        await Assert.That(query.Parameters["Text"]).IsEqualTo("%needle%");
        await Assert.That(query.Parameters.Keys).Contains("TimestampFrom");
        await Assert.That(query.Parameters.Keys).Contains("TimestampTo");
    }

    [Test]
    [Arguments(null, "FETCH 1000")]
    [Arguments(-5, "FETCH 0")]
    [Arguments(-1, "FETCH 0")]
    [Arguments(2000, "FETCH 1000")]
    [Arguments(5000, "FETCH 1000")]
    public async Task BuildQuery_ClampsTakeToMaxRecentLogQuerySize(int? take, string expectedLimit)
    {
        var query = _builder.BuildQuery(new() { Take = take });

        await Assert.That(query.Sql).Contains(expectedLimit).WithComparison(StringComparison.Ordinal);
    }

    [Test]
    [Arguments(null, "FETCH 50")]
    [Arguments(25, "FETCH 25")]
    [Arguments(200, "FETCH 50")]
    public async Task BuildQuery_UsesConfiguredMaxRecentLogQuerySize(int? take, string expectedLimit)
    {
        var builder = CreateBuilder(maxRecentLogQuerySize: 50);
        var query = builder.BuildQuery(new() { Take = take });

        await Assert.That(query.Sql).Contains(expectedLimit).WithComparison(StringComparison.Ordinal);
    }

    [Test]
    [Arguments(null)]
    [Arguments(-5)]
    [Arguments(25)]
    public async Task BuildQuery_WhenMaxRecentLogQuerySizeIsNegative_UsesZeroLimit(int? take)
    {
        var builder = CreateBuilder(maxRecentLogQuerySize: -10);
        var query = builder.BuildQuery(new() { Take = take });

        await Assert.That(query.Sql).Contains("FETCH 0").WithComparison(StringComparison.Ordinal);
    }

    [Test]
    public async Task BuildQuery_OrdersByTimestampReceivedAtSourceIdSequenceAndIdDescending()
    {
        var query = _builder.BuildQuery(new());

        await Assert.That(query.Sql).Contains("ORDER BY [Timestamp] DESC, [ReceivedAt] DESC, [SourceId] DESC, [Sequence] DESC, [Id] DESC").WithComparison(StringComparison.Ordinal);
    }

    [Test]
    public async Task BuildListSources_GroupsBySourceAndOrdersBySource()
    {
        var sql = _builder.BuildListSources();

        await Assert.That(sql).IsEqualTo("SELECT [SourceId], MAX([ReceivedAt]) AS [LastSeen] FROM [StructuredLogEvents] GROUP BY [SourceId] ORDER BY [SourceId]");
    }

    [Test]
    public async Task BuildDeleteOlderThan_UsesReceivedAtCutoffParameter()
    {
        var query = _builder.BuildDeleteOlderThan("2026-05-13T13:00:00.0000000+00:00");

        await Assert.That(query.Sql).IsEqualTo("DELETE FROM [StructuredLogEvents] WHERE [ReceivedAt] < @Cutoff");
        await Assert.That(query.Parameters["Cutoff"]).IsEqualTo("2026-05-13T13:00:00.0000000+00:00");
    }

    [Test]
    public async Task BuildQuery_AddsTextPredicateAcrossSearchableColumns()
    {
        var query = _builder.BuildQuery(new()
        {
            Text = "failure",
            TenantId = "tenant-a",
            SpanId = "span-a",
            Take = 25
        });

        const string expectedTextPredicate = "([Message] LIKE @Text OR [MessageTemplate] LIKE @Text OR [Category] LIKE @Text OR [ExceptionJson] LIKE @Text OR [ScopesJson] LIKE @Text OR [PropertiesJson] LIKE @Text)";

        await Assert.That(query.Sql).Contains(expectedTextPredicate).WithComparison(StringComparison.Ordinal);
        await Assert.That(query.Sql).Contains("[TenantId] = @TenantId").WithComparison(StringComparison.Ordinal);
        await Assert.That(query.Sql).Contains("[SpanId] = @SpanId").WithComparison(StringComparison.Ordinal);
        await Assert.That(query.Parameters["Text"]).IsEqualTo("%failure%");
    }

    [Test]
    public async Task BuildDeleteRowsBeyondMax_DelegatesOffsetSyntaxToDialect()
    {
        var query = _builder.BuildDeleteRowsBeyondMax(250);

        await Assert.That(query.Sql).Contains("SKIP 250").WithComparison(StringComparison.Ordinal);
        await Assert.That(query.Sql).DoesNotContain("LIMIT -1").WithComparison(StringComparison.Ordinal);
    }

    private static RelationalStructuredLogSqlBuilder CreateBuilder(int? maxRecentLogQuerySize = null)
    {
        var options = new StructuredLogsOptions();
        if (maxRecentLogQuerySize is { } maxTake)
            options.MaxRecentLogQuerySize = maxTake;

        return new(new FakeDialect(), MicrosoftOptions.Create(options));
    }

    private class FakeDialect : IRelationalStructuredLogDialect
    {
        public string ProviderName => "Fake";
        public string ParameterPrefix => "@";
        public string QuoteIdentifier(string identifier) => $"[{identifier}]";
        public string ApplyLimit(string sql, int limit) => $"{sql} FETCH {limit}";
        public string ApplyOffset(string sql, int offset) => $"{sql} SKIP {offset}";
    }
}

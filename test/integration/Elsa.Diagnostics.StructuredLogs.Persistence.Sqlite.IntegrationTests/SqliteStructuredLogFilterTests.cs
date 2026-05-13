using Elsa.Diagnostics.StructuredLogs.Models;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests;

public class SqliteStructuredLogFilterTests
{
    private readonly DateTimeOffset _baseTime = new(2026, 5, 13, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task QueryAsync_AppliesStructuredLogFilters()
    {
        await using var host = new SqliteStructuredLogTestHost();
        var target = StructuredLogTestEvents.Create(
            "filter-target",
            _baseTime.AddMinutes(2),
            StructuredLogLevel.Warning,
            "Elsa.Workflow.Target",
            "source-target",
            "wf-def",
            "wf-inst",
            "corr-target",
            "trace-target",
            "needle");
        await host.WriteAsync(target);

        await AssertMatchesAsync(host, new() { Levels = [StructuredLogLevel.Warning] });
        await AssertMatchesAsync(host, new() { CategoryPrefix = "Elsa.Workflow" });
        await AssertMatchesAsync(host, new() { SourceId = "source-target" });
        await AssertMatchesAsync(host, new() { WorkflowDefinitionId = "wf-def" });
        await AssertMatchesAsync(host, new() { WorkflowInstanceId = "wf-inst" });
        await AssertMatchesAsync(host, new() { CorrelationId = "corr-target" });
        await AssertMatchesAsync(host, new() { TraceId = "trace-target" });
        await AssertMatchesAsync(host, new() { From = _baseTime, To = _baseTime.AddMinutes(10) });
    }

    private static async Task AssertMatchesAsync(SqliteStructuredLogTestHost host, StructuredLogFilter filter)
    {
        var result = await host.Store.QueryAsync(filter with { Take = 1 });
        var item = Assert.Single(result.Items);
        Assert.Equal("filter-target", item.Id);
    }

    [Fact]
    public async Task QueryAsync_AppliesMinimumLevelAndLimit()
    {
        await using var host = new SqliteStructuredLogTestHost();
        await host.WriteAsync(
            StructuredLogTestEvents.Create("debug", _baseTime, StructuredLogLevel.Debug),
            StructuredLogTestEvents.Create("warning", _baseTime.AddMinutes(1), StructuredLogLevel.Warning),
            StructuredLogTestEvents.Create("error", _baseTime.AddMinutes(2), StructuredLogLevel.Error));

        var result = await host.Store.QueryAsync(new() { MinimumLevel = StructuredLogLevel.Warning, Take = 1 });

        var item = Assert.Single(result.Items);
        Assert.Equal("error", item.Id);
    }
}

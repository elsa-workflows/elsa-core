using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Options;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.ConformanceTests;

/// <summary>
/// Shared InMemory / Relational store-contract assertions for <c>IStructuredLogStore</c>.
/// Locks the Take clamp, SourceId tie-break, ListSources registry/heartbeat, DroppedEvents,
/// and portable filter contracts aligned after #8147/#8148.
/// </summary>
/// <remarks>
/// Equality filters that differ only by case are not a shared contract: InMemory uses
/// <see cref="StringComparison.OrdinalIgnoreCase"/> while Relational SQL uses <c>=</c>
/// (collation-dependent, BINARY on Sqlite). Tests use exact-case values. See README.md.
/// </remarks>
public abstract class StructuredLogStoreConformanceTests
{
    private static readonly DateTimeOffset BaseTime = new(2026, 5, 13, 10, 0, 0, TimeSpan.Zero);

    protected abstract Task<StructuredLogStoreScenario> CreateScenarioAsync(Action<StructuredLogsOptions>? configure = null);

    [Fact]
    public async Task QueryAsync_WhenTakeIsNull_UsesMaxRecentLogQuerySize()
    {
        await using var scenario = await CreateScenarioAsync(options => options.MaxRecentLogQuerySize = 2);
        await scenario.WriteAsync(
            Log("one", BaseTime, sequence: 1),
            Log("two", BaseTime.AddMinutes(1), sequence: 2),
            Log("three", BaseTime.AddMinutes(2), sequence: 3),
            Log("four", BaseTime.AddMinutes(3), sequence: 4));

        var result = await scenario.Store.QueryAsync(new());

        Assert.Equal(["three", "four"], result.Items.Select(x => x.Id).ToList());
    }

    [Fact]
    public async Task QueryAsync_WhenTakeExceedsMax_ClampsToMaxRecentLogQuerySize()
    {
        await using var scenario = await CreateScenarioAsync(options => options.MaxRecentLogQuerySize = 2);
        await scenario.WriteAsync(
            Log("one", BaseTime, sequence: 1),
            Log("two", BaseTime.AddMinutes(1), sequence: 2),
            Log("three", BaseTime.AddMinutes(2), sequence: 3));

        var result = await scenario.Store.QueryAsync(new() { Take = 100 });

        Assert.Equal(["two", "three"], result.Items.Select(x => x.Id).ToList());
    }

    [Fact]
    public async Task QueryAsync_WhenTakeIsNegative_ReturnsEmpty()
    {
        await using var scenario = await CreateScenarioAsync();
        await scenario.WriteAsync(Log("one", BaseTime));

        var result = await scenario.Store.QueryAsync(new() { Take = -5 });

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task QueryAsync_WhenMaxRecentLogQuerySizeIsNegative_ReturnsEmpty()
    {
        await using var scenario = await CreateScenarioAsync(options => options.MaxRecentLogQuerySize = -10);
        await scenario.WriteAsync(Log("one", BaseTime));

        var result = await scenario.Store.QueryAsync(new() { Take = 25 });

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task QueryAsync_WhenTimestampsTie_OrdersBySourceId()
    {
        await using var scenario = await CreateScenarioAsync();
        await scenario.WriteAsync(
            Log("zzz", BaseTime, sourceId: "pod-b", sequence: 1),
            Log("aaa", BaseTime, sourceId: "pod-a", sequence: 2));

        var result = await scenario.Store.QueryAsync(new() { Take = 10 });

        Assert.Equal(["pod-a", "pod-b"], result.Items.Select(x => x.SourceId).ToList());
    }

    [Fact]
    public async Task QueryAsync_WhenSourceIdTies_OrdersBySequence()
    {
        await using var scenario = await CreateScenarioAsync();
        await scenario.WriteAsync(
            Log("aaa", BaseTime, sourceId: "pod-a", sequence: 9),
            Log("zzz", BaseTime, sourceId: "pod-a", sequence: 2));

        var result = await scenario.Store.QueryAsync(new() { Take = 10 });

        Assert.Equal(["zzz", "aaa"], result.Items.Select(x => x.Id).ToList());
    }

    [Fact]
    public async Task QueryAsync_WhenSequenceTies_OrdersById()
    {
        await using var scenario = await CreateScenarioAsync();
        await scenario.WriteAsync(
            Log("zzz", BaseTime, sourceId: "pod-a", sequence: 1),
            Log("aaa", BaseTime, sourceId: "pod-a", sequence: 1));

        var result = await scenario.Store.QueryAsync(new() { Take = 10 });

        Assert.Equal(["aaa", "zzz"], result.Items.Select(x => x.Id).ToList());
    }

    [Fact]
    public async Task QueryAsync_AppliesExactEqualityFiltersAndTimeRangeBeforeTake()
    {
        await using var scenario = await CreateScenarioAsync();
        var target = Log(
            "filter-target",
            BaseTime.AddMinutes(2),
            StructuredLogLevel.Warning,
            category: "Elsa.Workflow.Target",
            sourceId: "source-target",
            sequence: 2,
            message: "needle in message",
            tenantId: "tenant-a",
            workflowDefinitionId: "wf-def",
            workflowInstanceId: "wf-inst",
            correlationId: "corr-target",
            traceId: "trace-target",
            spanId: "span-target");
        await scenario.WriteAsync(
            Log("distractor", BaseTime, StructuredLogLevel.Debug, sourceId: "source-other", sequence: 1),
            target,
            Log("recent", BaseTime.AddDays(1), StructuredLogLevel.Error, sequence: 3));

        await AssertMatchesAsync(scenario, new() { Levels = [StructuredLogLevel.Warning] });
        await AssertMatchesAsync(scenario, new() { MinimumLevel = StructuredLogLevel.Warning, To = BaseTime.AddHours(1) });
        await AssertMatchesAsync(scenario, new() { CategoryPrefix = "Elsa.Workflow" });
        await AssertMatchesAsync(scenario, new() { SourceId = "source-target" });
        await AssertMatchesAsync(scenario, new() { TenantId = "tenant-a" });
        await AssertMatchesAsync(scenario, new() { WorkflowDefinitionId = "wf-def" });
        await AssertMatchesAsync(scenario, new() { WorkflowInstanceId = "wf-inst" });
        await AssertMatchesAsync(scenario, new() { CorrelationId = "corr-target" });
        await AssertMatchesAsync(scenario, new() { TraceId = "trace-target" });
        await AssertMatchesAsync(scenario, new() { SpanId = "span-target" });
        await AssertMatchesAsync(scenario, new() { Text = "needle" });
        await AssertMatchesAsync(scenario, new()
        {
            From = BaseTime.AddMinutes(1),
            To = BaseTime.AddMinutes(3)
        });
    }

    [Fact]
    public async Task ListSourcesAsync_ExposesRegistryMetadataAndHeartbeat()
    {
        await using var scenario = await CreateScenarioAsync(options =>
            options.SourceHeartbeatTimeout = TimeSpan.FromSeconds(5));
        var current = scenario.SourceRegistry.Current;
        await scenario.WriteAsync(Log("local-1", DateTimeOffset.UtcNow, sourceId: current.Id, sequence: 1));
        scenario.SourceRegistry.MarkSeen("stale-pod", DateTimeOffset.UtcNow.AddMinutes(-1));
        await scenario.WriteAsync(Log("fresh-1", DateTimeOffset.UtcNow, sourceId: "fresh-pod", sequence: 2));

        var sources = await scenario.Store.ListSourcesAsync();
        var currentSource = Assert.Single(sources, x => x.Id == current.Id);
        var staleSource = Assert.Single(sources, x => x.Id == "stale-pod");
        var freshSource = Assert.Single(sources, x => x.Id == "fresh-pod");

        Assert.Equal(current.DisplayName, currentSource.DisplayName);
        Assert.Equal(current.ServiceName, currentSource.ServiceName);
        Assert.Equal(current.ProcessId, currentSource.ProcessId);
        Assert.NotEqual(0, currentSource.ProcessId);
        Assert.Equal(StructuredLogSourceStatus.Connected, currentSource.Status);
        Assert.Equal(StructuredLogSourceStatus.Stale, staleSource.Status);
        Assert.Equal(StructuredLogSourceStatus.Connected, freshSource.Status);
    }

    [Fact]
    public async Task QueryAsync_ReportsDroppedEventsPerStoreContract()
    {
        await using var scenario = await CreateScenarioAsync(options => options.RecentLogCapacity = 2);
        await scenario.WriteAsync(
            Log("one", BaseTime, sequence: 1),
            Log("two", BaseTime.AddMinutes(1), sequence: 2),
            Log("three", BaseTime.AddMinutes(2), sequence: 3),
            Log("four", BaseTime.AddMinutes(3), sequence: 4));

        var result = await scenario.Store.QueryAsync(new() { Take = 10 });

        Assert.True(result.DroppedEvents >= 0);
        if (scenario.ReportsRingDroppedEvents)
            Assert.Equal(2, result.DroppedEvents);
        else
            Assert.Equal(0, result.DroppedEvents);
    }

    [Fact]
    public async Task QueryAsync_WhenWritesFit_ReportsZeroDroppedEvents()
    {
        await using var scenario = await CreateScenarioAsync();
        await scenario.WriteAsync(
            Log("one", BaseTime, sequence: 1),
            Log("two", BaseTime.AddMinutes(1), sequence: 2));

        var result = await scenario.Store.QueryAsync(new() { Take = 10 });

        Assert.Equal(0, result.DroppedEvents);
        Assert.Equal(2, result.Items.Count);
    }

    private static async Task AssertMatchesAsync(StructuredLogStoreScenario scenario, StructuredLogFilter filter)
    {
        var result = await scenario.Store.QueryAsync(filter with { Take = 1 });
        var item = Assert.Single(result.Items);
        Assert.Equal("filter-target", item.Id);
    }

    private static StructuredLogEvent Log(
        string id,
        DateTimeOffset timestamp,
        StructuredLogLevel level = StructuredLogLevel.Information,
        string category = "Elsa.Tests",
        string sourceId = "source-a",
        long sequence = 1,
        string message = "Test event",
        string? tenantId = null,
        string? workflowDefinitionId = null,
        string? workflowInstanceId = null,
        string? correlationId = null,
        string? traceId = null,
        string? spanId = null) =>
        new()
        {
            Id = id,
            Sequence = sequence,
            Timestamp = timestamp,
            ReceivedAt = timestamp,
            Level = level,
            Category = category,
            Message = message,
            MessageTemplate = message,
            SourceId = sourceId,
            TenantId = tenantId,
            WorkflowDefinitionId = workflowDefinitionId,
            WorkflowInstanceId = workflowInstanceId,
            CorrelationId = correlationId,
            TraceId = traceId,
            SpanId = spanId
        };
}

public sealed class InMemoryStructuredLogStoreConformanceTests : StructuredLogStoreConformanceTests
{
    protected override Task<StructuredLogStoreScenario> CreateScenarioAsync(Action<StructuredLogsOptions>? configure = null) =>
        StructuredLogStoreScenario.CreateInMemoryAsync(configure);
}

public sealed class SqliteStructuredLogStoreConformanceTests : StructuredLogStoreConformanceTests
{
    protected override Task<StructuredLogStoreScenario> CreateScenarioAsync(Action<StructuredLogsOptions>? configure = null) =>
        StructuredLogStoreScenario.CreateSqliteAsync(configure);
}

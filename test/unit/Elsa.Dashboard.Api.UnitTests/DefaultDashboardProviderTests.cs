using System.Runtime.CompilerServices;
using ConsoleLogStreaming.Core;
using ConsoleLogStreaming.Core.Models;
using Elsa.Common;
using Elsa.Common.Services;
using Elsa.Dashboard.Api.Models;
using Elsa.Dashboard.Api.Services;
using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Stores;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Elsa.Dashboard.Api.UnitTests;

public class DefaultDashboardProviderTests
{
    private readonly DateTimeOffset _now = new(2026, 06, 01, 12, 00, 00, TimeSpan.Zero);
    private readonly MemoryWorkflowInstanceStore _workflowInstanceStore;
    private readonly TestRuntimeAdminService _runtimeAdminService = new();

    public DefaultDashboardProviderTests()
    {
        _workflowInstanceStore = new(new MemoryStore<WorkflowInstance>());
    }

    [Fact]
    public async Task GetOverviewAsync_ReturnsRuntimeWorkflowAndDiagnosticsMetrics()
    {
        await AddInstanceAsync("running", WorkflowStatus.Running, WorkflowSubStatus.Executing, _now.AddMinutes(-30));
        await AddInstanceAsync("completed", WorkflowStatus.Finished, WorkflowSubStatus.Finished, _now.AddHours(-2), finishedAt: _now.AddHours(-1.5));
        await AddInstanceAsync("faulted", WorkflowStatus.Finished, WorkflowSubStatus.Faulted, _now.AddHours(-4), updatedAt: _now.AddHours(-3), incidentCount: 2);
        await AddInstanceAsync("suspended", WorkflowStatus.Running, WorkflowSubStatus.Suspended, _now.AddHours(-5), updatedAt: _now.AddHours(-4));
        await AddInstanceAsync("system-completed", WorkflowStatus.Finished, WorkflowSubStatus.Finished, _now.AddHours(-3), finishedAt: _now.AddHours(-2), isSystem: true);
        var provider = CreateProvider(services =>
        {
            services.AddSingleton<IStructuredLogProvider>(new TestStructuredLogProvider(
                [new() { Id = "structured-1", DisplayName = "Structured 1", Status = StructuredLogSourceStatus.Stale }],
                [new() { Level = StructuredLogLevel.Error, SourceId = "structured-1" }],
                droppedEvents: 4));
            services.AddSingleton<IStructuredLogStorageDiagnostics>(new TestStructuredLogStorageDiagnostics(3));
            services.AddSingleton<IConsoleLogProvider>(new TestConsoleLogProvider(
                [new() { Id = "console-1", Health = ConsoleLogSourceHealth.Disconnected }],
                [new() { Text = "stderr", Stream = ConsoleStream.Stderr }]));
        });

        var overview = await provider.GetOverviewAsync(new(DashboardRangeKeys.TwentyFourHours), CancellationToken.None);

        Assert.Equal("Elsa.TestHost", overview.BackendName);
        Assert.Equal("Integration", overview.EnvironmentName);
        Assert.Equal(DashboardRuntimeStatusKeys.AcceptingWork, overview.Runtime.Status);
        Assert.Equal(2, overview.WorkflowInstances.Running);
        Assert.Equal(1, overview.WorkflowInstances.Completed);
        Assert.Equal(1, overview.WorkflowInstances.Faulted);
        Assert.Equal(1, overview.WorkflowInstances.Suspended);
        Assert.Equal(1, overview.WorkflowInstances.IncidentBearing);
        Assert.Equal(TimeSpan.FromMinutes(30), overview.WorkflowInstances.AverageDuration);
        Assert.Equal(DashboardCapabilityStatus.Available.Status, overview.Diagnostics.StructuredLogs.Capability.Status);
        Assert.Equal(1, overview.Diagnostics.StructuredLogs.SourceCount);
        Assert.Equal(1, overview.Diagnostics.StructuredLogs.StaleSourceCount);
        Assert.Equal(1, overview.Diagnostics.StructuredLogs.RecentErrorOrCriticalCount);
        Assert.Equal(3, overview.Diagnostics.StructuredLogs.DroppedWriteCount);
        Assert.Equal(4, overview.Diagnostics.StructuredLogs.DroppedEventCount);
        Assert.Equal(DashboardCapabilityStatus.Available.Status, overview.Diagnostics.ConsoleLogs.Capability.Status);
        Assert.Equal(1, overview.Diagnostics.ConsoleLogs.SourceCount);
        Assert.Equal(1, overview.Diagnostics.ConsoleLogs.StaleSourceCount);
        Assert.Equal(1, overview.Diagnostics.ConsoleLogs.RecentStderrCount);
    }

    [Fact]
    public async Task GetWorkflowTrendsAsync_BucketsWorkflowActivityByRange()
    {
        await AddInstanceAsync("created", WorkflowStatus.Running, WorkflowSubStatus.Executing, _now.AddHours(-2), updatedAt: _now.AddHours(-1.75));
        await AddInstanceAsync("finished", WorkflowStatus.Finished, WorkflowSubStatus.Finished, _now.AddHours(-2), updatedAt: _now.AddHours(-1), finishedAt: _now.AddHours(-1));
        await AddInstanceAsync("faulted", WorkflowStatus.Finished, WorkflowSubStatus.Faulted, _now.AddHours(-3), updatedAt: _now.AddHours(-1).AddMinutes(15));
        var provider = CreateProvider();

        var response = await provider.GetWorkflowTrendsAsync(new()
        {
            Range = DashboardRangeKeys.TwentyFourHours,
            Granularity = DashboardTrendGranularity.Hour
        }, CancellationToken.None);

        Assert.Equal(24, response.Buckets.Count);
        var createdBucket = response.Buckets.Single(x => x.From == _now.AddHours(-2) && x.To == _now.AddHours(-1));
        var finishedBucket = response.Buckets.Single(x => x.From == _now.AddHours(-1) && x.To == _now);
        Assert.Equal(2, createdBucket.CreatedOrStarted);
        Assert.Equal(1, finishedBucket.Finished);
        Assert.Equal(1, finishedBucket.Faulted);
    }

    [Fact]
    public async Task GetNeedsAttentionAsync_ReturnsPriorityOrderedFindings()
    {
        _runtimeAdminService.Status = new(
            new(QuiescenceReason.AdministrativePause, _now.AddMinutes(-10), null, "maintenance", "operator", "test"),
            [new("Webhook", IngressSourceState.PauseFailed, new InvalidOperationException("pause failed"), _now.AddMinutes(-5))],
            0);
        await AddInstanceAsync("faulted", WorkflowStatus.Finished, WorkflowSubStatus.Faulted, _now.AddHours(-3), updatedAt: _now.AddHours(-2), incidentCount: 1);
        var provider = CreateProvider(services =>
        {
            services.AddSingleton<IStructuredLogProvider>(new TestStructuredLogProvider([], [new() { Level = StructuredLogLevel.Critical, SourceId = "structured-1" }]));
            services.AddSingleton<IConsoleLogProvider>(new TestConsoleLogProvider([], [new() { Text = "stderr", Stream = ConsoleStream.Stderr }]));
        });

        var response = await provider.GetNeedsAttentionAsync(new(DashboardRangeKeys.TwentyFourHours), 4, CancellationToken.None);

        Assert.Equal(4, response.Findings.Count);
        Assert.Collection(response.Findings,
            finding => Assert.Equal("runtime-paused", finding.Id),
            finding => Assert.Equal("ingress-source-failures", finding.Id),
            finding => Assert.Equal("workflow-faults", finding.Id),
            finding => Assert.Equal("workflow-incidents", finding.Id));
    }

    [Fact]
    public async Task GetRecentActivityAsync_ReturnsDenseOrderedSummaries()
    {
        await AddInstanceAsync("old", WorkflowStatus.Finished, WorkflowSubStatus.Finished, _now.AddHours(-5), updatedAt: _now.AddHours(-4), finishedAt: _now.AddHours(-4));
        await AddInstanceAsync("newest", WorkflowStatus.Finished, WorkflowSubStatus.Faulted, _now.AddHours(-2), updatedAt: _now.AddMinutes(-5), incidentCount: 3, definitionId: "payments", name: "Payments");
        await AddInstanceAsync("middle", WorkflowStatus.Running, WorkflowSubStatus.Suspended, _now.AddHours(-3), updatedAt: _now.AddHours(-1));
        var provider = CreateProvider();

        var response = await provider.GetRecentActivityAsync(new(DashboardRangeKeys.TwentyFourHours), 2, CancellationToken.None);

        Assert.Collection(response.Items,
            item =>
            {
                Assert.Equal("newest", item.InstanceId);
                Assert.Equal("payments", item.DefinitionId);
                Assert.Equal("Payments", item.WorkflowName);
                Assert.Equal(nameof(WorkflowSubStatus.Faulted), item.SubStatus);
                Assert.Equal(3, item.IncidentCount);
            },
            item => Assert.Equal("middle", item.InstanceId));
    }

    [Fact]
    public async Task GetWorkflowHotspotsAsync_GroupsByWorkflowDefinitionAndMetric()
    {
        await AddInstanceAsync("payments-1", WorkflowStatus.Finished, WorkflowSubStatus.Faulted, _now.AddHours(-3), updatedAt: _now.AddHours(-2), incidentCount: 2, definitionId: "payments", name: "Payments");
        await AddInstanceAsync("payments-2", WorkflowStatus.Finished, WorkflowSubStatus.Finished, _now.AddHours(-2), updatedAt: _now.AddHours(-1), finishedAt: _now.AddMinutes(-45), incidentCount: 3, definitionId: "payments", name: "Payments");
        await AddInstanceAsync("orders-1", WorkflowStatus.Finished, WorkflowSubStatus.Faulted, _now.AddHours(-2), updatedAt: _now.AddMinutes(-30), incidentCount: 1, definitionId: "orders", name: "Orders");
        var provider = CreateProvider();

        var response = await provider.GetWorkflowHotspotsAsync(new()
        {
            Range = DashboardRangeKeys.TwentyFourHours,
            Metric = DashboardHotspotMetric.Incidents,
            Take = 2
        }, CancellationToken.None);

        Assert.Collection(response.Items,
            hotspot =>
            {
                Assert.Equal("payments", hotspot.DefinitionId);
                Assert.Equal("Payments", hotspot.WorkflowName);
                Assert.Equal(5, hotspot.Value);
            },
            hotspot =>
            {
                Assert.Equal("orders", hotspot.DefinitionId);
                Assert.Equal(1, hotspot.Value);
            });
    }

    [Fact]
    public async Task GetOverviewAsync_ReturnsDiagnosticsCapabilityStates()
    {
        var notInstalled = await CreateProvider().GetOverviewAsync(new(DashboardRangeKeys.TwentyFourHours), CancellationToken.None);
        var degraded = await CreateProvider(services =>
        {
            services.AddSingleton<IStructuredLogProvider>(new ThrowingStructuredLogProvider(new UnauthorizedAccessException()));
            services.AddSingleton<IConsoleLogProvider>(new ThrowingConsoleLogProvider(new InvalidOperationException()));
        }).GetOverviewAsync(new(DashboardRangeKeys.TwentyFourHours), CancellationToken.None);

        Assert.Equal(DashboardCapabilityStatus.NotInstalled.Status, notInstalled.Diagnostics.StructuredLogs.Capability.Status);
        Assert.Equal(DashboardCapabilityStatus.NotInstalled.Status, notInstalled.Diagnostics.ConsoleLogs.Capability.Status);
        Assert.Equal(DashboardCapabilityStatus.Unauthorized.Status, degraded.Diagnostics.StructuredLogs.Capability.Status);
        Assert.Equal(DashboardCapabilityStatus.Unavailable.Status, degraded.Diagnostics.ConsoleLogs.Capability.Status);
    }

    private async Task AddInstanceAsync(
        string id,
        WorkflowStatus status,
        WorkflowSubStatus subStatus,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt = null,
        DateTimeOffset? finishedAt = null,
        int incidentCount = 0,
        bool isSystem = false,
        string definitionId = "workflow",
        string? name = null)
    {
        await _workflowInstanceStore.SaveAsync(new()
        {
            Id = id,
            DefinitionId = definitionId,
            DefinitionVersionId = $"{definitionId}:1",
            Version = 1,
            Status = status,
            SubStatus = subStatus,
            IncidentCount = incidentCount,
            IsSystem = isSystem,
            Name = name ?? definitionId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt ?? createdAt,
            FinishedAt = finishedAt
        });
    }

    private DefaultDashboardProvider CreateProvider(Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        configureServices?.Invoke(services);
        return new(
            _workflowInstanceStore,
            _runtimeAdminService,
            new(new TestClock(_now)),
            services.BuildServiceProvider(),
            new TestHostEnvironment());
    }

    private sealed class TestClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class TestRuntimeAdminService : IWorkflowRuntimeAdminService
    {
        public RuntimeAdminStatus Status { get; set; } = new(QuiescenceState.Initial("test"), [], 0);

        public RuntimeAdminStatus GetStatus() => Status;

        public ValueTask<QuiescenceState> PauseAsync(string? reason, string? requestedBy, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask<QuiescenceState> ResumeAsync(string? requestedBy, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask<DrainOutcome> ForceDrainAsync(string? reason, string? requestedBy, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class TestStructuredLogProvider(
        IReadOnlyCollection<StructuredLogSource> sources,
        IReadOnlyCollection<StructuredLogEvent> recentItems,
        long droppedEvents = 0) : IStructuredLogProvider
    {
        public ValueTask PublishAsync(StructuredLogEvent logEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask<RecentStructuredLogsResult> GetRecentAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default) => ValueTask.FromResult(new RecentStructuredLogsResult(recentItems, droppedEvents));

        public async IAsyncEnumerable<StructuredLogEvent> SubscribeAsync(StructuredLogFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public ValueTask<IReadOnlyCollection<StructuredLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(sources);
    }

    private sealed class ThrowingStructuredLogProvider(Exception exception) : IStructuredLogProvider
    {
        public ValueTask PublishAsync(StructuredLogEvent logEvent, CancellationToken cancellationToken = default) => throw exception;

        public ValueTask<RecentStructuredLogsResult> GetRecentAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default) => throw exception;

        public IAsyncEnumerable<StructuredLogEvent> SubscribeAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default) => throw exception;

        public ValueTask<IReadOnlyCollection<StructuredLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default) => throw exception;
    }

    private sealed class TestStructuredLogStorageDiagnostics(long droppedWriteCount) : IStructuredLogStorageDiagnostics
    {
        public long DroppedWriteCount { get; } = droppedWriteCount;
    }

    private sealed class TestConsoleLogProvider(
        IReadOnlyCollection<ConsoleLogSource> sources,
        IReadOnlyList<ConsoleLogLine> recentItems) : IConsoleLogProvider
    {
        public ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask<RecentConsoleLogsResult> GetRecentAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default) => ValueTask.FromResult(new RecentConsoleLogsResult { Items = recentItems });

        public async IAsyncEnumerable<ConsoleLogStreamingItem> SubscribeAsync(ConsoleLogFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public ValueTask<IReadOnlyCollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(sources);
    }

    private sealed class ThrowingConsoleLogProvider(Exception exception) : IConsoleLogProvider
    {
        public ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default) => throw exception;

        public ValueTask<RecentConsoleLogsResult> GetRecentAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default) => throw exception;

        public IAsyncEnumerable<ConsoleLogStreamingItem> SubscribeAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default) => throw exception;

        public ValueTask<IReadOnlyCollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default) => throw exception;
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Integration";
        public string ApplicationName { get; set; } = "Elsa.TestHost";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

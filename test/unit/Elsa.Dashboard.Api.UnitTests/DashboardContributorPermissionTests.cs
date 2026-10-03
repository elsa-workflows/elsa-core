using ConsoleLogStreaming.Core;
using Elsa.Common.Models;
using Elsa.Dashboard.Abstractions.Contracts;
using Elsa.Dashboard.Abstractions.Models;
using Elsa.Diagnostics.ConsoleLogs.Dashboard;
using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Dashboard;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Dashboard;
using NSubstitute;
using static Elsa.Dashboard.Api.UnitTests.TestContributor;

namespace Elsa.Dashboard.Api.UnitTests;

/// <summary>
/// Each module guards the dashboard data it adds with the permission of that data. A contribution that declared nothing
/// would need <c>dashboard:view</c>, which hides it from the callers it is meant for.
/// </summary>
public class DashboardContributorPermissionTests
{
    private readonly DashboardContext _context = new(new(DashboardRangeKeys.TwentyFourHours, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1)), false, CancellationToken.None);

    [Fact]
    public async Task WorkflowContributor_GuardsRuntimeAndInstancesAndTheirFindings()
    {
        var store = CreateStore();
        var runtime = Substitute.For<IWorkflowRuntimeAdminService>();
        runtime.GetStatus().Returns(new RuntimeAdminStatus(
            QuiescenceState.Initial("generation") with { Reason = QuiescenceReason.AdministrativePause },
            [new("source", IngressSourceState.Running, new InvalidOperationException(), null)],
            0));

        await AssertGuardsAsync(
            new WorkflowDashboardContributor(store, runtime),
            permissions => Assert.Equal((RuntimeView, InstancesView), (permissions.Runtime!.Value, permissions.WorkflowInstances!.Value)),
            RuntimeView,
            InstancesView);
    }

    [Fact]
    public async Task WorkflowContributor_GuardsItsTrendsRecentActivityAndHotspotsWithInstancesView()
    {
        var contributor = new WorkflowDashboardContributor(CreateStore(), Substitute.For<IWorkflowRuntimeAdminService>());
        var range = _context.Range;

        var trends = await contributor.GetWorkflowTrendsAsync(new(range, DashboardTrendGranularity.Hour, false, CancellationToken.None));
        var activity = await contributor.GetRecentActivityAsync(new(range, 10, false, CancellationToken.None));
        var hotspots = await contributor.GetWorkflowHotspotsAsync(new(range, DashboardHotspotMetric.Faults, 10, false, CancellationToken.None));

        Assert.Equal(InstancesView, trends!.Permission);
        Assert.Equal(InstancesView, activity!.Permission);
        Assert.Equal(InstancesView, hotspots!.Permission);
    }

    [Fact]
    public async Task StructuredLogsContributor_GuardsItsSummaryAndFindings() =>
        await AssertGuardsAsync(
            new StructuredLogsDashboardContributor(Substitute.For<IStructuredLogProvider>(), []),
            permissions => Assert.Equal(StructuredLogsView, permissions.StructuredLogs),
            StructuredLogsView);

    [Fact]
    public async Task ConsoleLogsContributor_GuardsItsSummaryAndFindings() =>
        await AssertGuardsAsync(
            new ConsoleLogsDashboardContributor(Substitute.For<IConsoleLogProvider>()),
            permissions => Assert.Equal(ConsoleLogsView, permissions.ConsoleLogs),
            ConsoleLogsView);

    [Fact]
    public async Task BuiltInContributors_DeclareEveryPermissionTheirOverviewCardsAndPanelsUse()
    {
        var runtime = Substitute.For<IWorkflowRuntimeAdminService>();
        runtime.GetStatus().Returns(new RuntimeAdminStatus(QuiescenceState.Initial("generation"), [], 0));
        var contributors = new IDashboardContributor[]
        {
            new WorkflowDashboardContributor(CreateStore(), runtime),
            new StructuredLogsDashboardContributor(Substitute.For<IStructuredLogProvider>(), []),
            new ConsoleLogsDashboardContributor(Substitute.For<IConsoleLogProvider>())
        };

        foreach (var contributor in contributors)
        {
            var overview = await contributor.GetOverviewAsync(_context);
            var declared = contributor.OverviewPermissions!.All().ToList();
            var used = overview!.Metrics.Select(x => x.Permission).Concat(overview.Panels.Select(x => x.Permission)).ToList();

            Assert.All(used, permission => Assert.Contains(permission!.Value, declared));
        }
    }

    [Fact]
    public async Task WorkflowContributor_WithRuntimeOnlyCaller_DoesNotQueryTheInstanceStore()
    {
        var store = Substitute.For<IWorkflowInstanceStore>();
        store.CountAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>()).Returns(99L);
        store.SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<WorkflowInstanceSummary>());
        var runtime = Substitute.For<IWorkflowRuntimeAdminService>();
        runtime.GetStatus().Returns(new RuntimeAdminStatus(QuiescenceState.Initial("generation"), [], 0));
        var context = _context with { CanRead = permission => permission == RuntimeView };

        var overview = await new WorkflowDashboardContributor(store, runtime).GetOverviewAsync(context);

        await store.DidNotReceive().CountAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>());
        await store.DidNotReceive().SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>());
        Assert.Equal(DashboardRuntimeStatusKeys.AcceptingWork, overview!.Runtime!.Status);
        Assert.Null(overview.WorkflowInstances);
    }

    [Fact]
    public async Task WorkflowContributor_WithFailingInstanceStore_KeepsRuntimeAndReportsInstancesUnavailable()
    {
        var store = Substitute.For<IWorkflowInstanceStore>();
        store.CountAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>()).Returns<long>(_ => throw new InvalidOperationException("secret connection string"));
        store.SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>()).Returns<IEnumerable<WorkflowInstanceSummary>>(_ => throw new InvalidOperationException("secret connection string"));
        var runtime = Substitute.For<IWorkflowRuntimeAdminService>();
        runtime.GetStatus().Returns(new RuntimeAdminStatus(QuiescenceState.Initial("generation"), [], 0));

        var overview = await new WorkflowDashboardContributor(store, runtime).GetOverviewAsync(_context);

        Assert.Equal(DashboardRuntimeStatusKeys.AcceptingWork, overview!.Runtime!.Status);
        Assert.Equal(DashboardCapabilityStatus.Unavailable.Status, overview.WorkflowInstances!.Capability.Status);
        Assert.DoesNotContain("secret", overview.WorkflowInstances.Capability.Reason);
    }

    private static IWorkflowInstanceStore CreateStore()
    {
        var store = Substitute.For<IWorkflowInstanceStore>();
        store.CountAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>()).Returns(1L);
        store.SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<WorkflowInstanceSummary>());
        store.SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<PageArgs>(), Arg.Any<WorkflowInstanceOrder<DateTimeOffset?>>(), Arg.Any<CancellationToken>())
            .Returns(new Page<WorkflowInstanceSummary>([], 0));
        return store;
    }

    private async Task AssertGuardsAsync(IDashboardContributor contributor, Action<DashboardOverviewPermissions> assertDeclared, params DashboardPermission[] findingPermissions)
    {
        assertDeclared(contributor.OverviewPermissions!);

        var findings = await contributor.GetFindingsAsync(_context);

        Assert.NotEmpty(findings);
        Assert.All(findings, finding => Assert.Contains(finding.Permission!.Value, findingPermissions));
        Assert.All(findingPermissions, permission => Assert.Contains(findings, finding => finding.Permission == permission));
    }
}

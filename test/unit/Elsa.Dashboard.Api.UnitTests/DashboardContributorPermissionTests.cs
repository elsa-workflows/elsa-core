using ConsoleLogStreaming.Core;
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
        var store = Substitute.For<IWorkflowInstanceStore>();
        store.CountAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>()).Returns(1L);
        store.SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<WorkflowInstanceSummary>());
        var runtime = Substitute.For<IWorkflowRuntimeAdminService>();
        runtime.GetStatus().Returns(new RuntimeAdminStatus(
            QuiescenceState.Initial("generation") with { Reason = QuiescenceReason.AdministrativePause },
            [new("source", IngressSourceState.Running, new InvalidOperationException(), null)],
            0));

        await AssertGuardsAsync(
            new WorkflowDashboardContributor(store, runtime),
            overview => Assert.Equal((RuntimeView, InstancesView), (overview.Permissions.Runtime!.Value, overview.Permissions.WorkflowInstances!.Value)),
            RuntimeView,
            InstancesView);
    }

    [Fact]
    public async Task StructuredLogsContributor_GuardsItsSummaryAndFindings() =>
        await AssertGuardsAsync(
            new StructuredLogsDashboardContributor(Substitute.For<IStructuredLogProvider>(), []),
            overview => Assert.Equal(StructuredLogsView, overview.Permissions.StructuredLogs),
            StructuredLogsView);

    [Fact]
    public async Task ConsoleLogsContributor_GuardsItsSummaryAndFindings() =>
        await AssertGuardsAsync(
            new ConsoleLogsDashboardContributor(Substitute.For<IConsoleLogProvider>()),
            overview => Assert.Equal(ConsoleLogsView, overview.Permissions.ConsoleLogs),
            ConsoleLogsView);

    private async Task AssertGuardsAsync(IDashboardContributor contributor, Action<DashboardOverviewContribution> assertOverview, params DashboardPermission[] findingPermissions)
    {
        assertOverview((await contributor.GetOverviewAsync(_context))!);

        var findings = await contributor.GetFindingsAsync(_context);

        Assert.NotEmpty(findings);
        Assert.All(findings, finding => Assert.Contains(finding.Permission!.Value, findingPermissions));
        Assert.All(findingPermissions, permission => Assert.Contains(findings, finding => finding.Permission == permission));
    }
}

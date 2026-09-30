using Elsa.Authorization;
using Elsa.Dashboard.Abstractions.Contracts;
using Elsa.Dashboard.Abstractions.Models;
using Elsa.Dashboard.Api.Permissions;
using Elsa.Diagnostics.ConsoleLogs.Permissions;
using Elsa.Diagnostics.StructuredLogs.Permissions;
using Elsa.Workflows.Api.Permissions;

namespace Elsa.Dashboard.Api.UnitTests;

internal sealed class TestContributor(string id, int order) : IDashboardContributor
{
    public static readonly DashboardPermission WholeOverview = new(DashboardResourcePermissions.Dashboard, CoreVerbs.View);
    public static readonly DashboardPermission RuntimeView = new(WorkflowPermissions.Runtime, CoreVerbs.View);
    public static readonly DashboardPermission InstancesView = new(WorkflowPermissions.Instances, CoreVerbs.View);
    public static readonly DashboardPermission StructuredLogsView = new(StructuredLogsResourcePermissions.StructuredLogs, CoreVerbs.View);
    public static readonly DashboardPermission ConsoleLogsView = new(ConsoleLogsResourcePermissions.ConsoleLogs, CoreVerbs.View);

    public const string InstancesMetric = "instances";
    public const string InstancesPanel = "instances-panel";
    public const string InstancesFinding = "instances-finding";
    public const string LogsPanel = "logs-panel";

    /// <summary>A contributor supplying every section, each guarded by the permission of its data.</summary>
    public static TestContributor Declared() => new("declared", 1)
    {
        Overview = new()
        {
            Runtime = new() { Status = DashboardRuntimeStatusKeys.AcceptingWork },
            WorkflowInstances = new() { Running = 3 },
            Diagnostics = new()
            {
                StructuredLogs = new() { Capability = DashboardCapabilityStatus.Available, SourceCount = 2 },
                ConsoleLogs = new() { Capability = DashboardCapabilityStatus.Available, SourceCount = 4 }
            },
            Metrics =
            [
                new() { Id = InstancesMetric, Label = "Instances", Permission = InstancesView },
                new() { Id = "runtime", Label = "Runtime", Permission = RuntimeView },
                new() { Id = "both", Label = "Both", Permission = WholeOverview }
            ],
            Panels =
            [
                new() { Id = InstancesPanel, Title = "Instances", Permission = InstancesView },
                new() { Id = LogsPanel, Title = "Logs", Permission = StructuredLogsView },
                new() { Id = "both", Title = "Both", Permission = WholeOverview }
            ],
            Permissions = new()
            {
                Runtime = RuntimeView,
                WorkflowInstances = InstancesView,
                StructuredLogs = StructuredLogsView,
                ConsoleLogs = ConsoleLogsView
            }
        },
        Findings =
        [
            new() { Id = InstancesFinding, Message = "Instances", Permission = InstancesView },
            new() { Id = "logs-finding", Message = "Logs", Permission = StructuredLogsView }
        ],
        Trend = new() { Buckets = [new() { CreatedOrStarted = 1 }] },
        RecentActivity = new() { Items = [new() { InstanceId = "instance", DefinitionId = "definition", Status = "Finished", SubStatus = "Finished" }] },
        Hotspots = new() { Items = [new() { DefinitionId = "definition", Value = 1 }] }
    };

    public string Id { get; } = id;

    public int Order { get; } = order;

    public DashboardOverviewContribution? Overview { get; init; }

    public IReadOnlyCollection<DashboardFinding> Findings { get; init; } = [];

    public DashboardTrendResponse? Trend { get; init; }

    public DashboardRecentActivityResponse? RecentActivity { get; init; }

    public DashboardWorkflowHotspotsResponse? Hotspots { get; init; }

    public ValueTask<DashboardOverviewContribution?> GetOverviewAsync(DashboardContext context) => ValueTask.FromResult(Overview);

    public ValueTask<IReadOnlyCollection<DashboardFinding>> GetFindingsAsync(DashboardContext context) => ValueTask.FromResult(Findings);

    public ValueTask<DashboardTrendResponse?> GetWorkflowTrendsAsync(DashboardTrendContext context) => ValueTask.FromResult(Trend);

    public ValueTask<DashboardRecentActivityResponse?> GetRecentActivityAsync(DashboardListContext context) => ValueTask.FromResult(RecentActivity);

    public ValueTask<DashboardWorkflowHotspotsResponse?> GetWorkflowHotspotsAsync(DashboardHotspotsContext context) => ValueTask.FromResult(Hotspots);
}

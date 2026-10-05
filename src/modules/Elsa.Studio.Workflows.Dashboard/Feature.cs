using Elsa.Studio.Abstractions;
using Elsa.Studio.Attributes;
using Elsa.Studio.Dashboard;
using Elsa.Studio.Dashboard.Widgets;
using Elsa.Studio.Workflows.Dashboard.Widgets;

namespace Elsa.Studio.Workflows.Dashboard;

[RemoteFeature(RemoteFeatureName)]
public class Feature(IDashboardWidgetRegistry widgetRegistry) : FeatureBase
{
    public const string RemoteFeatureName = "Elsa.Workflows.Runtime.Dashboard.ShellFeatures.WorkflowRuntimeDashboard";

    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Every widget here shows workflow instance data.
        var requiredPermissions = DashboardPermissions.ForData(WorkflowPermissions.Instances);

        widgetRegistry.Add(new("dashboard.workflow.metrics", DashboardWidgetZones.Metrics, 100, typeof(DashboardWorkflowMetricsWidget), "Workflow metrics", PayloadKind: "WorkflowInstances") { RequiredPermissions = requiredPermissions });
        widgetRegistry.Add(new("dashboard.needs-attention", DashboardWidgetZones.Findings, 100, typeof(DashboardNeedsAttentionWidget), "Needs attention") { RequiredPermissions = requiredPermissions });
        widgetRegistry.Add(new("dashboard.workflow.trend", DashboardWidgetZones.Trend, 100, typeof(DashboardTrendWidget), "Workflow trends", PayloadKind: "WorkflowTrends") { RequiredPermissions = requiredPermissions });
        widgetRegistry.Add(new("dashboard.workflow.recent-activity", DashboardWidgetZones.Activity, 100, typeof(DashboardRecentActivityWidget), "Recent activity", PayloadKind: "RecentActivity") { RequiredPermissions = requiredPermissions });
        widgetRegistry.Add(new("dashboard.workflow.hotspots", DashboardWidgetZones.SecondaryPanels, 100, typeof(DashboardWorkflowHotspotsWidget), "Workflow hotspots", PayloadKind: "WorkflowHotspots") { RequiredPermissions = requiredPermissions });

        return base.InitializeAsync(cancellationToken);
    }
}

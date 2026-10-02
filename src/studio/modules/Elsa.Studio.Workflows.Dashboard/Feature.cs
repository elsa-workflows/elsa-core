using Elsa.Studio.Abstractions;
using Elsa.Studio.Attributes;
using Elsa.Studio.Dashboard.Widgets;
using Elsa.Studio.Workflows.Dashboard.Widgets;

namespace Elsa.Studio.Workflows.Dashboard;

[RemoteFeature(RemoteFeatureName)]
public class Feature(IDashboardWidgetRegistry widgetRegistry) : FeatureBase
{
    public const string RemoteFeatureName = "Elsa.Workflows.Runtime.Dashboard.ShellFeatures.WorkflowRuntimeDashboard";

    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        widgetRegistry.Add(new("dashboard.workflow.metrics", DashboardWidgetZones.Metrics, 100, typeof(DashboardWorkflowMetricsWidget), "Workflow metrics", PayloadKind: "WorkflowInstances"));
        widgetRegistry.Add(new("dashboard.needs-attention", DashboardWidgetZones.Findings, 100, typeof(DashboardNeedsAttentionWidget), "Needs attention"));
        widgetRegistry.Add(new("dashboard.workflow.trend", DashboardWidgetZones.Trend, 100, typeof(DashboardTrendWidget), "Workflow trends", PayloadKind: "WorkflowTrends"));
        widgetRegistry.Add(new("dashboard.workflow.recent-activity", DashboardWidgetZones.Activity, 100, typeof(DashboardRecentActivityWidget), "Recent activity", PayloadKind: "RecentActivity"));
        widgetRegistry.Add(new("dashboard.workflow.hotspots", DashboardWidgetZones.SecondaryPanels, 100, typeof(DashboardWorkflowHotspotsWidget), "Workflow hotspots", PayloadKind: "WorkflowHotspots"));

        return base.InitializeAsync(cancellationToken);
    }
}

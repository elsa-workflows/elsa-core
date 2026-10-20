import type { DashboardWidget } from "@/lib/modules/types";
import { Resources, Verbs } from "@/lib/permissions";
import { RecentActivityWidget } from "./activity-widget";
import { NeedsAttentionWidget } from "./findings-widget";
import { WorkflowHotspotsWidget } from "./hotspots-widget";
import { DefinitionCountWidget, InstanceMetricsWidget } from "./metrics-widget";
import { WorkflowTrendWidget } from "./trend-widget";
import { StatusBreakdownWidget } from "./status-breakdown-widget";
import { InstancesPerWorkflowWidget } from "./per-workflow-widget";

const instancesView = [{ resource: Resources.workflowInstances, verb: Verbs.view }];

/**
 * Built-in widgets, registered through the module's own `extensions.dashboardWidgets` so they are
 * collected exactly like widgets contributed by other modules.
 */
export const builtInDashboardWidgets: DashboardWidget[] = [
  { id: "dashboard.instance-metrics", zone: "metrics", order: 0, component: InstanceMetricsWidget, permissions: instancesView },
  {
    id: "dashboard.definition-count",
    zone: "metrics",
    order: 10,
    component: DefinitionCountWidget,
    permissions: [{ resource: Resources.workflowDefinitions, verb: Verbs.view }],
  },
  { id: "dashboard.workflow-trend", zone: "trend", order: 0, component: WorkflowTrendWidget, permissions: instancesView },
  { id: "dashboard.status-breakdown", zone: "primary", order: 0, component: StatusBreakdownWidget, permissions: instancesView },
  { id: "dashboard.instances-per-workflow", zone: "primary", order: 10, component: InstancesPerWorkflowWidget, permissions: instancesView },
  { id: "dashboard.recent-activity", zone: "activity", order: 0, component: RecentActivityWidget, permissions: instancesView },
  { id: "dashboard.needs-attention", zone: "findings", order: 0, component: NeedsAttentionWidget, permissions: instancesView },
  { id: "dashboard.workflow-hotspots", zone: "secondary", order: 0, component: WorkflowHotspotsWidget, permissions: instancesView },
];

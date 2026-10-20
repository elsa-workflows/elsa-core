import type { HttpClient } from "@/lib/api/http";
import type { DashboardRange } from "@/lib/modules/types";

/**
 * Endpoint group for `Elsa.Dashboard.Api` (`/dashboard/*`). Older servers do not ship it and answer
 * 404; `hooks.ts` detects that and switches the widgets to a fallback computed from the core API.
 *
 * Shapes mirror `Elsa.Dashboard.Abstractions/Models/DashboardModels.cs`. .NET `TimeSpan` values are
 * serialised as `[d.]hh:mm:ss[.fffffff]` strings; use `parseTimeSpan()` from `./format`.
 */

export type DashboardCapabilityStatusKey = "Available" | "NotInstalled" | "Unauthorized" | "Unavailable";

export interface DashboardCapabilityStatus {
  status: DashboardCapabilityStatusKey;
  reason?: string | null;
}

export type DashboardRuntimeStatusKey = "AcceptingWork" | "Paused" | "Draining" | "Unavailable";

export interface DashboardRuntimeStatus {
  capability: DashboardCapabilityStatus;
  status: DashboardRuntimeStatusKey;
  isAcceptingWork: boolean;
  activeExecutionCycleCount: number;
  ingressSourceCount: number;
  failedIngressSourceCount: number;
  pausedAt?: string | null;
  drainStartedAt?: string | null;
  reason?: string | null;
}

export interface DashboardWorkflowInstanceMetrics {
  capability: DashboardCapabilityStatus;
  running: number;
  completed: number;
  faulted: number;
  suspended: number;
  interrupted: number;
  incidentBearing: number;
  /** .NET TimeSpan string, absent when nothing finished in range. */
  averageDuration?: string | null;
}

export interface DashboardStructuredLogSummary {
  capability: DashboardCapabilityStatus;
  sourceCount: number;
  staleSourceCount: number;
  recentErrorOrCriticalCount: number;
  droppedWriteCount: number;
  droppedEventCount: number;
}

export interface DashboardConsoleLogSummary {
  capability: DashboardCapabilityStatus;
  sourceCount: number;
  staleSourceCount: number;
  recentStderrCount: number;
  droppedLineCount: number;
}

export interface DashboardDiagnosticsSummary {
  structuredLogs: DashboardStructuredLogSummary;
  consoleLogs: DashboardConsoleLogSummary;
}

export interface DashboardNavigationTarget {
  kind: string;
  target?: string | null;
  label?: string | null;
}

export interface DashboardMetricCard {
  id: string;
  label: string;
  value?: string | null;
  caption?: string | null;
  icon?: string | null;
  color?: string | null;
  navigation?: DashboardNavigationTarget | null;
  order: number;
}

export interface DashboardPanelSummary {
  id: string;
  title: string;
  summary?: string | null;
  capability: DashboardCapabilityStatus;
  navigation?: DashboardNavigationTarget | null;
  order: number;
}

export interface DashboardOverview {
  capability: DashboardCapabilityStatus;
  backendName?: string | null;
  environmentName?: string | null;
  runtime: DashboardRuntimeStatus;
  workflowInstances: DashboardWorkflowInstanceMetrics;
  diagnostics: DashboardDiagnosticsSummary;
  metrics: DashboardMetricCard[];
  panels: DashboardPanelSummary[];
  appliedRange: DashboardRange;
  from: string;
  to: string;
}

export type DashboardFindingSeverity = "Info" | "Warning" | "Error" | "Critical" | "Success";

export interface DashboardFinding {
  id: string;
  severity: DashboardFindingSeverity;
  message: string;
  targetKind?: string | null;
  target?: string | null;
  priority: number;
}

export interface DashboardNeedsAttentionResponse {
  findings: DashboardFinding[];
  capability: DashboardCapabilityStatus;
  appliedRange: DashboardRange;
}

export type DashboardTrendGranularity = "minute" | "hour" | "day";

export interface DashboardTrendRequest {
  range?: DashboardRange;
  granularity?: DashboardTrendGranularity;
  includeSystem?: boolean;
}

export interface DashboardTrendBucket {
  from: string;
  to: string;
  createdOrStarted: number;
  finished: number;
  faulted: number;
  suspended: number;
  incidentBearing: number;
}

export interface DashboardTrendResponse {
  buckets: DashboardTrendBucket[];
  appliedRange: DashboardRange;
  granularity: DashboardTrendGranularity;
  from: string;
  to: string;
}

export interface DashboardRecentActivityItem {
  instanceId: string;
  definitionId: string;
  workflowName?: string | null;
  status: string;
  subStatus: string;
  incidentCount: number;
  /** .NET TimeSpan string. */
  duration?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  finishedAt?: string | null;
}

export interface DashboardRecentActivityResponse {
  items: DashboardRecentActivityItem[];
  appliedRange: DashboardRange;
  from: string;
  to: string;
}

export type DashboardHotspotMetric = "Faults" | "Executions" | "Incidents" | "Duration";

export interface DashboardWorkflowHotspotsRequest {
  range?: DashboardRange;
  metric?: DashboardHotspotMetric;
  take?: number;
  includeSystem?: boolean;
}

export interface DashboardHotspot {
  definitionId: string;
  workflowName?: string | null;
  value: number;
  /** .NET TimeSpan string. */
  averageDuration?: string | null;
}

export interface DashboardWorkflowHotspotsResponse {
  items: DashboardHotspot[];
  appliedRange: DashboardRange;
  metric: DashboardHotspotMetric;
  from: string;
  to: string;
}

export interface DashboardQueryArgs {
  range?: DashboardRange;
  includeSystem?: boolean;
}

export function dashboardApi(http: HttpClient) {
  return {
    /** Any signed-in caller may call this; sections the caller may not read come back withheld. */
    overview: (args: DashboardQueryArgs = {}, signal?: AbortSignal) =>
      http.get<DashboardOverview>("dashboard/overview", { query: { range: args.range, includeSystem: args.includeSystem }, signal }),

    needsAttention: (args: DashboardQueryArgs & { take?: number } = {}, signal?: AbortSignal) =>
      http.get<DashboardNeedsAttentionResponse>("dashboard/needs-attention", {
        query: { range: args.range, take: args.take, includeSystem: args.includeSystem },
        signal,
      }),

    recentActivity: (args: DashboardQueryArgs & { take?: number } = {}, signal?: AbortSignal) =>
      http.get<DashboardRecentActivityResponse>("dashboard/recent-activity", {
        query: { range: args.range, take: args.take, includeSystem: args.includeSystem },
        signal,
      }),

    workflowTrends: (request: DashboardTrendRequest = {}, signal?: AbortSignal) =>
      http.post<DashboardTrendResponse>("dashboard/workflow-trends", request, { signal }),

    workflowHotspots: (request: DashboardWorkflowHotspotsRequest = {}, signal?: AbortSignal) =>
      http.post<DashboardWorkflowHotspotsResponse>("dashboard/workflow-hotspots", request, { signal }),
  };
}

export type DashboardApi = ReturnType<typeof dashboardApi>;

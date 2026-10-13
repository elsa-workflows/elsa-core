"use client";

import { keepPreviousData, useIsFetching, useQuery, useQueryClient, type UseQueryResult } from "@tanstack/react-query";
import { useCallback, useMemo } from "react";
import { useElsaClient, useQueryScope } from "@/lib/api/provider";
import { ApiError } from "@/lib/api/http";
import type { WorkflowInstanceSummary } from "@/lib/api/types/workflow-instances";
import type { DashboardRange } from "@/lib/modules/types";
import { dashboardApi, type DashboardHotspotMetric, type DashboardOverview } from "./api";

/**
 * Which data source feeds the widgets:
 * - `available`: the server ships `Elsa.Dashboard.Api` (`/dashboard/*`).
 * - `fallback`: `/dashboard/*` answers 404 (older server); counts and recent instances are computed
 *   from the core workflow-instances / workflow-definitions endpoints instead.
 */
export type DashboardCapability = "available" | "fallback";

export const REFETCH_INTERVAL_MS = 30_000;

const probeRange: DashboardRange = "24h";

function isNotFound(error: unknown) {
  return error instanceof ApiError && (error.status === 404 || error.status === 405);
}

function useDashboardApi() {
  const client = useElsaClient();
  return useMemo(() => dashboardApi(client.http), [client]);
}

/** Root of every dashboard query key; `useRefreshDashboard()` invalidates this prefix. */
export function useDashboardKey() {
  const scope = useQueryScope();
  return useMemo(() => [scope, "dashboard"] as const, [scope]);
}

/**
 * Probes `/dashboard/overview` once per server. A 404 marks the server as lacking the Dashboard API;
 * a successful probe also seeds the 24h overview cache so the first render costs one request.
 */
export function useDashboardCapability() {
  const api = useDashboardApi();
  const key = useDashboardKey();
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: [...key, "capability"],
    queryFn: async ({ signal }): Promise<DashboardCapability> => {
      try {
        const overview = await api.overview({ range: probeRange }, signal);
        queryClient.setQueryData([...key, "overview", probeRange], overview);
        return "available";
      } catch (e) {
        if (isNotFound(e)) return "fallback";
        throw e;
      }
    },
    staleTime: Infinity,
    gcTime: Infinity,
    retry: false,
  });
  return { capability: query.data, isLoading: query.isPending, error: query.error, isError: query.isError };
}

export interface DashboardQueryResult<T> {
  capability: DashboardCapability | undefined;
  data: T | undefined;
  error: unknown;
  isLoading: boolean;
  isFetching: boolean;
  dataUpdatedAt: number;
}

function toResult<T>(capability: DashboardCapability | undefined, probeError: unknown, query: UseQueryResult<T>): DashboardQueryResult<T> {
  return {
    capability,
    data: query.data,
    error: probeError ?? query.error,
    isLoading: capability === undefined ? !probeError : query.isPending && query.fetchStatus !== "idle",
    isFetching: query.isFetching,
    dataUpdatedAt: query.dataUpdatedAt,
  };
}

export function useDashboardOverview(range: DashboardRange): DashboardQueryResult<DashboardOverview> {
  const api = useDashboardApi();
  const key = useDashboardKey();
  const { capability, error } = useDashboardCapability();
  const query = useQuery({
    queryKey: [...key, "overview", range],
    queryFn: ({ signal }) => api.overview({ range }, signal),
    enabled: capability === "available",
    refetchInterval: REFETCH_INTERVAL_MS,
    placeholderData: keepPreviousData,
  });
  return toResult(capability, error, query);
}

export function useWorkflowTrends(range: DashboardRange) {
  const api = useDashboardApi();
  const key = useDashboardKey();
  const { capability, error } = useDashboardCapability();
  const query = useQuery({
    queryKey: [...key, "trends", range],
    queryFn: ({ signal }) => api.workflowTrends({ range }, signal),
    enabled: capability === "available",
    refetchInterval: REFETCH_INTERVAL_MS,
    placeholderData: keepPreviousData,
  });
  return toResult(capability, error, query);
}

export function useNeedsAttention(range: DashboardRange, take = 8) {
  const api = useDashboardApi();
  const key = useDashboardKey();
  const { capability, error } = useDashboardCapability();
  const query = useQuery({
    queryKey: [...key, "needs-attention", range, take],
    queryFn: ({ signal }) => api.needsAttention({ range, take }, signal),
    enabled: capability === "available",
    refetchInterval: REFETCH_INTERVAL_MS,
    placeholderData: keepPreviousData,
  });
  return toResult(capability, error, query);
}

export function useRecentActivity(range: DashboardRange, take = 10) {
  const api = useDashboardApi();
  const key = useDashboardKey();
  const { capability, error } = useDashboardCapability();
  const query = useQuery({
    queryKey: [...key, "recent-activity", range, take],
    queryFn: ({ signal }) => api.recentActivity({ range, take }, signal),
    enabled: capability === "available",
    refetchInterval: REFETCH_INTERVAL_MS,
    placeholderData: keepPreviousData,
  });
  return toResult(capability, error, query);
}

export function useWorkflowHotspots(range: DashboardRange, metric: DashboardHotspotMetric = "Faults", take = 8) {
  const api = useDashboardApi();
  const key = useDashboardKey();
  const { capability, error } = useDashboardCapability();
  const query = useQuery({
    queryKey: [...key, "hotspots", range, metric, take],
    queryFn: ({ signal }) => api.workflowHotspots({ range, metric, take }, signal),
    enabled: capability === "available",
    refetchInterval: REFETCH_INTERVAL_MS,
    placeholderData: keepPreviousData,
  });
  return toResult(capability, error, query);
}

/* ------------------------------------------------------------------------------------------------
 * Fallback source (servers without Elsa.Dashboard.Api)
 * ---------------------------------------------------------------------------------------------- */

/** Instance counts computed from `totalCount` of `pageSize: 1` list calls; not range-scoped. */
export interface FallbackInstanceCounts {
  running: number;
  suspended: number;
  faulted: number;
  completed: number;
  incidentBearing: number;
}

export function useFallbackInstanceCounts(): DashboardQueryResult<FallbackInstanceCounts> {
  const client = useElsaClient();
  const key = useDashboardKey();
  const { capability, error } = useDashboardCapability();
  const query = useQuery({
    queryKey: [...key, "fallback", "instance-counts"],
    queryFn: async ({ signal }): Promise<FallbackInstanceCounts> => {
      const count = async (filter: Parameters<typeof client.workflowInstances.list>[0]) =>
        (await client.workflowInstances.list({ ...filter, pageSize: 1 }, signal)).totalCount;
      const [running, suspended, faulted, completed, incidentBearing] = await Promise.all([
        count({ status: "Running" }),
        count({ subStatus: "Suspended" }),
        count({ subStatus: "Faulted" }),
        count({ subStatus: "Finished" }),
        count({ hasIncidents: true }),
      ]);
      return { running, suspended, faulted, completed, incidentBearing };
    },
    enabled: capability === "fallback",
    refetchInterval: REFETCH_INTERVAL_MS,
    placeholderData: keepPreviousData,
  });
  return toResult(capability, error, query);
}

export interface WorkflowInstanceCount {
  definitionId: string;
  name: string;
  total: number;
  running: number;
  faulted: number;
}

/** Instances per published workflow, computed from the core API (one count per definition, capped). */
export function useInstancesPerWorkflow(take = 8): DashboardQueryResult<WorkflowInstanceCount[]> {
  const client = useElsaClient();
  const key = useDashboardKey();
  const { capability, error } = useDashboardCapability();
  const query = useQuery({
    queryKey: [...key, "instances-per-workflow", take],
    queryFn: async ({ signal }): Promise<WorkflowInstanceCount[]> => {
      const definitions = await client.workflowDefinitions.list({ versionOptions: "Published", pageSize: 50, orderBy: "Name" }, signal);
      const rows = await Promise.all(
        definitions.items.map(async (d) => {
          const count = async (filter: Parameters<typeof client.workflowInstances.list>[0]) =>
            (await client.workflowInstances.list({ ...filter, definitionId: d.definitionId, pageSize: 1 }, signal)).totalCount;
          const [total, running, faulted] = await Promise.all([count({}), count({ status: "Running" }), count({ subStatus: "Faulted" })]);
          return { definitionId: d.definitionId, name: d.name ?? d.definitionId, total, running, faulted };
        }),
      );
      return rows.sort((a, b) => b.total - a.total).slice(0, take);
    },
    enabled: capability !== undefined,
    refetchInterval: REFETCH_INTERVAL_MS,
    placeholderData: keepPreviousData,
  });
  return toResult(capability, error, query);
}

/** Newest instances by creation date; stands in for `/dashboard/recent-activity`. */
export function useFallbackRecentInstances(take = 10): DashboardQueryResult<WorkflowInstanceSummary[]> {
  const client = useElsaClient();
  const key = useDashboardKey();
  const { capability, error } = useDashboardCapability();
  const query = useQuery({
    queryKey: [...key, "fallback", "recent-instances", take],
    queryFn: async ({ signal }) =>
      (await client.workflowInstances.list({ pageSize: take, orderBy: "Created", orderDirection: "Descending" }, signal)).items,
    enabled: capability === "fallback",
    refetchInterval: REFETCH_INTERVAL_MS,
    placeholderData: keepPreviousData,
  });
  return toResult(capability, error, query);
}

/** Number of workflow definitions (latest versions). Works on every server, so it never depends on the capability. */
export function useWorkflowDefinitionCount() {
  const client = useElsaClient();
  const key = useDashboardKey();
  return useQuery({
    queryKey: [...key, "definition-count"],
    // `/workflow-definitions/query/count` ignores its filters (it counts every version), so the
    // published, non-system total comes from the list endpoint instead.
    queryFn: async ({ signal }) => (await client.workflowDefinitions.list({ versionOptions: "Published", isSystem: false, pageSize: 1 }, signal)).totalCount,
    refetchInterval: REFETCH_INTERVAL_MS,
    placeholderData: keepPreviousData,
  });
}

/** Re-probes the capability and refetches every dashboard query for the active server. */
export function useRefreshDashboard() {
  const key = useDashboardKey();
  const queryClient = useQueryClient();
  const refresh = useCallback(() => queryClient.invalidateQueries({ queryKey: key }), [queryClient, key]);
  const isFetching = useIsFetching({ queryKey: key }) > 0;
  return { refresh, isFetching };
}

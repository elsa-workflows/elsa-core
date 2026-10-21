"use client";

import { keepPreviousData, useInfiniteQuery, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useMemo, useRef } from "react";
import { useStudioConfig } from "@/config/provider";
import { ApiError } from "@/lib/api/http";
import { useElsaClient, useQueryScope } from "@/lib/api/provider";
import type { JournalFilter } from "@/lib/api/endpoints/workflow-instances";
import type { WorkflowDefinitionSummary } from "@/lib/api/types/workflow-definitions";
import type {
  ActivityExecutionRecord,
  ActivityExecutionStats,
  ListWorkflowInstancesArgs,
  WorkflowExecutionLogRecord,
} from "@/lib/api/types/workflow-instances";
import { statsFromRecords } from "./model";

const JOURNAL_PAGE_SIZE = 100;

/** Query keys for this module. Everything hangs off `[scope, "workflow-instances"]` so one invalidation covers all. */
export function instanceKeys(scope: string) {
  const root = [scope, "workflow-instances"] as const;
  return {
    all: root,
    list: (args: ListWorkflowInstancesArgs) => [...root, "list", args] as const,
    instance: (id: string) => [...root, id] as const,
    detail: (id: string) => [...root, id, "detail"] as const,
    executionState: (id: string) => [...root, id, "execution-state"] as const,
    journal: (id: string, filter?: JournalFilter) => [...root, id, "journal", filter ?? null] as const,
    executions: (id: string, activityNodeId?: string) => [...root, id, "activity-executions", activityNodeId ?? null] as const,
    report: (id: string, activityNodeIds: string[]) => [...root, id, "report", activityNodeIds] as const,
    variables: (id: string) => [...root, id, "variables"] as const,
    definitionOptions: [scope, "workflow-definitions", "options"] as const,
    definition: (versionId: string) => [scope, "workflow-definitions", "by-id", versionId] as const,
  };
}

export function useInstanceKeys() {
  const scope = useQueryScope();
  return useMemo(() => instanceKeys(scope), [scope]);
}

/** Endpoints that may be missing on older servers answer 404/405; callers then fall back. */
function isUnsupported(error: unknown) {
  return error instanceof ApiError && (error.status === 404 || error.status === 405);
}

export function useWorkflowInstances(args: ListWorkflowInstancesArgs, options: { poll?: boolean } = {}) {
  const client = useElsaClient();
  const keys = useInstanceKeys();
  const config = useStudioConfig();
  return useQuery({
    queryKey: keys.list(args),
    queryFn: ({ signal }) => client.workflowInstances.list(args, signal),
    placeholderData: keepPreviousData,
    refetchInterval: options.poll ? config.ui.pollIntervalMs : false,
  });
}

/**
 * Loads one instance and keeps it fresh while it runs: the lightweight execution-state endpoint is
 * polled and the full instance (plus journal, executions, stats and variables) is re-fetched only
 * when something changed. Servers without that endpoint poll the instance itself instead.
 */
export function useWorkflowInstance(id: string, options: { poll?: boolean } = {}) {
  const poll = options.poll ?? true;
  const client = useElsaClient();
  const keys = useInstanceKeys();
  const queryClient = useQueryClient();
  const interval = useStudioConfig().ui.pollIntervalMs;

  const instance = useQuery({
    queryKey: keys.detail(id),
    queryFn: ({ signal }) => client.workflowInstances.get(id, signal),
    enabled: !!id,
    refetchInterval: (query) => {
      const running = query.state.data?.status === "Running";
      const stateUnsupported = queryClient.getQueryData(keys.executionState(id)) === null;
      return poll && running && stateUnsupported ? interval : false;
    },
  });

  const running = instance.data?.status === "Running";

  const executionState = useQuery({
    queryKey: keys.executionState(id),
    queryFn: async ({ signal }) => {
      try {
        return await client.workflowInstances.executionState(id, signal);
      } catch (error) {
        if (isUnsupported(error)) return null;
        throw error;
      }
    },
    enabled: !!id && poll && running,
    // Stop asking once the server answered 404/405; the instance query polls itself in that case.
    refetchInterval: (query) => (query.state.data === null ? false : interval),
    staleTime: 0,
    retry: false,
  });

  // Execution state moved on: fetch the full instance again (once per distinct state).
  const lastState = useRef<string | undefined>(undefined);
  const stateData = executionState.data;
  const instanceUpdatedAt = instance.data?.updatedAt;
  const instanceStatus = instance.data?.status;
  useEffect(() => {
    if (!stateData) return;
    const signature = `${stateData.status}|${stateData.subStatus}|${stateData.updatedAt}`;
    if (lastState.current === signature) return;
    lastState.current = signature;
    if (stateData.updatedAt !== instanceUpdatedAt || stateData.status !== instanceStatus) {
      void queryClient.invalidateQueries({ queryKey: keys.detail(id) });
    }
  }, [stateData, instanceUpdatedAt, instanceStatus, queryClient, keys, id]);

  // The instance changed: refresh its dependent resources (journal, executions, stats, variables).
  const lastSeen = useRef<string | undefined>(undefined);
  useEffect(() => {
    if (!instanceUpdatedAt) return;
    const signature = `${instanceStatus}|${instanceUpdatedAt}`;
    if (lastSeen.current !== undefined && lastSeen.current !== signature) {
      void queryClient.invalidateQueries({
        predicate: (q) => {
          const key = q.queryKey;
          return key[0] === keys.all[0] && key[1] === keys.all[1] && key[2] === id && key[3] !== "detail" && key[3] !== "execution-state";
        },
      });
    }
    lastSeen.current = signature;
  }, [instanceStatus, instanceUpdatedAt, queryClient, keys, id]);

  return { ...instance, isPolling: poll && running };
}

/** Paged journal; records are flattened in sequence order. */
export function useInstanceJournal(id: string, filter?: JournalFilter) {
  const client = useElsaClient();
  const keys = useInstanceKeys();
  const query = useInfiniteQuery({
    queryKey: keys.journal(id, filter),
    queryFn: ({ pageParam, signal }) => client.workflowInstances.journal(id, { page: pageParam, pageSize: JOURNAL_PAGE_SIZE, filter }, signal),
    initialPageParam: 0,
    getNextPageParam: (last, all) => {
      const loaded = all.reduce((n, p) => n + p.items.length, 0);
      return loaded < last.totalCount && last.items.length > 0 ? all.length : undefined;
    },
    enabled: !!id,
  });
  const records = useMemo<WorkflowExecutionLogRecord[]>(
    () => (query.data?.pages ?? []).flatMap((p) => p.items).sort((a, b) => a.sequence - b.sequence),
    [query.data],
  );
  const totalCount = query.data?.pages[0]?.totalCount ?? 0;
  return { ...query, records, totalCount };
}

export function useActivityExecutions(id: string, activityNodeId?: string) {
  const client = useElsaClient();
  const keys = useInstanceKeys();
  const query = useQuery({
    queryKey: keys.executions(id, activityNodeId),
    queryFn: ({ signal }) => client.workflowInstances.activityExecutions(id, activityNodeId, signal),
    enabled: !!id,
  });
  const records = useMemo<ActivityExecutionRecord[]>(
    () => [...(query.data?.items ?? [])].sort((a, b) => a.startedAt.localeCompare(b.startedAt)),
    [query.data],
  );
  return { ...query, records };
}

/**
 * Execution statistics per activity node, from the report endpoint when available and otherwise
 * derived from the activity execution records.
 */
export function useExecutionStats(id: string, activityNodeIds: string[], fallbackRecords?: ActivityExecutionRecord[]) {
  const client = useElsaClient();
  const keys = useInstanceKeys();
  const report = useQuery({
    queryKey: keys.report(id, activityNodeIds),
    queryFn: async ({ signal }) => {
      try {
        const response = await client.workflowInstances.executionReport(id, activityNodeIds, signal);
        return response.stats;
      } catch (error) {
        if (isUnsupported(error)) return null;
        throw error;
      }
    },
    enabled: !!id && activityNodeIds.length > 0,
    retry: false,
  });

  const stats = useMemo(() => {
    const map = new Map<string, ActivityExecutionStats>();
    if (report.data) {
      for (const s of report.data) {
        map.set(s.activityNodeId, {
          activityNodeId: s.activityNodeId,
          startedCount: s.startedCount,
          completedCount: s.completedCount,
          uncompletedCount: s.uncompletedCount,
          isBlocked: s.isBlocked,
          isFaulted: s.isFaulted,
        });
      }
      return map;
    }
    if (fallbackRecords) for (const s of statsFromRecords(fallbackRecords)) map.set(s.activityNodeId, s);
    return map;
  }, [report.data, fallbackRecords]);

  return { ...report, stats, usesFallback: report.data === null };
}

export function useInstanceVariables(id: string) {
  const client = useElsaClient();
  const keys = useInstanceKeys();
  return useQuery({
    queryKey: keys.variables(id),
    queryFn: ({ signal }) => client.workflowInstances.variables(id, signal),
    enabled: !!id,
  });
}

/** Latest-or-published definitions used to resolve names and populate the workflow filter. */
export function useWorkflowDefinitionOptions() {
  const client = useElsaClient();
  const keys = useInstanceKeys();
  const query = useQuery({
    queryKey: keys.definitionOptions,
    queryFn: ({ signal }) => client.workflowDefinitions.list({ versionOptions: "LatestOrPublished", pageSize: 100 }, signal),
    staleTime: 60_000,
  });
  const byDefinitionId = useMemo(() => {
    const map = new Map<string, WorkflowDefinitionSummary>();
    for (const d of query.data?.items ?? []) if (!map.has(d.definitionId)) map.set(d.definitionId, d);
    return map;
  }, [query.data]);
  const options = useMemo(
    () =>
      [...byDefinitionId.values()]
        .map((d) => ({ value: d.definitionId, label: d.name?.trim() || d.definitionId }))
        .sort((a, b) => a.label.localeCompare(b.label)),
    [byDefinitionId],
  );
  return { ...query, options, byDefinitionId, nameOf: (definitionId: string) => byDefinitionId.get(definitionId)?.name?.trim() || definitionId };
}

/** A specific definition version (the one an instance was created from). Versions are immutable. */
export function useWorkflowDefinitionVersion(versionId: string | undefined) {
  const client = useElsaClient();
  const keys = useInstanceKeys();
  return useQuery({
    queryKey: keys.definition(versionId ?? ""),
    queryFn: ({ signal }) => client.workflowDefinitions.getById(versionId!, signal),
    enabled: !!versionId,
    staleTime: Infinity,
  });
}

function useInvalidateInstances() {
  const queryClient = useQueryClient();
  const keys = useInstanceKeys();
  return () => queryClient.invalidateQueries({ queryKey: keys.all });
}

export function useDeleteWorkflowInstance() {
  const client = useElsaClient();
  const invalidate = useInvalidateInstances();
  return useMutation({ mutationFn: (id: string) => client.workflowInstances.delete(id), onSuccess: invalidate });
}

export function useBulkDeleteWorkflowInstances() {
  const client = useElsaClient();
  const invalidate = useInvalidateInstances();
  return useMutation({ mutationFn: (ids: string[]) => client.workflowInstances.bulkDelete(ids), onSuccess: invalidate });
}

export function useCancelWorkflowInstance() {
  const client = useElsaClient();
  const invalidate = useInvalidateInstances();
  return useMutation({ mutationFn: (id: string) => client.workflowInstances.cancel(id), onSuccess: invalidate });
}

export function useBulkCancelWorkflowInstances() {
  const client = useElsaClient();
  const invalidate = useInvalidateInstances();
  return useMutation({ mutationFn: (ids: string[]) => client.workflowInstances.bulkCancel({ ids }), onSuccess: invalidate });
}

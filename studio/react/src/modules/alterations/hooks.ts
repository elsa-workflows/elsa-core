"use client";

import { useMutation, useQueries, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo } from "react";
import { useElsaClient, useQueryScope } from "@/lib/api/provider";
import { ApiError } from "@/lib/api/http";
import { useStudioConfig } from "@/config/provider";
import {
  alterationsApi,
  type AlterationPlanParams,
  type AlterationPlanStatus,
  type GetAlterationPlanResponse,
  type RetryWorkflowsRequest,
  type RunAlterationsRequest,
} from "./api";
import { useRecentPlans } from "./recent-plans";

export function useAlterationsApi() {
  const client = useElsaClient();
  return useMemo(() => alterationsApi(client.http), [client]);
}

const TERMINAL: AlterationPlanStatus[] = ["Completed", "Failed"];

export function isPlanTerminal(status: AlterationPlanStatus | undefined) {
  return !!status && TERMINAL.includes(status);
}

export function planKey(scope: string, id: string) {
  return [scope, "alterations", "plan", id] as const;
}

/** Loads one plan with its jobs; polls while the plan is still running. */
export function useAlterationPlan(id: string | undefined) {
  const api = useAlterationsApi();
  const scope = useQueryScope();
  const { ui } = useStudioConfig();
  return useQuery({
    queryKey: planKey(scope, id ?? ""),
    queryFn: ({ signal }) => api.getPlan(id!, signal),
    enabled: !!id,
    refetchInterval: (query) => (isPlanTerminal(query.state.data?.plan.status) ? false : ui.pollIntervalMs),
    retry: (count, err) => count < 2 && !(err instanceof ApiError),
  });
}

/** Fetches the status of every remembered plan (recent plans list). */
export function useRecentPlanStatuses() {
  const api = useAlterationsApi();
  const scope = useQueryScope();
  const { ui } = useStudioConfig();
  const { plans, remember, forget, clear } = useRecentPlans(scope);
  const queries = useQueries({
    queries: plans.map((p) => ({
      queryKey: planKey(scope, p.id),
      queryFn: ({ signal }: { signal: AbortSignal }) => api.getPlan(p.id, signal),
      refetchInterval: (query: { state: { data?: GetAlterationPlanResponse } }) =>
        isPlanTerminal(query.state.data?.plan.status) ? false : ui.pollIntervalMs,
      retry: false,
    })),
  });
  const rows = plans.map((ref, i) => ({ ref, query: queries[i] }));
  return { rows, remember, forget, clear };
}

export function useSubmitAlterationPlan() {
  const api = useAlterationsApi();
  const scope = useQueryScope();
  const { remember } = useRecentPlans(scope);
  return useMutation({
    mutationFn: (params: AlterationPlanParams) => api.submit(params),
    onSuccess: (res, params) => {
      remember({
        id: res.planId,
        submittedAt: new Date().toISOString(),
        instanceCount: params.filter.workflowInstanceIds?.length,
        alterationCount: params.alterations.length,
      });
    },
  });
}

export function useRunAlterations() {
  const api = useAlterationsApi();
  const queryClient = useQueryClient();
  const scope = useQueryScope();
  return useMutation({
    mutationFn: (request: RunAlterationsRequest) => api.run(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [scope, "workflow-instances"] }),
  });
}

export function useRetryWorkflows() {
  const api = useAlterationsApi();
  const queryClient = useQueryClient();
  const scope = useQueryScope();
  return useMutation({
    mutationFn: (request: RetryWorkflowsRequest) => api.retryWorkflows(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [scope, "workflow-instances"] }),
  });
}

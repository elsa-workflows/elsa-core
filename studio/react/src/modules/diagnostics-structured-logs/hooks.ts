"use client";

import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useMemo } from "react";
import { useElsaClient, useQueryScope } from "@/lib/api/provider";
import { ApiError } from "@/lib/api/http";
import { useAuth } from "@/lib/auth/provider";
import { useStudioConfig } from "@/config/provider";
import { structuredLogsApi, type StructuredLogFilter } from "./api";

export function useStructuredLogsApi() {
  const client = useElsaClient();
  return useMemo(() => structuredLogsApi(client.http), [client]);
}

function key(scope: string) {
  return [scope, "diagnostics", "structured-logs"] as const;
}

export function useStructuredLogSources() {
  const api = useStructuredLogsApi();
  const scope = useQueryScope();
  const { ui } = useStudioConfig();
  return useQuery({
    queryKey: [...key(scope), "sources"],
    queryFn: ({ signal }) => api.sources(signal),
    refetchInterval: ui.pollIntervalMs * 6,
  });
}

/** Recent entries for a filter; polls every `pollIntervalMs` while `follow` is on. */
export function useRecentStructuredLogs(filter: StructuredLogFilter, follow: boolean) {
  const api = useStructuredLogsApi();
  const scope = useQueryScope();
  const { ui } = useStudioConfig();
  return useQuery({
    queryKey: [...key(scope), "recent", filter],
    queryFn: ({ signal }) => api.recent(filter, signal),
    placeholderData: keepPreviousData,
    refetchInterval: follow ? ui.pollIntervalMs : false,
  });
}

/** Storage diagnostics; `null` when the endpoint is missing (404) so the dashboard widget can hide. */
export function useStructuredLogStorage() {
  const api = useStructuredLogsApi();
  const scope = useQueryScope();
  const { status } = useAuth();
  const { ui } = useStudioConfig();
  return useQuery({
    queryKey: [...key(scope), "storage"],
    queryFn: async ({ signal }) => {
      try {
        return await api.storage(signal);
      } catch (e) {
        if (e instanceof ApiError && (e.status === 404 || e.status === 405)) return null;
        throw e;
      }
    },
    enabled: status === "authenticated",
    refetchInterval: ui.pollIntervalMs * 6,
    retry: false,
  });
}

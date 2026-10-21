"use client";

import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useCallback, useMemo, useState } from "react";
import { useElsaClient, useQueryScope } from "@/lib/api/provider";
import { ApiError } from "@/lib/api/http";
import { useAuth } from "@/lib/auth/provider";
import { useStudioConfig } from "@/config/provider";
import { consoleLogsApi, type ConsoleLogFilter } from "./api";

export function useConsoleLogsApi() {
  const client = useElsaClient();
  return useMemo(() => consoleLogsApi(client.http), [client]);
}

function key(scope: string) {
  return [scope, "diagnostics", "console-logs"] as const;
}

export function useConsoleLogSources() {
  const api = useConsoleLogsApi();
  const scope = useQueryScope();
  const { ui } = useStudioConfig();
  return useQuery({
    queryKey: [...key(scope), "sources"],
    queryFn: ({ signal }) => api.sources(signal),
    refetchInterval: ui.pollIntervalMs * 6,
  });
}

/** Recent lines for a filter; polls every `pollIntervalMs` while `follow` is on. */
export function useRecentConsoleLogs(filter: ConsoleLogFilter, follow: boolean) {
  const api = useConsoleLogsApi();
  const scope = useQueryScope();
  const { ui } = useStudioConfig();
  return useQuery({
    queryKey: [...key(scope), "recent", filter],
    queryFn: ({ signal }) => api.recent(filter, signal),
    placeholderData: keepPreviousData,
    refetchInterval: follow ? ui.pollIntervalMs : false,
  });
}

/** Storage diagnostics; `null` data means the server has no such endpoint (404) and the widget hides. */
export function useConsoleLogStorage() {
  const api = useConsoleLogsApi();
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

/** Per-viewer display preference kept in localStorage (wrap / compact); safe when storage is unavailable. */
export function useViewPreference(name: string, fallback: boolean): [boolean, (v: boolean) => void] {
  const storageKey = `elsa-studio:console-logs:${name}`;
  const [value, setValue] = useState<boolean>(() => {
    try {
      const raw = typeof window !== "undefined" ? window.localStorage.getItem(storageKey) : null;
      return raw === null ? fallback : raw === "1";
    } catch {
      return fallback;
    }
  });
  const set = useCallback(
    (v: boolean) => {
      setValue(v);
      try {
        window.localStorage.setItem(storageKey, v ? "1" : "0");
      } catch {
        // ignore: the preference only lives for this session
      }
    },
    [storageKey],
  );
  return [value, set];
}

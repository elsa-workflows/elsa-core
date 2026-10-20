"use client";

import { useQuery } from "@tanstack/react-query";
import { useMemo } from "react";
import { ApiError } from "@/lib/api/http";
import { useElsaClient, useQueryScope } from "@/lib/api/provider";
import { environmentsApi, type EnvironmentsResponse } from "./api";

export const environmentKeys = {
  list: (scope: string) => [scope, "environments"] as const,
};

export function useEnvironmentsApi() {
  const client = useElsaClient();
  return useMemo(() => environmentsApi(client.http), [client]);
}

const EMPTY: EnvironmentsResponse = { environments: [], defaultEnvironmentName: null };

/**
 * Environments the active server advertises. A 404 (feature not installed) resolves to an empty
 * list rather than an error so the picker degrades to the configured servers alone.
 */
export function useServerEnvironments() {
  const api = useEnvironmentsApi();
  const scope = useQueryScope();
  return useQuery({
    queryKey: environmentKeys.list(scope),
    queryFn: async ({ signal }) => {
      try {
        return await api.list(signal);
      } catch (err) {
        if (err instanceof ApiError && err.status === 404) return EMPTY;
        throw err;
      }
    },
    retry: false,
    staleTime: 5 * 60_000,
  });
}

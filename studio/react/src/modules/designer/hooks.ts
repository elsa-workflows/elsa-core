"use client";

import { useQuery } from "@tanstack/react-query";
import { useMemo } from "react";
import { useElsaClient, useQueryScope } from "@/lib/api/provider";
import { ActivityRegistry } from "./model/registry";

/** Loads activity descriptors once per backend and exposes them as a registry. */
export function useActivityRegistry() {
  const client = useElsaClient();
  const scope = useQueryScope();
  const query = useQuery({
    queryKey: [scope, "descriptors", "activities"],
    queryFn: ({ signal }) => client.descriptors.activities(false, signal),
    staleTime: 5 * 60_000,
  });
  const registry = useMemo(() => new ActivityRegistry(query.data?.items ?? []), [query.data]);
  return { registry, isLoading: query.isLoading, error: query.error, refetch: query.refetch };
}

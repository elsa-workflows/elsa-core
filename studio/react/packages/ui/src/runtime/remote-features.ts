"use client";

import { useQuery } from "@tanstack/react-query";
import { useMemo } from "react";
import { useElsaClient, useQueryScope } from "@elsa-workflows/react-studio-api-client/react";
import { useAuth } from "@elsa-workflows/react-studio-auth";
import { ApiError } from "@elsa-workflows/react-studio-api-client/http";
import type { FeatureDescriptor } from "@elsa-workflows/react-studio-api-client/types/descriptors";

/**
 * The server's installed feature catalog (`GET /features/installed`). Modules can declare that they
 * need a server feature; such a module is hidden when the backend doesn't have it, even if the
 * configuration flag enables it. Matches either the full shell-feature name or the short `Elsa.<Name>`.
 */
export class RemoteFeatureCatalog {
  static readonly Empty = new RemoteFeatureCatalog([]);
  private readonly names: Set<string>;

  constructor(features: FeatureDescriptor[]) {
    this.names = new Set<string>();
    for (const f of features) {
      this.names.add(f.fullName);
      this.names.add(`${f.namespace}.${f.name}`);
      this.names.add(f.name);
    }
  }

  has(name: string) {
    if (this.names.has(name)) return true;
    const short = name.split(".").pop();
    return !!short && this.names.has(short);
  }
}

export function useRemoteFeatures() {
  const client = useElsaClient();
  const scope = useQueryScope();
  const { status } = useAuth();
  const query = useQuery({
    queryKey: [scope, "features", "installed"],
    queryFn: ({ signal }) => client.descriptors.installedFeatures(AbortSignal.any([signal, AbortSignal.timeout(15_000)])),
    enabled: status === "authenticated",
    staleTime: 5 * 60_000,
    retry: false,
  });
  const catalog = useMemo(() => new RemoteFeatureCatalog(query.data?.items ?? []), [query.data]);
  return {
    catalog,
    isLoading: query.isLoading,
    isError: query.isError,
    isResolved: query.isSuccess || query.isError,
    legacyWithoutCatalog: query.error instanceof ApiError && query.error.status === 404,
  };
}

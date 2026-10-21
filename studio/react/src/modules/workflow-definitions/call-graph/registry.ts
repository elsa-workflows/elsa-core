"use client";

import { useMemo, useSyncExternalStore } from "react";
import { useStudioConfig } from "@/config/provider";
import { useElsaClient, useQueryScope } from "@/lib/api/provider";
import { CallGraphService } from "./call-graph-service";
import { resolveCallGraphConfig, type CallGraphConfig } from "./config";

// One graph per server for the lifetime of the page: toggling and drilling between workflows reuse it.
const services = new Map<string, CallGraphService>();

export function getCallGraphService(scope: string, api: ConstructorParameters<typeof CallGraphService>[0], config: CallGraphConfig): CallGraphService {
  let service = services.get(scope);
  if (!service) {
    service = new CallGraphService(api, config);
    services.set(scope, service);
  }
  return service;
}

/** The cached call-graph service for the active server. Limits come from `features["workflow-definitions"].options.callContext`. */
export function useCallGraphService(): CallGraphService {
  const client = useElsaClient();
  const scope = useQueryScope();
  const studio = useStudioConfig();
  const config = useMemo(() => resolveCallGraphConfig(studio.features["workflow-definitions"]?.options?.callContext), [studio.features]);
  return useMemo(() => getCallGraphService(scope, client.workflowDefinitions, config), [scope, client, config]);
}

/** Re-renders the caller whenever the service's graph changes. Returns the change counter. */
export function useCallGraphVersion(service: CallGraphService): number {
  return useSyncExternalStore(
    (listener) => service.subscribe(listener),
    () => service.version,
    () => service.version,
  );
}

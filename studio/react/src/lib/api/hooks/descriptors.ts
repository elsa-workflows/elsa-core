"use client";

import { useQuery } from "@tanstack/react-query";
import { useElsaClient, useQueryScope } from "@/lib/api/provider";

const LONG = 5 * 60_000;

function useDescriptor<T>(key: string, fetcher: (signal?: AbortSignal) => Promise<{ items: T[] }>) {
  const scope = useQueryScope();
  const query = useQuery({ queryKey: [scope, "descriptors", key], queryFn: ({ signal }) => fetcher(signal), staleTime: LONG });
  return { items: query.data?.items ?? [], isLoading: query.isLoading, error: query.error };
}

export function useExpressionDescriptors() {
  const client = useElsaClient();
  return useDescriptor("expressions", client.descriptors.expressions);
}

export function useVariableTypes() {
  const client = useElsaClient();
  return useDescriptor("variables", client.descriptors.variables);
}

export function useStorageDrivers() {
  const client = useElsaClient();
  return useDescriptor("storage-drivers", client.descriptors.storageDrivers);
}

export function useActivationStrategies() {
  const client = useElsaClient();
  return useDescriptor("activation-strategies", client.descriptors.activationStrategies);
}

export function useIncidentStrategies() {
  const client = useElsaClient();
  return useDescriptor("incident-strategies", client.descriptors.incidentStrategies);
}

export function useLogPersistenceStrategies() {
  const client = useElsaClient();
  return useDescriptor("log-persistence-strategies", client.descriptors.logPersistenceStrategies);
}

export function useWorkflowCommitStrategies() {
  const client = useElsaClient();
  return useDescriptor("commit-strategies-workflows", client.descriptors.workflowCommitStrategies);
}

export function useActivityCommitStrategies() {
  const client = useElsaClient();
  return useDescriptor("commit-strategies-activities", client.descriptors.activityCommitStrategies);
}

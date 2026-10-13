"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo } from "react";
import { useElsaClient, useQueryScope } from "@/lib/api/provider";
import { ALL_LABELS_PAGE_SIZE, labelsApi, type LabelInput } from "./api";

export const labelKeys = {
  all: (scope: string) => [scope, "labels"] as const,
  list: (scope: string, page: number, pageSize: number) => [scope, "labels", "list", page, pageSize] as const,
  every: (scope: string) => [scope, "labels", "list", "all"] as const,
  definition: (scope: string, id: string) => [scope, "labels", "definition", id] as const,
};

export function useLabelsApi() {
  const client = useElsaClient();
  return useMemo(() => labelsApi(client.http), [client]);
}

export function useLabels(page: number, pageSize: number) {
  const api = useLabelsApi();
  const scope = useQueryScope();
  return useQuery({
    queryKey: labelKeys.list(scope, page, pageSize),
    queryFn: ({ signal }) => api.list({ page, pageSize }, signal),
    placeholderData: (prev) => prev,
  });
}

/** Every label, for pickers. */
export function useAllLabels(enabled = true) {
  const api = useLabelsApi();
  const scope = useQueryScope();
  return useQuery({
    queryKey: labelKeys.every(scope),
    queryFn: async ({ signal }) => (await api.list({ page: 0, pageSize: ALL_LABELS_PAGE_SIZE }, signal)).items,
    enabled,
  });
}

function useInvalidateLabels() {
  const queryClient = useQueryClient();
  const scope = useQueryScope();
  return () => queryClient.invalidateQueries({ queryKey: labelKeys.all(scope) });
}

export function useCreateLabel() {
  const api = useLabelsApi();
  const invalidate = useInvalidateLabels();
  return useMutation({ mutationFn: (body: LabelInput) => api.create(body), onSuccess: () => invalidate() });
}

export function useUpdateLabel() {
  const api = useLabelsApi();
  const invalidate = useInvalidateLabels();
  return useMutation({ mutationFn: ({ id, ...body }: LabelInput & { id: string }) => api.update(id, body), onSuccess: () => invalidate() });
}

export function useDeleteLabel() {
  const api = useLabelsApi();
  const invalidate = useInvalidateLabels();
  return useMutation({ mutationFn: (id: string) => api.delete(id), onSuccess: () => invalidate() });
}

/** Label ids applied to a workflow definition version. */
export function useDefinitionLabels(definitionVersionId: string | undefined) {
  const api = useLabelsApi();
  const scope = useQueryScope();
  return useQuery({
    queryKey: labelKeys.definition(scope, definitionVersionId ?? ""),
    queryFn: async ({ signal }) => (await api.definitionLabels(definitionVersionId!, signal)).items,
    enabled: !!definitionVersionId,
  });
}

export function useSetDefinitionLabels(definitionVersionId: string | undefined) {
  const api = useLabelsApi();
  const queryClient = useQueryClient();
  const scope = useQueryScope();
  return useMutation({
    mutationFn: (labelIds: string[]) => api.setDefinitionLabels(definitionVersionId!, labelIds),
    onSuccess: (result) => {
      queryClient.setQueryData(labelKeys.definition(scope, definitionVersionId ?? ""), result.labelIds);
    },
  });
}

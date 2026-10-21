"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useElsaClient, useQueryScope } from "@/lib/api/provider";
import type { ListWorkflowDefinitionsArgs, SaveWorkflowDefinitionRequest, WorkflowDefinition } from "@/lib/api/types/workflow-definitions";

export const definitionKeys = {
  all: (scope: string) => [scope, "workflow-definitions"] as const,
  list: (scope: string, args: ListWorkflowDefinitionsArgs) => [scope, "workflow-definitions", "list", args] as const,
  one: (scope: string, definitionId: string, versionOptions: string) => [scope, "workflow-definitions", "one", definitionId, versionOptions] as const,
  versions: (scope: string, definitionId: string) => [scope, "workflow-definitions", "versions", definitionId] as const,
};

export function useWorkflowDefinitions(args: ListWorkflowDefinitionsArgs, options: { enabled?: boolean } = {}) {
  const client = useElsaClient();
  const scope = useQueryScope();
  return useQuery({
    queryKey: definitionKeys.list(scope, args),
    queryFn: ({ signal }) => client.workflowDefinitions.list(args, signal),
    placeholderData: (prev) => prev,
    enabled: options.enabled ?? true,
  });
}

export function useWorkflowDefinition(definitionId: string | undefined, versionOptions = "Latest") {
  const client = useElsaClient();
  const scope = useQueryScope();
  return useQuery({
    queryKey: definitionKeys.one(scope, definitionId ?? "", versionOptions),
    queryFn: ({ signal }) => client.workflowDefinitions.getByDefinitionId(definitionId!, versionOptions, signal),
    enabled: !!definitionId,
  });
}

export function useWorkflowDefinitionVersions(definitionId: string | undefined) {
  const client = useElsaClient();
  const scope = useQueryScope();
  return useQuery({
    queryKey: definitionKeys.versions(scope, definitionId ?? ""),
    queryFn: ({ signal }) => client.workflowDefinitions.versions(definitionId!, signal),
    enabled: !!definitionId,
    select: (items) => [...items].sort((a, b) => b.version - a.version),
  });
}

/** All mutations invalidate the definition caches so lists and editors refresh. */
export function useWorkflowDefinitionMutations() {
  const client = useElsaClient();
  const scope = useQueryScope();
  const queryClient = useQueryClient();
  const invalidate = () => queryClient.invalidateQueries({ queryKey: definitionKeys.all(scope) });
  const api = client.workflowDefinitions;

  return {
    save: useMutation({ mutationFn: (request: SaveWorkflowDefinitionRequest) => api.save(request), onSuccess: invalidate }),
    publish: useMutation({ mutationFn: (definitionId: string) => api.publish(definitionId), onSuccess: invalidate }),
    retract: useMutation({ mutationFn: (definitionId: string) => api.retract(definitionId), onSuccess: invalidate }),
    remove: useMutation({ mutationFn: (definitionId: string) => api.delete(definitionId), onSuccess: invalidate }),
    removeVersion: useMutation({ mutationFn: (versionId: string) => api.deleteVersion(versionId), onSuccess: invalidate }),
    revert: useMutation({ mutationFn: (p: { definitionId: string; version: number }) => api.revert(p.definitionId, p.version), onSuccess: invalidate }),
    bulkDelete: useMutation({ mutationFn: (ids: string[]) => api.bulkDelete(ids), onSuccess: invalidate }),
    bulkDeleteVersions: useMutation({ mutationFn: (ids: string[]) => api.bulkDeleteVersions(ids), onSuccess: invalidate }),
    bulkPublish: useMutation({ mutationFn: (ids: string[]) => api.bulkPublish(ids), onSuccess: invalidate }),
    bulkRetract: useMutation({ mutationFn: (ids: string[]) => api.bulkRetract(ids), onSuccess: invalidate }),
    importFiles: useMutation({ mutationFn: (files: File[]) => api.importFiles(files), onSuccess: invalidate }),
    importModel: useMutation({ mutationFn: (model: WorkflowDefinition) => api.import(toModel(model)), onSuccess: invalidate }),
    execute: useMutation({ mutationFn: (p: { definitionId: string; input?: Record<string, unknown>; correlationId?: string }) => api.execute(p.definitionId, { input: p.input, correlationId: p.correlationId }) }),
    dispatch: useMutation({ mutationFn: (p: { definitionId: string; input?: Record<string, unknown>; correlationId?: string }) => api.dispatch(p.definitionId, { input: p.input, correlationId: p.correlationId }) }),
    invalidate,
  };
}

/** Strips server-only fields so a fetched definition can be posted back as a model. */
export function toModel(definition: WorkflowDefinition): SaveWorkflowDefinitionRequest["model"] {
  const { links: _links, createdAt: _createdAt, isLatest: _isLatest, isPublished: _isPublished, id: _id, ...rest } = definition as WorkflowDefinition & { links?: unknown };
  void _links;
  void _createdAt;
  void _isLatest;
  void _isPublished;
  void _id;
  return {
    ...rest,
    definitionId: definition.definitionId,
    variables: definition.variables ?? [],
    inputs: definition.inputs ?? [],
    outputs: definition.outputs ?? [],
    outcomes: definition.outcomes ?? [],
    customProperties: definition.customProperties ?? {},
    options: definition.options ?? {},
    root: definition.root,
  };
}

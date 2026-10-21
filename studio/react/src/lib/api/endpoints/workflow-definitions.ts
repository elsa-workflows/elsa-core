import type { HttpClient } from "../http";
import type { PagedList } from "../types/common";
import type {
  BulkDeleteResponse,
  BulkPublishResponse,
  BulkRetractResponse,
  DispatchWorkflowDefinitionRequest,
  DispatchWorkflowDefinitionResponse,
  ExecuteWorkflowDefinitionRequest,
  ListWorkflowDefinitionsArgs,
  SaveWorkflowDefinitionRequest,
  SaveWorkflowDefinitionResponse,
  WorkflowDefinition,
  WorkflowDefinitionModel,
  WorkflowDefinitionSummary,
} from "../types/workflow-definitions";
import type { WorkflowState } from "../types/workflow-instances";

export function workflowDefinitionsApi(http: HttpClient) {
  return {
    list: (args: ListWorkflowDefinitionsArgs = {}, signal?: AbortSignal) =>
      http.get<PagedList<WorkflowDefinitionSummary>>("workflow-definitions", {
        query: {
          page: args.page,
          pageSize: args.pageSize,
          versionOptions: args.versionOptions,
          definitionIds: args.definitionIds,
          materializer: args.materializerName,
          searchTerm: args.searchTerm,
          isSystem: args.isSystem,
          orderBy: args.orderBy,
          orderDirection: args.orderDirection,
          label: args.labels,
        },
        signal,
      }),

    count: (args: Pick<ListWorkflowDefinitionsArgs, "versionOptions" | "isSystem"> = {}) =>
      http.get<{ count: number }>("workflow-definitions/query/count", { query: args }),

    getByDefinitionId: (definitionId: string, versionOptions = "Latest", signal?: AbortSignal) =>
      http.get<WorkflowDefinition>(`workflow-definitions/by-definition-id/${encodeURIComponent(definitionId)}`, {
        query: { versionOptions },
        signal,
      }),

    getById: (id: string, signal?: AbortSignal) =>
      http.get<WorkflowDefinition>(`workflow-definitions/by-id/${encodeURIComponent(id)}`, { signal }),

    getMany: (ids: string[], signal?: AbortSignal) =>
      http.get<{ items: WorkflowDefinition[] }>("workflow-definitions/many-by-id", { query: { ids }, signal }),

    versions: (definitionId: string, signal?: AbortSignal) =>
      http.get<WorkflowDefinition[]>(`workflow-definitions/${encodeURIComponent(definitionId)}/versions`, { signal }),

    isNameUnique: (name: string, definitionId?: string) =>
      http.get<{ isUnique: boolean }>("workflow-definitions/validation/is-name-unique", { query: { name, definitionId } }),

    save: (request: SaveWorkflowDefinitionRequest) =>
      http.post<SaveWorkflowDefinitionResponse>("workflow-definitions", request),

    publish: (definitionId: string) =>
      http.post<SaveWorkflowDefinitionResponse>(`workflow-definitions/${encodeURIComponent(definitionId)}/publish`),

    retract: (definitionId: string) =>
      http.post<SaveWorkflowDefinitionResponse>(`workflow-definitions/${encodeURIComponent(definitionId)}/retract`),

    delete: (definitionId: string) =>
      http.delete<void>(`workflow-definitions/${encodeURIComponent(definitionId)}`, { parse: "none" }),

    deleteVersion: (versionId: string) =>
      http.delete<void>(`workflow-definition-versions/${encodeURIComponent(versionId)}`, { parse: "none" }),

    revert: (definitionId: string, version: number) =>
      http.post<WorkflowDefinition>(`workflow-definitions/${encodeURIComponent(definitionId)}/revert/${version}`),

    bulkDelete: (definitionIds: string[]) =>
      http.post<BulkDeleteResponse>("bulk-actions/delete/workflow-definitions/by-definition-id", { definitionIds }),

    bulkDeleteVersions: (ids: string[]) =>
      http.post<BulkDeleteResponse>("bulk-actions/delete/workflow-definitions/by-id", { ids }),

    bulkPublish: (definitionIds: string[]) =>
      http.post<BulkPublishResponse>("bulk-actions/publish/workflow-definitions/by-definition-ids", { definitionIds }),

    bulkRetract: (definitionIds: string[]) =>
      http.post<BulkRetractResponse>("bulk-actions/retract/workflow-definitions/by-definition-ids", { definitionIds }),

    export: (definitionId: string, versionOptions = "Latest") =>
      http.get<Blob>(`workflow-definitions/${encodeURIComponent(definitionId)}/export`, {
        query: { versionOptions },
        parse: "blob",
      }),

    bulkExport: (ids: string[]) =>
      http.post<Blob>("bulk-actions/export/workflow-definitions", { ids }, { parse: "blob" }),

    import: (model: WorkflowDefinitionModel) =>
      http.post<SaveWorkflowDefinitionResponse>("workflow-definitions/import", model),

    importFiles: (files: File[]) => {
      const form = new FormData();
      files.forEach((f) => form.append("files", f, f.name));
      return http.post<{ count?: number }>("workflow-definitions/import-files", form);
    },

    execute: (definitionId: string, request: ExecuteWorkflowDefinitionRequest = {}) =>
      http.post<{ workflowState: WorkflowState }>(`workflow-definitions/${encodeURIComponent(definitionId)}/execute`, request),

    dispatch: (definitionId: string, request: DispatchWorkflowDefinitionRequest = {}) =>
      http.post<DispatchWorkflowDefinitionResponse>(`workflow-definitions/${encodeURIComponent(definitionId)}/dispatch`, request),

    updateReferences: (definitionId: string) =>
      http.post<{ affectedWorkflows: string[] }>(`workflow-definitions/${encodeURIComponent(definitionId)}/update-references`),
  };
}

export type WorkflowDefinitionsApi = ReturnType<typeof workflowDefinitionsApi>;

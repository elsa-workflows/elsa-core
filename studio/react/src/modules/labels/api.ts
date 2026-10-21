import type { HttpClient } from "@/lib/api/http";

/** A label as returned by `GET /labels` (the full entity). */
export interface Label {
  id: string;
  name: string;
  normalizedName: string;
  description?: string;
  /** CSS colour, usually `#rrggbb`. */
  color?: string;
  tenantId?: string;
}

/** Create/update/get responses carry the normalised name only. */
export interface LabelResponse {
  id: string;
  normalizedName: string;
  description?: string;
  color?: string;
}

export interface LabelInput {
  name: string;
  description?: string;
  color?: string;
}

export interface LabelsPage {
  items: Label[];
  totalCount: number;
}

/** Endpoints of the Elsa.Labels module (routes relative to the API base). */
export function labelsApi(http: HttpClient) {
  return {
    /** `page` is 0-based. */
    list: (query?: { page?: number; pageSize?: number }, signal?: AbortSignal) => http.get<LabelsPage>("labels", { query, signal }),
    get: (id: string, signal?: AbortSignal) => http.get<LabelResponse>(`labels/${encodeURIComponent(id)}`, { signal }),
    create: (body: LabelInput) => http.post<LabelResponse>("labels", body),
    /** The server updates with POST on the item route. */
    update: (id: string, body: LabelInput) => http.post<LabelResponse>(`labels/${encodeURIComponent(id)}`, body),
    delete: (id: string) => http.delete<void>(`labels/${encodeURIComponent(id)}`, { parse: "none" }),

    /** Ids of the labels applied to a workflow definition *version* (`WorkflowDefinition.id`). */
    definitionLabels: (definitionVersionId: string, signal?: AbortSignal) =>
      http.get<{ items: string[] }>(`workflow-definitions/${encodeURIComponent(definitionVersionId)}/labels`, { signal }),
    setDefinitionLabels: (definitionVersionId: string, labelIds: string[]) =>
      http.post<{ labelIds: string[] }>(`workflow-definitions/${encodeURIComponent(definitionVersionId)}/labels`, { labelIds }),
  };
}

export type LabelsApi = ReturnType<typeof labelsApi>;

/** Page size used when the UI needs every label (pickers, tabs). */
export const ALL_LABELS_PAGE_SIZE = 500;

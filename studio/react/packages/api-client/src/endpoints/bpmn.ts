import type { HttpClient } from "../http.js";
import type { BpmnImportAnalysis, BpmnImportOptions, BpmnImportResult } from "../types/bpmn.js";

function upload(file: File, options: BpmnImportOptions = {}) {
  const form = new FormData();
  form.append("file", file, file.name);
  for (const [key, value] of Object.entries(options)) if (value !== undefined) form.append(key[0].toUpperCase() + key.slice(1), value);
  return form;
}

/** Server-side interchange; importing never interprets executable XML in the browser. */
export function bpmnApi(http: HttpClient) {
  return {
    analyze: (file: File, signal?: AbortSignal) => http.post<BpmnImportAnalysis>("bpmn/analyze", upload(file), { signal }),
    import: (file: File, options: BpmnImportOptions = {}, signal?: AbortSignal) => http.post<BpmnImportResult>("bpmn/import", upload(file, options), { signal }),
    export: (definitionId: string, signal?: AbortSignal) => http.get<Blob>(`bpmn/definitions/${encodeURIComponent(definitionId)}/export`, { parse: "blob", signal }),
  };
}

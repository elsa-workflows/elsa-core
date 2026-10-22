/** Findings returned by the server's loss-aware BPMN reader. */
export interface BpmnImportIssue {
  severity: "Info" | "Degraded" | "Dropped" | (string & {});
  message: string;
  elementId?: string;
  processId?: string;
}

export interface BpmnImportAnalysis {
  processIds: string[];
  elementCounts: Record<string, number>;
  issues: BpmnImportIssue[];
}

export interface BpmnImportResult {
  id: string;
  definitionId: string;
  version: number;
  analysis: BpmnImportAnalysis;
}

export interface BpmnImportOptions {
  definitionId?: string;
  name?: string;
  processId?: string;
}

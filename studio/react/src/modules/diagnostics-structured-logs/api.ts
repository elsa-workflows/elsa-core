import type { HttpClient } from "@/lib/api/http";

/** Contract derived from `Elsa.Diagnostics.StructuredLogs/Models/*` and `specs/004-diagnostics-structured-logs/contracts/rest-api.md`. */

export type StructuredLogLevel = "Trace" | "Debug" | "Information" | "Warning" | "Error" | "Critical" | "None";
export const StructuredLogLevels: StructuredLogLevel[] = ["Trace", "Debug", "Information", "Warning", "Error", "Critical"];

export type StructuredLogSourceStatus = "Unknown" | "Connected" | "Stale" | "Disconnected";

export interface StructuredLogException {
  type: string;
  message: string;
  stackTrace?: string | null;
}

export interface StructuredLogEvent {
  id: string;
  sequence: number;
  timestamp: string;
  receivedAt?: string | null;
  /** Serialized as the enum name or its numeric value. Use `levelName()`. */
  level: StructuredLogLevel | number;
  category: string;
  eventId?: number | null;
  eventName?: string | null;
  message: string;
  messageTemplate?: string | null;
  exception?: StructuredLogException | null;
  scopes?: Record<string, string | null> | null;
  properties?: Record<string, string | null> | null;
  traceId?: string | null;
  spanId?: string | null;
  correlationId?: string | null;
  tenantId?: string | null;
  workflowDefinitionId?: string | null;
  workflowInstanceId?: string | null;
  sourceId: string;
}

export interface StructuredLogSource {
  id: string;
  displayName?: string | null;
  /** Older spec revisions name this `name`. */
  name?: string | null;
  serviceName?: string | null;
  machineName?: string | null;
  processId?: number | null;
  processName?: string | null;
  podName?: string | null;
  namespace?: string | null;
  containerName?: string | null;
  nodeName?: string | null;
  startedAt?: string | null;
  lastSeen?: string | null;
  status?: StructuredLogSourceStatus | number | string | null;
}

/** Mirrors `StructuredLogFilter` (body of `POST /diagnostics/structured-logs/recent`). */
export interface StructuredLogFilter {
  minimumLevel?: StructuredLogLevel;
  levels?: StructuredLogLevel[];
  categoryPrefix?: string;
  text?: string;
  tenantId?: string;
  workflowDefinitionId?: string;
  workflowInstanceId?: string;
  traceId?: string;
  spanId?: string;
  correlationId?: string;
  sourceId?: string;
  from?: string;
  to?: string;
  take?: number;
}

export interface RecentStructuredLogsResult {
  items: StructuredLogEvent[];
  /** `droppedEvents` in the model, `droppedCount` in the spec sample. */
  droppedEvents?: number | null;
  droppedCount?: number | null;
}

export interface StructuredLogStorageDiagnostics {
  droppedWriteCount: number;
  hasStorageDiagnosticsProvider: boolean;
}

const LEVEL_NAMES: Record<number, StructuredLogLevel> = { 0: "Trace", 1: "Debug", 2: "Information", 3: "Warning", 4: "Error", 5: "Critical", 6: "None" };
const STATUS_NAMES: Record<number, StructuredLogSourceStatus> = { 0: "Unknown", 1: "Connected", 2: "Stale", 3: "Disconnected" };

export function levelName(level: StructuredLogEvent["level"]): StructuredLogLevel {
  return typeof level === "number" ? (LEVEL_NAMES[level] ?? "None") : level;
}

export function sourceStatusName(status: StructuredLogSource["status"]): string {
  if (typeof status === "number") return STATUS_NAMES[status] ?? "Unknown";
  return status ?? "Unknown";
}

export function sourceLabel(source: StructuredLogSource): string {
  return source.displayName ?? source.name ?? source.serviceName ?? source.id;
}

export function droppedEvents(result: RecentStructuredLogsResult | undefined): number {
  return result?.droppedEvents ?? result?.droppedCount ?? 0;
}

export function structuredLogsApi(http: HttpClient) {
  const base = "diagnostics/structured-logs";
  return {
    recent: (filter: StructuredLogFilter, signal?: AbortSignal) => http.post<RecentStructuredLogsResult>(`${base}/recent`, filter, { signal }),
    sources: (signal?: AbortSignal) => http.get<StructuredLogSource[]>(`${base}/sources`, { signal }),
    storage: (signal?: AbortSignal) => http.get<StructuredLogStorageDiagnostics>(`${base}/storage`, { signal }),
  };
}

export type StructuredLogsApi = ReturnType<typeof structuredLogsApi>;

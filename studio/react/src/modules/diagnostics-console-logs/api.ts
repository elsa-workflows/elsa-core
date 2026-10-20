import type { HttpClient } from "@/lib/api/http";

/**
 * Contract derived from `Elsa.Diagnostics.ConsoleLogs` (endpoints) and the `ConsoleLogStreaming.Core`
 * models it returns. Enums may arrive as names or numbers depending on server serialization.
 */
export type ConsoleStream = "stdout" | "stderr";
export type ConsoleLogSourceHealth = "Unknown" | "Connected" | "Stale" | "Disconnected";

export interface ConsoleLogSource {
  id: string;
  displayName: string;
  serviceName?: string | null;
  machineName?: string | null;
  processId?: number | null;
  podName?: string | null;
  namespace?: string | null;
  containerName?: string | null;
  nodeName?: string | null;
  startedAt?: string | null;
  lastSeen?: string | null;
  health?: ConsoleLogSourceHealth | number | null;
  metadata?: Record<string, string> | null;
}

export interface ConsoleLogLine {
  id: string;
  sequence: number;
  timestamp: string;
  receivedAt?: string | null;
  /** Source id, or an embedded source object depending on the streaming library version. */
  source?: string | { id: string; displayName?: string | null } | null;
  stream: ConsoleStream | "Stdout" | "Stderr" | number;
  text: string;
  metadata?: Record<string, string> | null;
  dropped?: boolean | number | null;
  truncated?: boolean | null;
}

export interface ConsoleLogDropped {
  sourceId?: string | null;
  source?: string | null;
  count: number;
}

export interface RecentConsoleLogsResult {
  items: ConsoleLogLine[];
  dropped?: ConsoleLogDropped[] | null;
  sources?: ConsoleLogSource[] | null;
}

/** Mirrors `ElsaConsoleLogFilter` (body of `POST /diagnostics/console-logs/recent`). */
export interface ConsoleLogFilter {
  sourceId?: string;
  stream?: ConsoleStream | null;
  query?: string;
  workflowInstanceId?: string;
  workflowDefinitionId?: string;
  workflowDefinitionVersionId?: string;
  activityInstanceId?: string;
  activityId?: string;
  activityNodeId?: string;
  metadata?: Record<string, string>;
  from?: string;
  to?: string;
  limit?: number;
}

/** Shape shared with the structured-logs storage endpoint; console logs may not expose it (404 hides the widget). */
export interface ConsoleLogStorageDiagnostics {
  droppedWriteCount: number;
  hasStorageDiagnosticsProvider: boolean;
}

const HEALTH_NAMES: Record<number, ConsoleLogSourceHealth> = { 0: "Unknown", 1: "Connected", 2: "Stale", 3: "Disconnected" };

export function healthName(health: ConsoleLogSource["health"]): ConsoleLogSourceHealth {
  if (typeof health === "number") return HEALTH_NAMES[health] ?? "Unknown";
  return health ?? "Unknown";
}

export function streamName(stream: ConsoleLogLine["stream"]): ConsoleStream {
  if (typeof stream === "number") return stream === 1 ? "stderr" : "stdout";
  return stream.toLowerCase() === "stderr" ? "stderr" : "stdout";
}

export function sourceIdOf(line: ConsoleLogLine): string {
  if (!line.source) return "";
  return typeof line.source === "string" ? line.source : line.source.id;
}

export function droppedCount(result: RecentConsoleLogsResult | undefined): number {
  return result?.dropped?.reduce((acc, d) => acc + (d.count ?? 0), 0) ?? 0;
}

/** CSI escape sequences (`ESC [ ... <final byte>`), built from the char code to keep the source free of control characters. */
const ANSI = new RegExp(`${String.fromCharCode(27)}\\[[0-9;?]*[ -/]*[@-~]`, "g");

export function stripAnsi(text: string): string {
  return text.replace(ANSI, "");
}

export function consoleLogsApi(http: HttpClient) {
  const base = "diagnostics/console-logs";
  return {
    sources: (signal?: AbortSignal) => http.get<ConsoleLogSource[]>(`${base}/sources`, { signal }),
    recent: (filter: ConsoleLogFilter, signal?: AbortSignal) => http.post<RecentConsoleLogsResult>(`${base}/recent`, filter, { signal }),
    storage: (signal?: AbortSignal) => http.get<ConsoleLogStorageDiagnostics>(`${base}/storage`, { signal }),
  };
}

export type ConsoleLogsApi = ReturnType<typeof consoleLogsApi>;

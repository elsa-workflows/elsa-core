/**
 * Settings for the call-context view. Everything here is a backstop or a policy choice that a
 * deployment may want to tune; see `resolveCallGraphConfig` for the configuration override.
 */
export interface CallGraphLimits {
  /** Maximum distance (in calls) walked away from the current workflow on each side. */
  maxDepth: number;
  /** Maximum number of workflow nodes shown on both sides together. */
  maxNodes: number;
  /** Maximum number of HTTP requests one graph build may issue. */
  maxRequests: number;
  /** Overall time budget for one graph build. */
  timeoutMs: number;
}

export interface CallGraphConfig extends CallGraphLimits {
  /**
   * Which version of every *other* definition is inspected. "Published" matches what a Dispatch /
   * Execute call runs at runtime; switch to "Latest" to include drafts. The workflow being edited
   * always uses the editor's in-memory state.
   */
  versionOptions: "Published" | "Latest";
  /** Definitions fetched per `many-by-id` request (keeps the query string short). */
  bulkBatchSize: number;
  /** Summaries requested per list page. */
  listPageSize: number;
  /** Parallel requests in flight at once. */
  concurrency: number;
  /** External nodes further away than this start collapsed (still loaded, expandable). */
  collapseBeyondDepth: number;
}

export const DEFAULT_CALL_GRAPH_CONFIG: CallGraphConfig = {
  maxDepth: 10,
  maxNodes: 300,
  maxRequests: 200,
  timeoutMs: 30_000,
  versionOptions: "Published",
  bulkBatchSize: 25,
  listPageSize: 100,
  concurrency: 4,
  collapseBeyondDepth: 2,
};

/** Merges a partial override (e.g. `features["workflow-definitions"].options.callContext`) onto the defaults. */
export function resolveCallGraphConfig(override: unknown): CallGraphConfig {
  if (!override || typeof override !== "object") return DEFAULT_CALL_GRAPH_CONFIG;
  const o = override as Partial<Record<keyof CallGraphConfig, unknown>>;
  const num = (key: keyof CallGraphLimits | "bulkBatchSize" | "listPageSize" | "concurrency" | "collapseBeyondDepth") => {
    const v = o[key];
    return typeof v === "number" && Number.isFinite(v) && v > 0 ? v : DEFAULT_CALL_GRAPH_CONFIG[key];
  };
  return {
    maxDepth: num("maxDepth"),
    maxNodes: num("maxNodes"),
    maxRequests: num("maxRequests"),
    timeoutMs: num("timeoutMs"),
    bulkBatchSize: num("bulkBatchSize"),
    listPageSize: num("listPageSize"),
    concurrency: num("concurrency"),
    collapseBeyondDepth: num("collapseBeyondDepth"),
    versionOptions: o.versionOptions === "Latest" ? "Latest" : "Published",
  };
}

/** Activity types that start another workflow, with the call type each one represents. */
export const CALL_ACTIVITY_TYPES: Record<string, CallType> = {
  "Elsa.DispatchWorkflow": "Dispatch",
  "Elsa.ExecuteWorkflow": "Execute",
  // Dispatches one instance per item; still a dispatch of the target definition.
  "Elsa.BulkDispatchWorkflows": "Dispatch",
};

/** The activity input (camelCase, as serialised) that holds the target workflow definition id. */
export const TARGET_INPUT_NAME = "workflowDefinitionId";

export type CallType = "Dispatch" | "Execute";

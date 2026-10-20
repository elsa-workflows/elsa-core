import type { Activity } from "@/lib/api/types/activities";
import type { PagedList } from "@/lib/api/types/common";
import type { WorkflowDefinition, WorkflowDefinitionSummary } from "@/lib/api/types/workflow-definitions";
import { DEFAULT_CALL_GRAPH_CONFIG, type CallGraphConfig } from "./config";
import { extractCallEdges } from "./edge-extractor";
import type { CallGraph, CallGraphNode, CallGraphWarning } from "./types";

/** The slice of the workflow-definitions API the graph needs (kept narrow so tests can fake it). */
export interface CallGraphApi {
  list(args: { versionOptions: string; page: number; pageSize: number }, signal?: AbortSignal): Promise<PagedList<WorkflowDefinitionSummary>>;
  getMany(ids: string[], signal?: AbortSignal): Promise<{ items: WorkflowDefinition[] }>;
  getByDefinitionId(definitionId: string, versionOptions: string, signal?: AbortSignal): Promise<WorkflowDefinition>;
}

export interface CallGraphServiceOptions {
  now?: () => number;
  warn?: (message: string) => void;
}

type Listener = () => void;

/**
 * Builds and caches the workflow call graph for one server (one instance per query scope).
 *
 * Strategy: enumerate every definition through the paged summary endpoint, fetch full models in bulk
 * (`many-by-id`, by version id) with bounded concurrency, extract the Dispatch/Execute edges of each,
 * and fall back to one `by-definition-id` request per referenced id that the list did not return
 * (drafts, deleted targets). Each definition is fetched at most once per build; in-flight requests are
 * shared; failures become "missing" nodes and are never retried within the build. Hard limits on
 * requests, nodes and time stop a build cleanly with a warning and a partial graph.
 *
 * The cache lives for the session: `build()` is a no-op once the graph is complete, and the editor keeps
 * it current through `setLiveEdges` (unsaved state), `updateDefinition` (after save/publish) and
 * `revertLive` (discard).
 */
export class CallGraphService {
  readonly graph: CallGraph = { nodes: new Map(), status: "idle", warnings: [], complete: false };
  /** Incremented on every change so React can subscribe with `useSyncExternalStore`. */
  version = 0;

  private readonly listeners = new Set<Listener>();
  private controller: AbortController | undefined;
  private building: Promise<CallGraph> | undefined;
  private readonly now: () => number;
  private readonly warn: (message: string) => void;

  constructor(
    private readonly api: CallGraphApi,
    readonly config: CallGraphConfig = DEFAULT_CALL_GRAPH_CONFIG,
    options: CallGraphServiceOptions = {},
  ) {
    this.now = options.now ?? (() => Date.now());
    this.warn = options.warn ?? ((m) => console.warn(`[call-graph] ${m}`));
  }

  subscribe(listener: Listener): () => void {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  }

  private notify() {
    this.version++;
    for (const l of this.listeners) l();
  }

  /** True when a completed build is cached and can be reused as-is. */
  get isReady() {
    return this.graph.status === "ready" && this.graph.complete;
  }

  /**
   * Starts a build unless a complete one is cached (`force` rebuilds). Only one build runs at a time:
   * starting another cancels the previous one. Resolves with the graph (possibly partial) and never
   * rejects for load failures; those are recorded on the graph.
   */
  build(options: { force?: boolean } = {}): Promise<CallGraph> {
    if (!options.force && this.isReady) return Promise.resolve(this.graph);
    if (this.building && !options.force) return this.building;
    this.cancel();
    const controller = new AbortController();
    this.controller = controller;
    this.building = this.run(controller.signal).finally(() => {
      if (this.controller === controller) {
        this.controller = undefined;
        this.building = undefined;
      }
    });
    return this.building;
  }

  /** Cancels the build in progress, if any. The graph keeps whatever was loaded. */
  cancel() {
    if (!this.controller) return;
    this.controller.abort();
    this.controller = undefined;
    this.building = undefined;
  }

  /** Outgoing edges from the editor's unsaved state. Creates the node when the definition is new. */
  setLiveEdges(definitionId: string, name: string | undefined, root: Activity | undefined) {
    const node = this.ensureNode(definitionId, name);
    node.liveEdges = extractCallEdges(definitionId, root);
    if (name) node.name = name;
    this.notify();
  }

  /** Drops the unsaved override so the stored version's edges apply again (discard). */
  revertLive(definitionId: string) {
    const node = this.graph.nodes.get(definitionId);
    if (!node || node.liveEdges === undefined) return;
    delete node.liveEdges;
    this.notify();
  }

  /** After a save or publish: the stored edges become those of the saved model. Nothing else changes. */
  updateDefinition(saved: { definitionId: string; name?: string; id?: string; version?: number; isPublished?: boolean; root: Activity | undefined }) {
    const node = this.ensureNode(saved.definitionId, saved.name);
    node.storedEdges = extractCallEdges(saved.definitionId, saved.root);
    delete node.liveEdges;
    node.status = "loaded";
    if (saved.name) node.name = saved.name;
    if (saved.id) node.versionId = saved.id;
    if (saved.version !== undefined) node.version = saved.version;
    if (saved.isPublished !== undefined) node.isPublished = saved.isPublished;
    this.notify();
  }

  private ensureNode(definitionId: string, name?: string): CallGraphNode {
    let node = this.graph.nodes.get(definitionId);
    if (!node) {
      node = { definitionId, name: name ?? definitionId, status: "loaded", storedEdges: [] };
      this.graph.nodes.set(definitionId, node);
    }
    return node;
  }

  private async run(signal: AbortSignal): Promise<CallGraph> {
    const graph = this.graph;
    graph.status = "loading";
    graph.warnings = [];
    graph.error = undefined;
    graph.complete = false;
    this.notify();

    const deadline = this.now() + this.config.timeoutMs;
    const budget = new RequestBudget(this.config.maxRequests, deadline, this.now, (w) => this.addWarning(w));
    // Definition ids seen in this build (listed, referenced or in flight): each is loaded at most once.
    const seen = new Set<string>(graph.nodes.keys());
    const inFlight = new Map<string, Promise<void>>();
    const uninspected = new Set<string>();

    try {
      const summaries = await this.listAll(signal, budget);
      if (summaries === undefined) return this.finish(graph, "loading");
      for (const s of summaries) {
        seen.add(s.definitionId);
        const node = this.ensureNode(s.definitionId, s.name);
        node.name = s.name ?? s.definitionId;
        node.versionId = s.id;
        node.version = s.version;
        node.isPublished = s.isPublished;
        uninspected.add(s.definitionId);
      }
      if (graph.nodes.size > this.config.maxNodes) this.addWarning({ code: "max-nodes", message: `More than ${this.config.maxNodes} workflow definitions; only the first ${this.config.maxNodes} were inspected.` });
      this.notify();

      // Full models in bulk, bounded concurrency. Newly discovered targets that were not listed go to
      // the single-fetch queue; nothing restarts, the queue just grows while it is drained.
      const toBulk = summaries.slice(0, this.config.maxNodes).filter((s) => !!s.id);
      const batches: WorkflowDefinitionSummary[][] = [];
      for (let i = 0; i < toBulk.length; i += this.config.bulkBatchSize) batches.push(toBulk.slice(i, i + this.config.bulkBatchSize));
      const singles: string[] = [];
      const discover = (edgesTargets: (string | undefined)[]) => {
        for (const target of edgesTargets) {
          if (!target || seen.has(target)) continue;
          seen.add(target);
          singles.push(target);
        }
      };

      await runBounded(batches, this.config.concurrency, async (batch) => {
        if (signal.aborted || !budget.take()) return;
        const ids = batch.map((s) => s.id);
        let items: WorkflowDefinition[] = [];
        try {
          items = (await this.api.getMany(ids, signal)).items ?? [];
        } catch (e) {
          if (signal.aborted) return;
          this.addWarning({ code: "load-failed", message: `Could not load ${ids.length} definitions: ${describe(e)}` });
          for (const s of batch) this.markMissing(s.definitionId, s.name);
          return;
        }
        const byDefinitionId = new Map(items.map((d) => [d.definitionId, d]));
        for (const s of batch) {
          const definition = byDefinitionId.get(s.definitionId);
          uninspected.delete(s.definitionId);
          if (!definition) {
            this.markMissing(s.definitionId, s.name);
            continue;
          }
          const node = this.ensureNode(definition.definitionId, definition.name);
          node.storedEdges = extractCallEdges(definition.definitionId, definition.root);
          node.status = "loaded";
          discover(node.storedEdges.map((e) => e.targetId));
        }
        this.notify();
      });

      // Referenced but not listed (unpublished drafts, deleted targets): one request each, drained
      // until no new ids appear. Each id enters `singles` exactly once thanks to `seen`.
      while (singles.length > 0 && !signal.aborted) {
        const wave = singles.splice(0, singles.length);
        await runBounded(wave, this.config.concurrency, async (definitionId) => {
          if (signal.aborted) return;
          if (graph.nodes.size >= this.config.maxNodes) {
            this.addWarning({ code: "max-nodes", message: `Node limit of ${this.config.maxNodes} reached; some referenced workflows were not loaded.` });
            return;
          }
          if (!budget.take()) return;
          const task = (async () => {
            try {
              // Latest, not the configured version: a draft-only target still exists and should not show as missing.
              const definition = await this.api.getByDefinitionId(definitionId, "Latest", signal);
              const node = this.ensureNode(definition.definitionId, definition.name);
              node.name = definition.name ?? definition.definitionId;
              node.versionId = definition.id;
              node.version = definition.version;
              node.isPublished = definition.isPublished;
              node.storedEdges = extractCallEdges(definition.definitionId, definition.root);
              node.status = "loaded";
              discover(node.storedEdges.map((e) => e.targetId));
            } catch (e) {
              if (signal.aborted) return;
              this.markMissing(definitionId, undefined, e);
            }
            this.notify();
          })();
          inFlight.set(definitionId, task);
          await task;
          inFlight.delete(definitionId);
        });
      }

      if (signal.aborted) return this.finish(graph, "loading");
      graph.complete = uninspected.size === 0 && !graph.warnings.some((w) => w.code !== "load-failed");
      return this.finish(graph, "ready");
    } catch (e) {
      if (signal.aborted) return this.finish(graph, "loading");
      graph.error = e;
      this.warn(`Call graph build failed: ${describe(e)}`);
      return this.finish(graph, "error");
    }
  }

  private finish(graph: CallGraph, status: "ready" | "error" | "loading"): CallGraph {
    graph.status = status === "loading" ? "cancelled" : status;
    this.notify();
    return graph;
  }

  private async listAll(signal: AbortSignal, budget: RequestBudget): Promise<WorkflowDefinitionSummary[] | undefined> {
    const all: WorkflowDefinitionSummary[] = [];
    for (let page = 0; ; page++) {
      if (!budget.take()) return all;
      const result = await this.api.list({ versionOptions: this.config.versionOptions, page, pageSize: this.config.listPageSize }, signal);
      all.push(...(result.items ?? []));
      const total = result.totalCount ?? all.length;
      if (all.length >= total || (result.items?.length ?? 0) === 0) return all;
      if (all.length >= this.config.maxNodes) return all;
    }
  }

  private markMissing(definitionId: string, name?: string, error?: unknown) {
    const node = this.ensureNode(definitionId, name);
    node.status = "missing";
    node.storedEdges = [];
    if (error) this.warn(`Workflow definition ${definitionId} could not be loaded: ${describe(error)}`);
  }

  private addWarning(warning: CallGraphWarning) {
    if (this.graph.warnings.some((w) => w.code === warning.code)) return;
    this.graph.warnings.push(warning);
    this.warn(warning.message);
    this.notify();
  }
}

/** Counts requests and watches the deadline; `take()` returns false (once warned) when a limit is hit. */
class RequestBudget {
  private used = 0;
  private exhausted = false;
  constructor(
    private readonly max: number,
    private readonly deadline: number,
    private readonly now: () => number,
    private readonly onLimit: (w: CallGraphWarning) => void,
  ) {}

  take(): boolean {
    if (this.exhausted) return false;
    if (this.now() > this.deadline) {
      this.exhausted = true;
      this.onLimit({ code: "timeout", message: "The call graph took too long to load; showing what was loaded." });
      return false;
    }
    if (this.used >= this.max) {
      this.exhausted = true;
      this.onLimit({ code: "max-requests", message: `Request limit of ${this.max} reached; showing what was loaded.` });
      return false;
    }
    this.used++;
    return true;
  }
}

/** Runs `work` over `items` with at most `limit` in flight. Rejections are swallowed by the worker contract. */
export async function runBounded<T>(items: T[], limit: number, work: (item: T) => Promise<void>): Promise<void> {
  let index = 0;
  const workers = Array.from({ length: Math.max(1, Math.min(limit, items.length)) }, async () => {
    while (index < items.length) {
      const item = items[index++];
      await work(item);
    }
  });
  await Promise.all(workers);
}

function describe(e: unknown): string {
  return e instanceof Error ? e.message : String(e);
}

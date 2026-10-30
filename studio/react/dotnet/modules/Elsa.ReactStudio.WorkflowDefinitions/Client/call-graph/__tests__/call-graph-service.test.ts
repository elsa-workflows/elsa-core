import { describe, expect, it } from "vitest";
import { CallGraphService } from "../call-graph-service";
import { DEFAULT_CALL_GRAPH_CONFIG } from "../config";
import { definition, dispatch, execute, fakeApi, flowchart, writeLine } from "./helpers";

const quiet = { warn: () => {} };
const config = (override: Partial<typeof DEFAULT_CALL_GRAPH_CONFIG> = {}) => ({ ...DEFAULT_CALL_GRAPH_CONFIG, bulkBatchSize: 2, listPageSize: 2, ...override });

describe("CallGraphService", () => {
  it("enumerates with the list endpoint, loads full models in bulk and extracts edges", async () => {
    const a = definition("a", flowchart([dispatch("b"), execute("c")]));
    const b = definition("b", flowchart([writeLine()]));
    const c = definition("c", flowchart([dispatch("a")]));
    const { api, calls } = fakeApi([a, b, c]);
    const service = new CallGraphService(api, config(), quiet);
    const graph = await service.build();
    expect(graph.status).toBe("ready");
    expect(graph.complete).toBe(true);
    expect([...graph.nodes.keys()].sort()).toEqual(["a", "b", "c"]);
    expect(graph.nodes.get("a")!.storedEdges.map((e) => e.targetId)).toEqual(["b", "c"]);
    // 3 definitions, page size 2 -> 2 list pages; batch size 2 -> 2 bulk requests; nothing unlisted.
    expect(calls).toMatchObject({ list: 2, getMany: 2, getByDefinitionId: 0 });
    expect(service.isReady).toBe(true);
  });

  it("fetches referenced-but-unlisted targets once each and records missing ones", async () => {
    const a = definition("a", flowchart([dispatch("draft"), dispatch("draft"), dispatch("gone")]));
    const draft = definition("draft", flowchart([dispatch("gone")]), { isPublished: false });
    const { api, calls } = fakeApi([a], [draft]);
    const service = new CallGraphService(api, config(), quiet);
    const graph = await service.build();
    expect(graph.nodes.get("draft")?.status).toBe("loaded");
    expect(graph.nodes.get("draft")?.isPublished).toBe(false);
    expect(graph.nodes.get("gone")?.status).toBe("missing");
    // draft once (two references), gone once (two references from different definitions): no retries.
    expect(calls.getByDefinitionIdIds.sort()).toEqual(["draft", "gone"]);
  });

  it("reuses a completed build and rebuilds only when forced", async () => {
    const { api, calls } = fakeApi([definition("a", flowchart([]))]);
    const service = new CallGraphService(api, config(), quiet);
    await service.build();
    await service.build();
    expect(calls.list).toBe(1);
    await service.build({ force: true });
    expect(calls.list).toBe(2);
  });

  it("shares an in-progress build instead of starting a second one", async () => {
    const { api, calls } = fakeApi([definition("a", flowchart([]))], [], { delayMs: 5 });
    const service = new CallGraphService(api, config(), quiet);
    const first = service.build();
    const second = service.build();
    expect(second).toBe(first);
    await first;
    expect(calls.list).toBe(1);
  });

  it("cancels the previous build when a new one is forced and stops issuing requests", async () => {
    const defs = Array.from({ length: 6 }, (_, i) => definition(`d${i}`, flowchart([])));
    const { api, calls } = fakeApi(defs, [], { delayMs: 10 });
    const service = new CallGraphService(api, config({ concurrency: 1 }), quiet);
    const first = service.build();
    service.cancel();
    const firstGraph = await first;
    expect(firstGraph.status).toBe("cancelled");
    const requestsAfterCancel = calls.list + calls.getMany;
    await new Promise((r) => setTimeout(r, 60));
    expect(calls.list + calls.getMany).toBe(requestsAfterCancel);
  });

  it("marks a whole batch missing when the bulk request fails and still finishes", async () => {
    const { api } = fakeApi([definition("a", flowchart([])), definition("b", flowchart([]))], [], { failMany: true });
    const service = new CallGraphService(api, config(), quiet);
    const graph = await service.build();
    expect(graph.status).toBe("ready");
    expect(graph.nodes.get("a")?.status).toBe("missing");
    expect(graph.warnings.map((w) => w.code)).toContain("load-failed");
  });

  it("records an error state when the list itself fails, without throwing", async () => {
    const service = new CallGraphService({ list: async () => { throw new Error("boom"); }, getMany: async () => ({ items: [] }), getByDefinitionId: async () => { throw new Error("x"); } }, config(), quiet);
    const graph = await service.build();
    expect(graph.status).toBe("error");
    expect(String(graph.error)).toContain("boom");
  });

  it("stops at the request limit, keeps the partial graph and warns", async () => {
    const defs = Array.from({ length: 6 }, (_, i) => definition(`d${i}`, flowchart([])));
    const { api, calls } = fakeApi(defs);
    const service = new CallGraphService(api, config({ maxRequests: 4, concurrency: 1 }), quiet);
    const graph = await service.build();
    expect(calls.list + calls.getMany).toBe(4);
    expect(graph.status).toBe("ready");
    expect(graph.complete).toBe(false);
    expect(graph.warnings.map((w) => w.code)).toContain("max-requests");
    expect(graph.nodes.size).toBe(6);
  });

  it("stops at the node limit and warns", async () => {
    const defs = Array.from({ length: 5 }, (_, i) => definition(`d${i}`, flowchart([dispatch(`x${i}`)])));
    const { api, calls } = fakeApi(defs);
    const service = new CallGraphService(api, config({ maxNodes: 5, listPageSize: 10 }), quiet);
    const graph = await service.build();
    expect(graph.warnings.map((w) => w.code)).toContain("max-nodes");
    expect(calls.getByDefinitionId).toBe(0);
    expect(graph.complete).toBe(false);
  });

  it("stops at the timeout and warns", async () => {
    let t = 0;
    const defs = Array.from({ length: 4 }, (_, i) => definition(`d${i}`, flowchart([])));
    const { api, calls } = fakeApi(defs);
    const service = new CallGraphService(api, config({ timeoutMs: 100, concurrency: 1, listPageSize: 10 }), { warn: () => {}, now: () => (t += 60) });
    const graph = await service.build();
    expect(graph.warnings.map((w) => w.code)).toContain("timeout");
    expect(calls.getMany).toBeLessThan(2);
    expect(graph.complete).toBe(false);
  });

  it("applies and reverts live (unsaved) edges, and replaces stored edges on save", async () => {
    const a = definition("a", flowchart([dispatch("b")]));
    const { api, calls } = fakeApi([a, definition("b", flowchart([]))]);
    const service = new CallGraphService(api, config(), quiet);
    await service.build();
    const node = service.graph.nodes.get("a")!;

    service.setLiveEdges("a", "A", flowchart([execute("c")]));
    expect(node.liveEdges?.map((e) => e.targetId)).toEqual(["c"]);
    expect(node.storedEdges.map((e) => e.targetId)).toEqual(["b"]);

    service.revertLive("a");
    expect(node.liveEdges).toBeUndefined();

    service.setLiveEdges("a", "A", flowchart([execute("c")]));
    service.updateDefinition({ definitionId: "a", name: "A renamed", id: "a-v2", version: 2, root: flowchart([execute("c")]) });
    expect(node.storedEdges.map((e) => e.targetId)).toEqual(["c"]);
    expect(node.liveEdges).toBeUndefined();
    expect(node.name).toBe("A renamed");
    expect(node.version).toBe(2);
    // No request was triggered by any of the updates.
    expect(calls.list).toBe(1);
  });

  it("creates the node for a brand-new, never-listed definition when live edges arrive", () => {
    const { api } = fakeApi([]);
    const service = new CallGraphService(api, config(), quiet);
    service.setLiveEdges("new", "New", flowchart([dispatch("a")]));
    expect(service.graph.nodes.get("new")?.liveEdges?.map((e) => e.targetId)).toEqual(["a"]);
  });
});

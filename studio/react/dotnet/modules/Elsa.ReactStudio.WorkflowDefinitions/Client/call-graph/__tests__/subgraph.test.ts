import { describe, expect, it } from "vitest";
import { computeContextSubgraph } from "../subgraph";
import { graphOf } from "./helpers";

const limits = { maxDepth: 10, maxNodes: 100 };
const ids = (nodes: { id: string }[]) => nodes.map((n) => n.id).sort();

describe("computeContextSubgraph", () => {
  it("walks callees to the leaves and callers to the roots", () => {
    const graph = graphOf({ root1: ["mid"], root2: ["mid"], mid: ["cur"], cur: ["leaf1"], leaf1: ["leaf2"] });
    const sub = computeContextSubgraph(graph, "cur", limits);
    expect(ids(sub.nodes.filter((n) => n.side === "incoming"))).toEqual(["mid", "root1", "root2"]);
    expect(ids(sub.nodes.filter((n) => n.side === "outgoing"))).toEqual(["leaf1", "leaf2"]);
    expect(sub.nodes.find((n) => n.id === "root1")?.depth).toBe(2);
    expect(sub.nodes.find((n) => n.id === "leaf2")?.depth).toBe(2);
    expect(sub.truncated).toEqual({ incoming: false, outgoing: false });
  });

  it("shows a node reached through several paths once, with all its edges", () => {
    const graph = graphOf({ cur: ["x", "y"], x: ["shared"], y: ["shared"] });
    const sub = computeContextSubgraph(graph, "cur", limits);
    expect(sub.nodes.filter((n) => n.id === "shared")).toHaveLength(1);
    expect(sub.edges.filter((e) => e.target === "shared")).toHaveLength(2);
    expect(sub.edges.filter((e) => e.target === "shared").every((e) => !e.back)).toBe(true);
  });

  it("marks missing targets as distinct nodes and never expands them", () => {
    const graph = graphOf({ cur: ["gone"] }, ["gone"]);
    const sub = computeContextSubgraph(graph, "cur", limits);
    expect(sub.nodes).toEqual([expect.objectContaining({ id: "gone", kind: "missing", side: "outgoing", depth: 1 })]);
  });

  it("shows an expression target as an unresolved node on the outgoing side only", () => {
    const graph = graphOf({ cur: [undefined], other: [undefined] });
    const sub = computeContextSubgraph(graph, "cur", limits);
    expect(sub.nodes).toHaveLength(1);
    expect(sub.nodes[0].kind).toBe("unresolved");
    // `other` calls an expression target, which can never be matched as a caller of `cur`.
    expect(sub.nodes.filter((n) => n.side === "incoming")).toHaveLength(0);
  });

  it("terminates on a self-call and draws it as a back edge", () => {
    const graph = graphOf({ cur: ["cur"] });
    const sub = computeContextSubgraph(graph, "cur", limits);
    expect(sub.nodes).toHaveLength(0);
    expect(sub.edges).toHaveLength(2); // seen from both walks, both back edges
    expect(sub.edges.every((e) => e.back)).toBe(true);
  });

  it("terminates on two- and three-node cycles away from the current workflow", () => {
    const graph = graphOf({ cur: ["a"], a: ["b"], b: ["a", "c"], c: ["d"], d: ["b"] });
    const sub = computeContextSubgraph(graph, "cur", limits);
    expect(ids(sub.nodes)).toEqual(["a", "b", "c", "d"]);
    const backs = sub.edges.filter((e) => e.back).map((e) => `${e.source}->${e.target}`).sort();
    expect(backs).toEqual(["b->a", "d->b"]);
  });

  it("shows a cycle through the current workflow once, with the cycle visible", () => {
    const graph = graphOf({ cur: ["a"], a: ["b"], b: ["cur"] });
    const sub = computeContextSubgraph(graph, "cur", limits);
    expect(ids(sub.nodes)).toEqual(["a", "b"]);
    // a and b are callees (depth 1, 2) and also callers (depth 2, 1): each is placed once.
    expect(sub.nodes.find((n) => n.id === "a")?.side).toBe("outgoing");
    expect(sub.nodes.find((n) => n.id === "b")?.side).toBe("incoming");
    const closing = sub.edges.find((e) => e.source === "b" && e.target === "cur");
    expect(closing?.back).toBe(true);
    const crossing = sub.edges.find((e) => e.source === "a" && e.target === "b");
    expect(crossing?.back).toBe(true);
  });

  it("terminates with many paths to the same node (dense DAG)", () => {
    const edges: Record<string, string[]> = { cur: [] };
    for (let layer = 0; layer < 6; layer++) {
      for (let i = 0; i < 5; i++) {
        const id = `n${layer}_${i}`;
        edges[id] = Array.from({ length: 5 }, (_, j) => `n${layer + 1}_${j}`);
        if (layer === 0) edges.cur.push(id);
      }
    }
    const sub = computeContextSubgraph(graphOf(edges), "cur", { maxDepth: 20, maxNodes: 1000 });
    expect(sub.nodes).toHaveLength(35);
    expect(sub.edges.length).toBeGreaterThan(35);
  });

  it("stops at the depth limit and marks the branch as truncated", () => {
    const graph = graphOf({ cur: ["a"], a: ["b"], b: ["c"], r3: ["r2"], r2: ["r1"], r1: ["cur"] });
    const sub = computeContextSubgraph(graph, "cur", { maxDepth: 2, maxNodes: 100 });
    expect(ids(sub.nodes.filter((n) => n.kind === "workflow"))).toEqual(["a", "b", "r1", "r2"]);
    expect(sub.truncated).toEqual({ incoming: true, outgoing: true });
    expect(sub.nodes.filter((n) => n.kind === "truncated").map((n) => n.side).sort()).toEqual(["incoming", "outgoing"]);
  });

  it("stops at the node limit with a partial, truncated graph", () => {
    const graph = graphOf({ cur: ["a", "b", "c", "d"] });
    const sub = computeContextSubgraph(graph, "cur", { maxDepth: 10, maxNodes: 2 });
    expect(sub.nodes.filter((n) => n.kind === "workflow")).toHaveLength(2);
    expect(sub.truncated.outgoing).toBe(true);
  });

  it("uses the unsaved (live) edges of the current workflow when present", () => {
    const graph = graphOf({ cur: ["stored"] });
    graph.nodes.get("cur")!.liveEdges = [{ callerId: "cur", targetId: "live", callType: "Execute", activityId: "x", activityType: "Elsa.ExecuteWorkflow" }];
    graph.nodes.set("live", { definitionId: "live", name: "LIVE", status: "loaded", storedEdges: [] });
    const sub = computeContextSubgraph(graph, "cur", limits);
    expect(ids(sub.nodes)).toEqual(["live"]);
  });
});

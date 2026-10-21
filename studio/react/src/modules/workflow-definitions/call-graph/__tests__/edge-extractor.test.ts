import { describe, expect, it } from "vitest";
import type { Activity } from "@/lib/api/types/activities";
import { extractCallEdges } from "../edge-extractor";
import { dispatch, execute, expr, flowchart, forEach, ifActivity, sequence, writeLine } from "./helpers";

describe("extractCallEdges", () => {
  it("finds both activity types at the top level with their call type and activity id", () => {
    const edges = extractCallEdges("a", flowchart([dispatch("b", "d1"), execute("c", "e1"), writeLine()]));
    expect(edges).toEqual([
      expect.objectContaining({ callerId: "a", targetId: "b", callType: "Dispatch", activityId: "d1", activityType: "Elsa.DispatchWorkflow" }),
      expect.objectContaining({ callerId: "a", targetId: "c", callType: "Execute", activityId: "e1", activityType: "Elsa.ExecuteWorkflow" }),
    ]);
  });

  it("walks nested containers: sequence inside flowchart, If branches, ForEach body", () => {
    const root = flowchart([
      sequence([writeLine(), dispatch("deep-seq")]),
      ifActivity(execute("in-then"), flowchart([forEach(sequence([dispatch("in-foreach")]))])),
    ]);
    const targets = extractCallEdges("a", root).map((e) => e.targetId).sort();
    expect(targets).toEqual(["deep-seq", "in-foreach", "in-then"]);
  });

  it("reports an expression target as unresolved with its syntax", () => {
    const edges = extractCallEdges("a", flowchart([dispatch(expr("getTarget()"))]));
    expect(edges).toHaveLength(1);
    expect(edges[0].targetId).toBeUndefined();
    expect(edges[0].expressionType).toBe("JavaScript");
  });

  it("treats a blank literal as unresolved-free (no target) and an empty root as no calls", () => {
    const blank = { id: "x", type: "Elsa.DispatchWorkflow", version: 1, workflowDefinitionId: { typeName: "String", expression: { type: "Literal", value: "  " } } } as Activity;
    expect(extractCallEdges("a", flowchart([blank]))[0].targetId).toBeUndefined();
    expect(extractCallEdges("a", undefined)).toEqual([]);
    expect(extractCallEdges("a", flowchart([writeLine(), writeLine()]))).toEqual([]);
  });

  it("terminates on a self-referencing activity structure", () => {
    const loop: Activity = { id: "loop", type: "Elsa.Sequence", version: 1, activities: [] };
    (loop.activities as Activity[]).push(loop, dispatch("t"));
    const edges = extractCallEdges("a", flowchart([loop]));
    expect(edges.map((e) => e.targetId)).toEqual(["t"]);
  });

  it("visits an activity that is referenced from two places only once", () => {
    const shared = dispatch("shared", "shared-call");
    const root = flowchart([shared, ifActivity(shared, shared)]);
    expect(extractCallEdges("a", root)).toHaveLength(1);
  });
});

import { describe, expect, it } from "vitest";
import type { WorkflowExecutionLogRecord } from "@/lib/api/types/workflow-instances";
import { buildJournalSpans, countSpans, flattenContainers, journalException, visibleSpans } from "../model";

let sequence = 0;
const at = (seconds: number) => new Date(Date.UTC(2026, 9, 13, 9, 0, seconds)).toISOString();

function event(nodeId: string, type: string, eventName: string, opts: Partial<WorkflowExecutionLogRecord> = {}): WorkflowExecutionLogRecord {
  const segments = nodeId.split(":");
  return {
    id: `r${sequence}`,
    activityInstanceId: opts.activityInstanceId ?? nodeId,
    activityId: segments[segments.length - 1],
    activityType: type,
    activityTypeVersion: 1,
    nodeId,
    timestamp: at(sequence),
    sequence: sequence++,
    eventName,
    ...opts,
  };
}

/** The shape the order-review sample produces: workflow > flowchart > three activities. */
function sampleJournal() {
  sequence = 0;
  return [
    event("Workflow1", "Elsa.Workflow", "Started"),
    event("Workflow1:flowchart1", "Elsa.Flowchart", "Started"),
    event("Workflow1:flowchart1:start", "Elsa.WriteLine", "Started"),
    event("Workflow1:flowchart1:start", "Elsa.WriteLine", "Completed"),
    event("Workflow1:flowchart1:review", "Elsa.UserTask", "Started"),
    event("Workflow1:flowchart1:review", "Elsa.UserTask", "Suspended"),
    event("Workflow1:flowchart1:review", "Elsa.UserTask", "Resumed"),
    event("Workflow1:flowchart1:review", "Elsa.UserTask", "Completed"),
    event("Workflow1:flowchart1", "Elsa.Flowchart", "Completed"),
    event("Workflow1", "Elsa.Workflow", "Completed"),
  ];
}

describe("buildJournalSpans", () => {
  it("folds the events of one activity into a single span", () => {
    const roots = buildJournalSpans(sampleJournal());

    // Ten events, four activity executions: workflow, flowchart, start, review.
    expect(countSpans(roots)).toBe(4);
    const workflow = roots[0];
    const review = workflow.children[0].children[1];
    // No activityName on these fixtures, so the display name falls back to the type's last segment.
    expect(review.name).toBe("UserTask");
    expect(review.events.map((e) => e.eventName)).toEqual(["Started", "Suspended", "Resumed", "Completed"]);
  });

  it("nests by node path, so a flowchart owns the activities inside it", () => {
    const roots = buildJournalSpans(sampleJournal());

    expect(roots).toHaveLength(1);
    expect(roots[0].nodeId).toBe("Workflow1");
    expect(roots[0].children.map((c) => c.nodeId)).toEqual(["Workflow1:flowchart1"]);
    expect(roots[0].children[0].children.map((c) => c.activityId)).toEqual(["start", "review"]);
  });

  it("prefers the parent the server declares over the node path", () => {
    sequence = 0;
    const roots = buildJournalSpans([
      event("Workflow1", "Elsa.Workflow", "Started", { activityInstanceId: "wf" }),
      event("Workflow1:a", "Elsa.WriteLine", "Started", { activityInstanceId: "a", parentActivityInstanceId: "wf" }),
      event("Workflow1:a", "Elsa.WriteLine", "Completed", { activityInstanceId: "a", parentActivityInstanceId: "wf" }),
    ]);

    expect(roots[0].children.map((c) => c.activityInstanceId)).toEqual(["a"]);
  });

  it("derives status from the events, not from the last one alone", () => {
    sequence = 0;
    const [completed, faulted, suspended, running] = buildJournalSpans([
      event("w:ok", "Elsa.WriteLine", "Started", { activityInstanceId: "ok" }),
      event("w:ok", "Elsa.WriteLine", "Completed", { activityInstanceId: "ok" }),
      event("w:bad", "Elsa.WriteLine", "Started", { activityInstanceId: "bad" }),
      event("w:bad", "Elsa.WriteLine", "Faulted", { activityInstanceId: "bad" }),
      event("w:wait", "Elsa.UserTask", "Started", { activityInstanceId: "wait" }),
      event("w:wait", "Elsa.UserTask", "Suspended", { activityInstanceId: "wait" }),
      event("w:busy", "Elsa.Delay", "Started", { activityInstanceId: "busy" }),
    ]);

    expect([completed.status, faulted.status, suspended.status, running.status]).toEqual(["completed", "faulted", "suspended", "running"]);
    // A resumed-then-completed activity is not still suspended.
    expect(completed.durationMs).toBeGreaterThanOrEqual(0);
    expect(running.durationMs).toBeUndefined();
  });

  it("measures a span from its start to its terminal event", () => {
    const roots = buildJournalSpans(sampleJournal());
    const review = roots[0].children[0].children[1];

    // Started at sequence 4, completed at sequence 7: three seconds in this fixture's clock.
    expect(review.durationMs).toBe(3000);
  });

  it("gives each iteration of a looping activity its own parent", () => {
    sequence = 0;
    const roots = buildJournalSpans([
      event("w:loop", "Elsa.Sequence", "Started", { activityInstanceId: "loop-1" }),
      event("w:loop:body", "Elsa.WriteLine", "Started", { activityInstanceId: "body-1" }),
      event("w:loop:body", "Elsa.WriteLine", "Completed", { activityInstanceId: "body-1" }),
      event("w:loop", "Elsa.Sequence", "Completed", { activityInstanceId: "loop-1" }),
      event("w:loop", "Elsa.Sequence", "Started", { activityInstanceId: "loop-2" }),
      event("w:loop:body", "Elsa.WriteLine", "Started", { activityInstanceId: "body-2" }),
      event("w:loop:body", "Elsa.WriteLine", "Completed", { activityInstanceId: "body-2" }),
      event("w:loop", "Elsa.Sequence", "Completed", { activityInstanceId: "loop-2" }),
    ]);

    expect(roots.map((r) => r.activityInstanceId)).toEqual(["loop-1", "loop-2"]);
    expect(roots[0].children.map((c) => c.activityInstanceId)).toEqual(["body-1"]);
    expect(roots[1].children.map((c) => c.activityInstanceId)).toEqual(["body-2"]);
  });
});

describe("wait time", () => {
  it("separates time spent suspended from time spent running", () => {
    const roots = buildJournalSpans(sampleJournal());
    const review = roots[0].children[0].children[1];

    // Started at 4s, suspended at 5s, resumed at 6s, completed at 7s.
    expect(review.durationMs).toBe(3000);
    expect(review.suspendedMs).toBe(1000);
    expect(review.activeMs).toBe(2000);
  });

  it("leaves a span that never suspended alone", () => {
    const start = buildJournalSpans(sampleJournal())[0].children[0].children[0];

    expect(start.suspendedMs).toBeUndefined();
    expect(start.activeMs).toBe(start.durationMs);
  });

  it("does not count a suspension that has not resumed", () => {
    sequence = 0;
    const [span] = buildJournalSpans([
      event("w:wait", "Elsa.UserTask", "Started", { activityInstanceId: "wait" }),
      event("w:wait", "Elsa.UserTask", "Suspended", { activityInstanceId: "wait" }),
    ]);

    expect(span.status).toBe("suspended");
    expect(span.suspendedMs).toBeUndefined();
    expect(span.activeMs).toBeUndefined();
  });

  it("sums repeated suspensions", () => {
    sequence = 0;
    const [span] = buildJournalSpans([
      event("w:wait", "Elsa.UserTask", "Started", { activityInstanceId: "wait" }), // 0s
      event("w:wait", "Elsa.UserTask", "Suspended", { activityInstanceId: "wait" }), // 1s
      event("w:wait", "Elsa.UserTask", "Resumed", { activityInstanceId: "wait" }), // 2s
      event("w:wait", "Elsa.UserTask", "Suspended", { activityInstanceId: "wait" }), // 3s
      event("w:wait", "Elsa.UserTask", "Resumed", { activityInstanceId: "wait" }), // 4s
      event("w:wait", "Elsa.UserTask", "Completed", { activityInstanceId: "wait" }), // 5s
    ]);

    expect(span.durationMs).toBe(5000);
    expect(span.suspendedMs).toBe(2000);
    expect(span.activeMs).toBe(3000);
  });

  it("charges a container for what its children waited on", () => {
    const workflow = buildJournalSpans(sampleJournal())[0];
    const flowchart = workflow.children[0];

    // The flowchart itself never suspended; the user task inside it did.
    expect(flowchart.suspendedMs).toBe(1000);
    expect(flowchart.activeMs).toBe((flowchart.durationMs ?? 0) - 1000);
    expect(workflow.suspendedMs).toBe(1000);
  });

  it("never reports more waiting than the span lasted", () => {
    sequence = 0;
    // Two branches suspended over the same wall-clock window; summing them would exceed the parent.
    const roots = buildJournalSpans([
      event("w:fork", "Elsa.Sequence", "Started", { activityInstanceId: "fork" }), // 0s
      event("w:fork:a", "Elsa.UserTask", "Started", { activityInstanceId: "a" }), // 1s
      event("w:fork:a", "Elsa.UserTask", "Suspended", { activityInstanceId: "a" }), // 2s
      event("w:fork:b", "Elsa.UserTask", "Started", { activityInstanceId: "b" }), // 3s
      event("w:fork:b", "Elsa.UserTask", "Suspended", { activityInstanceId: "b" }), // 4s
      event("w:fork:a", "Elsa.UserTask", "Resumed", { activityInstanceId: "a" }), // 5s
      event("w:fork:a", "Elsa.UserTask", "Completed", { activityInstanceId: "a" }), // 6s
      event("w:fork:b", "Elsa.UserTask", "Resumed", { activityInstanceId: "b" }), // 7s
      event("w:fork:b", "Elsa.UserTask", "Completed", { activityInstanceId: "b" }), // 8s
      event("w:fork", "Elsa.Sequence", "Completed", { activityInstanceId: "fork" }), // 9s
    ]);

    const fork = roots[0];
    expect(fork.durationMs).toBe(9000);
    // 3s + 3s of child waiting, clamped to the parent's own 9s rather than reported as 6s of nothing.
    expect(fork.suspendedMs).toBe(6000);
    expect(fork.activeMs).toBe(3000);
    expect(fork.suspendedMs!).toBeLessThanOrEqual(fork.durationMs!);
  });
});

describe("flattenContainers", () => {
  it("promotes the activities an author placed and drops the engine's containers", () => {
    const flat = flattenContainers(buildJournalSpans(sampleJournal()));

    expect(flat.map((s) => s.activityId)).toEqual(["start", "review"]);
    expect(flat.every((s) => !s.isContainer)).toBe(true);
  });
});

describe("visibleSpans", () => {
  it("walks depth-first and carries the depth for indentation", () => {
    const rows = visibleSpans(buildJournalSpans(sampleJournal()), () => false);

    expect(rows.map((r) => r.depth)).toEqual([0, 1, 2, 2]);
  });

  it("stops at a collapsed subtree", () => {
    const roots = buildJournalSpans(sampleJournal());
    const rows = visibleSpans(roots, (span) => span.nodeId === "Workflow1:flowchart1");

    expect(rows.map((r) => r.span.nodeId)).toEqual(["Workflow1", "Workflow1:flowchart1"]);
  });
});

describe("journalException", () => {
  it("reads the exception the server puts in the payload", () => {
    sequence = 0;
    const record = event("w:a", "Elsa.WriteLine", "Faulted", {
      payload: { exception: "Elsa.Workflows.Exceptions.InputEvaluationException", message: "Failed to evaluate activity input 'Text'", stackTrace: "   at Elsa…" },
    });

    expect(journalException(record)).toMatchObject({ type: "Elsa.Workflows.Exceptions.InputEvaluationException", message: "Failed to evaluate activity input 'Text'" });
  });

  it("returns nothing for an ordinary record", () => {
    sequence = 0;
    expect(journalException(event("w:a", "Elsa.WriteLine", "Completed"))).toBeUndefined();
  });
});

import { describe, expect, it } from "vitest";
import type { Activity, Flowchart } from "@elsa-workflows/react-studio-api-client/types/activities";
import { createDesignerStore } from "./store.js";

function graph(): Flowchart {
  return { id: "root", nodeId: "root", type: "Elsa.Flowchart", version: 1, activities: [
    { id: "alpha", nodeId: "root:alpha", type: "Elsa.WriteLine", version: 1, name: "Alpha1", customProperties: { canStartWorkflow: true },
      text: { typeName: "String", expression: { type: "Literal", value: "original" } }, metadata: { designer: { position: { x: 10, y: 20 } } } },
    { id: "beta", nodeId: "root:beta", type: "Elsa.WriteLine", version: 1, name: "Beta1" },
    { id: "outside", nodeId: "root:outside", type: "Elsa.WriteLine", version: 1, name: "Outside1" },
  ], connections: [
    { source: { activity: "alpha", port: "Done" }, target: { activity: "beta", port: "In" } },
    { source: { activity: "beta", port: "Done" }, target: { activity: "outside", port: "In" } },
  ] };
}

function current(store: ReturnType<typeof createDesignerStore>) { return store.getState().root as Flowchart; }

describe("designer history and clipboard parity", () => {
  it("undoes and redoes activity data without changing connections and clears redo after another edit", () => {
    const store = createDesignerStore(graph());
    const original = structuredClone(current(store));
    store.getState().updateActivity("alpha", activity => ({ ...activity, customProperties: { ...activity.customProperties, commitStrategyName: "CommitAlways" } }));
    expect(current(store).activities[0].customProperties?.commitStrategyName).toBe("CommitAlways");
    store.getState().undo();
    expect(current(store)).toEqual(original);
    store.getState().redo();
    expect(current(store).activities[0].customProperties?.commitStrategyName).toBe("CommitAlways");
    expect(current(store).connections).toEqual(original.connections);
    store.getState().undo();
    store.getState().updateActivity("alpha", activity => ({ ...activity, name: "Different" }));
    store.getState().redo();
    expect(current(store).activities[0].name).toBe("Different");
    expect(store.getState().future).toHaveLength(0);
  });

  it("pastes deep copies with new IDs and only connections between copied activities", () => {
    const store = createDesignerStore(graph());
    store.getState().select(["alpha", "beta"]);
    store.getState().copySelection();
    store.getState().paste();
    const root = current(store);
    const [alpha, beta] = root.activities.slice(3);
    expect(root.activities).toHaveLength(5);
    expect(alpha.id).not.toBe("alpha");
    expect(beta.id).not.toBe("beta");
    expect(alpha.nodeId).toBe(`root:${alpha.id}`);
    expect(alpha.customProperties?.canStartWorkflow).toBe(false);
    expect(alpha.metadata?.designer?.position).toEqual({ x: 50, y: 60 });
    expect(root.connections).toHaveLength(3);
    expect(root.connections[2]).toEqual({ source: { activity: alpha.id, port: "Done" }, target: { activity: beta.id, port: "In" } });
    expect(store.getState().selectedIds).toEqual([alpha.id, beta.id]);
    const copiedText = alpha.text as { expression: { value: string } };
    copiedText.expression.value = "changed copy";
    expect((root.activities[0].text as typeof copiedText).expression.value).toBe("original");
  });

  it("cuts the selection and its edges, supports undo, and retains a pasteable clipboard", () => {
    const store = createDesignerStore(graph());
    const original = structuredClone(current(store));
    store.getState().select(["alpha", "beta"]);
    store.getState().cut();
    expect(current(store).activities.map(activity => activity.id)).toEqual(["outside"]);
    expect(current(store).connections).toEqual([]);
    store.getState().undo();
    expect(current(store)).toEqual(original);
    store.getState().paste();
    expect(current(store).activities).toHaveLength(5);
    expect(current(store).connections).toHaveLength(3);
  });

  it("duplicates a nested activity without reusing descendant IDs", () => {
    const nested: Activity = { id: "nested", type: "Elsa.Sequence", version: 1, name: "Sequence1", activities: [{ id: "inner", type: "Elsa.WriteLine", version: 1 }] };
    const initial = graph();
    initial.activities.push(nested);
    const store = createDesignerStore(initial);
    store.getState().duplicate("nested");
    const copy = current(store).activities.at(-1)!;
    expect(copy.id).not.toBe("nested");
    expect((copy.activities as Activity[])[0].id).not.toBe("inner");
    expect((copy.activities as Activity[])[0].type).toBe("Elsa.WriteLine");
    store.getState().undo();
    expect(current(store).activities).toEqual(initial.activities);
  });
});

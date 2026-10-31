import { describe, expect, it } from "vitest";
import type { Activity } from "@elsa-workflows/react-studio-api-client/types/activities";
import type { ActivityDescriptor } from "@elsa-workflows/react-studio-api-client/types/descriptors";
import { cloneActivity, emptyFlowchart, indexActivities } from "@elsa-workflows/react-studio-sdk/designer/model/flowchart-ops";
import { childActivities, findSlotOf, getSlot, listSlots, setSlot } from "@elsa-workflows/react-studio-sdk/designer/model/containers";

const descriptor = {
  typeName: "Elsa.Switch",
  ports: [{ name: "Default", type: "Embedded" }],
} as unknown as ActivityDescriptor;

const child = (id: string): Activity => ({ id, type: "Elsa.Sequence", version: 1 }) as unknown as Activity;

const switchActivity = (cases: { label?: string; activity?: Activity | null }[], fallback?: Activity): Activity =>
  ({ id: "sw", type: "Elsa.Switch", version: 1, cases, default: fallback ?? null }) as unknown as Activity;

describe("Switch case slots", () => {
  it("exposes one slot per case, before the Default fallback", () => {
    const slots = listSlots(switchActivity([{ label: "Gold" }, { label: "Silver" }]), descriptor);

    expect(slots.map((s) => s.slot)).toEqual(["cases.0.activity", "cases.1.activity", "default"]);
    expect(slots.map((s) => s.label)).toEqual(["Gold", "Silver", "Default"]);
  });

  it("names an unlabelled case by position", () => {
    const slots = listSlots(switchActivity([{}, { label: "   " }]), descriptor);

    expect(slots.slice(0, 2).map((s) => s.label)).toEqual(["Case 1", "Case 2"]);
  });

  it("reports what each case holds", () => {
    const slots = listSlots(switchActivity([{ label: "Gold", activity: child("a") }, { label: "Silver" }]), descriptor);

    expect(slots[0].child?.id).toBe("a");
    expect(slots[1].child).toBeUndefined();
  });

  it("writes a child into a case without touching its siblings", () => {
    const before = switchActivity([{ label: "Gold", condition: 1 } as never, { label: "Silver" }]);

    const after = setSlot(before, "cases.1.activity", child("b"));

    expect(getSlot(after, "cases.1.activity")?.id).toBe("b");
    expect(getSlot(after, "cases.0.activity")).toBeUndefined();
    // The case's other fields survive the write.
    expect((after as unknown as { cases: { label: string }[] }).cases[0].label).toBe("Gold");
    expect((after as unknown as { cases: { label: string }[] }).cases[1].label).toBe("Silver");
  });

  it("finds the slot a case activity lives in", () => {
    const sw = switchActivity([{ label: "Gold" }, { label: "Silver", activity: child("b") }]);

    expect(findSlotOf(sw, "b", descriptor)).toBe("cases.1.activity");
    // And without a descriptor, where the generic property scan cannot see inside the array.
    expect(findSlotOf(sw, "b")).toBe("cases.1.activity");
  });

  it("counts case activities and the fallback as children", () => {
    const sw = switchActivity([{ label: "Gold", activity: child("a") }, { label: "Silver" }], child("d"));

    expect(childActivities(sw, descriptor).map((c) => c.id)).toEqual(["a", "d"]);
    expect(childActivities(sw).map((c) => c.id)).toEqual(["a", "d"]);
  });

  it("leaves a Switch with no cases to its Default alone", () => {
    expect(listSlots(switchActivity([]), descriptor).map((s) => s.slot)).toEqual(["default"]);
  });
});

it("copying a Switch gives every branch descendant fresh IDs and preserves original lookup and internal links", () => {
  const branch = { ...emptyFlowchart("branch"), nodeId: "sw:branch", start: "first", activities: [
    { id: "first", nodeId: "sw:branch:first", type: "Elsa.WriteLine", version: 1 },
    { id: "last", nodeId: "sw:branch:last", type: "Elsa.WriteLine", version: 1 },
  ], connections: [{ source: { activity: "first", port: "Done" }, target: { activity: "last", port: "In" } }] };
  const original = switchActivity([{ label: "Gold", activity: branch }], { id: "fallback", type: "Elsa.WriteLine", version: 1 });
  original.nodeId = "root:sw";
  const before = structuredClone(original);
  const root = { ...emptyFlowchart("root"), activities: [original] };
  const copy = cloneActivity(original, "root", root, "Switch");
  const copiedBranch = getSlot(copy, "cases.0.activity") as typeof branch;
  const copiedFirst = copiedBranch.activities[0];
  expect(copy.id).not.toBe(original.id);
  expect(copy.nodeId).toBe(`root:${copy.id}`);
  expect(copiedBranch.id).not.toBe(branch.id);
  expect(copiedBranch.nodeId).toBe(`${copy.nodeId}:${copiedBranch.id}`);
  expect(copiedFirst.id).not.toBe("first");
  expect(copiedFirst.nodeId).toBe(`${copiedBranch.nodeId}:${copiedFirst.id}`);
  expect(copiedBranch.start).toBe(copiedFirst.id);
  expect(copiedBranch.connections[0]).toEqual({ source: { activity: copiedFirst.id, port: "Done" }, target: { activity: copiedBranch.activities[1].id, port: "In" } });
  expect(getSlot(copy, "default")?.id).not.toBe("fallback");
  const index = indexActivities({ ...root, activities: [original, copy] });
  expect(index.get("first")?.activity).toBe(branch.activities[0]);
  expect(index.get(copiedFirst.id)?.activity).toBe(copiedFirst);
  expect(original).toEqual(before);
});

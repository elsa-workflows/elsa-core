import type { Activity, Flowchart, Sequence, StateMachine } from "@/lib/api/types/activities";
import { FLOWCHART_TYPE, isFlowchart, isSequence, isStateMachine, SEQUENCE_TYPE, STATE_MACHINE_TYPE } from "@/lib/api/types/activities";
import type { ActivityDescriptor } from "@/lib/api/types/descriptors";

/**
 * How an activity holds children, as elsa-core defines it:
 *  - Flowchart: `activities[]` + `connections[]`
 *  - Sequence: ordered `activities[]`
 *  - StateMachine: `states[i].entry|exit`, `transitions[i].trigger|action`
 *  - any other activity: one child per Embedded port (`then`, `else`, `body`, …)
 */
export type ContainerKind = "flowchart" | "sequence" | "stateMachine" | "ports" | "none";

export function containerKind(activity: Activity | undefined | null): ContainerKind {
  if (!activity) return "none";
  if (isFlowchart(activity)) return "flowchart";
  if (isSequence(activity)) return "sequence";
  if (isStateMachine(activity)) return "stateMachine";
  return "ports";
}

/** True for the three activity types that have their own designer surface. */
export function isDesignableContainer(activity: Activity | undefined | null): activity is Flowchart | Sequence | StateMachine {
  const k = containerKind(activity);
  return k === "flowchart" || k === "sequence" || k === "stateMachine";
}

export const CONTAINER_TYPES = [FLOWCHART_TYPE, SEQUENCE_TYPE, STATE_MACHINE_TYPE] as const;

/**
 * A slot is an addressable child position on an activity, expressed as a dotted path:
 *  `then`, `body`                      embedded port properties
 *  `states.0.entry`, `states.0.exit`   state machine state activities
 *  `transitions.2.trigger`, `transitions.2.action`
 */
export type SlotPath = string;

export interface SlotRef {
  activityId: string;
  slot: SlotPath;
}

export interface SlotInfo {
  slot: SlotPath;
  label: string;
  child?: Activity;
}

function segments(path: SlotPath) {
  return path.split(".");
}

export function getSlot(activity: Activity, slot: SlotPath): Activity | undefined {
  let cur: unknown = activity;
  for (const seg of segments(slot)) {
    if (cur == null || typeof cur !== "object") return undefined;
    cur = (cur as Record<string, unknown>)[seg];
  }
  return cur && typeof cur === "object" && typeof (cur as Activity).type === "string" ? (cur as Activity) : undefined;
}

/** Immutable write of a slot; intermediate arrays/objects are copied along the path. */
export function setSlot(activity: Activity, slot: SlotPath, child: Activity | undefined): Activity {
  const segs = segments(slot);
  const write = (obj: unknown, i: number): unknown => {
    const key = segs[i];
    const last = i === segs.length - 1;
    if (Array.isArray(obj)) {
      const copy = [...obj];
      const idx = Number(key);
      copy[idx] = last ? (child ?? null) : write(copy[idx], i + 1);
      return copy;
    }
    const o = { ...((obj ?? {}) as Record<string, unknown>) };
    if (last) {
      if (child === undefined) delete o[key];
      else o[key] = child;
    } else o[key] = write(o[key], i + 1);
    return o;
  };
  return write(activity, 0) as Activity;
}

/** The port property name for an Embedded port (`Then` → `then`). */
export function portSlot(portName: string): SlotPath {
  return portName.charAt(0).toLowerCase() + portName.slice(1);
}

/** All child slots an activity exposes, with their current occupant. */
export function listSlots(activity: Activity, descriptor: ActivityDescriptor | undefined): SlotInfo[] {
  if (isStateMachine(activity)) {
    const out: SlotInfo[] = [];
    activity.states.forEach((st, i) => {
      out.push({ slot: `states.${i}.entry`, label: `${st.name || `State ${i + 1}`}: entry`, child: st.entry ?? undefined });
      out.push({ slot: `states.${i}.exit`, label: `${st.name || `State ${i + 1}`}: exit`, child: st.exit ?? undefined });
    });
    activity.transitions.forEach((t, i) => {
      const label = t.displayName || t.name || `${t.from} → ${t.to}`;
      out.push({ slot: `transitions.${i}.trigger`, label: `${label}: trigger`, child: t.trigger ?? undefined });
      out.push({ slot: `transitions.${i}.action`, label: `${label}: action`, child: t.action ?? undefined });
    });
    return out;
  }
  if (isFlowchart(activity) || isSequence(activity)) return [];
  return (descriptor?.ports ?? [])
    .filter((p) => p.type === "Embedded")
    .map((p) => ({ slot: portSlot(p.name), label: p.displayName ?? p.name, child: getSlot(activity, portSlot(p.name)) }));
}

/** Direct children of an activity regardless of how it stores them. */
export function childActivities(activity: Activity, descriptor?: ActivityDescriptor): Activity[] {
  if (isFlowchart(activity) || isSequence(activity)) return activity.activities;
  if (isStateMachine(activity)) {
    const out: Activity[] = [];
    for (const st of activity.states) {
      if (st.entry) out.push(st.entry);
      if (st.exit) out.push(st.exit);
    }
    for (const t of activity.transitions) {
      if (t.trigger) out.push(t.trigger);
      if (t.action) out.push(t.action);
    }
    return out;
  }
  if (descriptor) return listSlots(activity, descriptor).map((s) => s.child).filter((c): c is Activity => !!c);
  // Without a descriptor, treat any activity-shaped property as an embedded child.
  return Object.entries(activity)
    .filter(([k]) => !["metadata", "customProperties"].includes(k))
    .map(([, v]) => v)
    .filter((v): v is Activity => !!v && typeof v === "object" && !Array.isArray(v) && typeof (v as Activity).type === "string" && typeof (v as Activity).id === "string");
}

/** Slot path where `childId` lives on `parent`, if any. */
export function findSlotOf(parent: Activity, childId: string, descriptor?: ActivityDescriptor): SlotPath | undefined {
  if (isStateMachine(parent)) {
    for (let i = 0; i < parent.states.length; i++) {
      if (parent.states[i].entry?.id === childId) return `states.${i}.entry`;
      if (parent.states[i].exit?.id === childId) return `states.${i}.exit`;
    }
    for (let i = 0; i < parent.transitions.length; i++) {
      if (parent.transitions[i].trigger?.id === childId) return `transitions.${i}.trigger`;
      if (parent.transitions[i].action?.id === childId) return `transitions.${i}.action`;
    }
    return undefined;
  }
  if (isFlowchart(parent) || isSequence(parent)) return undefined;
  if (descriptor) return listSlots(parent, descriptor).find((s) => s.child?.id === childId)?.slot;
  for (const [k, v] of Object.entries(parent)) if (v && typeof v === "object" && (v as Activity).id === childId) return k;
  return undefined;
}

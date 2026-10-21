import type { Activity, Connection, Flowchart, Sequence, StateMachine } from "@/lib/api/types/activities";
import { FLOWCHART_TYPE, isFlowchart, isSequence, isStateMachine, SEQUENCE_TYPE, STATE_MACHINE_TYPE } from "@/lib/api/types/activities";
import { childActivities, findSlotOf, getSlot, isDesignableContainer, setSlot, type SlotPath } from "./containers";
import { childNodeId, newActivityId } from "./ids";

/**
 * Tree operations shared by every designer surface. A container is any activity with a designer
 * (Flowchart, Sequence, StateMachine); the path from the root to the shown container is a list of
 * `(activityId, slot)` steps, where the slot is the property that holds the child (`then`,
 * `states.0.entry`, …). Flowcharts and sequences keep their children in `activities[]`, which is
 * not a slot: the next step's activity is simply looked up in that list.
 */
export interface PathSegment {
  activityId: string;
  slot: SlotPath;
}

export type DesignableContainer = Flowchart | Sequence | StateMachine;

export function emptyFlowchart(id = newActivityId(), parentNodeId?: string): Flowchart {
  return { id, nodeId: childNodeId(parentNodeId, id), type: FLOWCHART_TYPE, version: 1, activities: [], connections: [], variables: [], metadata: {}, customProperties: {} };
}

export function emptySequence(id = newActivityId(), parentNodeId?: string): Sequence {
  return { id, nodeId: childNodeId(parentNodeId, id), type: SEQUENCE_TYPE, version: 1, activities: [], variables: [], metadata: {}, customProperties: {} };
}

export function emptyStateMachine(id = newActivityId(), parentNodeId?: string): StateMachine {
  return { id, nodeId: childNodeId(parentNodeId, id), type: STATE_MACHINE_TYPE, version: 1, states: [], transitions: [], initialState: null, metadata: {}, customProperties: {} };
}

/** Creates an empty container of the given type (used when filling an empty slot). */
export function emptyContainer(type: string, parentNodeId?: string): DesignableContainer | undefined {
  if (type === FLOWCHART_TYPE) return emptyFlowchart(undefined, parentNodeId);
  if (type === SEQUENCE_TYPE) return emptySequence(undefined, parentNodeId);
  if (type === STATE_MACHINE_TYPE) return emptyStateMachine(undefined, parentNodeId);
  return undefined;
}

/** Finds a direct child of a container by id. */
export function findChild(container: Activity, id: string): Activity | undefined {
  return childActivities(container).find((a) => a.id === id);
}

/**
 * Walks `path` from `root`, returning the container at the end (or `undefined` if the path is broken).
 * A segment with an empty slot means "the child activity itself" (a container shown as a node of a
 * flowchart or sequence); otherwise the segment names the property on the activity holding the child.
 */
export function resolveContainer(root: Activity, path: PathSegment[]): DesignableContainer | undefined {
  let current: Activity = root;
  for (const seg of path) {
    const owner = current.id === seg.activityId ? current : findChild(current, seg.activityId);
    const child = owner ? (seg.slot ? getSlot(owner, seg.slot) : owner) : undefined;
    if (!child) return undefined;
    current = child;
  }
  return isDesignableContainer(current) ? current : undefined;
}

/** Immutable update of the container at `path`. */
export function updateContainer(root: Activity, path: PathSegment[], update: (c: DesignableContainer) => Activity): Activity {
  if (path.length === 0) return isDesignableContainer(root) ? update(root) : root;
  const [head, ...rest] = path;
  return replaceChild(root, head.activityId, (owner) => {
    if (!head.slot) return updateContainer(owner, rest, update);
    const child = getSlot(owner, head.slot);
    if (!child) return owner;
    return setSlot(owner, head.slot, updateContainer(child, rest, update));
  });
}

/** Replaces a direct child (by id) of `container` with `update(child)`, whatever kind of container it is. */
export function replaceChild(container: Activity, childId: string, update: (child: Activity) => Activity): Activity {
  if (container.id === childId) return update(container);
  if (isFlowchart(container) || isSequence(container)) {
    return { ...container, activities: container.activities.map((a) => (a.id === childId ? update(a) : a)) };
  }
  const slot = findSlotOf(container, childId);
  if (!slot) return container;
  const child = getSlot(container, slot)!;
  return setSlot(container, slot, update(child));
}

export function connectionKey(c: Connection) {
  return `${c.source.activity}:${c.source.port}->${c.target.activity}:${c.target.port}`;
}

export function sameConnection(a: Connection, b: Connection) {
  return connectionKey(a) === connectionKey(b);
}

/** Removes activities (and, for flowcharts, every connection touching them) from a flowchart or sequence. */
export function removeActivitiesFrom(container: Flowchart | Sequence, ids: Set<string>): Flowchart | Sequence {
  if (isFlowchart(container)) {
    return {
      ...container,
      activities: container.activities.filter((a) => !ids.has(a.id)),
      connections: container.connections.filter((c) => !ids.has(c.source.activity) && !ids.has(c.target.activity)),
      start: container.start && ids.has(container.start) ? undefined : container.start,
    };
  }
  return { ...container, activities: container.activities.filter((a) => !ids.has(a.id)) };
}

export interface IndexedActivity {
  activity: Activity;
  /** Path that opens the canvas on which the activity is shown (or, for state-machine children, the state machine). */
  path: PathSegment[];
  /** For children held in a slot (embedded ports, state entry/exit, transition trigger/action): the owner and slot. */
  owner?: { activityId: string; slot: SlotPath };
}

/** Collects every activity in the tree, keyed by id, with the path needed to reveal it. */
export function indexActivities(root: Activity): Map<string, IndexedActivity> {
  const map = new Map<string, IndexedActivity>();
  const isList = (a: Activity) => isFlowchart(a) || isSequence(a);

  const walk = (container: Activity, pathToContainer: PathSegment[]) => {
    for (const child of childActivities(container)) {
      const owner = isList(container) ? undefined : { activityId: container.id, slot: findSlotOf(container, child.id)! };
      map.set(child.id, { activity: child, path: pathToContainer, owner });
      if (isDesignableContainer(child)) {
        const seg: PathSegment = owner ? { activityId: owner.activityId, slot: owner.slot } : { activityId: child.id, slot: "" };
        walk(child, [...pathToContainer, seg]);
      } else {
        // A regular activity with embedded-port children: those are reached by entering the slot.
        for (const grand of childActivities(child)) {
          const slot = findSlotOf(child, grand.id)!;
          const entry = [...pathToContainer, { activityId: child.id, slot }];
          map.set(grand.id, { activity: grand, path: pathToContainer, owner: { activityId: child.id, slot } });
          if (isDesignableContainer(grand)) walk(grand, entry);
          else walk(grand, entry);
        }
      }
    }
  };
  walk(root, []);
  return map;
}

/** Finds the first free `Name{n}` for a new activity of the given descriptor name across the whole tree. */
export function nextActivityName(root: Activity, baseName: string): string {
  const names = new Set<string>();
  const collect = (a: Activity) => {
    if (a.name) names.add(a.name);
    for (const c of childActivities(a)) collect(c);
  };
  collect(root);
  let n = 1;
  while (names.has(`${baseName}${n}`)) n++;
  return `${baseName}${n}`;
}

/** Deep clones an activity with fresh ids and node ids (copy/paste, duplicate). */
export function cloneActivity(activity: Activity, parentNodeId: string | undefined, root: Activity, baseName?: string): Activity {
  const id = newActivityId();
  const copy: Activity = structuredClone({ ...activity, id, nodeId: childNodeId(parentNodeId, id) });
  if (baseName) copy.name = nextActivityName(root, baseName);
  const reId = (a: Activity, parent: string | undefined): Activity => {
    const nid = newActivityId();
    const out: Activity = { ...a, id: nid, nodeId: childNodeId(parent, nid) };
    if (isFlowchart(out) || isSequence(out)) {
      const map = new Map<string, string>();
      const activities = out.activities.map((c) => {
        const r = reId(c, out.nodeId);
        map.set(c.id, r.id);
        return r;
      });
      const result = { ...out, activities } as Flowchart | Sequence;
      if (isFlowchart(result)) {
        result.connections = result.connections.map((c) => ({
          source: { ...c.source, activity: map.get(c.source.activity) ?? c.source.activity },
          target: { ...c.target, activity: map.get(c.target.activity) ?? c.target.activity },
        }));
        result.start = result.start ? map.get(result.start) : undefined;
      }
      return result;
    }
    if (isStateMachine(out)) {
      return {
        ...out,
        states: out.states.map((s) => ({ ...s, entry: s.entry ? reId(s.entry, out.nodeId) : s.entry, exit: s.exit ? reId(s.exit, out.nodeId) : s.exit })),
        transitions: out.transitions.map((t) => ({ ...t, trigger: t.trigger ? reId(t.trigger, out.nodeId) : t.trigger, action: t.action ? reId(t.action, out.nodeId) : t.action })),
      } as StateMachine;
    }
    for (const [k, v] of Object.entries(out)) {
      if (v && typeof v === "object" && !Array.isArray(v) && typeof (v as Activity).type === "string" && typeof (v as Activity).id === "string" && k !== "metadata") {
        out[k] = reId(v as Activity, out.nodeId);
      }
    }
    return out;
  };
  // Re-id the children (the top-level copy already has its fresh id).
  const rebuilt = reId(copy, parentNodeId);
  return { ...rebuilt, id, nodeId: childNodeId(parentNodeId, id), name: copy.name };
}

/** Re-labels node ids under a parent after a move (sequence reorder keeps parent, so this is a no-op there). */
export function withParentNodeId(activity: Activity, parentNodeId: string | undefined): Activity {
  return { ...activity, nodeId: childNodeId(parentNodeId, activity.id) };
}

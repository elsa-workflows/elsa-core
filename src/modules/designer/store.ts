"use client";

import { createStore, type StoreApi } from "zustand";
import type { Activity, Connection, Flowchart, Sequence, StateMachine, StateMachineState, StateTransition } from "@/lib/api/types/activities";
import { isFlowchart, isSequence, isStateMachine } from "@/lib/api/types/activities";
import type { ActivityDescriptor } from "@/lib/api/types/descriptors";
import { childActivities, getSlot, setSlot, type SlotPath } from "./model/containers";
import {
  cloneActivity,
  emptyContainer,
  indexActivities,
  nextActivityName,
  removeActivitiesFrom,
  replaceChild,
  resolveContainer,
  sameConnection,
  updateContainer,
  type DesignableContainer,
  type PathSegment,
} from "./model/flowchart-ops";
import { childNodeId, newActivityId } from "./model/ids";

const HISTORY_LIMIT = 100;
/** Stable empty list for selectors (a fresh array per call would make `useSyncExternalStore` loop). */
const NO_CHILDREN: Activity[] = [];

/** Selection ids: plain activity ids, or `state:<name>` / `transition:<index>` on a state machine canvas. */
export const selectionId = {
  state: (name: string) => `state:${name}`,
  transition: (index: number) => `transition:${index}`,
  isState: (id: string) => id.startsWith("state:"),
  isTransition: (id: string) => id.startsWith("transition:"),
  stateName: (id: string) => id.slice("state:".length),
  transitionIndex: (id: string) => Number(id.slice("transition:".length)),
};

/** Where a quick-add request came from; decides where the new activity lands and how it is connected. */
export interface QuickAddRequest {
  /** Screen coordinates for the popover. */
  screen: { x: number; y: number };
  /** Flowchart: canvas position for a free placement. */
  flowPosition?: { x: number; y: number };
  /** Flowchart: append after this activity's port, connected automatically. */
  source?: { activityId: string; port: string };
  /** Flowchart: insert in the middle of this connection. */
  insertInto?: Connection;
  /** Sequence: insert at this index. */
  sequenceIndex?: number;
  /** Fill a slot on an activity (embedded port, state entry/exit, transition trigger/action). */
  slot?: { activityId: string; slot: SlotPath };
  /** Offer Flowchart / Sequence / State machine in addition to regular activities. */
  allowContainers?: boolean;
  /** Only offer triggers (transition trigger slot). */
  triggersOnly?: boolean;
}

export interface DesignerState {
  root: Activity;
  /** Path from the root into the container currently shown on the canvas. */
  path: PathSegment[];
  selectedIds: string[];
  /** Incremented on every change so consumers can react (dirty tracking, auto-save). */
  version: number;
  readOnly: boolean;
  past: Activity[];
  future: Activity[];
  clipboard: Activity[];
  quickAdd?: QuickAddRequest;
  renamingId?: string;
  lastAddedId?: string;
  lastAddedSource?: { activityId: string; port: string };
  settingsRequest: number;
  connectFrom?: { activityId: string; port: string };
  showDataFlow: boolean;
  dataHighlight?: string;

  // derived helpers
  container(): DesignableContainer;
  /** The children shown on the current canvas (flowchart / sequence nodes; empty for state machines). */
  children(): Activity[];
  selectedActivity(): Activity | undefined;

  // generic
  replaceRoot(root: Activity, options?: { keepHistory?: boolean }): void;
  addActivity(descriptor: ActivityDescriptor, position: { x: number; y: number }, extra?: Partial<Activity>): Activity;
  updateActivity(id: string, update: (a: Activity) => Activity, options?: { history?: boolean }): void;
  removeActivities(ids: string[]): void;
  select(ids: string[]): void;
  enter(segment: PathSegment): void;
  navigateTo(depth: number): void;
  /** Puts an activity into a slot (creating containers via `emptyContainer`) and enters it when it is a container. */
  fillSlot(activityId: string, slot: SlotPath, activity: Activity | undefined, options?: { enter?: boolean }): void;
  /** Enters the activity held by a slot (when it is a designable container). */
  enterSlot(activityId: string, slot: SlotPath): void;
  copySelection(): void;
  cut(): void;
  paste(offset?: { x: number; y: number }): void;
  duplicate(id: string): void;
  undo(): void;
  redo(): void;
  openQuickAdd(request: QuickAddRequest): void;
  closeQuickAdd(): void;
  quickAddActivity(descriptor: ActivityDescriptor, request: QuickAddRequest): Activity;
  startRename(id: string | undefined): void;
  requestSettings(): void;
  setShowDataFlow(show: boolean): void;
  setDataHighlight(key: string | undefined): void;

  // flowchart
  moveActivities(positions: Record<string, { x: number; y: number }>, options?: { history?: boolean }): void;
  setActivitySize(id: string, size: { width: number; height: number }): void;
  addConnection(connection: Connection): void;
  removeConnections(connections: Connection[]): void;
  /** Stores routed waypoints per connection (`source->target`); connections not listed keep theirs. */
  setConnectionVertices(vertices: Record<string, { x: number; y: number }[]>, options?: { history?: boolean }): void;
  setStart(id: string | undefined): void;
  startConnect(from: { activityId: string; port: string } | undefined): void;
  completeConnect(targetId: string): boolean;

  // sequence
  moveChild(fromIndex: number, toIndex: number): void;

  // state machine
  addState(name?: string, position?: { x: number; y: number }): string;
  renameState(oldName: string, newName: string): void;
  removeStates(names: string[]): void;
  setInitialState(name: string | undefined): void;
  moveState(name: string, position: { x: number; y: number }, options?: { history?: boolean }): void;
  addTransition(transition: Partial<StateTransition> & { from: string; to: string }): number;
  updateTransition(index: number, update: (t: StateTransition) => StateTransition): void;
  removeTransitions(indexes: number[]): void;
}

export type DesignerStore = StoreApi<DesignerState>;

export interface StateDesignerMetadata {
  states?: Record<string, { position?: { x: number; y: number } }>;
}

export function stateMachineDesignerMetadata(sm: StateMachine): StateDesignerMetadata {
  return { states: sm.metadata?.designer?.states };
}

export function createDesignerStore(initialRoot: Activity, readOnly = false): DesignerStore {
  return createStore<DesignerState>()((set, get) => {
    const commit = (nextRoot: Activity, extra: Partial<DesignerState> = {}, history = true) =>
      set((s) => ({
        root: nextRoot,
        version: s.version + 1,
        past: history ? [...s.past.slice(-HISTORY_LIMIT + 1), s.root] : s.past,
        future: history ? [] : s.future,
        ...extra,
      }));

    const editContainer = (update: (c: DesignableContainer) => Activity, history = true, extra: Partial<DesignerState> = {}) => {
      const { root, path } = get();
      commit(updateContainer(root, path, update), extra, history);
    };

    const editList = (update: (c: Flowchart | Sequence) => Activity, history = true, extra: Partial<DesignerState> = {}) =>
      editContainer((c) => (isFlowchart(c) || isSequence(c) ? update(c) : c), history, extra);

    const editFlowchart = (update: (c: Flowchart) => Flowchart, history = true, extra: Partial<DesignerState> = {}) =>
      editContainer((c) => (isFlowchart(c) ? update(c) : c), history, extra);

    const editStateMachine = (update: (c: StateMachine) => StateMachine, history = true, extra: Partial<DesignerState> = {}) =>
      editContainer((c) => (isStateMachine(c) ? update(c) : c), history, extra);

    const buildActivity = (descriptor: ActivityDescriptor, parentNodeId: string | undefined, extra?: Partial<Activity>, position?: { x: number; y: number }): Activity => {
      const id = newActivityId();
      const container = get().container();
      const base: Activity = {
        ...(emptyContainer(descriptor.typeName, parentNodeId) ?? {}),
        ...descriptor.constructionProperties,
        id,
        nodeId: childNodeId(parentNodeId, id),
        type: descriptor.typeName,
        version: descriptor.version,
        name: nextActivityName(get().root, descriptor.name),
        metadata: position ? { designer: { position } } : {},
        customProperties: {
          canStartWorkflow:
            descriptor.kind === "Trigger" && get().path.length === 0 && isFlowchart(container) && !container.activities.some((a) => a.customProperties?.canStartWorkflow),
        },
      };
      return { ...base, ...extra };
    };

    return {
      root: initialRoot,
      path: [],
      selectedIds: [],
      version: 0,
      readOnly,
      past: [],
      future: [],
      clipboard: [],
      quickAdd: undefined,
      renamingId: undefined,
      settingsRequest: 0,
      connectFrom: undefined,
      showDataFlow: false,
      dataHighlight: undefined,

      container: () => resolveContainer(get().root, get().path) ?? (get().root as DesignableContainer),
      children: () => {
        const c = get().container();
        return isFlowchart(c) || isSequence(c) ? c.activities : NO_CHILDREN;
      },
      selectedActivity: () => {
        const { selectedIds } = get();
        if (selectedIds.length !== 1 || selectionId.isState(selectedIds[0]) || selectionId.isTransition(selectedIds[0])) return undefined;
        const id = selectedIds[0];
        return get().children().find((a) => a.id === id) ?? childActivities(get().container()).find((a) => a.id === id);
      },

      replaceRoot: (root, options) => {
        const path = resolveContainer(root, get().path) ? get().path : [];
        commit(root, { path, selectedIds: [] }, options?.keepHistory ?? true);
      },

      addActivity: (descriptor, position, extra) => {
        const container = get().container();
        const activity = buildActivity(descriptor, container.nodeId, extra, isFlowchart(container) ? position : undefined);
        editList((c) => ({ ...c, activities: [...c.activities, activity] }), true, { selectedIds: [activity.id] });
        return activity;
      },

      updateActivity: (id, update, options) => {
        const { root, path } = get();
        // The activity may be a node on this canvas or a slot child (state entry, transition trigger …).
        commit(updateContainer(root, path, (c) => replaceChild(c, id, update)), {}, options?.history ?? true);
      },

      removeActivities: (ids) => {
        if (ids.length === 0) return;
        const set = new Set(ids);
        const container = get().container();
        if (isFlowchart(container) || isSequence(container)) {
          editList((c) => removeActivitiesFrom(c, set), true, { selectedIds: get().selectedIds.filter((id) => !set.has(id)) });
        } else {
          // Slot children: clear the slot that holds each id.
          editStateMachine((sm) => ({
            ...sm,
            states: sm.states.map((s) => ({ ...s, entry: s.entry && set.has(s.entry.id) ? null : s.entry, exit: s.exit && set.has(s.exit.id) ? null : s.exit })),
            transitions: sm.transitions.map((t) => ({ ...t, trigger: t.trigger && set.has(t.trigger.id) ? null : t.trigger, action: t.action && set.has(t.action.id) ? null : t.action })),
          }));
        }
      },

      select: (ids) => set({ selectedIds: ids }),
      enter: (segment) => set((s) => ({ path: [...s.path, segment], selectedIds: [] })),
      navigateTo: (depth) => set((s) => ({ path: s.path.slice(0, depth), selectedIds: [] })),

      fillSlot: (activityId, slot, activity, options) => {
        const { root, path } = get();
        const container = get().container();
        const owner = container.id === activityId ? container : childActivities(container).find((a) => a.id === activityId);
        if (!owner) return;
        const child = activity ? { ...activity, nodeId: childNodeId(owner.nodeId, activity.id) } : undefined;
        commit(updateContainer(root, path, (c) => (c.id === activityId ? setSlot(c, slot, child) : replaceChild(c, activityId, (o) => setSlot(o, slot, child)))));
        if (child && options?.enter && (isFlowchart(child) || isSequence(child) || isStateMachine(child))) get().enter({ activityId, slot });
      },

      enterSlot: (activityId, slot) => {
        const container = get().container();
        const owner = container.id === activityId ? container : childActivities(container).find((a) => a.id === activityId);
        const child = owner ? getSlot(owner, slot) : undefined;
        if (child && (isFlowchart(child) || isSequence(child) || isStateMachine(child))) get().enter({ activityId, slot });
        else if (child) set({ selectedIds: [child.id] });
      },

      copySelection: () => {
        const selected = get().children().filter((a) => get().selectedIds.includes(a.id));
        set({ clipboard: structuredClone(selected) });
      },
      cut: () => {
        get().copySelection();
        get().removeActivities(get().selectedIds.filter((id) => !selectionId.isState(id) && !selectionId.isTransition(id)));
      },
      paste: (offset = { x: 40, y: 40 }) => {
        const { clipboard, root } = get();
        const container = get().container();
        if (clipboard.length === 0 || !(isFlowchart(container) || isSequence(container))) return;
        const idMap = new Map<string, string>();
        const clones = clipboard.map((a) => {
          const clone = cloneActivity(a, container.nodeId, root, (a.name ?? a.type).replace(/\d+$/, ""));
          const pos = a.metadata?.designer?.position ?? { x: 0, y: 0 };
          clone.metadata = { ...clone.metadata, designer: { ...clone.metadata?.designer, position: { x: pos.x + offset.x, y: pos.y + offset.y } } };
          clone.customProperties = { ...clone.customProperties, canStartWorkflow: false };
          idMap.set(a.id, clone.id);
          return clone;
        });
        editList(
          (c) => {
            const next = { ...c, activities: [...c.activities, ...clones] } as Flowchart | Sequence;
            if (isFlowchart(next) && isFlowchart(c)) {
              const inner = c.connections
                .filter((x) => idMap.has(x.source.activity) && idMap.has(x.target.activity))
                .map((x) => ({ source: { ...x.source, activity: idMap.get(x.source.activity)! }, target: { ...x.target, activity: idMap.get(x.target.activity)! } }));
              next.connections = [...c.connections, ...inner];
            }
            return next;
          },
          true,
          { selectedIds: clones.map((c) => c.id) },
        );
      },
      duplicate: (id) => {
        get().select([id]);
        get().copySelection();
        get().paste();
      },

      undo: () =>
        set((s) => {
          const previous = s.past[s.past.length - 1];
          if (!previous) return s;
          const path = resolveContainer(previous, s.path) ? s.path : [];
          return { root: previous, past: s.past.slice(0, -1), future: [s.root, ...s.future], version: s.version + 1, path, selectedIds: [] };
        }),
      redo: () =>
        set((s) => {
          const next = s.future[0];
          if (!next) return s;
          const path = resolveContainer(next, s.path) ? s.path : [];
          return { root: next, past: [...s.past, s.root], future: s.future.slice(1), version: s.version + 1, path, selectedIds: [] };
        }),

      openQuickAdd: (request) => set({ quickAdd: request }),
      closeQuickAdd: () => set({ quickAdd: undefined }),
      startRename: (id) => set({ renamingId: id }),
      requestSettings: () => set((s) => ({ settingsRequest: s.settingsRequest + 1 })),
      setShowDataFlow: (showDataFlow) => set({ showDataFlow }),
      setDataHighlight: (dataHighlight) => set({ dataHighlight }),

      quickAddActivity: (descriptor, request) => {
        const container = get().container();

        if (request.slot) {
          const owner = container.id === request.slot.activityId ? container : childActivities(container).find((a) => a.id === request.slot!.activityId);
          const activity = buildActivity(descriptor, owner?.nodeId, { customProperties: { canStartWorkflow: false } });
          get().fillSlot(request.slot.activityId, request.slot.slot, activity, { enter: true });
          set({ quickAdd: undefined, selectedIds: isFlowchart(activity) || isSequence(activity) || isStateMachine(activity) ? [] : [activity.id] });
          return activity;
        }

        if (isSequence(container)) {
          const activity = buildActivity(descriptor, container.nodeId, { customProperties: { canStartWorkflow: false } });
          const index = request.sequenceIndex ?? container.activities.length;
          editList((c) => ({ ...c, activities: [...c.activities.slice(0, index), activity, ...c.activities.slice(index)] }), true, {
            quickAdd: undefined,
            selectedIds: [activity.id],
            lastAddedId: activity.id,
          });
          return activity;
        }

        // Flowchart placement.
        const GAP_X = 90;
        const GAP_Y = 110;
        let position = request.flowPosition ?? { x: 0, y: 0 };
        const fc = container as Flowchart;
        if (request.source) {
          const source = fc.activities.find((a) => a.id === request.source!.activityId);
          const pos = source?.metadata?.designer?.position ?? { x: 0, y: 0 };
          const width = source?.metadata?.designer?.size?.width ?? 220;
          const siblings = fc.connections.filter((c) => c.source.activity === request.source!.activityId).length;
          position = { x: pos.x + width + GAP_X, y: pos.y + siblings * GAP_Y };
        } else if (request.insertInto) {
          const a = fc.activities.find((x) => x.id === request.insertInto!.source.activity);
          const b = fc.activities.find((x) => x.id === request.insertInto!.target.activity);
          const pa = a?.metadata?.designer?.position ?? { x: 0, y: 0 };
          const pb = b?.metadata?.designer?.position ?? { x: 0, y: 0 };
          const wa = a?.metadata?.designer?.size?.width ?? 220;
          const needed = 220 + 2 * GAP_X;
          const gap = pb.x - (pa.x + wa);
          if (gap < needed) {
            const delta = needed - gap;
            const moved: Record<string, { x: number; y: number }> = {};
            for (const act of fc.activities) {
              const p = act.metadata?.designer?.position;
              if (p && p.x >= pb.x && act.id !== a?.id) moved[act.id] = { x: p.x + delta, y: p.y };
            }
            get().moveActivities(moved, { history: false });
          }
          position = { x: pa.x + wa + GAP_X, y: pa.y };
        }
        const activity = get().addActivity(descriptor, { x: Math.round(position.x), y: Math.round(position.y) });
        if (request.source) {
          get().addConnection({ source: { activity: request.source.activityId, port: request.source.port }, target: { activity: activity.id, port: "In" } });
        } else if (request.insertInto) {
          const edge = request.insertInto;
          get().removeConnections([edge]);
          get().addConnection({ source: edge.source, target: { activity: activity.id, port: "In" } });
          get().addConnection({ source: { activity: activity.id, port: "Done" }, target: edge.target });
        }
        set({ quickAdd: undefined, selectedIds: [activity.id], lastAddedId: activity.id, lastAddedSource: request.source });
        return activity;
      },

      // ---- flowchart
      moveActivities: (positions, options) =>
        editFlowchart(
          (c) => ({
            ...c,
            activities: c.activities.map((a) => {
              const p = positions[a.id];
              return p ? { ...a, metadata: { ...a.metadata, designer: { ...a.metadata?.designer, position: p } } } : a;
            }),
            // Routed waypoints belong to a layout; moving an endpoint makes them stale.
            connections: c.connections.map((x) => (x.vertices?.length && (positions[x.source.activity] || positions[x.target.activity]) ? { ...x, vertices: [] } : x)),
          }),
          options?.history ?? true,
        ),
      setConnectionVertices: (vertices, options) =>
        editFlowchart(
          (c) => ({
            ...c,
            connections: c.connections.map((x) => {
              const v = vertices[`${x.source.activity}->${x.target.activity}`];
              return v ? { ...x, vertices: v } : x;
            }),
          }),
          options?.history ?? true,
        ),
      setActivitySize: (id, size) => {
        const existing = get().children().find((a) => a.id === id)?.metadata?.designer?.size;
        if (existing && Math.abs(existing.width - size.width) < 1 && Math.abs(existing.height - size.height) < 1) return;
        editFlowchart((c) => ({ ...c, activities: c.activities.map((a) => (a.id === id ? { ...a, metadata: { ...a.metadata, designer: { ...a.metadata?.designer, size } } } : a)) }), false);
      },
      addConnection: (connection) =>
        editFlowchart((c) => (c.connections.some((x) => sameConnection(x, connection)) ? c : { ...c, connections: [...c.connections, connection] })),
      removeConnections: (connections) => editFlowchart((c) => ({ ...c, connections: c.connections.filter((x) => !connections.some((r) => sameConnection(x, r))) })),
      setStart: (id) => editFlowchart((c) => ({ ...c, start: id })),
      startConnect: (from) => set({ connectFrom: from }),
      completeConnect: (targetId) => {
        const from = get().connectFrom;
        if (!from) return false;
        set({ connectFrom: undefined });
        if (from.activityId === targetId) return false;
        get().addConnection({ source: { activity: from.activityId, port: from.port }, target: { activity: targetId, port: "In" } });
        return true;
      },

      // ---- sequence
      moveChild: (fromIndex, toIndex) =>
        editList((c) => {
          if (fromIndex === toIndex || fromIndex < 0 || fromIndex >= c.activities.length) return c;
          const list = [...c.activities];
          const [item] = list.splice(fromIndex, 1);
          list.splice(Math.max(0, Math.min(toIndex, list.length)), 0, item);
          return { ...c, activities: list };
        }),

      // ---- state machine
      addState: (name, position) => {
        const sm = get().container();
        const existing = new Set(isStateMachine(sm) ? sm.states.map((s) => s.name) : []);
        let candidate = name ?? "State";
        let n = 1;
        while (existing.has(candidate)) candidate = `${name ?? "State"}${++n}`;
        const stateName = candidate;
        editStateMachine(
          (c) => ({
            ...c,
            states: [...c.states, { name: stateName, entry: null, exit: null }],
            initialState: c.initialState || stateName,
            metadata: { ...c.metadata, designer: { ...c.metadata?.designer, states: { ...stateMachineDesignerMetadata(c).states, [stateName]: { position: position ?? { x: 80 + c.states.length * 260, y: 120 } } } } },
          }),
          true,
          { selectedIds: [selectionId.state(stateName)] },
        );
        return stateName;
      },
      renameState: (oldName, newName) => {
        const name = newName.trim();
        if (!name || name === oldName) return;
        editStateMachine(
          (c) => {
            if (c.states.some((s) => s.name === name)) return c;
            const meta = stateMachineDesignerMetadata(c);
            const states = { ...meta.states };
            if (states[oldName]) {
              states[name] = states[oldName];
              delete states[oldName];
            }
            return {
              ...c,
              states: c.states.map((s) => (s.name === oldName ? { ...s, name } : s)),
              transitions: c.transitions.map((t) => ({ ...t, from: t.from === oldName ? name : t.from, to: t.to === oldName ? name : t.to })),
              initialState: c.initialState === oldName ? name : c.initialState,
              currentState: c.currentState === oldName ? name : c.currentState,
              metadata: { ...c.metadata, designer: { ...c.metadata?.designer, states } },
            };
          },
          true,
          { selectedIds: [selectionId.state(name)] },
        );
      },
      removeStates: (names) => {
        const set = new Set(names);
        editStateMachine(
          (c) => {
            const meta = stateMachineDesignerMetadata(c);
            const states = { ...meta.states };
            names.forEach((n) => delete states[n]);
            const remaining = c.states.filter((s) => !set.has(s.name));
            return {
              ...c,
              states: remaining,
              transitions: c.transitions.filter((t) => !set.has(t.from) && !set.has(t.to)),
              initialState: c.initialState && set.has(c.initialState) ? (remaining[0]?.name ?? null) : c.initialState,
              metadata: { ...c.metadata, designer: { ...c.metadata?.designer, states } },
            };
          },
          true,
          { selectedIds: [] },
        );
      },
      setInitialState: (name) => editStateMachine((c) => ({ ...c, initialState: name ?? null })),
      moveState: (name, position, options) =>
        editStateMachine(
          (c) => ({
            ...c,
            metadata: { ...c.metadata, designer: { ...c.metadata?.designer, states: { ...stateMachineDesignerMetadata(c).states, [name]: { ...stateMachineDesignerMetadata(c).states?.[name], position } } } },
          }),
          options?.history ?? true,
        ),
      addTransition: (transition) => {
        let index = -1;
        editStateMachine(
          (c) => {
            index = c.transitions.length;
            return { ...c, transitions: [...c.transitions, { name: null, displayName: null, trigger: null, condition: null, action: null, ...transition }] };
          },
          true,
          {},
        );
        set({ selectedIds: [selectionId.transition(index)] });
        return index;
      },
      updateTransition: (index, update) => editStateMachine((c) => ({ ...c, transitions: c.transitions.map((t, i) => (i === index ? update(t) : t)) })),
      removeTransitions: (indexes) => {
        const set = new Set(indexes);
        editStateMachine((c) => ({ ...c, transitions: c.transitions.filter((_, i) => !set.has(i)) }), true, { selectedIds: [] });
      },
    };
  });
}

/** Locates an activity anywhere in the tree and navigates the canvas to its container. */
export function revealActivity(store: DesignerStore, activityId: string) {
  const entry = indexActivities(store.getState().root).get(activityId);
  if (!entry) return false;
  const container = resolveContainer(store.getState().root, entry.path);
  if (container && isStateMachine(container) && entry.owner) {
    // Children of a state machine are shown through the state/transition that holds them.
    const [kind, idx] = entry.owner.slot.split(".");
    const sel = kind === "states" ? selectionId.state(container.states[Number(idx)]?.name ?? "") : selectionId.transition(Number(idx));
    store.setState({ path: entry.path, selectedIds: [sel] });
    return true;
  }
  store.setState({ path: entry.path, selectedIds: [activityId] });
  return true;
}

export type { StateMachineState };

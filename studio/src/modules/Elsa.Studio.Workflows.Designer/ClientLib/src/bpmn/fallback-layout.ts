/**
 * A deterministic layered layout, used only where the document carries no BPMN DI to place an
 * element with.
 *
 * This is not auto-layout in the sense the design rules out: a document that carries DI is never
 * touched by it. It exists so that a `.bpmn` file written without a diagram section -- which the
 * BPMN spec permits, and which elsa-core's own test assets include -- still renders as something
 * an author can read and correct, rather than as every element stacked at the origin. Whenever it
 * runs, the view model says so: `layout.source` reads `'fallback'` and every element it placed
 * reports `geometry.source: 'fallback'`.
 *
 * The layout is left-to-right by sequence flow order, one column per layer, and recurses into
 * subprocess bodies so a nested scope is drawn inside the element that hosts it.
 */
import { BOUNDARY_EVENT_ELEMENT_TYPE, defaultSizeFor } from './element-kinds';

/** Padding between a scope's border and the elements inside it. */
const PADDING = 40;
/** Horizontal gap between two layers. */
const LAYER_GAP = 60;
/** Vertical gap between two elements in the same layer. */
const ROW_GAP = 40;
/** How far a boundary event's box overlaps its host's bottom edge, i.e. half its height. */
const BOUNDARY_OVERLAP = 18;
/** Horizontal step between two boundary events on the same host. */
const BOUNDARY_STEP = 44;
/** Inset from the host's left edge to the first boundary event. */
const BOUNDARY_INSET = 20;

const MINIMUM_SCOPE_WIDTH = 200;
const MINIMUM_SCOPE_HEIGHT = 120;

export interface LayoutElement {
    readonly id: string;
    readonly elementType: string;
    /** The host element id for a boundary event whose host is in this scope; null otherwise. */
    readonly attachedToElementId: string | null;
    /** The process id of the scope this element hosts, when it is a subprocess. */
    readonly childScopeId: string | null;
}

export interface LayoutScope {
    readonly id: string;
    readonly elements: readonly LayoutElement[];
    readonly flows: readonly { readonly sourceRef: string; readonly targetRef: string }[];
}

export interface LayoutRect {
    readonly x: number;
    readonly y: number;
    readonly width: number;
    readonly height: number;
}

/** Keys a placement by the scope it belongs to, because element ids are only unique per document. */
export function layoutKey(scopeId: string, elementId: string): string {
    return `${scopeId}/${elementId}`;
}

/**
 * Places every element of every scope, in the coordinate space the document's own plane would use.
 *
 * Returns absolute rectangles keyed by {@link layoutKey}. A scope that is unreachable from
 * `rootScopeId` -- which should not happen, and would mean the activity tree disagrees with itself
 * -- is simply not placed; the caller reports the element as unplaced rather than this function
 * inventing a second root.
 */
export function computeFallbackLayout(scopes: readonly LayoutScope[], rootScopeId: string): Map<string, LayoutRect> {
    const scopesById = new Map(scopes.map(scope => [scope.id, scope]));
    const localLayouts = new Map<string, ScopeLayout>();
    const placements = new Map<string, LayoutRect>();
    const root = scopesById.get(rootScopeId);

    if (root == null) return placements;

    layoutScope(root, scopesById, localLayouts, new Set());
    assignAbsolute(rootScopeId, 0, 0, scopesById, localLayouts, placements, new Set());

    return placements;
}

interface ScopeLayout {
    readonly width: number;
    readonly height: number;
    /** Element rectangles relative to the scope's own top-left corner. */
    readonly local: Map<string, LayoutRect>;
}

/**
 * Lays one scope out, sizing any subprocess element to the scope it hosts first, and returns the
 * size the scope's own box therefore needs.
 *
 * `visiting` guards against a cycle in the scope tree. The activity tree cannot legitimately
 * contain one, but a cycle here would be an unbounded recursion rather than a wrong picture, so it
 * is closed rather than assumed away.
 */
function layoutScope(
    scope: LayoutScope,
    scopesById: ReadonlyMap<string, LayoutScope>,
    localLayouts: Map<string, ScopeLayout>,
    visiting: Set<string>): ScopeLayout {
    const cached = localLayouts.get(scope.id);

    if (cached != null) return cached;

    if (visiting.has(scope.id)) {
        const degenerate: ScopeLayout = { width: MINIMUM_SCOPE_WIDTH, height: MINIMUM_SCOPE_HEIGHT, local: new Map() };

        localLayouts.set(scope.id, degenerate);

        return degenerate;
    }

    visiting.add(scope.id);

    const sizes = new Map<string, { width: number; height: number }>();

    for (const element of scope.elements) {
        const child = element.childScopeId == null ? null : scopesById.get(element.childScopeId);
        const fallbackSize = defaultSizeFor(element.elementType);

        sizes.set(element.id, child == null
            ? fallbackSize
            : sizeOfChildScope(child, scopesById, localLayouts, visiting, fallbackSize));
    }

    visiting.delete(scope.id);

    const attached = scope.elements.filter(element => isAttached(element, scope));
    const attachedIds = new Set(attached.map(element => element.id));
    const layered = scope.elements.filter(element => !attachedIds.has(element.id));
    const layerByElementId = assignLayers(layered, scope.flows, attachedIds);
    const local = new Map<string, LayoutRect>();

    // One column per layer, each as wide as its widest element; elements are centred in their column
    // and stacked top-down in document order, so the same document always produces the same picture.
    const layerCount = layered.length === 0
        ? 0
        : Math.max(...layered.map(element => layerByElementId.get(element.id) ?? 0)) + 1;
    const columnWidths: number[] = [];
    const columnOffsets: number[] = [];

    for (let layer = 0; layer < layerCount; layer++) {
        const widths = layered
            .filter(element => (layerByElementId.get(element.id) ?? 0) === layer)
            .map(element => sizes.get(element.id)!.width);

        columnWidths[layer] = widths.length === 0 ? 0 : Math.max(...widths);
        columnOffsets[layer] = layer === 0
            ? PADDING
            : columnOffsets[layer - 1] + columnWidths[layer - 1] + LAYER_GAP;
    }

    const nextRowTop: number[] = new Array(layerCount).fill(PADDING);

    for (const element of layered) {
        const layer = layerByElementId.get(element.id) ?? 0;
        const size = sizes.get(element.id)!;
        const x = columnOffsets[layer] + (columnWidths[layer] - size.width) / 2;
        const y = nextRowTop[layer];

        nextRowTop[layer] = y + size.height + ROW_GAP;
        local.set(element.id, { x, y, width: size.width, height: size.height });
    }

    for (const [hostId, events] of groupByHost(attached)) {
        const host = local.get(hostId);

        if (host == null) continue;

        events.forEach((element, index) => {
            const size = sizes.get(element.id)!;

            local.set(element.id, {
                x: host.x + BOUNDARY_INSET + index * BOUNDARY_STEP,
                y: host.y + host.height - BOUNDARY_OVERLAP,
                width: size.width,
                height: size.height,
            });
        });
    }

    const rects = Array.from(local.values());
    const width = Math.max(MINIMUM_SCOPE_WIDTH, ...rects.map(rect => rect.x + rect.width + PADDING));
    const height = Math.max(MINIMUM_SCOPE_HEIGHT, ...rects.map(rect => rect.y + rect.height + PADDING));
    const layout: ScopeLayout = { width, height, local };

    localLayouts.set(scope.id, layout);

    return layout;
}

function sizeOfChildScope(
    child: LayoutScope,
    scopesById: ReadonlyMap<string, LayoutScope>,
    localLayouts: Map<string, ScopeLayout>,
    visiting: Set<string>,
    minimum: { width: number; height: number }): { width: number; height: number } {
    const childLayout = layoutScope(child, scopesById, localLayouts, visiting);

    return {
        width: Math.max(minimum.width, childLayout.width),
        height: Math.max(minimum.height, childLayout.height),
    };
}

/**
 * A boundary event is placed on its host rather than in a layer -- but only when that host is
 * genuinely in the same scope. One whose `attachedToRef` resolves to nothing is laid out as an
 * ordinary node, so a broken document still shows the element somewhere findable.
 */
function isAttached(element: LayoutElement, scope: LayoutScope): boolean {
    return element.elementType === BOUNDARY_EVENT_ELEMENT_TYPE
        && element.attachedToElementId != null
        && scope.elements.some(candidate => candidate.id === element.attachedToElementId);
}

function groupByHost(attached: readonly LayoutElement[]): Map<string, LayoutElement[]> {
    const byHost = new Map<string, LayoutElement[]>();

    for (const element of attached) {
        const hostId = element.attachedToElementId!;
        const existing = byHost.get(hostId);

        if (existing == null) byHost.set(hostId, [element]);
        else existing.push(element);
    }

    return byHost;
}

/**
 * Longest-path layering over the sequence flows, by Kahn's algorithm.
 *
 * A BPMN process may legitimately contain a loop, and Kahn's algorithm leaves every node on a cycle
 * unassigned. Those are then taken in document order and given one more than the deepest layer any
 * already-placed predecessor reached, which terminates and is stable for a given document -- the
 * point here is a readable, reproducible picture, not an optimal one.
 */
function assignLayers(
    layered: readonly LayoutElement[],
    flows: readonly { readonly sourceRef: string; readonly targetRef: string }[],
    excludedIds: ReadonlySet<string>): Map<string, number> {
    const ids = new Set(layered.map(element => element.id));
    const successors = new Map<string, string[]>();
    const predecessors = new Map<string, string[]>();

    for (const flow of flows) {
        // A flow out of a boundary event starts at the host's edge, so it says nothing about layers.
        if (!ids.has(flow.sourceRef) || !ids.has(flow.targetRef) || excludedIds.has(flow.sourceRef)) continue;

        append(successors, flow.sourceRef, flow.targetRef);
        append(predecessors, flow.targetRef, flow.sourceRef);
    }

    const layers = new Map<string, number>();
    const settled = new Set<string>();
    const remainingPredecessors = new Map<string, number>(
        layered.map(element => [element.id, (predecessors.get(element.id) ?? []).length]));
    const queue = layered.filter(element => remainingPredecessors.get(element.id) === 0).map(element => element.id);

    for (let index = 0; index < queue.length; index++) {
        const id = queue[index];
        const layer = layers.get(id) ?? 0;

        layers.set(id, layer);
        settled.add(id);

        for (const successor of successors.get(id) ?? []) {
            layers.set(successor, Math.max(layers.get(successor) ?? 0, layer + 1));

            const remaining = (remainingPredecessors.get(successor) ?? 0) - 1;

            remainingPredecessors.set(successor, remaining);

            if (remaining === 0) queue.push(successor);
        }
    }

    for (const element of layered) {
        if (settled.has(element.id)) continue;

        const resolved = (predecessors.get(element.id) ?? [])
            .filter(id => settled.has(id))
            .map(id => layers.get(id)!);

        layers.set(element.id, resolved.length === 0 ? 0 : Math.max(...resolved) + 1);
        settled.add(element.id);
    }

    return layers;
}

function append(map: Map<string, string[]>, key: string, value: string): void {
    const existing = map.get(key);

    if (existing == null) map.set(key, [value]);
    else existing.push(value);
}

function assignAbsolute(
    scopeId: string,
    originX: number,
    originY: number,
    scopesById: ReadonlyMap<string, LayoutScope>,
    localLayouts: ReadonlyMap<string, ScopeLayout>,
    placements: Map<string, LayoutRect>,
    visited: Set<string>): void {
    if (visited.has(scopeId)) return;

    visited.add(scopeId);

    const scope = scopesById.get(scopeId);
    const layout = localLayouts.get(scopeId);

    if (scope == null || layout == null) return;

    for (const element of scope.elements) {
        const local = layout.local.get(element.id);

        if (local == null) continue;

        const absolute: LayoutRect = {
            x: originX + local.x,
            y: originY + local.y,
            width: local.width,
            height: local.height,
        };

        placements.set(layoutKey(scopeId, element.id), absolute);

        if (element.childScopeId != null) {
            assignAbsolute(element.childScopeId, absolute.x, absolute.y, scopesById, localLayouts, placements, visited);
        }
    }
}

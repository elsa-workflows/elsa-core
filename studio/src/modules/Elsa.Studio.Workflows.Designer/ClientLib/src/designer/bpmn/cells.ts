/**
 * The mapping: one canvas-neutral BPMN view model in, one set of X6 node and edge metadata out.
 *
 * This layer is deliberately pure. It takes no `Graph`, touches no DOM and imports nothing from
 * `@antv/x6` except types, so the whole mapping can be run against every W9a fixture under vitest
 * and a divergence between the view model and the canvas -- an element that produces no node, a
 * boundary event that loses its host, a default flow that loses its marker -- fails a test rather
 * than showing up as a diagram that merely looks plausible.
 *
 * What it does *not* do is decide anything about BPMN. Kinds, geometry, waypoints, bindings,
 * boundary attachment, lane membership and instance state all arrive already resolved on the view
 * model; this file only chooses shapes, attributes and painting order for them.
 */
import type { Edge, Node } from '@antv/x6';
import type {
    BpmnActivityStats,
    BpmnBindingKind,
    BpmnBindingState,
    BpmnElementKind,
    BpmnElementStats,
    BpmnViewElement,
    BpmnViewFlow,
    BpmnViewLane,
    BpmnViewModel,
    BpmnViewPool,
} from '../../bpmn';
import { BOUNDARY_EVENT_ELEMENT_TYPE } from '../../bpmn';
import {
    BPMN_ACTIVITY_SHAPE,
    BPMN_ASSOCIATION_CELL_PREFIX,
    BPMN_ASSOCIATION_SHAPE,
    BPMN_EVENT_SHAPE,
    BPMN_FLOW_SHAPE,
    BPMN_GATEWAY_SHAPE,
    BPMN_LANE_CELL_PREFIX,
    BPMN_LANE_SHAPE,
    BPMN_POOL_CELL_PREFIX,
    BPMN_POOL_SHAPE,
    BPMN_WAYPOINT_ANCHOR,
    LANE_Z_INDEX,
    POOL_Z_INDEX,
    boundaryZIndex,
    elementZIndex,
    flowZIndex,
} from './constants';
import {
    ACTIVITY_MARKER_GLYPHS,
    GATEWAY_GLYPHS,
    TASK_TYPE_GLYPHS,
    resolveEventGlyph,
    type Glyph,
} from './glyphs';
import {
    BADGE_SURFACE_BY_TONE,
    BADGE_TEXT_BY_TONE,
    CONTAINER_STROKE,
    CONTAINER_SURFACE,
    EDGE,
    HEADER_SURFACE,
    MUTED,
    STROKE,
    SURFACE,
    TEXT,
    UNBOUND,
} from './palette';
import { isBpmnFlowTaken, resolveBpmnStatsBadge, type BpmnStatsBadge } from './stats';

const CATCHING_EVENT_TYPES: readonly string[] = ['startEvent', 'intermediateCatchEvent', BOUNDARY_EVENT_ELEMENT_TYPE];
const RINGED_EVENT_TYPES: readonly string[] = ['intermediateCatchEvent', 'intermediateThrowEvent', BOUNDARY_EVENT_ELEMENT_TYPE];

// ---------------------------------------------------------------------------------------------
// Cell data
// ---------------------------------------------------------------------------------------------

/**
 * What every element node carries as X6 `data`.
 *
 * The mount layer reads nothing else off a node: selection, double-click and both stats updates are
 * all answered from here, which is what keeps the interop payload the same whether the user clicked
 * a task with a bound activity or a gateway with none.
 */
export interface BpmnElementCellData {
    readonly cellKind: 'element';
    readonly elementId: string;
    readonly elementType: string;
    readonly kind: BpmnElementKind;
    readonly name: string | null;
    /** The process id of the scope the element lives in. */
    readonly scopeId: string;
    /** The `Elsa.BpmnProcess` activity that runs {@link scopeId}. W10 needs it to open the scope. */
    readonly scopeActivityId: string;
    /** The bound Elsa activity, or null for an element that performs no work. */
    readonly activityId: string | null;
    readonly bindingState: BpmnBindingState | null;
    /** Whether the bound activity is authored on the element or bound automatically, or null for no work. */
    readonly bindingKind: BpmnBindingKind | null;
    /** Set only on a boundary event: the element it is drawn on. */
    readonly boundaryHostElementId: string | null;
    readonly childScopeId: string | null;
    readonly stats: BpmnElementStats | null;
    readonly activityStats: BpmnActivityStats | null;
}

/** What a lane or pool node carries. Containers are visual only; nothing is bound to them. */
export interface BpmnContainerCellData {
    readonly cellKind: 'lane' | 'pool';
    readonly id: string;
    readonly name: string | null;
}

export interface BpmnFlowCellData {
    readonly cellKind: 'flow' | 'association';
    readonly id: string;
    readonly scopeId: string;
    readonly sourceElementId: string;
    readonly targetElementId: string;
    readonly isDefault: boolean;
    readonly conditionOutcome: string | null;
    /** Instance state keyed by this flow's own id. Always null for an association. */
    readonly stats: BpmnElementStats | null;
}

export type BpmnCellData = BpmnElementCellData | BpmnContainerCellData | BpmnFlowCellData;

/** Something the view model describes that the canvas cannot draw, and why. */
export interface BpmnUndrawn {
    readonly kind: 'lane' | 'pool';
    readonly id: string;
    readonly reason: string;
}

/**
 * A cell the mapping chose not to emit because its id was already taken.
 *
 * X6 keys its whole model by cell id, document-wide, and a second cell with an id already in use
 * silently replaces the first rather than erroring. Element and flow ids are only guaranteed unique
 * within the BPMN document as a whole -- which the view model already checks and reports as
 * `duplicate-element-id` / `duplicate-flow-id` -- so a malformed document can still ask this mapping
 * for two cells with the same id. Rather than let X6 pick a survivor arbitrarily, the mapping keeps
 * the first cell it built for a given id and drops every later one, and it drops with it any edge
 * that would otherwise attach to the wrong survivor.
 */
export interface BpmnCollision {
    readonly kind: 'pool' | 'lane' | 'element' | 'flow' | 'association';
    readonly id: string;
    readonly reason: string;
}

export interface BpmnX6Cells {
    readonly nodes: readonly Node.Metadata[];
    readonly edges: readonly Edge.Metadata[];
    /**
     * Lanes and pools left off the canvas because the document places them nowhere.
     *
     * Never elements or flows: the view model always resolves a rectangle for an element, and an
     * edge that is linked to its two nodes needs no geometry of its own.
     */
    readonly undrawn: readonly BpmnUndrawn[];
    /** Cells dropped because their id collided with a cell already emitted. See {@link BpmnCollision}. */
    readonly collisions: readonly BpmnCollision[];
}

// ---------------------------------------------------------------------------------------------
// Entry point
// ---------------------------------------------------------------------------------------------

/**
 * Maps a whole view model onto X6 cells.
 *
 * Guarantees, all of them checked by `__tests__/cells.test.ts` against every W9a fixture:
 *
 *  * one node per element, keyed by the element id verbatim, plus one node per lane and per pool
 *    the document places;
 *  * one edge per sequence flow, keyed by the flow id, plus one per resolved compensation
 *    association;
 *  * every boundary event's node names its host in `data.boundaryHostElementId`, and no boundary
 *    event is an X6 child of anything -- see `nodeForElement` for why;
 *  * every cell id is unique, so `fromJSON` cannot silently drop one -- a document that would make
 *    two cells share an id instead has the later one, and any edge that would attach to it, reported
 *    in {@link BpmnX6Cells.collisions} and left off the canvas.
 */
export function buildBpmnX6Cells(viewModel: BpmnViewModel): BpmnX6Cells {
    const context = createContext(viewModel);
    const nodes: Node.Metadata[] = [];
    const edges: Edge.Metadata[] = [];
    const undrawn: BpmnUndrawn[] = [];
    const collisions: BpmnCollision[] = [];
    const claimedCellIds = new Set<string>();
    /** The scope+element pairs whose node survived onto the canvas -- see {@link flowSurvives}. */
    const survivingScopedElementIds = new Set<string>();

    // A cell id already claimed by an earlier cell in this pass replaces nothing: it is dropped and
    // reported instead, so the diagram never depends on which of two colliding cells X6 happened to
    // keep.
    const claim = (id: string): boolean => {
        if (claimedCellIds.has(id)) return false;

        claimedCellIds.add(id);

        return true;
    };

    for (const pool of viewModel.pools) {
        const node = nodeForPool(pool);

        if (node == null) {
            undrawn.push({ kind: 'pool', id: pool.id, reason: 'The BPMN source places no shape for this pool.' });
        } else if (!claim(String(node.id))) {
            collisions.push({ kind: 'pool', id: pool.id, reason: `Cell id '${node.id}' is already used by an earlier cell; the first cell is kept and this pool is dropped.` });
        } else {
            nodes.push(node);
        }
    }

    for (const lane of viewModel.lanes) {
        const node = nodeForLane(lane);

        if (node == null) {
            undrawn.push({ kind: 'lane', id: lane.id, reason: 'The BPMN source places no shape for this lane.' });
        } else if (!claim(String(node.id))) {
            collisions.push({ kind: 'lane', id: lane.id, reason: `Cell id '${node.id}' is already used by an earlier cell; the first cell is kept and this lane is dropped.` });
        } else {
            nodes.push(node);
        }
    }

    for (const element of viewModel.elements) {
        const node = nodeForElement(element, context);

        if (!claim(String(node.id))) {
            collisions.push({
                kind: 'element',
                id: element.id,
                reason: `The element id '${element.id}' is already drawn by another element (BPMN requires element ids to be `
                    + `document-unique); the first one is kept and the element in scope '${element.scopeId}' is dropped.`,
            });
            continue;
        }

        survivingScopedElementIds.add(scopedKey(element.scopeId, element.id));
        nodes.push(node);
    }

    for (const flow of viewModel.flows) {
        if (!flowSurvives(flow.scopeId, flow.sourceElementId, flow.targetElementId, context, survivingScopedElementIds)) {
            collisions.push({
                kind: 'flow',
                id: flow.id,
                reason: `The flow '${flow.id}' connects an element id that was dropped as a duplicate; the flow is dropped `
                    + 'rather than risk attaching it to the wrong element.',
            });
            continue;
        }

        const edge = edgeForFlow(flow, context);

        if (!claim(String(edge.id))) {
            collisions.push({ kind: 'flow', id: flow.id, reason: `Cell id '${edge.id}' is already used by an earlier cell; the first cell is kept and this flow is dropped.` });
            continue;
        }

        edges.push(edge);
    }

    for (const association of viewModel.associations) {
        if (!association.targetResolved) continue;

        const associationId = `${association.sourceElementId}->${association.targetElementId}`;

        if (!flowSurvives(association.scopeId, association.sourceElementId, association.targetElementId, context, survivingScopedElementIds)) {
            collisions.push({
                kind: 'association',
                id: associationId,
                reason: `The compensation association '${associationId}' connects an element id that was dropped as a `
                    + 'duplicate; the association is dropped rather than risk attaching it to the wrong element.',
            });
            continue;
        }

        const edge = edgeForAssociation(association.sourceElementId, association.targetElementId, association.scopeId, context);

        if (!claim(String(edge.id))) {
            collisions.push({ kind: 'association', id: associationId, reason: `Cell id '${edge.id}' is already used by an earlier cell; the first cell is kept and this association is dropped.` });
            continue;
        }

        edges.push(edge);
    }

    return { nodes, edges, undrawn, collisions };
}

/**
 * Whether a flow's (or association's) two endpoints still resolve to a node that made it onto the
 * canvas.
 *
 * An endpoint that the view model does not resolve at all (a dangling flow, already reported by the
 * view model itself) is left alone here -- that is not this function's concern. What it catches is
 * the narrower case: the endpoint *did* resolve to a specific element, but that element's node lost
 * out to an earlier one with the same id, so drawing the edge now would attach it to a node that is
 * not the element the document meant.
 */
function flowSurvives(
    scopeId: string,
    sourceElementId: string,
    targetElementId: string,
    context: BuildContext,
    survivingScopedElementIds: ReadonlySet<string>): boolean {
    const sourceKey = scopedKey(scopeId, sourceElementId);
    const targetKey = scopedKey(scopeId, targetElementId);

    if (context.elementsByScopedId.has(sourceKey) && !survivingScopedElementIds.has(sourceKey)) return false;
    if (context.elementsByScopedId.has(targetKey) && !survivingScopedElementIds.has(targetKey)) return false;

    return true;
}

interface BuildContext {
    readonly depthByScopeId: ReadonlyMap<string, number>;
    readonly activityIdByScopeId: ReadonlyMap<string, string>;
    readonly scopeIdsWithElements: ReadonlySet<string>;
    readonly elementsByScopedId: ReadonlyMap<string, BpmnViewElement>;
}

function createContext(viewModel: BpmnViewModel): BuildContext {
    return {
        depthByScopeId: new Map(viewModel.scopes.map(scope => [scope.id, scope.depth])),
        activityIdByScopeId: new Map(viewModel.scopes.map(scope => [scope.id, scope.activityId])),
        scopeIdsWithElements: new Set(viewModel.elements.map(element => element.scopeId)),
        elementsByScopedId: new Map(viewModel.elements.map(element => [scopedKey(element.scopeId, element.id), element])),
    };
}

/** Scope-qualified, because an element id is only guaranteed unique within the document. */
function scopedKey(scopeId: string, elementId: string): string {
    return `${scopeId}\u0000${elementId}`;
}

function depthOf(context: BuildContext, scopeId: string): number {
    return context.depthByScopeId.get(scopeId) ?? 0;
}

// ---------------------------------------------------------------------------------------------
// Element nodes
// ---------------------------------------------------------------------------------------------

/**
 * One element, one node -- and, for a boundary event, a node that is a *sibling* of its host rather
 * than an X6 child of it.
 *
 * X6's embedding was rejected here twice over. A boundary event sits astride its host's border, so
 * making it a child would hand X6 a child that overlaps its own parent's body and change which of
 * the two a click on that border resolves to. And `parent`/`children` is X6's model hierarchy, not
 * a drawing hint: using it here would make the canvas assert a containment the BPMN document does
 * not have, which is the same reason lanes and pools do not embed the elements they hold. The host
 * is recorded as data instead, which is everything a read-only canvas needs and everything W14 will
 * need to move the two together.
 */
function nodeForElement(element: BpmnViewElement, context: BuildContext): Node.Metadata {
    const depth = depthOf(context, element.scopeId);
    const isBoundary = element.elementType === BOUNDARY_EVENT_ELEMENT_TYPE;
    const data: BpmnElementCellData = {
        cellKind: 'element',
        elementId: element.id,
        elementType: element.elementType,
        kind: element.kind,
        name: element.name,
        scopeId: element.scopeId,
        scopeActivityId: context.activityIdByScopeId.get(element.scopeId) ?? '',
        activityId: element.binding?.activityId ?? null,
        bindingState: element.binding?.state ?? null,
        bindingKind: element.binding?.kind ?? null,
        boundaryHostElementId: element.boundary?.hostElementId ?? null,
        childScopeId: element.childScopeId,
        stats: element.stats,
        activityStats: element.activityStats,
    };

    return {
        id: element.id,
        shape: shapeForElement(element),
        x: element.geometry.x,
        y: element.geometry.y,
        width: element.geometry.width,
        height: element.geometry.height,
        zIndex: isBoundary ? boundaryZIndex(depth) : elementZIndex(depth),
        data,
        attrs: attrsForElement(element, context),
    };
}

function shapeForElement(element: BpmnViewElement): string {
    switch (element.kind) {
        case 'event':
            return BPMN_EVENT_SHAPE;
        case 'gateway':
            return BPMN_GATEWAY_SHAPE;
        default:
            return BPMN_ACTIVITY_SHAPE;
    }
}

function attrsForElement(element: BpmnViewElement, context: BuildContext): Record<string, any> {
    const base = element.kind === 'event'
        ? eventAttrs(element)
        : element.kind === 'gateway'
            ? gatewayAttrs(element)
            : activityAttrs(element, context);

    return { ...base, ...statsBadgeAttrs(resolveBpmnStatsBadge(element.stats, element.activityStats)) };
}

// -- events ------------------------------------------------------------------------------------

function eventAttrs(element: BpmnViewElement): Record<string, any> {
    const radius = Math.min(element.geometry.width, element.geometry.height) / 2;
    const isEnd = element.elementType === 'endEvent';
    const isRinged = RINGED_EVENT_TYPES.includes(element.elementType);
    // A non-interrupting boundary event is drawn dashed. That is the document's `cancelActivity`
    // flag as the view model reports it, not something inferred from what the event does.
    const isNonInterrupting = element.boundary != null && !element.boundary.interrupting;
    const strokeDasharray = isNonInterrupting ? '4 3' : null;
    const glyph = resolveEventGlyph(element.eventDefinitions.map(definition => definition.type));
    const solid = glyph != null && isSolidGlyph(glyph, !CATCHING_EVENT_TYPES.includes(element.elementType));

    return {
        body: {
            fill: SURFACE,
            stroke: STROKE,
            // A thick single ring is what makes an end event an end event; an intermediate event is
            // a thin double ring; a start event is a thin single ring.
            strokeWidth: isEnd ? 3.5 : 1.5,
            strokeDasharray,
        },
        ring: {
            display: isRinged ? 'block' : 'none',
            cx: element.geometry.width / 2,
            cy: element.geometry.height / 2,
            r: Math.max(radius - 3, 1),
            fill: 'none',
            stroke: STROKE,
            strokeWidth: 1.5,
            strokeDasharray,
        },
        icon: glyph == null
            ? { display: 'none' }
            : {
                display: 'block',
                d: glyph.d,
                fill: solid ? STROKE : 'none',
                stroke: solid ? 'none' : STROKE,
                strokeWidth: 1.2,
                refX: '50%',
                refY: '50%',
            },
        label: externalLabelAttrs(element),
    };
}

function isSolidGlyph(glyph: Glyph, throwing: boolean): boolean {
    if (glyph.fill === 'solid') return true;
    if (glyph.fill === 'outline') return false;

    return throwing;
}

// -- gateways ----------------------------------------------------------------------------------

function gatewayAttrs(element: BpmnViewElement): Record<string, any> {
    const glyph = GATEWAY_GLYPHS[element.elementType] ?? null;

    return {
        body: { fill: SURFACE, stroke: STROKE, strokeWidth: 1.5 },
        marker: glyph == null || !showsGatewayMarker(element)
            ? { display: 'none' }
            : {
                display: 'block',
                d: glyph.d,
                fill: glyph.fill === 'solid' ? STROKE : 'none',
                stroke: glyph.fill === 'solid' ? 'none' : STROKE,
                strokeWidth: glyph.fill === 'solid' ? 0 : 2,
                refX: '50%',
                refY: '50%',
            },
        label: externalLabelAttrs(element),
    };
}

/**
 * Whether a gateway draws its own marker.
 *
 * BPMN makes the exclusive gateway's cross optional and records the choice in DI as
 * `isMarkerVisible`; every other gateway always draws its marker. Where the document carries a
 * shape for this gateway its answer is honoured exactly, so the canvas matches the modeller that
 * wrote it. Where it does not -- a diagram Studio laid out itself -- the marker is drawn, because
 * an unmarked diamond in a document with no DI at all is far more likely to be a missing fact than
 * a deliberate one.
 */
function showsGatewayMarker(element: BpmnViewElement): boolean {
    if (element.elementType !== 'exclusiveGateway') return true;
    if (element.geometry.source === 'fallback') return element.isMarkerVisible ?? true;

    return element.isMarkerVisible === true;
}

// -- tasks, subprocesses and call activities ---------------------------------------------------

function activityAttrs(element: BpmnViewElement, context: BuildContext): Record<string, any> {
    const expanded = isExpandedContainer(element, context);
    const typeGlyph = TASK_TYPE_GLYPHS[element.elementType] ?? null;
    const markers = activityMarkers(element, expanded);
    const name = element.name ?? element.binding?.displayName ?? null;
    const label = {
        display: 'block',
        text: wrapOnWords(name ?? '', element.geometry.width - ACTIVITY_LABEL_INSET, ACTIVITY_LABEL_FONT_SIZE, ACTIVITY_LABEL_MAX_LINES),
        fill: TEXT,
        fontSize: ACTIVITY_LABEL_FONT_SIZE,
        lineHeight: ACTIVITY_LABEL_LINE_HEIGHT,
        textWrap: {
            width: -ACTIVITY_LABEL_INSET,
            height: ACTIVITY_LABEL_LINE_HEIGHT * ACTIVITY_LABEL_MAX_LINES,
            ellipsis: true,
        },
    };

    return {
        body: {
            fill: expanded ? CONTAINER_SURFACE : SURFACE,
            stroke: element.kind === 'unknown' ? MUTED : STROKE,
            // A call activity's thick border is BPMN's way of saying "this runs another process".
            strokeWidth: element.elementType === 'callActivity' ? 3.5 : 1.5,
            strokeDasharray: element.kind === 'unknown'
                ? '5 4'
                // An event subprocess is drawn with a dotted border, expanded or collapsed.
                : element.isEventSubProcess ? '2 3' : null,
        },
        // BPMN's double border for a transaction subprocess.
        innerBorder: { display: element.isTransaction ? 'block' : 'none' },
        typeMarker: typeGlyph == null
            ? { display: 'none' }
            : {
                display: 'block',
                d: typeGlyph.d,
                fill: typeGlyph.fill === 'solid' ? STROKE : 'none',
                stroke: typeGlyph.fill === 'solid' ? 'none' : STROKE,
                strokeWidth: 1.2,
                refX: 14,
                refY: 14,
            },
        // An expanded subprocess is a container: its name belongs in the corner, not written across
        // the middle of the elements it holds.
        label: expanded
            ? { ...label, fontWeight: 600, textAnchor: 'start', textVerticalAnchor: 'top', refX: 10, refY: 8 }
            : {
                ...label,
                fontWeight: 500,
                textAnchor: 'middle',
                textVerticalAnchor: 'middle',
                refX: '50%',
                refY: '50%',
                // Lifted so the bound activity's name has room underneath it.
                refY2: element.binding == null ? 0 : -10,
            },
        subLabel: subLabelAttrs(element, expanded),
        ...markerAttrs(markers),
    };
}

/**
 * Whether a subprocess is drawn as a container rather than as a collapsed box.
 *
 * `isExpanded` is the document's own answer and is used whenever the document gives one. When it
 * does not -- a document with no DI at all -- a subprocess whose body has elements is expanded,
 * because that is what the fallback layout did with it: it sized the subprocess to hold its
 * children and placed them inside. Drawing a collapse marker over them would describe a diagram
 * other than the one on screen.
 */
function isExpandedContainer(element: BpmnViewElement, context: BuildContext): boolean {
    if (element.isExpanded != null) return element.isExpanded;

    return element.childScopeId != null && context.scopeIdsWithElements.has(element.childScopeId);
}

/** The second line inside a task: what Elsa actually runs, or why nothing does. */
function subLabelAttrs(element: BpmnViewElement, expanded: boolean): Record<string, any> {
    const binding = element.binding;

    if (expanded || binding == null) return { display: 'none', text: '' };

    const text = binding.state === 'bound'
        ? binding.displayName ?? ''
        : binding.state === 'unbound' ? 'Not bound' : 'Binding not resolved';

    if (text.length === 0) return { display: 'none', text: '' };

    return {
        display: 'block',
        text,
        fill: binding.state === 'bound' ? MUTED : UNBOUND,
        fontSize: 10,
        fontStyle: binding.state === 'bound' ? 'normal' : 'italic',
        textAnchor: 'middle',
        textVerticalAnchor: 'top',
        refX: '50%',
        refY: '50%',
        refY2: 10,
        textWrap: { width: -16, height: 24, ellipsis: true },
    };
}

// How a task's own name is typeset inside its box.
const ACTIVITY_LABEL_FONT_SIZE = 11;
/** How much of the box's width the name may not use, so it does not touch the border. */
const ACTIVITY_LABEL_INSET = 16;
const ACTIVITY_LABEL_MAX_LINES = 2;
/**
 * Stated rather than left to X6 to derive.
 *
 * X6 turns `textWrap.height` into a line budget by dividing it by a line height, and works that out
 * from the font size only when one is passed under the key it reads -- which it is not, so it falls
 * back to 14px and a 20px line whatever the label's actual size. Setting `lineHeight` explicitly on
 * the label makes the budget mean what it says; without it a two-line name silently comes out as
 * one line with its second half missing.
 */
const ACTIVITY_LABEL_LINE_HEIGHT = 15;

/**
 * A rough average character width, as a fraction of the font size, for the sans-serif faces
 * MudBlazor themes use. Deliberately a little generous: a name that wraps one word early reads
 * perfectly well, a name that spills out of its box does not.
 */
const AVERAGE_CHARACTER_WIDTH_RATIO = 0.5;

/**
 * Breaks a label on word boundaries, and truncates it here rather than leaving that to X6.
 *
 * Two things make this necessary. X6's `textWrap` splits an over-long line by *character* count, so
 * "Announce Shipment" in a 100-wide task comes out as "Announce Shipm" / "ent", and it does not
 * expose the `breakWord` option that would stop it. And when a wrapped text runs past the line
 * budget X6 drops the overflowing lines outright -- its ellipsis only applies to text it wrapped
 * itself -- so "Apply Discount Rules" would silently lose "Rules". Doing both here means a name is
 * either shown whole or shown to have been shortened.
 *
 * A single word wider than the box is still left to X6, which is the one case where breaking
 * through a word is the only thing to do.
 */
function wrapOnWords(text: string, width: number, fontSize: number, maxLines: number): string {
    const perLine = Math.max(Math.floor(width / (fontSize * AVERAGE_CHARACTER_WIDTH_RATIO)), 1);
    const lines: string[] = [];
    let line = '';

    for (const word of text.split(/\s+/).filter(candidate => candidate.length > 0)) {
        const combined = line.length === 0 ? word : `${line} ${word}`;

        if (line.length === 0 || combined.length <= perLine) line = combined;
        else {
            lines.push(line);
            line = word;
        }
    }

    if (line.length > 0) lines.push(line);
    if (lines.length <= maxLines) return lines.join('\n');

    const kept = lines.slice(0, maxLines - 1);
    const last = lines.slice(maxLines - 1).join(' ');

    kept.push(`${last.slice(0, Math.max(perLine - 1, 1)).trimEnd()}\u2026`);

    return kept.join('\n');
}

function activityMarkers(element: BpmnViewElement, expanded: boolean): readonly Glyph[] {
    const markers: Glyph[] = [];

    if (element.childScopeId != null && !expanded) markers.push(ACTIVITY_MARKER_GLYPHS.collapsed);

    if (element.loopCharacteristics != null) {
        markers.push(element.loopCharacteristics.isSequential
            ? ACTIVITY_MARKER_GLYPHS.multiInstanceSequential
            : ACTIVITY_MARKER_GLYPHS.multiInstanceParallel);
    }

    if (element.isForCompensation) markers.push(ACTIVITY_MARKER_GLYPHS.compensation);

    return markers;
}

/** The marker row along the bottom edge, centred, with a fixed number of slots in the shape. */
const MARKER_SLOTS = ['markerA', 'markerB', 'markerC'] as const;
const MARKER_SPACING = 18;

function markerAttrs(markers: readonly Glyph[]): Record<string, any> {
    const shown = markers.slice(0, MARKER_SLOTS.length);
    const attrs: Record<string, any> = {};

    MARKER_SLOTS.forEach((slot, index) => {
        const glyph = shown[index];

        if (glyph == null) {
            attrs[slot] = { display: 'none' };
            return;
        }

        attrs[slot] = {
            display: 'block',
            d: glyph.d,
            fill: glyph.fill === 'solid' ? STROKE : 'none',
            stroke: glyph.fill === 'solid' ? 'none' : STROKE,
            strokeWidth: 1.2,
            refX: '50%',
            refX2: Math.round((index - (shown.length - 1) / 2) * MARKER_SPACING),
            refY: '100%',
            refY2: -11,
        };
    });

    return attrs;
}

// -- labels ------------------------------------------------------------------------------------

/**
 * The label of an event or gateway, which BPMN draws outside the shape.
 *
 * When the document says where the label goes, it is put there. Those coordinates are absolute in
 * the plane, so they are re-expressed relative to the node and rounded: X6 reads a `refX` strictly
 * between 0 and 1 as a fraction of the node's width rather than as an offset in pixels, and an
 * integer can never fall into that range. The document's own label width is used as the wrap
 * width, which is how the diagram keeps the line breaks its author chose.
 */
function externalLabelAttrs(element: BpmnViewElement): Record<string, any> {
    const text = element.name ?? '';

    if (text.length === 0) return { display: 'none', text: '' };

    const common = {
        display: 'block',
        text,
        fill: TEXT,
        fontSize: 11,
        textAnchor: 'middle',
    };
    const label = element.labelGeometry;

    if (label == null) {
        return {
            ...common,
            textVerticalAnchor: 'top',
            refX: '50%',
            refY: '100%',
            refY2: 6,
            textWrap: { width: 120, height: 32, ellipsis: true },
        };
    }

    return {
        ...common,
        textVerticalAnchor: 'middle',
        refX: Math.round(label.x - element.geometry.x + label.width / 2),
        refY: Math.round(label.y - element.geometry.y + label.height / 2),
        textWrap: { width: Math.max(Math.round(label.width), 60), height: 40, ellipsis: true },
    };
}

// -- stats -------------------------------------------------------------------------------------

/**
 * The attributes that draw -- or clear -- the instance-state badge on one element node.
 *
 * Exported because `updateBpmnElementStats` re-applies them to a live node, and the two paths have
 * to agree: a badge built at load time and a badge built by a later update must be the same badge
 * for the same stats, and a null badge has to actively clear what is on screen rather than leave
 * the previous one there. A stale fault is worse than no fault at all.
 */
export function statsBadgeAttrs(badge: BpmnStatsBadge | null): Record<string, any> {
    if (badge == null) {
        return {
            statsBadge: { display: 'none' },
            statsLabel: { display: 'none', text: '' },
            statsBadgeGroup: { 'aria-label': null },
            statsBadgeTitle: { text: '' },
        };
    }

    return {
        statsBadge: {
            display: 'block',
            fill: BADGE_SURFACE_BY_TONE[badge.tone],
            stroke: SURFACE,
            strokeWidth: 1.5,
        },
        statsLabel: {
            display: 'block',
            text: badge.count == null ? '' : `${badge.count}`,
            fill: BADGE_TEXT_BY_TONE[badge.tone],
        },
        // `<title>` is the SVG tooltip, and `aria-label` says the same thing to a screen reader --
        // between them the badge's state is not colour alone.
        statsBadgeGroup: { 'aria-label': badge.title },
        statsBadgeTitle: { text: badge.title },
    };
}

// ---------------------------------------------------------------------------------------------
// Containers
// ---------------------------------------------------------------------------------------------

function nodeForPool(pool: BpmnViewPool): Node.Metadata | null {
    if (pool.geometry == null) return null;

    return containerNode(
        `${BPMN_POOL_CELL_PREFIX}${pool.id}`,
        BPMN_POOL_SHAPE,
        POOL_Z_INDEX,
        pool.geometry,
        pool.name,
        pool.isHorizontal ?? true,
        { cellKind: 'pool', id: pool.id, name: pool.name });
}

function nodeForLane(lane: BpmnViewLane): Node.Metadata | null {
    if (lane.geometry == null) return null;

    return containerNode(
        `${BPMN_LANE_CELL_PREFIX}${lane.id}`,
        BPMN_LANE_SHAPE,
        LANE_Z_INDEX,
        lane.geometry,
        lane.name,
        lane.isHorizontal ?? true,
        { cellKind: 'lane', id: lane.id, name: lane.name });
}

const CONTAINER_HEADER_SIZE = 30;

function containerNode(
    id: string,
    shape: string,
    zIndex: number,
    geometry: { readonly x: number; readonly y: number; readonly width: number; readonly height: number },
    name: string | null,
    isHorizontal: boolean,
    data: BpmnContainerCellData): Node.Metadata {
    return {
        id,
        shape,
        x: geometry.x,
        y: geometry.y,
        width: geometry.width,
        height: geometry.height,
        zIndex,
        data,
        attrs: {
            body: { fill: CONTAINER_SURFACE, stroke: CONTAINER_STROKE, strokeWidth: 1.5 },
            header: isHorizontal
                ? { x: 0, y: 0, width: CONTAINER_HEADER_SIZE, refHeight: '100%', fill: HEADER_SURFACE, stroke: CONTAINER_STROKE, strokeWidth: 1.5 }
                : { x: 0, y: 0, refWidth: '100%', height: CONTAINER_HEADER_SIZE, fill: HEADER_SURFACE, stroke: CONTAINER_STROKE, strokeWidth: 1.5 },
            label: {
                text: name ?? '',
                fill: TEXT,
                fontSize: 12,
                fontWeight: 600,
                textAnchor: 'middle',
                textVerticalAnchor: 'middle',
                ...(isHorizontal
                    ? { transform: 'rotate(-90)', refX: CONTAINER_HEADER_SIZE / 2, refY: '50%' }
                    : { refX: '50%', refY: CONTAINER_HEADER_SIZE / 2 }),
            },
        },
    };
}

// ---------------------------------------------------------------------------------------------
// Edges
// ---------------------------------------------------------------------------------------------

/**
 * One sequence flow, drawn through the document's own waypoints.
 *
 * The ends are pinned to the exact first and last waypoint through the `bpmnWaypoint` anchor, which
 * expresses each as an offset from the node's own rectangle. So the line is the author's line, to
 * the pixel, and it still belongs to the two nodes it joins rather than floating free of them. A
 * flow the document draws no edge for -- a fallback layout, or partial DI -- falls back to X6's own
 * centre-to-boundary routing, which reads as a plain straight line between two shapes rather than
 * as something pretending to be authored geometry.
 */
function edgeForFlow(flow: BpmnViewFlow, context: BuildContext): Edge.Metadata {
    const data: BpmnFlowCellData = {
        cellKind: 'flow',
        id: flow.id,
        scopeId: flow.scopeId,
        sourceElementId: flow.sourceElementId,
        targetElementId: flow.targetElementId,
        isDefault: flow.isDefault,
        conditionOutcome: flow.conditionOutcome,
        stats: flow.stats,
    };
    const source = context.elementsByScopedId.get(scopedKey(flow.scopeId, flow.sourceElementId)) ?? null;
    const target = context.elementsByScopedId.get(scopedKey(flow.scopeId, flow.targetElementId)) ?? null;
    const waypoints = flow.waypoints;

    return {
        id: flow.id,
        shape: BPMN_FLOW_SHAPE,
        zIndex: flowZIndex(depthOf(context, flow.scopeId)),
        source: terminal(flow.sourceElementId, source, waypoints[0]),
        target: terminal(flow.targetElementId, target, waypoints[waypoints.length - 1]),
        vertices: waypoints.length > 2 ? waypoints.slice(1, -1).map(point => ({ x: point.x, y: point.y })) : [],
        attrs: { line: { ...flowLineAttrs(flow, source), ...flowTakenLineAttrs(flow.stats) } },
        labels: flow.name == null || flow.name.length === 0 ? [] : [edgeLabel(flow.name)],
        data,
    };
}

function edgeForAssociation(
    sourceElementId: string,
    targetElementId: string,
    scopeId: string,
    context: BuildContext): Edge.Metadata {
    const data: BpmnFlowCellData = {
        cellKind: 'association',
        id: `${sourceElementId}->${targetElementId}`,
        scopeId,
        sourceElementId,
        targetElementId,
        isDefault: false,
        conditionOutcome: null,
        stats: null,
    };

    return {
        id: `${BPMN_ASSOCIATION_CELL_PREFIX}${sourceElementId}:${targetElementId}`,
        shape: BPMN_ASSOCIATION_SHAPE,
        zIndex: flowZIndex(depthOf(context, scopeId)),
        source: { cell: sourceElementId },
        target: { cell: targetElementId },
        data,
    };
}

type EdgeTerminal = NonNullable<Edge.Metadata['source']>;

function terminal(
    elementId: string,
    element: BpmnViewElement | null,
    waypoint: { readonly x: number; readonly y: number } | undefined): EdgeTerminal {
    if (waypoint == null || element == null) return { cell: elementId };

    return {
        cell: elementId,
        anchor: {
            name: BPMN_WAYPOINT_ANCHOR,
            args: { dx: waypoint.x - element.geometry.x, dy: waypoint.y - element.geometry.y },
        },
        connectionPoint: 'anchor',
    };
}

/**
 * The line's own markers.
 *
 * The slash on a default flow is drawn wherever the document declares one. The diamond on a
 * conditional flow is drawn only where BPMN puts one -- on a flow leaving an *activity* -- because
 * a gateway's outgoing flows are conditional by construction and no modeller decorates them. The
 * two rules are separate on purpose: a default flow leaving a gateway is the common case and keeps
 * its marker.
 */
function flowLineAttrs(flow: BpmnViewFlow, source: BpmnViewElement | null): Record<string, any> {
    const attrs: Record<string, any> = {
        stroke: EDGE,
        strokeWidth: 1.5,
        targetMarker: { name: 'block', width: 10, height: 8 },
        sourceMarker: null,
    };

    if (flow.isDefault) {
        attrs.sourceMarker = { name: 'path', d: 'M 4 -5 L 10 5', fill: 'none', stroke: EDGE, strokeWidth: 1.5 };
    } else if (flow.conditionOutcome != null && source != null && source.kind !== 'gateway') {
        attrs.sourceMarker = { name: 'diamond', width: 16, height: 10, fill: SURFACE, stroke: EDGE, strokeWidth: 1.5 };
    }

    return attrs;
}

/**
 * The line-only style override for whether a sequence flow has been taken, reusing the "completed"
 * badge tone -- the same colour a node turns once its own token count says it is done -- so a taken
 * flow reads as part of the same visual language rather than inventing a second one.
 *
 * Always returns both properties, never a partial object: a flow that stops being taken (the map no
 * longer mentions it) must fall back to the plain, untaken line exactly as loudly as one that starts
 * being taken lights up, since {@link updateBpmnElementStats} merges this into the edge's existing
 * `line` attrs rather than replacing them wholesale.
 */
export function flowTakenLineAttrs(stats: BpmnElementStats | null | undefined): Record<string, any> {
    const taken = isBpmnFlowTaken(stats);

    return {
        stroke: taken ? BADGE_SURFACE_BY_TONE.completed : EDGE,
        strokeWidth: taken ? 2.5 : 1.5,
    };
}

function edgeLabel(text: string): Record<string, any> {
    return {
        position: { distance: 0.5 },
        attrs: {
            text: { text, fill: MUTED, fontSize: 11, textAnchor: 'middle', textVerticalAnchor: 'middle' },
            rect: { fill: SURFACE, stroke: 'none', rx: 2, ry: 2 },
        },
    };
}

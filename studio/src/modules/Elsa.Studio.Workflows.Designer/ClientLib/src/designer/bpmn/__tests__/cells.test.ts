/**
 * W9c's gate: run every W9a rendering fixture through the X6 mapping and check what came out.
 *
 * The mapping is a pure function precisely so this can be done without a browser. What is asserted
 * is the correspondence between the view model and the cell set -- a node per element, an edge per
 * flow, a boundary event that still names its host, a default flow that still carries its marker --
 * because that correspondence is the one thing a screenshot cannot check and the one thing that,
 * when it breaks, produces a diagram that looks entirely reasonable and is missing something.
 */
import type { Edge } from '@antv/x6';
import { describe, expect, it } from 'vitest';
import { buildBpmnViewModel } from '../../../bpmn';
import type { BpmnActivity, BpmnViewModel } from '../../../bpmn';
import { FIXTURE_NAMES, loadFixture, type FixtureName } from '../../../bpmn/__tests__/fixtures';
import { buildBpmnX6Cells, type BpmnElementCellData, type BpmnFlowCellData, type BpmnX6Cells } from '../cells';
import { ACTIVITY_MARKER_GLYPHS } from '../glyphs';
import {
    BPMN_ACTIVITY_SHAPE,
    BPMN_ASSOCIATION_SHAPE,
    BPMN_EVENT_SHAPE,
    BPMN_FLOW_SHAPE,
    BPMN_GATEWAY_SHAPE,
    BPMN_LANE_CELL_PREFIX,
    BPMN_POOL_CELL_PREFIX,
    BPMN_WAYPOINT_ANCHOR,
} from '../constants';

interface CellExpectation {
    /** Element count, from the W9a fixture expectations, restated so a drift in either shows up. */
    readonly elements: number;
    readonly flows: number;
    readonly resolvedAssociations: number;
    readonly placedLanes: number;
    readonly placedPools: number;
    readonly boundaryEvents: number;
}

const EXPECTATIONS: Record<FixtureName, CellExpectation> = {
    'camunda-order-process': { elements: 3, flows: 2, resolvedAssociations: 0, placedLanes: 0, placedPools: 0, boundaryEvents: 0 },
    'gateway-routing': { elements: 11, flows: 13, resolvedAssociations: 0, placedLanes: 0, placedPools: 0, boundaryEvents: 0 },
    'non-interrupting-error-event-subprocess': { elements: 3, flows: 2, resolvedAssociations: 0, placedLanes: 0, placedPools: 0, boundaryEvents: 0 },
    'publish-gate-process': { elements: 3, flows: 2, resolvedAssociations: 0, placedLanes: 0, placedPools: 0, boundaryEvents: 0 },
    'subprocess-boundary-events': { elements: 16, flows: 11, resolvedAssociations: 0, placedLanes: 0, placedPools: 0, boundaryEvents: 3 },
    'task-kinds-and-lanes': { elements: 11, flows: 10, resolvedAssociations: 0, placedLanes: 2, placedPools: 1, boundaryEvents: 0 },
    'transaction-compensation': { elements: 13, flows: 8, resolvedAssociations: 1, placedLanes: 0, placedPools: 0, boundaryEvents: 2 },
};

const SHAPE_BY_KIND: Readonly<Record<string, string>> = {
    event: BPMN_EVENT_SHAPE,
    gateway: BPMN_GATEWAY_SHAPE,
    task: BPMN_ACTIVITY_SHAPE,
    callActivity: BPMN_ACTIVITY_SHAPE,
    subProcess: BPMN_ACTIVITY_SHAPE,
    unknown: BPMN_ACTIVITY_SHAPE,
};

describe.each(FIXTURE_NAMES)('%s', name => {
    const model = build(name);
    const cells = buildBpmnX6Cells(model);
    const expectation = EXPECTATIONS[name];
    const nodesById = new Map(cells.nodes.map(node => [node.id!, node]));
    const edgesById = new Map(cells.edges.map(edge => [edge.id!, edge]));

    it('agrees with the view model about how much there is to draw', () => {
        // Both directions: the counts the view model reports, and the counts W9a's own fixture
        // expectations state. A change that quietly loses an element fails whichever way it came in.
        expect(model.elements).toHaveLength(expectation.elements);
        expect(model.flows).toHaveLength(expectation.flows);

        expect(cells.nodes).toHaveLength(expectation.elements + expectation.placedLanes + expectation.placedPools);
        expect(cells.edges).toHaveLength(expectation.flows + expectation.resolvedAssociations);
        expect(cells.undrawn).toEqual([]);
        expect(cells.collisions).toEqual([]);
    });

    it('gives every cell an id of its own', () => {
        const ids = [...cells.nodes, ...cells.edges].map(cell => cell.id);

        // X6 keys its model by id and a repeat replaces the cell already there, so a duplicate is a
        // silently missing shape rather than an error.
        expect(new Set(ids).size).toBe(ids.length);
        expect(ids.every(id => typeof id === 'string' && id.length > 0)).toBe(true);
    });

    it('draws one node per element, at the geometry the view model resolved', () => {
        for (const element of model.elements) {
            const node = nodesById.get(element.id);

            expect(node, element.id).toBeDefined();
            expect(node!.shape, element.id).toBe(SHAPE_BY_KIND[element.kind]);
            expect({ x: node!.x, y: node!.y, width: node!.width, height: node!.height }, element.id).toEqual({
                x: element.geometry.x,
                y: element.geometry.y,
                width: element.geometry.width,
                height: element.geometry.height,
            });
        }
    });

    it('never parents one node inside another', () => {
        // The embedding trap, both halves of it: a boundary event embedded in its host breaks
        // hit-testing on the host's border, and a lane embedding its contents would make the canvas
        // assert a containment the BPMN document does not have.
        for (const node of cells.nodes) {
            expect((node as any).parent, `${node.id} parent`).toBeUndefined();
            expect((node as any).children, `${node.id} children`).toBeUndefined();
        }
    });

    // What .NET is told on selection: the "Performed by" section decides from this alone whether to
    // offer the activity picker, so it has to be exactly the view model's own classification.
    it('carries each element\'s binding kind onto its node, exactly as the view model classified it', () => {
        for (const element of model.elements) {
            const data = nodesById.get(element.id)!.data as BpmnElementCellData;

            expect(data.bindingKind, element.id).toBe(element.binding?.kind ?? null);
        }
    });

    it('keeps every boundary event attached to a host that is on the canvas', () => {
        const boundaries = model.elements.filter(element => element.boundary != null);

        expect(boundaries).toHaveLength(expectation.boundaryEvents);

        for (const boundary of boundaries) {
            const node = nodesById.get(boundary.id)!;
            const data = node.data as BpmnElementCellData;
            const host = nodesById.get(boundary.boundary!.hostElementId);

            expect(data.boundaryHostElementId, boundary.id).toBe(boundary.boundary!.hostElementId);
            expect(host, `${boundary.id} host`).toBeDefined();
            // In front of the host, so a click on the shared border reaches the boundary event.
            expect(node.zIndex!, `${boundary.id} z-index`).toBeGreaterThan(host!.zIndex!);
        }
    });

    it('draws one edge per flow, linked to the two elements it joins', () => {
        for (const flow of model.flows) {
            const edge = edgesById.get(flow.id);

            expect(edge, flow.id).toBeDefined();
            expect(terminalCell(edge!.source), `${flow.id} source`).toBe(flow.sourceElementId);
            expect(terminalCell(edge!.target), `${flow.id} target`).toBe(flow.targetElementId);
            expect(edge!.shape, flow.id).toBe(BPMN_FLOW_SHAPE);
        }
    });

    it('draws a flow through the document\'s own waypoints, end to end', () => {
        for (const flow of model.flows) {
            const edge = edgesById.get(flow.id)!;

            if (flow.waypoints.length === 0) {
                // Nothing to be verbatim about: the edge is linked to its nodes and X6 routes it.
                expect(anchorArgs(edge.source), flow.id).toBeNull();
                expect(edge.vertices, flow.id).toEqual([]);
                continue;
            }

            const source = model.elements.find(element => element.scopeId === flow.scopeId && element.id === flow.sourceElementId)!;
            const target = model.elements.find(element => element.scopeId === flow.scopeId && element.id === flow.targetElementId)!;
            const first = flow.waypoints[0];
            const last = flow.waypoints[flow.waypoints.length - 1];

            expect(anchorArgs(edge.source), `${flow.id} source anchor`)
                .toEqual({ dx: first.x - source.geometry.x, dy: first.y - source.geometry.y });
            expect(anchorArgs(edge.target), `${flow.id} target anchor`)
                .toEqual({ dx: last.x - target.geometry.x, dy: last.y - target.geometry.y });
            expect(edge.vertices, `${flow.id} vertices`).toEqual(flow.waypoints.slice(1, -1).map(point => ({ x: point.x, y: point.y })));
        }
    });

    it('marks every default flow the document declares', () => {
        for (const flow of model.flows) {
            const marker = sourceMarker(edgesById.get(flow.id)!);

            if (flow.isDefault) expect(marker?.name, `${flow.id}`).toBe('path');
            else expect(marker?.name, `${flow.id}`).not.toBe('path');
        }
    });

    it('draws each lane and pool the document places, behind everything in it', () => {
        const elementZ = Math.min(...model.elements.map(element => nodesById.get(element.id)!.zIndex!));

        for (const lane of model.lanes.filter(candidate => candidate.geometry != null)) {
            const node = nodesById.get(`${BPMN_LANE_CELL_PREFIX}${lane.id}`);

            expect(node, lane.id).toBeDefined();
            expect(node!.zIndex!, lane.id).toBeLessThan(elementZ);
        }

        for (const pool of model.pools.filter(candidate => candidate.geometry != null)) {
            const node = nodesById.get(`${BPMN_POOL_CELL_PREFIX}${pool.id}`);

            expect(node, pool.id).toBeDefined();
            expect(node!.zIndex!, pool.id).toBeLessThan(elementZ);
        }
    });

    it('draws a nested scope in front of the subprocess that contains it', () => {
        for (const scope of model.scopes.filter(candidate => candidate.hostElementId != null)) {
            const host = nodesById.get(scope.hostElementId!);
            const contained = model.elements.filter(element => element.scopeId === scope.id);

            if (host == null || contained.length === 0) continue;

            for (const element of contained) {
                expect(nodesById.get(element.id)!.zIndex!, `${element.id} over ${scope.hostElementId}`)
                    .toBeGreaterThan(host.zIndex!);
            }
        }
    });

    it('draws a scope\'s flows behind that scope\'s elements', () => {
        for (const flow of model.flows) {
            const edge = edgesById.get(flow.id)!;
            const source = nodesById.get(flow.sourceElementId)!;

            expect(edge.zIndex!, flow.id).toBeLessThan(source.zIndex!);
        }
    });
});

describe('fallback layouts', () => {
    it('produce a complete cell set even though nothing in the document is placed', () => {
        for (const name of FIXTURE_NAMES) {
            const model = build(name);

            if (model.layout.source !== 'fallback') continue;

            const cells = buildBpmnX6Cells(model);

            expect(cells.nodes, name).toHaveLength(model.elements.length);
            expect(cells.edges, name).toHaveLength(model.flows.length);
            expect(cells.nodes.every(node => Number.isFinite(node.x) && (node.width ?? 0) > 0), name).toBe(true);
        }

        // Guards the guard: at least one fixture must actually reach the branch above.
        expect(FIXTURE_NAMES.filter(name => build(name).layout.source === 'fallback').length).toBeGreaterThan(0);
    });
});

describe('compensation associations', () => {
    it('are drawn as their own dashed edge from the boundary event to the handler', () => {
        const model = build('transaction-compensation');
        const cells = buildBpmnX6Cells(model);
        const association = model.associations[0];
        const edge = cells.edges.find(candidate => candidate.shape === BPMN_ASSOCIATION_SHAPE);

        expect(association).toBeDefined();
        expect(edge, 'association edge').toBeDefined();
        expect(terminalCell(edge!.source)).toBe(association.sourceElementId);
        expect(terminalCell(edge!.target)).toBe(association.targetElementId);
        expect((edge!.data as BpmnFlowCellData).cellKind).toBe('association');
    });
});

describe('conditional flow markers', () => {
    // No fixture carries a conditional flow out of an activity, so the rule -- diamond on a flow
    // leaving an activity, nothing on one leaving a gateway, which is what every BPMN modeller
    // draws -- is checked against a view model edited to produce both cases from one document.
    const model = build('gateway-routing');
    const fromGateway = model.flows.find(flow => flow.conditionOutcome != null)!;

    it('leaves a gateway\'s conditional flows undecorated', () => {
        const edge = buildBpmnX6Cells(model).edges.find(candidate => candidate.id === fromGateway.id)!;

        expect(model.elements.find(element => element.id === fromGateway.sourceElementId)!.kind).toBe('gateway');
        expect(sourceMarker(edge)).toBeNull();
    });

    it('puts a diamond on a conditional flow leaving an activity', () => {
        const fromTask = model.flows.find(flow => model.elements.some(element => element.id === flow.sourceElementId && element.kind === 'task'))!;
        const edited: BpmnViewModel = {
            ...model,
            flows: model.flows.map(flow => flow.id === fromTask.id ? { ...flow, conditionOutcome: 'Approved' } : flow),
        };
        const edge = buildBpmnX6Cells(edited).edges.find(candidate => candidate.id === fromTask.id)!;

        expect(sourceMarker(edge)?.name).toBe('diamond');
    });
});

describe('activity markers', () => {
    const model = build('subprocess-boundary-events');

    it('marks a multi-instance activity with the parallel bars the document implies', () => {
        const node = nodeOf(buildBpmnX6Cells(model), 'Fulfil');

        expect(model.elements.find(element => element.id === 'Fulfil')!.loopCharacteristics!.isSequential).toBe(false);
        expect(attr(node, 'markerA').display).toBe('block');
        expect(attr(node, 'markerA').d).toBe(ACTIVITY_MARKER_GLYPHS.multiInstanceParallel.d);
    });

    it('draws an expanded subprocess as a container, with no collapse marker', () => {
        const node = nodeOf(buildBpmnX6Cells(model), 'Fulfil');

        expect(model.elements.find(element => element.id === 'Fulfil')!.isExpanded).toBe(true);
        expect([attr(node, 'markerA').d, attr(node, 'markerB').d])
            .not.toContain(ACTIVITY_MARKER_GLYPHS.collapsed.d);
    });

    it('marks a subprocess the document draws collapsed', () => {
        const collapsed: BpmnViewModel = {
            ...model,
            elements: model.elements.map(element => element.id === 'Fulfil' ? { ...element, isExpanded: false } : element),
        };
        const node = nodeOf(buildBpmnX6Cells(collapsed), 'Fulfil');

        expect(attr(node, 'markerA').d).toBe(ACTIVITY_MARKER_GLYPHS.collapsed.d);
        // Collapsed, so the name goes back to the middle of the box rather than the top corner.
        expect(attr(node, 'label').textAnchor).toBe('middle');
    });

    it('marks a compensation handler', () => {
        const compensation = build('transaction-compensation');
        const handler = compensation.elements.find(element => element.isForCompensation)!;
        const node = nodeOf(buildBpmnX6Cells(compensation), handler.id);

        expect(attr(node, 'markerA').d).toBe(ACTIVITY_MARKER_GLYPHS.compensation.d);
    });

    it('draws BPMN\'s double border on a transaction subprocess, and on nothing else', () => {
        const compensation = build('transaction-compensation');
        const cells = buildBpmnX6Cells(compensation);
        const transaction = compensation.elements.find(element => element.isTransaction)!;

        expect(attr(nodeOf(cells, transaction.id), 'innerBorder').display).toBe('block');

        for (const element of compensation.elements.filter(candidate => !candidate.isTransaction && candidate.kind !== 'event' && candidate.kind !== 'gateway')) {
            expect(attr(nodeOf(cells, element.id), 'innerBorder').display, element.id).toBe('none');
        }
    });
});

describe('activity labels', () => {
    it('breaks a name between words rather than through one', () => {
        const model = build('task-kinds-and-lanes');
        const node = nodeOf(buildBpmnX6Cells(model), 'SendShipped');
        const lines = (attr(node, 'label').text as string).split('\n');

        // X6's own textWrap splits by character count, which turns "Announce Shipment" into
        // "Announce Shipm" / "ent"; the mapping breaks it first so it cannot.
        expect(model.elements.find(element => element.id === 'SendShipped')!.name).toBe('Announce Shipment');
        expect(lines).toEqual(['Announce', 'Shipment']);
    });

    it('leaves a name that fits on one line alone', () => {
        const model = build('task-kinds-and-lanes');

        expect(attr(nodeOf(buildBpmnX6Cells(model), 'PackBox'), 'label').text).toBe('Pack Box');
        expect(attr(nodeOf(buildBpmnX6Cells(model), 'ServiceWork'), 'label').text).toBe('Reserve Stock');
    });

    it('shortens a name too long for the box instead of losing the end of it', () => {
        // X6 drops the lines past its own budget without an ellipsis when the text it is given is
        // already wrapped, so a long name would come out looking like a shorter, different name.
        const model = build('task-kinds-and-lanes');
        const named: BpmnViewModel = {
            ...model,
            elements: model.elements.map(element => element.id === 'PackBox'
                ? { ...element, name: 'Pack the box and print the customs declaration form' }
                : element),
        };
        const text = attr(nodeOf(buildBpmnX6Cells(named), 'PackBox'), 'label').text as string;

        expect(text.split('\n')).toHaveLength(2);
        expect(text.endsWith('\u2026')).toBe(true);
    });
});

describe('lanes and pools the document does not place', () => {
    it('are reported as undrawn rather than dropped in silence', () => {
        const model = build('task-kinds-and-lanes');
        const edited: BpmnViewModel = {
            ...model,
            lanes: model.lanes.map((lane, index) => index === 0 ? { ...lane, geometry: null } : lane),
        };
        const cells = buildBpmnX6Cells(edited);

        expect(cells.undrawn).toEqual([{
            kind: 'lane',
            id: model.lanes[0].id,
            reason: 'The BPMN source places no shape for this lane.',
        }]);
        expect(cells.nodes).toHaveLength(model.elements.length + model.lanes.length - 1 + model.pools.length);
    });
});

describe('duplicate cell ids', () => {
    // BPMN requires element ids to be document-unique, and the view model already reports a
    // violation as `duplicate-element-id` -- but it still renders both elements, which is what would
    // otherwise hand X6 two cells sharing an id and let it pick a survivor arbitrarily.
    it('keeps the first element with a colliding id and drops the duplicate along with the flow attached to it', () => {
        const fixture = loadFixture('subprocess-boundary-events');
        const activity = structuredClone(fixture.activity) as BpmnActivity;
        const nested = activity.activities!.find(candidate => candidate.process?.processId === 'Fulfil')!;
        const subStart = nested.process!.elements.find(candidate => candidate.elementId === 'Sub_Start')! as { elementId: string };
        const subFlow1 = nested.process!.sequenceFlows!.find(candidate => candidate.flowId === 'Sub_Flow_1')! as { sourceRef: string };

        // 'Start_1' already names the root scope's start event; renaming this nested scope's start
        // event to the same id, and its outgoing flow along with it, reproduces the collision without
        // touching anything the view model itself would refuse to build.
        subStart.elementId = 'Start_1';
        subFlow1.sourceRef = 'Start_1';

        const model = buildBpmnViewModel({ activity, sourceXml: fixture.sourceXml });
        const cells = buildBpmnX6Cells(model);
        const rootStart = model.elements.find(candidate => candidate.scopeId === model.processId && candidate.id === 'Start_1')!;
        const survivors = cells.nodes.filter(node => node.id === 'Start_1');

        expect(survivors).toHaveLength(1);
        expect({ x: survivors[0].x, y: survivors[0].y }).toEqual({ x: rootStart.geometry.x, y: rootStart.geometry.y });

        expect(cells.collisions).toContainEqual(expect.objectContaining({ kind: 'element', id: 'Start_1' }));
        expect(cells.collisions).toContainEqual(expect.objectContaining({ kind: 'flow', id: 'Sub_Flow_1' }));

        // The dropped duplicate's own flow never reaches the canvas, so nothing from the 'Fulfil'
        // scope ends up attached to the root's surviving 'Start_1' node.
        expect(cells.edges.some(edge => edge.id === 'Sub_Flow_1')).toBe(false);

        for (const edge of cells.edges) {
            const data = edge.data as BpmnFlowCellData;

            if (terminalCell(edge.source) === 'Start_1' || terminalCell(edge.target) === 'Start_1') {
                expect(data.scopeId, edge.id as string).toBe(model.processId);
            }
        }
    });
});

function build(name: FixtureName): BpmnViewModel {
    const fixture = loadFixture(name);

    return buildBpmnViewModel({ activity: fixture.activity, sourceXml: fixture.sourceXml });
}

function nodeOf(cells: BpmnX6Cells, id: string) {
    return cells.nodes.find(node => node.id === id)!;
}

function attr(node: { attrs?: unknown }, selector: string): Record<string, any> {
    return (node.attrs as Record<string, Record<string, any>>)[selector] ?? {};
}

function terminalCell(terminal: Edge.Metadata['source']): string | null {
    return (terminal as { cell?: string } | undefined)?.cell ?? null;
}

function anchorArgs(terminal: Edge.Metadata['source']): { dx: number; dy: number } | null {
    const anchor = (terminal as { anchor?: { name?: string; args?: { dx: number; dy: number } } } | undefined)?.anchor;

    if (anchor?.name !== BPMN_WAYPOINT_ANCHOR) return null;

    return anchor.args ?? null;
}

function sourceMarker(edge: Edge.Metadata): { name?: string } | null {
    return (edge.attrs as any)?.line?.sourceMarker ?? null;
}

/**
 * R3b: run every `.bpmn` fixture through the view model and check what came out.
 *
 * These are the assertions each adapter will later be held to as well -- element and flow sets,
 * endpoints, and the fact that every shape the document draws resolves to something the view model
 * exposes -- which is why the expected values here are written out by hand from the documents rather
 * than snapshotted from whatever the builder happened to produce.
 */
import { describe, expect, it } from 'vitest';
import { buildBpmnViewModel } from '../view-model';
import { FIXTURE_NAMES, loadFixture, readDiagramReferences, type FixtureName } from './fixtures';

interface FixtureExpectation {
    /** Scope process ids, root first then depth-first. */
    readonly scopeIds: readonly string[];
    readonly elementCount: number;
    readonly flowCount: number;
    readonly layoutSource: 'document' | 'fallback';
    /**
     * DI edges the document draws that are legitimately not sequence flows. Only associations
     * qualify today: the payload records one as `compensationHandlerElementId` on the boundary
     * event, so it has no id for an edge to be matched by. See ../__fixtures__/README.md.
     */
    readonly unresolvedEdgeIds?: readonly string[];
}

const EXPECTATIONS: Record<FixtureName, FixtureExpectation> = {
    'camunda-order-process': {
        scopeIds: ['order-process'],
        elementCount: 3,
        flowCount: 2,
        layoutSource: 'document',
    },
    'gateway-routing': {
        scopeIds: ['gateway-routing'],
        elementCount: 11,
        flowCount: 13,
        layoutSource: 'document',
    },
    // The reader drops this document's non-interrupting error event subprocess outright, and the
    // document carries no BPMNDI at all -- both asserted, because both are what Studio will see.
    'non-interrupting-error-event-subprocess': {
        scopeIds: ['non-interrupting-error-event-subprocess'],
        elementCount: 3,
        flowCount: 2,
        layoutSource: 'fallback',
    },
    'publish-gate-process': {
        scopeIds: ['publish-gate-process'],
        elementCount: 3,
        flowCount: 2,
        layoutSource: 'fallback',
    },
    'subprocess-boundary-events': {
        scopeIds: ['subprocess-boundary-events', 'Fulfil', 'OnRecall'],
        elementCount: 16,
        flowCount: 11,
        layoutSource: 'document',
    },
    'task-kinds-and-lanes': {
        scopeIds: ['task-kinds-and-lanes'],
        elementCount: 11,
        flowCount: 10,
        layoutSource: 'document',
    },
    'transaction-compensation': {
        scopeIds: ['transaction-compensation', 'BookTrip'],
        elementCount: 13,
        flowCount: 8,
        layoutSource: 'document',
        unresolvedEdgeIds: ['Compensation_Association'],
    },
};

describe.each(FIXTURE_NAMES)('%s', name => {
    const fixture = loadFixture(name);
    const expectation = EXPECTATIONS[name];
    const model = buildBpmnViewModel({ activity: fixture.activity, sourceXml: fixture.sourceXml });

    it('reads the scope tree the payload declares', () => {
        expect(model.scopes.map(scope => scope.id)).toEqual(expectation.scopeIds);
        expect(model.processId).toBe(expectation.scopeIds[0]);
    });

    it('reads every element and flow', () => {
        expect(model.elements).toHaveLength(expectation.elementCount);
        expect(model.flows).toHaveLength(expectation.flowCount);
    });

    it('reports no errors', () => {
        expect(model.diagnostics.filter(diagnostic => diagnostic.severity === 'error')).toEqual([]);
    });

    it('gives every flow endpoints that exist in the flow\'s own scope', () => {
        const elementIdsByScope = new Map<string, Set<string>>();

        for (const element of model.elements) {
            const ids = elementIdsByScope.get(element.scopeId) ?? new Set<string>();

            ids.add(element.id);
            elementIdsByScope.set(element.scopeId, ids);
        }

        for (const flow of model.flows) {
            const ids = elementIdsByScope.get(flow.scopeId) ?? new Set<string>();

            expect(ids.has(flow.sourceElementId), `${flow.id} source ${flow.sourceElementId}`).toBe(true);
            expect(ids.has(flow.targetElementId), `${flow.id} target ${flow.targetElementId}`).toBe(true);
        }
    });

    it('resolves every DI shape to an element, lane or pool', () => {
        const references = readDiagramReferences(fixture.sourceXml);
        const drawable = new Set<string>([
            ...model.elements.map(element => element.id),
            ...model.lanes.map(lane => lane.id),
            ...model.pools.map(pool => pool.id),
        ]);

        expect(references.shapes.filter(id => !drawable.has(id))).toEqual([]);
        expect(model.diagnostics.filter(diagnostic => diagnostic.code === 'unresolved-di-shape')).toEqual([]);
    });

    it('resolves every DI edge to a sequence flow, apart from the documented exceptions', () => {
        const references = readDiagramReferences(fixture.sourceXml);
        const flowIds = new Set(model.flows.map(flow => flow.id));

        expect(references.edges.filter(id => !flowIds.has(id))).toEqual(expectation.unresolvedEdgeIds ?? []);
    });

    it(`lays the diagram out from the ${expectation.layoutSource}`, () => {
        expect(model.layout.source).toBe(expectation.layoutSource);

        // The direction that could be mistaken for success: a fallback layout that quietly claims to
        // be the author's own coordinates, or DI that quietly stops being read. Both are visible only
        // by checking every element agrees with what the diagram as a whole claims.
        for (const element of model.elements) {
            expect(element.geometry.source, element.id).toBe(expectation.layoutSource);
        }

        if (expectation.layoutSource === 'fallback') {
            expect(model.layout.reason).toBeTruthy();
        } else {
            expect(model.layout.reason).toBeNull();
        }
    });

    it('places every element somewhere with a real size', () => {
        for (const element of model.elements) {
            expect(Number.isFinite(element.geometry.x), element.id).toBe(true);
            expect(Number.isFinite(element.geometry.y), element.id).toBe(true);
            expect(element.geometry.width, element.id).toBeGreaterThan(0);
            expect(element.geometry.height, element.id).toBeGreaterThan(0);
        }
    });
});

describe('geometry read straight from BPMN DI', () => {
    it('uses the exact bounds, label bounds and waypoints the document states', () => {
        const fixture = loadFixture('camunda-order-process');
        const model = buildBpmnViewModel({ activity: fixture.activity, sourceXml: fixture.sourceXml });
        const task = model.elements.find(element => element.id === 'NotifyWarehouse')!;

        expect(task.geometry).toEqual({ x: 240, y: 80, width: 100, height: 80, source: 'document' });
        expect(model.elements.find(element => element.id === 'StartEvent_1')!.geometry)
            .toEqual({ x: 152, y: 102, width: 36, height: 36, source: 'document' });
        expect(model.flows.find(flow => flow.id === 'Flow_1')!.waypoints)
            .toEqual([{ x: 188, y: 120 }, { x: 240, y: 120 }]);
    });

    it('reads a label\'s own bounds off the shape and the edge that carry one', () => {
        const fixture = loadFixture('gateway-routing');
        const model = buildBpmnViewModel({ activity: fixture.activity, sourceXml: fixture.sourceXml });

        expect(model.elements.find(element => element.id === 'Decide')!.labelGeometry)
            .toEqual({ x: 338, y: 75, width: 64, height: 14 });
        expect(model.flows.find(flow => flow.id === 'Flow_Decide_Approve')!.labelGeometry)
            .toEqual({ x: 392, y: 62, width: 20, height: 14 });
        // A shape with no BPMNLabel says nothing about where the label goes; it does not report zeroes.
        expect(model.elements.find(element => element.id === 'Fork')!.labelGeometry).toBeNull();
    });

    it('reads the DI flags a renderer needs from the shape', () => {
        const model = build('subprocess-boundary-events');

        expect(model.elements.find(element => element.id === 'Fulfil')!.isExpanded).toBe(true);
        expect(model.elements.find(element => element.id === 'Escalate')!.isExpanded).toBeNull();
        expect(build('gateway-routing').elements.find(element => element.id === 'Decide')!.isMarkerVisible).toBe(true);
    });
});

function build(name: FixtureName) {
    const fixture = loadFixture(name);

    return buildBpmnViewModel({ activity: fixture.activity, sourceXml: fixture.sourceXml });
}

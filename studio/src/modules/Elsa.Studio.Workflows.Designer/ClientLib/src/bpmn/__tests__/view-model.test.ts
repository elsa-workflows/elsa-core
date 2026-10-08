/**
 * What the view model says about a document, kind by kind.
 *
 * The invariant every test here is really about: **the view model reports the document, and reports
 * when it cannot.** Nothing it emits may be indistinguishable from the document having said so --
 * an invented coordinate must be marked as invented, an unbound task must be a state rather than an
 * absence, and a fault in the input must leave a diagnostic behind rather than an empty diagram that
 * looks like an empty process.
 */
import { beforeEach, describe, expect, it } from 'vitest';
import type { BpmnActivity, BpmnActivityDescriptor, BpmnViewElement, BpmnViewModel } from '../model';
import { buildBpmnViewModel } from '../view-model';
import { loadFixture, type FixtureName } from './fixtures';

const WRITE_LINE_DESCRIPTOR: BpmnActivityDescriptor = {
    typeName: 'Elsa.WriteLine',
    version: 1,
    name: 'WriteLine',
    displayName: 'Write Line',
    category: 'Console',
    color: '#fff',
};

function build(name: FixtureName, overrides: { sourceXml?: string | null } = {}): BpmnViewModel {
    const fixture = loadFixture(name);

    return buildBpmnViewModel({
        activity: fixture.activity,
        sourceXml: 'sourceXml' in overrides ? overrides.sourceXml : fixture.sourceXml,
        activityDescriptors: [WRITE_LINE_DESCRIPTOR],
    });
}

function element(model: BpmnViewModel, id: string): BpmnViewElement {
    const found = model.elements.find(candidate => candidate.id === id);

    expect(found, `no element '${id}' in the view model`).toBeDefined();

    return found!;
}

describe('elements', () => {
    it('reads all eight task kinds, the call activity and the subprocess as their own element types', () => {
        const model = build('task-kinds-and-lanes');
        const kinds = new Map(model.elements.map(candidate => [candidate.id, candidate]));

        expect([...kinds.values()].filter(candidate => candidate.kind === 'task').map(candidate => candidate.elementType).sort())
            .toEqual([
                'businessRuleTask',
                'manualTask',
                'receiveTask',
                'scriptTask',
                'sendTask',
                'serviceTask',
                'task',
                'userTask',
            ]);
        expect(element(model, 'ArchiveOrder').kind).toBe('callActivity');
        expect(element(model, 'ArchiveOrder').properties['bpmn.calledElement']).toBe('archive-order-process');
        expect(element(model, 'AwaitConfirm').properties['bpmn.messageName']).toBe('ShipmentConfirmed');
    });

    it('reads all four gateways', () => {
        const model = build('gateway-routing');

        expect(model.elements.filter(candidate => candidate.kind === 'gateway').map(candidate => candidate.elementType).sort())
            .toEqual(['eventBasedGateway', 'exclusiveGateway', 'inclusiveGateway', 'parallelGateway', 'parallelGateway']);
    });

    it('reads start, intermediate and end events with their event definitions', () => {
        const model = build('subprocess-boundary-events');

        expect(element(model, 'Start_1').eventDefinitions).toEqual([]);
        expect(element(model, 'RaiseLate').elementType).toBe('intermediateThrowEvent');
        expect(element(model, 'RaiseLate').eventDefinitions.map(definition => definition.type)).toEqual(['escalation']);
        expect(element(model, 'End_Terminated').eventDefinitions.map(definition => definition.type)).toEqual(['terminate']);
        expect(element(build('gateway-routing'), 'Timeout').eventDefinitions)
            .toEqual([{ type: 'timer', properties: { interval: 'PT10M' } }]);
    });

    it('classifies an element type it has never heard of as unknown rather than dropping it', () => {
        const fixture = loadFixture('camunda-order-process');
        const activity = structuredClone(fixture.activity) as BpmnActivity;

        (activity.process!.elements[0] as { elementType: string }).elementType = 'adHocSubProcess';

        const model = buildBpmnViewModel({ activity, sourceXml: fixture.sourceXml });

        expect(model.elements).toHaveLength(3);
        expect(element(model, 'StartEvent_1').kind).toBe('unknown');
        expect(element(model, 'StartEvent_1').elementType).toBe('adHocSubProcess');
    });
});

describe('boundary events', () => {
    // The dangerous direction here is a non-interrupting boundary event reported as interrupting, or
    // the other way round: both render, both look plausible, and only one is what the document says.
    it('reports interrupting and non-interrupting attachment as the document states each', () => {
        const model = build('subprocess-boundary-events');

        expect(element(model, 'FulfilTimeout').boundary)
            .toEqual({ hostElementId: 'Fulfil', hostResolved: true, interrupting: true });
        expect(element(model, 'FulfilNudge').boundary)
            .toEqual({ hostElementId: 'Fulfil', hostResolved: true, interrupting: false });
        expect(element(model, 'FulfilFailed').boundary)
            .toEqual({ hostElementId: 'Fulfil', hostResolved: true, interrupting: true });
    });

    it('leaves boundary attachment null on everything that is not a boundary event', () => {
        // `cancelActivity` reads `true` on every element in the payload, boundary or not, so exposing
        // the raw flag would make every task look interrupting. Only a boundary event gets one.
        const model = build('subprocess-boundary-events');

        expect(element(model, 'Fulfil').boundary).toBeNull();
        expect(element(model, 'Escalate').boundary).toBeNull();
        expect(element(model, 'Start_1').boundary).toBeNull();
    });

    it('reports a boundary event whose host is not in its scope, and still renders it', () => {
        const fixture = loadFixture('subprocess-boundary-events');
        const activity = structuredClone(fixture.activity) as BpmnActivity;
        const boundary = activity.process!.elements.find(candidate => candidate.elementId === 'FulfilTimeout')!;

        (boundary as { attachedToRef: string }).attachedToRef = 'NoSuchElement';

        const model = buildBpmnViewModel({ activity, sourceXml: fixture.sourceXml });

        expect(element(model, 'FulfilTimeout').boundary)
            .toEqual({ hostElementId: 'NoSuchElement', hostResolved: false, interrupting: true });
        expect(model.diagnostics.map(diagnostic => diagnostic.code)).toContain('unresolved-boundary-host');
    });
});

describe('scopes', () => {
    it('flattens subprocess and event subprocess bodies into scopes with a parent and a host', () => {
        const model = build('subprocess-boundary-events');

        expect(model.scopes.map(scope => [scope.id, scope.parentScopeId, scope.hostElementId, scope.depth])).toEqual([
            ['subprocess-boundary-events', null, null, 0],
            ['Fulfil', 'subprocess-boundary-events', 'Fulfil', 1],
            ['OnRecall', 'subprocess-boundary-events', 'OnRecall', 1],
        ]);
        expect(element(model, 'Fulfil').childScopeId).toBe('Fulfil');
        expect(element(model, 'PickLine').scopeId).toBe('Fulfil');
        expect(element(model, 'PickLine').parentElementId).toBe('Fulfil');
        expect(element(model, 'Start_1').parentElementId).toBeNull();
    });

    it('marks an event subprocess and carries the listener it arms', () => {
        const model = build('subprocess-boundary-events');
        const eventSubProcess = element(model, 'OnRecall');

        expect(eventSubProcess.isEventSubProcess).toBe(true);
        expect(element(model, 'Fulfil').isEventSubProcess).toBe(false);
        expect(eventSubProcess.listenerBinding).toMatchObject({
            state: 'bound',
            bindingRef: 'node-OnRecall-listener',
            activityType: 'Elsa.Event',
        });
        expect(element(model, 'Fulfil').listenerBinding).toBeNull();
    });

    it('reports a listener binding ref that workBindings does not map as unresolved, not silently', () => {
        const fixture = loadFixture('subprocess-boundary-events');
        const activity = structuredClone(fixture.activity) as BpmnActivity;

        delete (activity.workBindings as Record<string, string>)['node-OnRecall-listener'];

        const model = buildBpmnViewModel({ activity, sourceXml: fixture.sourceXml });

        expect(element(model, 'OnRecall').listenerBinding).toMatchObject({
            state: 'unresolved',
            bindingRef: 'node-OnRecall-listener',
            activityId: null,
        });

        const diagnostic = model.diagnostics.find(candidate => candidate.code === 'unresolved-listener-binding')!;

        expect(diagnostic).toMatchObject({ severity: 'error', elementId: 'OnRecall', scopeId: 'subprocess-boundary-events' });
        expect(diagnostic.message).toContain('OnRecall');
        expect(diagnostic.message).toContain('node-OnRecall-listener');
    });

    it('marks a transaction on both the element and the scope it hosts', () => {
        const model = build('transaction-compensation');

        expect(element(model, 'BookTrip').isTransaction).toBe(true);
        expect(model.scopes.find(scope => scope.id === 'BookTrip')!.isTransaction).toBe(true);
        expect(model.scopes.find(scope => scope.id === 'transaction-compensation')!.isTransaction).toBe(false);
    });

    it('reports two distinct nested processes reusing a processId, and still renders the first one', () => {
        const fixture = loadFixture('subprocess-boundary-events');
        const activity = structuredClone(fixture.activity) as BpmnActivity;
        const onRecall = activity.activities!.find(candidate => candidate.id === 'subprocess-boundary-events:node-OnRecall')!;

        // Two distinct payload objects -- `Fulfil`'s and `OnRecall`'s -- now claim the same processId.
        (onRecall.process as { processId: string }).processId = 'Fulfil';

        const model = buildBpmnViewModel({ activity, sourceXml: fixture.sourceXml });
        const diagnostic = model.diagnostics.find(candidate => candidate.code === 'duplicate-process-id')!;

        expect(diagnostic).toMatchObject({ severity: 'error', elementId: 'OnRecall', scopeId: 'subprocess-boundary-events' });
        expect(diagnostic.message).toContain('Fulfil');
        expect(diagnostic.message).toContain('subprocess-boundary-events:node-Fulfil');
        expect(diagnostic.message).toContain('subprocess-boundary-events:node-OnRecall');

        // The first scope to claim the processId is kept, elements and all.
        expect(model.scopes.map(scope => scope.id)).toEqual(['subprocess-boundary-events', 'Fulfil']);
        expect(element(model, 'PickLine').scopeId).toBe('Fulfil');
    });

    it('reads multi-instance markers', () => {
        const model = build('subprocess-boundary-events');

        expect(element(model, 'Fulfil').loopCharacteristics)
            .toEqual({ isSequential: false, collectionVariable: 'orderLines', itemVariable: 'line' });
        expect(element(model, 'Escalate').loopCharacteristics).toBeNull();
    });
});

describe('compensation', () => {
    it('reads the handler, its association, and the cancel events around a transaction', () => {
        const model = build('transaction-compensation');

        expect(element(model, 'RefundCard').isForCompensation).toBe(true);
        expect(element(model, 'ChargeCard').isForCompensation).toBe(false);
        expect(model.associations).toEqual([{
            sourceElementId: 'ChargeCardCompensation',
            targetElementId: 'RefundCard',
            scopeId: 'BookTrip',
            targetResolved: true,
        }]);
        expect(element(model, 'ChargeCardCompensation').eventDefinitions.map(definition => definition.type))
            .toEqual(['compensation']);
        expect(element(model, 'BookTripCancelled').eventDefinitions.map(definition => definition.type))
            .toEqual(['cancel']);
        expect(element(model, 'Tx_Cancel_End').eventDefinitions.map(definition => definition.type))
            .toEqual(['cancel']);
    });

    it('reports a compensation handler that is not in the boundary event\'s own scope', () => {
        const fixture = loadFixture('transaction-compensation');
        const activity = structuredClone(fixture.activity) as BpmnActivity;
        const nested = activity.activities!.find(candidate => candidate.process?.processId === 'BookTrip')!;
        const boundary = nested.process!.elements.find(candidate => candidate.elementId === 'ChargeCardCompensation')!;

        (boundary as { compensationHandlerElementId: string }).compensationHandlerElementId = 'NoSuchHandler';

        const model = buildBpmnViewModel({ activity, sourceXml: fixture.sourceXml });

        expect(model.associations[0]).toMatchObject({ targetElementId: 'NoSuchHandler', targetResolved: false });
        expect(model.diagnostics.map(diagnostic => diagnostic.code)).toContain('unresolved-compensation-handler');
    });
});

describe('flows', () => {
    it('reads condition outcomes and the gateway\'s default flow', () => {
        const model = build('gateway-routing');
        const conditional = model.flows.find(flow => flow.id === 'Flow_Decide_Approve')!;
        const fallbackFlow = model.flows.find(flow => flow.id === 'Flow_Decide_Reject')!;

        expect(conditional).toMatchObject({ conditionOutcome: 'Approved', isDefault: false, name: 'yes' });
        // The payload records the default on the *gateway* (`defaultFlowId`), not on the flow, so a
        // view model that only looked at `BpmnSequenceFlow.isDefault` would silently mark none.
        expect(fallbackFlow).toMatchObject({ conditionOutcome: null, isDefault: true, name: 'otherwise' });
        expect(element(model, 'Decide').defaultFlowId).toBe('Flow_Decide_Reject');
    });

    it('drops a flow with a dangling endpoint and says so, rather than emitting an undrawable edge', () => {
        const fixture = loadFixture('camunda-order-process');
        const activity = structuredClone(fixture.activity) as BpmnActivity;

        (activity.process!.sequenceFlows[0] as { targetRef: string }).targetRef = 'NoSuchElement';

        const model = buildBpmnViewModel({ activity, sourceXml: fixture.sourceXml });

        expect(model.flows.map(flow => flow.id)).toEqual(['Flow_2']);

        const diagnostic = model.diagnostics.find(candidate => candidate.code === 'dangling-flow')!;

        expect(diagnostic.severity).toBe('error');
        expect(diagnostic.message).toContain('NoSuchElement');
    });
});

describe('lanes and pools', () => {
    it('retains lanes and pools with their geometry, and attaches no meaning to membership', () => {
        const model = build('task-kinds-and-lanes');

        expect(model.pools).toEqual([{
            id: 'Participant_Fulfilment',
            name: 'Fulfilment',
            processId: 'task-kinds-and-lanes',
            geometry: { x: 120, y: 60, width: 1400, height: 400, source: 'document' },
            isHorizontal: true,
        }]);
        expect(model.lanes.map(lane => [lane.id, lane.name, lane.poolId, lane.isHorizontal])).toEqual([
            ['Lane_Automated', 'Automated', 'Participant_Fulfilment', true],
            ['Lane_People', 'People', 'Participant_Fulfilment', true],
        ]);
        expect(model.lanes[1].geometry).toEqual({ x: 150, y: 290, width: 1370, height: 170, source: 'document' });
        expect(element(model, 'ReviewOrder').laneId).toBe('Lane_People');
        expect(element(model, 'ServiceWork').laneId).toBe('Lane_Automated');
    });

    it('still gives a lane a pool when the source document is gone, and says the name is unknown', () => {
        // Pools live on the document's collaboration, which the activity payload does not carry at
        // all. Dropping the pool would make the lane look like it belongs to the process itself.
        const model = build('task-kinds-and-lanes', { sourceXml: null });

        expect(model.pools).toEqual([
            { id: 'Participant_Fulfilment', name: null, processId: null, geometry: null, isHorizontal: null },
        ]);
        expect(model.diagnostics.map(diagnostic => diagnostic.code)).toContain('unresolved-pool');
    });
});

describe('binding display', () => {
    it('names a bound activity through workBindings and the descriptor catalogue', () => {
        const model = build('camunda-order-process');

        expect(element(model, 'NotifyWarehouse').binding).toEqual({
            state: 'bound',
            kind: 'unboundTask',
            bindingRef: 'node-NotifyWarehouse',
            activityId: 'order-process:node-NotifyWarehouse',
            activityType: 'Elsa.WriteLine',
            activityName: null,
            displayName: 'Write Line',
            descriptor: WRITE_LINE_DESCRIPTOR,
        });
    });

    it('falls back to the activity type when no descriptor was supplied', () => {
        const fixture = loadFixture('camunda-order-process');
        const model = buildBpmnViewModel({ activity: fixture.activity, sourceXml: fixture.sourceXml });

        expect(element(model, 'NotifyWarehouse').binding).toMatchObject({
            state: 'bound',
            displayName: 'Elsa.WriteLine',
            descriptor: null,
        });
    });

    it('prefers the activity\'s own name over the descriptor\'s display name', () => {
        const fixture = loadFixture('camunda-order-process');
        const activity = structuredClone(fixture.activity) as BpmnActivity;

        (activity.activities![0] as { name: string }).name = 'Tell the warehouse';

        const model = buildBpmnViewModel({
            activity,
            sourceXml: fixture.sourceXml,
            activityDescriptors: [WRITE_LINE_DESCRIPTOR],
        });

        expect(element(model, 'NotifyWarehouse').binding).toMatchObject({ displayName: 'Tell the warehouse' });
    });

    it('leaves binding null for an element that performs no work of its own', () => {
        const model = build('gateway-routing');

        expect(element(model, 'Decide').binding).toBeNull();
        expect(element(model, 'Start_1').binding).toBeNull();
        expect(element(model, 'End_1').binding).toBeNull();
    });

    // An unbound task is a *state*, not an absence: W8 makes it a publish error, so the canvas has to
    // be able to show it. elsa-core's own binder refuses to import such a document, so this is the
    // shape a Studio-side edit (W11/W14) produces -- see ../__fixtures__/README.md.
    it('reports a task that declares no binding ref as unbound, loudly', () => {
        const fixture = loadFixture('camunda-order-process');
        const activity = structuredClone(fixture.activity) as BpmnActivity;
        const task = activity.process!.elements.find(candidate => candidate.elementId === 'NotifyWarehouse')!;

        delete (task as { bindingRef?: string | null }).bindingRef;

        const model = buildBpmnViewModel({ activity, sourceXml: fixture.sourceXml });

        expect(element(model, 'NotifyWarehouse').binding).toEqual({
            state: 'unbound',
            kind: 'unboundTask',
            bindingRef: null,
            activityId: null,
            activityType: null,
            activityName: null,
            displayName: null,
            descriptor: null,
        });

        const diagnostic = model.diagnostics.find(candidate => candidate.code === 'unbound-work')!;

        expect(diagnostic).toMatchObject({ severity: 'warning', elementId: 'NotifyWarehouse', scopeId: 'order-process' });
    });

    it('reports a binding ref that workBindings does not map as unresolved, not as bound to nothing', () => {
        const fixture = loadFixture('camunda-order-process');
        const activity = structuredClone(fixture.activity) as BpmnActivity;

        delete (activity.workBindings as Record<string, string>)['node-NotifyWarehouse'];

        const model = buildBpmnViewModel({ activity, sourceXml: fixture.sourceXml });

        expect(element(model, 'NotifyWarehouse').binding).toMatchObject({
            state: 'unresolved',
            bindingRef: 'node-NotifyWarehouse',
            activityId: null,
            displayName: null,
        });
        expect(model.diagnostics.find(candidate => candidate.code === 'unresolved-binding')!.severity).toBe('error');
    });

    it('reports a binding ref mapped to an activity the scope does not carry as unresolved', () => {
        const fixture = loadFixture('camunda-order-process');
        const activity = structuredClone(fixture.activity) as BpmnActivity;

        (activity.workBindings as Record<string, string>)['node-NotifyWarehouse'] = 'order-process:gone';

        const model = buildBpmnViewModel({ activity, sourceXml: fixture.sourceXml });

        expect(element(model, 'NotifyWarehouse').binding).toMatchObject({
            state: 'unresolved',
            activityId: 'order-process:gone',
        });
    });
});

// Which elements a user binds by hand. The dangerous direction is an automatically bound element that
// looks authored: the "Performed by" section would offer to write an elsa:activityBinding onto it, and
// elsa-core refuses that document at import. The opposite mistake -- an authored task shown as
// automatic -- silently leaves the user with no way to bind it. Both directions are pinned here.
describe('binding kind', () => {
    it('marks every task that resolves no message as authored, and a message send or receive as automatic', () => {
        const model = build('task-kinds-and-lanes');

        for (const id of ['AbstractTask', 'ReviewOrder', 'ServiceWork', 'ScriptWork', 'PackBox', 'DecideRules']) {
            expect(element(model, id).binding?.kind, id).toBe('unboundTask');
        }

        expect(element(model, 'SendShipped').binding?.kind).toBe('automatic');
        expect(element(model, 'AwaitConfirm').binding?.kind).toBe('automatic');
        expect(element(model, 'ArchiveOrder').binding?.kind).toBe('automatic');
    });

    it('marks a send or receive task that names no message as authored, the way the reader binds it', () => {
        const fixture = loadFixture('task-kinds-and-lanes');
        const activity = structuredClone(fixture.activity) as BpmnActivity;

        for (const id of ['SendShipped', 'AwaitConfirm']) {
            const task = activity.process!.elements.find(candidate => candidate.elementId === id)!;
            (task as { properties: Record<string, string> }).properties = {};
        }

        const model = buildBpmnViewModel({ activity, sourceXml: fixture.sourceXml });

        expect(element(model, 'SendShipped').binding?.kind).toBe('unboundTask');
        expect(element(model, 'AwaitConfirm').binding?.kind).toBe('unboundTask');
    });

    it('marks timer and message boundaries, subprocesses and an event subprocess listener as automatic', () => {
        const model = build('subprocess-boundary-events');

        expect(element(model, 'Escalate').binding?.kind).toBe('unboundTask');
        expect(element(model, 'Fulfil').binding?.kind).toBe('automatic');
        expect(element(model, 'FulfilTimeout').binding?.kind).toBe('automatic');
        expect(element(model, 'FulfilNudge').binding?.kind).toBe('automatic');
        expect(element(model, 'OnRecall').listenerBinding?.kind).toBe('automatic');
    });
});

describe('instance state', () => {
    it('keys element stats by BPMN element id, so a gateway can light up too', () => {
        const fixture = loadFixture('gateway-routing');
        const model = buildBpmnViewModel({
            activity: fixture.activity,
            sourceXml: fixture.sourceXml,
            elementStats: { Decide: { started: 3, completed: 3 }, Race: { active: 1, blocked: true } },
        });

        expect(element(model, 'Decide').stats).toEqual({ started: 3, completed: 3 });
        expect(element(model, 'Race').stats).toEqual({ active: 1, blocked: true });
        expect(element(model, 'Fork').stats).toBeNull();
    });

    it('resolves activity stats onto the element whose binding names that activity', () => {
        const fixture = loadFixture('gateway-routing');
        const model = buildBpmnViewModel({
            activity: fixture.activity,
            sourceXml: fixture.sourceXml,
            activityStats: { 'gateway-routing:node-Approve': { started: 2, completed: 1, faulted: true } },
        });

        expect(element(model, 'Approve').activityStats).toEqual({ started: 2, completed: 1, faulted: true });
        expect(element(model, 'Reject').activityStats).toBeNull();
        // A gateway has no bound activity, so activity-keyed state can never reach it -- which is the
        // whole reason the element-keyed map exists alongside it.
        expect(element(model, 'Decide').activityStats).toBeNull();
    });

    it('keeps the two maps apart even when an activity id and an element id collide', () => {
        const fixture = loadFixture('gateway-routing');
        const model = buildBpmnViewModel({
            activity: fixture.activity,
            sourceXml: fixture.sourceXml,
            elementStats: { Approve: { started: 9 } },
            activityStats: { 'gateway-routing:node-Approve': { started: 2 } },
        });

        expect(element(model, 'Approve').stats).toEqual({ started: 9 });
        expect(element(model, 'Approve').activityStats).toEqual({ started: 2 });
    });
});

describe('camunda extensions', () => {
    it('still carries camunda extension elements, foreign attributes and documentation', () => {
        const model = build('camunda-order-process');
        const task = element(model, 'NotifyWarehouse');

        expect(task.extensions.extensionElements.map(node => `${node.name.ns}#${node.name.localName}`)).toEqual([
            'http://camunda.org/schema/1.0/bpmn#inputOutput',
            'https://elsaworkflows.io/schemas/bpmn/v1#activityBinding',
        ]);
        expect(task.extensions.foreignAttributes).toEqual([
            { name: { ns: 'http://camunda.org/schema/1.0/bpmn', localName: 'asyncBefore' }, value: 'true' },
        ]);
        expect(task.extensions.documentation).toEqual([{ text: 'Tells the warehouse an order needs picking.' }]);
        expect(task.extensions.extensionElements[0].children[0])
            .toMatchObject({ value: '${orderId}', attributes: [{ name: { localName: 'name' }, value: 'orderId' }] });
    });
});

describe('geometry fallback', () => {
    it('uses the document\'s coordinates when it has them and says so', () => {
        const model = build('camunda-order-process');

        expect(model.layout).toEqual({ source: 'document', reason: null });
        expect(model.diagnostics.filter(diagnostic => diagnostic.code === 'missing-source-xml')).toEqual([]);
    });

    // The same document, both ways: the one thing that must never happen is a fallback layout that is
    // indistinguishable from the author's own coordinates.
    it('falls back and says so when the same document arrives with no source XML', () => {
        const withSource = build('camunda-order-process');
        const withoutSource = build('camunda-order-process', { sourceXml: null });

        expect(withoutSource.layout.source).toBe('fallback');
        expect(withoutSource.layout.reason).toContain('no BPMN source document');
        expect(withoutSource.diagnostics.map(diagnostic => diagnostic.code)).toContain('missing-source-xml');
        expect(withoutSource.elements.every(candidate => candidate.geometry.source === 'fallback')).toBe(true);
        expect(withoutSource.elements.map(candidate => candidate.geometry))
            .not.toEqual(withSource.elements.map(candidate => candidate.geometry));
    });

    it('falls back when the document carries no BPMNDI section at all', () => {
        const model = build('publish-gate-process');

        expect(model.layout.source).toBe('fallback');
        expect(model.layout.reason).toContain('no BPMNDI diagram section');
        expect(model.diagnostics.map(diagnostic => diagnostic.code)).toContain('missing-diagram');
    });

    it('falls back, loudly, when the document has a diagram but none of it is for this process', () => {
        // The quietest way this could go wrong: DI that is present but for a different process (a
        // stale source, the wrong plane) placing nothing while the model still claims to be drawn
        // from the document.
        const fixture = loadFixture('camunda-order-process');
        const otherProcess = fixture.sourceXml.replace(/bpmnElement="(StartEvent_1|NotifyWarehouse|EndEvent_1)"/g, 'bpmnElement="$1_elsewhere"');
        const model = buildBpmnViewModel({ activity: fixture.activity, sourceXml: otherProcess });

        expect(model.layout.source).toBe('fallback');
        expect(model.layout.reason).toContain('no BPMNShape for any element of this process');
        expect(model.diagnostics.filter(diagnostic => diagnostic.code === 'missing-diagram'))
            .toMatchObject([{ severity: 'warning', message: model.layout.reason }]);
        expect(model.diagnostics.filter(diagnostic => diagnostic.code === 'unresolved-di-shape')).toHaveLength(3);
    });

    it('says the same thing in layout.reason and in the diagnostic that explains it', () => {
        for (const model of [
            build('camunda-order-process', { sourceXml: null }),
            build('publish-gate-process'),
            build('camunda-order-process', { sourceXml: '<bpmn:definitions><not-closed>' }),
        ]) {
            const explanation = model.diagnostics.find(diagnostic =>
                diagnostic.code === 'missing-source-xml'
                || diagnostic.code === 'missing-diagram'
                || diagnostic.code === 'source-xml-parse-error')!;

            expect(explanation.message).toBe(model.layout.reason);
        }
    });

    it('reports unparseable source XML as an error and still returns a usable diagram', () => {
        const model = build('camunda-order-process', { sourceXml: '<bpmn:definitions><not-closed>' });

        expect(model.elements).toHaveLength(3);
        expect(model.layout.source).toBe('fallback');
        expect(model.diagnostics.find(diagnostic => diagnostic.code === 'source-xml-parse-error')!.severity).toBe('error');
    });

    it('places only the elements a partial diagram misses, and marks each one', () => {
        const fixture = loadFixture('camunda-order-process');
        const withoutOneShape = fixture.sourceXml.replace(
            /<bpmndi:BPMNShape id="NotifyWarehouse_di"[\s\S]*?<\/bpmndi:BPMNShape>/,
            '');
        const model = buildBpmnViewModel({ activity: fixture.activity, sourceXml: withoutOneShape });

        // The document still chose the coordinates for everything else, so the diagram as a whole is
        // still the document's; only the one element it forgot is this module's guess.
        expect(model.layout.source).toBe('document');
        expect(element(model, 'StartEvent_1').geometry.source).toBe('document');
        expect(element(model, 'NotifyWarehouse').geometry.source).toBe('fallback');
        expect(model.diagnostics.filter(diagnostic => diagnostic.code === 'missing-shape'))
            .toMatchObject([{ severity: 'warning', elementId: 'NotifyWarehouse' }]);
    });

    it('lays a document with no DI out left to right, nesting a subprocess body inside its host', () => {
        const model = build('subprocess-boundary-events', { sourceXml: null });
        const host = element(model, 'Fulfil').geometry;
        const nested = element(model, 'PickLine').geometry;

        expect(model.layout.source).toBe('fallback');
        expect(element(model, 'Start_1').geometry.x).toBeLessThan(host.x);
        expect(nested.x).toBeGreaterThan(host.x);
        expect(nested.y).toBeGreaterThan(host.y);
        expect(nested.x + nested.width).toBeLessThanOrEqual(host.x + host.width);
        expect(nested.y + nested.height).toBeLessThanOrEqual(host.y + host.height);

        // A boundary event sits on its host's edge rather than in a column of its own.
        const boundary = element(model, 'FulfilTimeout').geometry;

        expect(boundary.x).toBeGreaterThanOrEqual(host.x);
        expect(boundary.y).toBeGreaterThan(host.y);
    });

    it('is deterministic: the same input lays out identically every time', () => {
        expect(build('subprocess-boundary-events', { sourceXml: null }).elements.map(candidate => candidate.geometry))
            .toEqual(build('subprocess-boundary-events', { sourceXml: null }).elements.map(candidate => candidate.geometry));
    });

    it('marks a flow with no DI edge as fallback-routed rather than reporting empty waypoints as real', () => {
        const model = build('publish-gate-process');

        expect(model.flows.every(flow => flow.geometrySource === 'fallback' && flow.waypoints.length === 0)).toBe(true);
        expect(build('camunda-order-process').flows.every(flow => flow.geometrySource === 'document')).toBe(true);
    });
});

describe('refusals', () => {
    it('returns an empty diagram with an error when the activity is not a BPMN process', () => {
        const model = buildBpmnViewModel({ activity: { id: 'root', type: 'Elsa.Flowchart' } });

        expect(model.elements).toEqual([]);
        expect(model.scopes).toEqual([]);
        expect(model.diagnostics).toMatchObject([{ code: 'not-a-bpmn-process', severity: 'error' }]);
    });

    // buildBpmnViewModel sits on a JS interop boundary, so what reaches it is JSON.parse output rather
    // than a value TypeScript has checked -- and W11/W14 edit that document in place before sending it
    // back whole. A half-built element has to render as an element with nothing on it.
    it('renders a payload whose optional collections an in-place edit has not created yet', () => {
        const fixture = loadFixture('camunda-order-process');
        const activity = structuredClone(fixture.activity) as BpmnActivity;
        const task = activity.process!.elements.find(candidate => candidate.elementId === 'NotifyWarehouse')!;

        delete (task as Partial<typeof task>).extensions;
        delete (task as Partial<typeof task>).eventDefinitions;
        delete (task as Partial<typeof task>).properties;
        delete (activity.process as Partial<typeof activity.process>).lanes;

        const model = buildBpmnViewModel({ activity, sourceXml: fixture.sourceXml });
        const rendered = element(model, 'NotifyWarehouse');

        expect(model.elements).toHaveLength(3);
        expect(model.lanes).toEqual([]);
        expect(rendered.eventDefinitions).toEqual([]);
        expect(rendered.properties).toEqual({});
        expect(rendered.extensions)
            .toEqual({ documentation: [], extensionElements: [], foreignAttributes: [], foreignChildren: [] });
    });

    it('reports an element id used by two scopes, because instance state cannot tell them apart', () => {
        const fixture = loadFixture('subprocess-boundary-events');
        const activity = structuredClone(fixture.activity) as BpmnActivity;
        const nested = activity.activities!.find(candidate => candidate.process?.processId === 'Fulfil')!;

        (nested.process!.elements[0] as { elementId: string }).elementId = 'Start_1';

        const model = buildBpmnViewModel({ activity, sourceXml: fixture.sourceXml });
        const diagnostic = model.diagnostics.find(candidate => candidate.code === 'duplicate-element-id')!;

        expect(diagnostic).toMatchObject({ severity: 'error', elementId: 'Start_1', scopeId: 'Fulfil' });
    });

    it('reports a flow id used by two scopes, mirroring the duplicate-element-id diagnostic', () => {
        const fixture = loadFixture('subprocess-boundary-events');
        const activity = structuredClone(fixture.activity) as BpmnActivity;
        const nested = activity.activities!.find(candidate => candidate.process?.processId === 'Fulfil')!;

        (nested.process!.sequenceFlows![0] as { flowId: string }).flowId = 'Flow_1';

        const model = buildBpmnViewModel({ activity, sourceXml: fixture.sourceXml });
        const diagnostic = model.diagnostics.find(candidate => candidate.code === 'duplicate-flow-id')!;

        expect(diagnostic).toMatchObject({ severity: 'error', elementId: 'Flow_1', scopeId: 'Fulfil' });
    });
});

describe('the module reads and never writes', () => {
    let fixture: ReturnType<typeof loadFixture>;
    let before: string;

    beforeEach(() => {
        fixture = loadFixture('transaction-compensation');
        before = JSON.stringify(fixture.activity);
    });

    // D4: the client holds the entire document, and this module owns no save path. A build that
    // quietly edited its input would corrupt the very document W11/W14 send back whole.
    it('leaves the activity payload it was given untouched', () => {
        buildBpmnViewModel({
            activity: fixture.activity,
            sourceXml: fixture.sourceXml,
            activityDescriptors: [WRITE_LINE_DESCRIPTOR],
            elementStats: { BookTrip: { active: 1 } },
            activityStats: { 'transaction-compensation:node-BookTrip': { started: 1 } },
        });

        expect(JSON.stringify(fixture.activity)).toBe(before);
    });
});

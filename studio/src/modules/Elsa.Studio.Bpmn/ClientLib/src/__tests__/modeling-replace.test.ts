// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { BpmnModelingSession, compound, replaceElement, replaceOptions, updateProperties } from '../modeling/index.js';

const shape = (id: string, x: number, y: number, width: number, height: number, extra = '') =>
    `<bpmndi:BPMNShape id="${id}_di" bpmnElement="${id}"${extra}><dc:Bounds x="${x}" y="${y}" width="${width}" height="${height}" /></bpmndi:BPMNShape>`;

const edge = (id: string, points: [number, number][]) =>
    `<bpmndi:BPMNEdge id="${id}_di" bpmnElement="${id}">${points.map(([x, y]) => `<di:waypoint x="${x}" y="${y}" />`).join('')}</bpmndi:BPMNEdge>`;

// Start -> Work -> Choice (default Flow_3 to Done, Flow_4 to Throw), in a lane; Work has a binding, a timer on its
// border and a data input; Throw flows to Last; a subprocess holds a start event; a transaction holds an end event.
const XML = `<?xml version="1.0" encoding="UTF-8"?>
<bpmn:definitions xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL" xmlns:bpmndi="http://www.omg.org/spec/BPMN/20100524/DI" xmlns:dc="http://www.omg.org/spec/DD/20100524/DC" xmlns:di="http://www.omg.org/spec/DD/20100524/DI" xmlns:elsa="https://elsaworkflows.io/schemas/bpmn/v1" id="Definitions_1" targetNamespace="http://bpmn.io/schema/bpmn">
  <bpmn:collaboration id="Collaboration_1"><bpmn:participant id="Pool" processRef="Process_1" /></bpmn:collaboration>
  <bpmn:process id="Process_1" isExecutable="true">
    <bpmn:laneSet id="LaneSet_1"><bpmn:lane id="Lane"><bpmn:flowNodeRef>Start</bpmn:flowNodeRef><bpmn:flowNodeRef>Work</bpmn:flowNodeRef></bpmn:lane></bpmn:laneSet>
    <bpmn:startEvent id="Start"><bpmn:outgoing>Flow_1</bpmn:outgoing></bpmn:startEvent>
    <bpmn:scriptTask id="Work" name="Work" scriptFormat="javascript"><bpmn:extensionElements><elsa:activityBinding activityType="Elsa.WriteLine" /></bpmn:extensionElements><bpmn:incoming>Flow_1</bpmn:incoming><bpmn:outgoing>Flow_2</bpmn:outgoing><bpmn:dataInputAssociation id="Read"><bpmn:sourceRef>DataRef</bpmn:sourceRef></bpmn:dataInputAssociation><bpmn:script>log()</bpmn:script></bpmn:scriptTask>
    <bpmn:boundaryEvent id="Timer" attachedToRef="Work"><bpmn:timerEventDefinition id="TimerDefinition" /></bpmn:boundaryEvent>
    <bpmn:exclusiveGateway id="Choice" default="Flow_3"><bpmn:incoming>Flow_2</bpmn:incoming><bpmn:outgoing>Flow_3</bpmn:outgoing><bpmn:outgoing>Flow_4</bpmn:outgoing></bpmn:exclusiveGateway>
    <bpmn:endEvent id="Done"><bpmn:incoming>Flow_3</bpmn:incoming></bpmn:endEvent>
    <bpmn:intermediateCatchEvent id="Throw"><bpmn:incoming>Flow_4</bpmn:incoming><bpmn:outgoing>Flow_5</bpmn:outgoing><bpmn:messageEventDefinition id="MessageDefinition" /></bpmn:intermediateCatchEvent>
    <bpmn:task id="Last"><bpmn:incoming>Flow_5</bpmn:incoming></bpmn:task>
    <bpmn:dataObjectReference id="DataRef" dataObjectRef="Data" />
    <bpmn:dataObject id="Data" />
    <bpmn:parallelGateway id="Join" />
    <bpmn:subProcess id="Sub"><bpmn:startEvent id="SubStart" /></bpmn:subProcess>
    <bpmn:transaction id="Tx"><bpmn:endEvent id="TxEnd" /></bpmn:transaction>
    <bpmn:sequenceFlow id="Flow_1" sourceRef="Start" targetRef="Work" />
    <bpmn:sequenceFlow id="Flow_2" sourceRef="Work" targetRef="Choice" />
    <bpmn:sequenceFlow id="Flow_3" sourceRef="Choice" targetRef="Done" />
    <bpmn:sequenceFlow id="Flow_4" sourceRef="Choice" targetRef="Throw" />
    <bpmn:sequenceFlow id="Flow_5" sourceRef="Throw" targetRef="Last" />
  </bpmn:process>
  <bpmndi:BPMNDiagram id="Diagram_1">
    <bpmndi:BPMNPlane id="Plane_1" bpmnElement="Collaboration_1">
      ${shape('Pool', 0, 0, 1200, 400, ' isHorizontal="true"')}
      ${shape('Lane', 30, 0, 1170, 400, ' isHorizontal="true"')}
      ${shape('Start', 60, 62, 36, 36)}
      ${shape('Work', 140, 40, 100, 80)}
      ${shape('Timer', 172, 102, 36, 36)}
      ${shape('Choice', 290, 55, 50, 50, ' isMarkerVisible="true"')}
      ${shape('Done', 400, 62, 36, 36)}
      ${shape('Throw', 400, 200, 36, 36)}
      ${shape('Last', 500, 180, 100, 80)}
      ${shape('DataRef', 150, 250, 36, 50)}
      ${shape('Join', 600, 300, 50, 50)}
      ${shape('Sub', 700, 40, 200, 150, ' isExpanded="true"')}
      ${shape('SubStart', 720, 80, 36, 36)}
      ${shape('Tx', 950, 40, 200, 150, ' isExpanded="true"')}
      ${shape('TxEnd', 970, 80, 36, 36)}
      ${edge('Flow_1', [[96, 80], [140, 80]])}
      ${edge('Flow_2', [[240, 80], [290, 80]])}
      ${edge('Flow_3', [[340, 80], [400, 80]])}
      ${edge('Flow_4', [[315, 105], [315, 218], [400, 218]])}
      ${edge('Flow_5', [[436, 218], [500, 218]])}
      ${edge('Read', [[168, 250], [168, 120]])}
    </bpmndi:BPMNPlane>
  </bpmndi:BPMNDiagram>
</bpmn:definitions>`;

async function loaded() {
    const session = new BpmnModelingSession();
    expect((await session.load(XML)).imported).toBe(true);
    const document = session.document!;
    return { session, document, original: (await session.save()).xml, save: async () => (await session.save()).xml };
}

const labels = (document: Parameters<typeof replaceOptions>[0], id: string) => replaceOptions(document, id).map(entry => entry.label);

describe('replaceElement', () => {
    it('replaces a task with another kind, keeping its id, name, binding, flows, lane, boundary event and data input, and dropping what the new kind lacks', async () => {
        const { session, document, original, save } = await loaded();
        const before = document.element('Work');

        session.execute(replaceElement(document, 'Work', { type: 'bpmn:UserTask' }));

        const work = document.element('Work')!;
        expect(work).not.toBe(before);
        expect(work.$type).toBe('bpmn:UserTask');
        expect(document.element('Flow_1')!.targetRef).toBe(work);
        expect(document.element('Flow_2')!.sourceRef).toBe(work);
        expect(document.element('Timer')!.attachedToRef).toBe(work);
        expect(document.planeView('Plane_1')!.shapes.find(shape => shape.elementId === 'Work')).toMatchObject({ type: 'bpmn:UserTask', bounds: { x: 140, y: 40, width: 100, height: 80 } });
        const xml = await save();
        expect(xml).toContain('<bpmn:userTask id="Work" name="Work">\n      <bpmn:extensionElements>\n        <elsa:activityBinding activityType="Elsa.WriteLine" />');
        expect(xml).toContain('<bpmn:dataInputAssociation id="Read">');
        expect(xml).not.toContain('scriptFormat');
        expect(xml).not.toContain('log()');
        expect(xml).toContain('<bpmn:flowNodeRef>Work</bpmn:flowNodeRef>');
        // Only the element changed: the rest of the document is written as before.
        expect(xml.replace(/<bpmn:userTask[\s\S]*?<\/bpmn:userTask>/, '')).toBe(original.replace(/<bpmn:scriptTask[\s\S]*?<\/bpmn:scriptTask>/, ''));

        expect(session.undo()).toBe(true);
        expect(await save()).toBe(original);
        expect(document.element('Work')).toBe(before);
        expect(session.redo()).toBe(true);
        expect(await save()).toBe(xml);
    });

    it('keeps a gateway\'s default flow where the new kind has one, and shows an exclusive gateway\'s marker', async () => {
        const { session, document, save } = await loaded();

        session.execute(replaceElement(document, 'Choice', { type: 'bpmn:InclusiveGateway' }));
        expect(await save()).toContain('<bpmn:inclusiveGateway id="Choice" default="Flow_3">');

        session.execute(replaceElement(document, 'Choice', { type: 'bpmn:ParallelGateway' }));
        expect(await save()).toContain('<bpmn:parallelGateway id="Choice">');

        session.execute(replaceElement(document, 'Choice', { type: 'bpmn:EventBasedGateway' }));
        expect(await save()).toContain('<bpmn:eventBasedGateway id="Choice">');

        session.execute(replaceElement(document, 'Join', { type: 'bpmn:ExclusiveGateway' }));
        expect(await save()).toContain('<bpmndi:BPMNShape id="Join_di" bpmnElement="Join" isMarkerVisible="true">');
    });

    it('gives an event a new definition of the target kind, keeps one of the same kind, and sets how it interrupts', async () => {
        const { session, document, save } = await loaded();

        session.execute(replaceElement(document, 'Start', { type: 'bpmn:StartEvent', eventDefinitionType: 'bpmn:TimerEventDefinition', isInterrupting: true }));
        expect(await save()).toMatch(/<bpmn:startEvent id="Start">\n {6}<bpmn:outgoing>Flow_1<\/bpmn:outgoing>\n {6}<bpmn:timerEventDefinition id="TimerEventDefinition_[0-9a-z]{7}" \/>/);

        const definition = document.element('MessageDefinition');
        session.execute(replaceElement(document, 'Throw', { type: 'bpmn:IntermediateThrowEvent', eventDefinitionType: 'bpmn:MessageEventDefinition' }));
        expect(document.element('Throw')!.$type).toBe('bpmn:IntermediateThrowEvent');
        expect(document.element('MessageDefinition')).toBe(definition);

        session.execute(replaceElement(document, 'Timer', { type: 'bpmn:BoundaryEvent', eventDefinitionType: 'bpmn:TimerEventDefinition', cancelActivity: false }));
        expect(await save()).toContain('<bpmn:boundaryEvent id="Timer" cancelActivity="false" attachedToRef="Work">\n      <bpmn:timerEventDefinition id="TimerDefinition" />');

        session.execute(replaceElement(document, 'Throw', { type: 'bpmn:IntermediateThrowEvent' }));
        expect(document.element('MessageDefinition')).toBeUndefined();
    });

    it('drops the flows a start event cannot have, and undo brings them back', async () => {
        const { session, document, original, save } = await loaded();
        const command = replaceElement(document, 'Throw', { type: 'bpmn:StartEvent' });

        session.execute(command);

        expect(command.elementIds).toEqual(['Throw', 'Flow_4']);
        expect(document.element('Flow_4')).toBeUndefined();
        expect(await save()).toContain('<bpmn:exclusiveGateway id="Choice" default="Flow_3">\n      <bpmn:incoming>Flow_2</bpmn:incoming>\n      <bpmn:outgoing>Flow_3</bpmn:outgoing>\n    </bpmn:exclusiveGateway>');
        expect(session.undo()).toBe(true);
        expect(await save()).toBe(original);
    });

    it('replaces the element it was created for, even when an earlier command in the same compound renamed it', async () => {
        const { session, document } = await loaded();

        session.execute(compound([updateProperties(document, 'Last', { id: 'Renamed' }), replaceElement(document, 'Last', { type: 'bpmn:ServiceTask' })]));

        expect(document.element('Renamed')!.$type).toBe('bpmn:ServiceTask');
        expect(document.element('Flow_5')!.targetRef).toBe(document.element('Renamed'));
    });

    it('puts back the same element and event definition on redo, so the edits made to them after it redo too', async () => {
        const { session, document, save } = await loaded();
        session.execute(replaceElement(document, 'Last', { type: 'bpmn:ServiceTask' }));
        session.execute(updateProperties(document, 'Last', { name: 'Call' }));
        session.execute(replaceElement(document, 'Last', { type: 'bpmn:UserTask' }));
        session.execute(replaceElement(document, 'Done', { type: 'bpmn:EndEvent', eventDefinitionType: 'bpmn:MessageEventDefinition' }));
        const definitionId = String((document.element('Done')!.eventDefinitions as { id: string }[])[0].id);
        session.execute(updateProperties(document, definitionId, { id: 'Sent' }));
        const xml = await save();

        for (let index = 0; index < 5; index++) expect(session.undo()).toBe(true);
        for (let index = 0; index < 5; index++) expect(session.redo()).toBe(true);

        expect(await save()).toBe(xml);
        expect(xml).toContain('<bpmn:userTask id="Last" name="Call">');
        expect(xml).toContain('<bpmn:messageEventDefinition id="Sent" />');
    });
});

describe('replaceOptions', () => {
    it('offers what the bpmn-js designer offers, without what the element already is', async () => {
        const { document } = await loaded();

        expect(labels(document, 'Last')).toEqual(['User task', 'Service task', 'Send task', 'Receive task', 'Manual task', 'Business rule task', 'Script task']);
        expect(labels(document, 'Choice')).toEqual(['Parallel gateway', 'Inclusive gateway', 'Complex gateway', 'Event-based gateway']);
        expect(labels(document, 'Start')).toEqual(['Intermediate throw event', 'End event', 'Message start event', 'Timer start event', 'Conditional start event', 'Signal start event']);
        // A subprocess's start event is a plain one; a cancel end event only belongs in a transaction.
        expect(labels(document, 'SubStart')).toEqual(['Intermediate throw event', 'End event']);
        expect(labels(document, 'Done')).not.toContain('Cancel end event');
        expect(labels(document, 'TxEnd')).toContain('Cancel end event');
        expect(labels(document, 'Timer')).not.toContain('Timer boundary event');
        expect(labels(document, 'Timer')).toContain('Timer boundary event (non-interrupting)');
        expect(labels(document, 'Throw')).toContain('Message intermediate throw event');
        expect(labels(document, 'Throw')).not.toContain('Message intermediate catch event');
        for (const id of ['Sub', 'Tx', 'DataRef', 'Pool', 'Lane', 'Flow_1', 'Missing']) expect(replaceOptions(document, id), id).toEqual([]);
    });

    it('refuses a target the element does not offer', async () => {
        const { document } = await loaded();

        expect(() => replaceElement(document, 'Last', { type: 'bpmn:Task' })).toThrow("The element 'Last' cannot be replaced with a bpmn:Task.");
        expect(() => replaceElement(document, 'Last', { type: 'bpmn:SubProcess' })).toThrow("The element 'Last' cannot be replaced with a bpmn:SubProcess.");
        expect(() => replaceElement(document, 'Done', { type: 'bpmn:EndEvent', eventDefinitionType: 'bpmn:CancelEventDefinition' })).toThrow('cannot be replaced');
        expect(() => replaceElement(document, 'Missing', { type: 'bpmn:Task' })).toThrow("The document has no element 'Missing'.");
    });
});

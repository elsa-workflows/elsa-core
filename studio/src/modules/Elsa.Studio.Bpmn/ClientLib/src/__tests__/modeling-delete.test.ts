// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { BpmnModelingSession, compound, deleteElements, updateProperties } from '../modeling/index.js';

const shape = (id: string, x: number, y: number, width = 100, height = 80, extra = '') =>
    `<bpmndi:BPMNShape id="${id}_di" bpmnElement="${id}"${extra}><dc:Bounds x="${x}" y="${y}" width="${width}" height="${height}" /></bpmndi:BPMNShape>`;

const edge = (id: string, points: [number, number][]) =>
    `<bpmndi:BPMNEdge id="${id}_di" bpmnElement="${id}">${points.map(([x, y]) => `<di:waypoint x="${x}" y="${y}" />`).join('')}</bpmndi:BPMNEdge>`;

// Start -> A -> Gateway (default Flow_3 to B, Flow_4 to C) in a lane; a timer on A flows to C; C flows into a collapsed
// subprocess drawn on its own plane; A reads a data object; a note is associated with B.
const XML = `<?xml version="1.0" encoding="UTF-8"?>
<bpmn:definitions xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL" xmlns:bpmndi="http://www.omg.org/spec/BPMN/20100524/DI" xmlns:dc="http://www.omg.org/spec/DD/20100524/DC" xmlns:di="http://www.omg.org/spec/DD/20100524/DI" id="Definitions_1" targetNamespace="http://bpmn.io/schema/bpmn">
  <bpmn:collaboration id="Collaboration_1"><bpmn:participant id="Pool" processRef="Process_1" /></bpmn:collaboration>
  <bpmn:process id="Process_1" isExecutable="true">
    <bpmn:laneSet id="LaneSet_1"><bpmn:lane id="Lane"><bpmn:flowNodeRef>Start</bpmn:flowNodeRef><bpmn:flowNodeRef>A</bpmn:flowNodeRef><bpmn:flowNodeRef>Gateway</bpmn:flowNodeRef></bpmn:lane></bpmn:laneSet>
    <bpmn:startEvent id="Start"><bpmn:outgoing>Flow_1</bpmn:outgoing></bpmn:startEvent>
    <bpmn:task id="A"><bpmn:incoming>Flow_1</bpmn:incoming><bpmn:outgoing>Flow_2</bpmn:outgoing><bpmn:dataInputAssociation id="Read"><bpmn:sourceRef>DataRef</bpmn:sourceRef></bpmn:dataInputAssociation></bpmn:task>
    <bpmn:exclusiveGateway id="Gateway" default="Flow_3"><bpmn:incoming>Flow_2</bpmn:incoming><bpmn:outgoing>Flow_3</bpmn:outgoing><bpmn:outgoing>Flow_4</bpmn:outgoing></bpmn:exclusiveGateway>
    <bpmn:task id="B"><bpmn:incoming>Flow_3</bpmn:incoming></bpmn:task>
    <bpmn:task id="C"><bpmn:incoming>Flow_4</bpmn:incoming><bpmn:incoming>Flow_t</bpmn:incoming><bpmn:outgoing>Flow_s</bpmn:outgoing></bpmn:task>
    <bpmn:boundaryEvent id="Timer" attachedToRef="A"><bpmn:outgoing>Flow_t</bpmn:outgoing><bpmn:timerEventDefinition id="TimerDefinition" /></bpmn:boundaryEvent>
    <bpmn:subProcess id="Sub"><bpmn:incoming>Flow_s</bpmn:incoming><bpmn:task id="Inner" /></bpmn:subProcess>
    <bpmn:dataObjectReference id="DataRef" dataObjectRef="Data" />
    <bpmn:dataObject id="Data" />
    <bpmn:sequenceFlow id="Flow_1" sourceRef="Start" targetRef="A" />
    <bpmn:sequenceFlow id="Flow_2" sourceRef="A" targetRef="Gateway" />
    <bpmn:sequenceFlow id="Flow_3" sourceRef="Gateway" targetRef="B" />
    <bpmn:sequenceFlow id="Flow_4" sourceRef="Gateway" targetRef="C" />
    <bpmn:sequenceFlow id="Flow_t" sourceRef="Timer" targetRef="C" />
    <bpmn:sequenceFlow id="Flow_s" sourceRef="C" targetRef="Sub" />
    <bpmn:textAnnotation id="Note"><bpmn:text>Why</bpmn:text></bpmn:textAnnotation>
    <bpmn:association id="Link" sourceRef="B" targetRef="Note" />
  </bpmn:process>
  <bpmndi:BPMNDiagram id="Diagram_1">
    <bpmndi:BPMNPlane id="Plane_1" bpmnElement="Collaboration_1">
      ${shape('Pool', 0, 0, 900, 400, ' isHorizontal="true"')}
      ${shape('Lane', 30, 0, 870, 400, ' isHorizontal="true"')}
      ${shape('Start', 60, 62, 36, 36)}
      ${shape('A', 140, 40)}
      ${shape('Gateway', 290, 55, 50, 50, ' isMarkerVisible="true"')}
      ${shape('B', 400, 40)}
      ${shape('C', 400, 200)}
      ${shape('Timer', 172, 102, 36, 36)}
      ${shape('Sub', 560, 200, 100, 80, ' isExpanded="false"')}
      ${shape('DataRef', 150, 250, 36, 50)}
      ${shape('Note', 560, 40, 100, 50)}
      ${edge('Flow_1', [[96, 80], [140, 80]])}
      ${edge('Flow_2', [[240, 80], [290, 80]])}
      ${edge('Flow_3', [[340, 80], [400, 80]])}
      ${edge('Flow_4', [[315, 105], [315, 240], [400, 240]])}
      ${edge('Flow_t', [[190, 138], [190, 260], [400, 260]])}
      ${edge('Flow_s', [[500, 240], [560, 240]])}
      ${edge('Read', [[168, 250], [168, 120]])}
      ${edge('Link', [[500, 60], [560, 60]])}
    </bpmndi:BPMNPlane>
  </bpmndi:BPMNDiagram>
  <bpmndi:BPMNDiagram id="Diagram_Sub">
    <bpmndi:BPMNPlane id="Plane_Sub" bpmnElement="Sub">${shape('Inner', 100, 100)}</bpmndi:BPMNPlane>
  </bpmndi:BPMNDiagram>
</bpmn:definitions>`;

async function loaded() {
    const session = new BpmnModelingSession();
    expect((await session.load(XML)).imported).toBe(true);
    const document = session.document!;
    return { session, document, original: (await session.save()).xml, save: async () => (await session.save()).xml };
}

describe('deleteElements', () => {
    it('deletes a task with its boundary event, its flows, its data association and its drawings, and undo puts back the document', async () => {
        const { session, document, original, save } = await loaded();
        const command = deleteElements(document, ['A']);

        session.execute(command);

        expect(command.elementIds).toEqual(expect.arrayContaining(['A', 'Read', 'Timer', 'TimerDefinition', 'Flow_1', 'Flow_2', 'Flow_t']));
        for (const id of ['A', 'Timer', 'Flow_1', 'Flow_2', 'Flow_t', 'Read']) expect(document.element(id), id).toBeUndefined();
        const xml = await save();
        expect(xml).not.toMatch(/"(A|Timer|Flow_1|Flow_2|Flow_t|Read)(_di)?"/);
        expect(xml).not.toContain('<bpmn:flowNodeRef>A</bpmn:flowNodeRef>');
        expect(xml).toContain('<bpmn:startEvent id="Start" />');
        expect(xml).toContain('<bpmn:task id="C">\n      <bpmn:incoming>Flow_4</bpmn:incoming>\n      <bpmn:outgoing>Flow_s</bpmn:outgoing>');
        // The data object is still referenced, so it stays.
        expect(document.element('DataRef')).toBeDefined();
        expect(document.element('Data')).toBeDefined();

        expect(session.undo()).toBe(true);
        expect(await save()).toBe(original);
        expect(session.redo()).toBe(true);
        expect(await save()).toBe(xml);
    });

    it('clears a gateway default that names a deleted flow', async () => {
        const { session, document, original, save } = await loaded();

        session.execute(deleteElements(document, ['Flow_3']));

        const xml = await save();
        expect(xml).toContain('<bpmn:exclusiveGateway id="Gateway">');
        expect(xml).toContain('<bpmn:task id="B" />');
        expect(session.undo()).toBe(true);
        expect(await save()).toBe(original);
    });

    it('deletes a collapsed subprocess with its content and the diagram of its own plane', async () => {
        const { session, document, original, save } = await loaded();

        session.execute(deleteElements(document, ['Sub']));

        expect(document.element('Inner')).toBeUndefined();
        expect(document.planes().map(plane => plane.planeId)).toEqual(['Plane_1']);
        expect(await save()).not.toContain('Flow_s');
        expect(session.undo()).toBe(true);
        expect(await save()).toBe(original);
    });

    it('deletes a data object with its last reference, and the associations that read it', async () => {
        const { session, document, original, save } = await loaded();

        session.execute(deleteElements(document, ['DataRef']));

        expect(document.element('Data')).toBeUndefined();
        expect(document.element('Read')).toBeUndefined();
        expect(await save()).toContain('<bpmn:task id="A">\n      <bpmn:incoming>Flow_1</bpmn:incoming>\n      <bpmn:outgoing>Flow_2</bpmn:outgoing>\n    </bpmn:task>');
        expect(session.undo()).toBe(true);
        expect(await save()).toBe(original);
    });

    it('deletes the associations of a deleted task and keeps the annotation', async () => {
        const { session, document } = await loaded();

        session.execute(deleteElements(document, ['B']));

        expect(document.element('Link')).toBeUndefined();
        expect(document.element('Note')).toBeDefined();
        expect(document.element('Flow_3')).toBeUndefined();
    });

    it('deletes several elements as one undo step', async () => {
        const { session, document, original, save } = await loaded();

        session.execute(deleteElements(document, ['B', 'C', 'Flow_4']));

        expect(document.element('Sub')).toBeDefined();
        expect(document.element('Flow_s')).toBeUndefined();
        expect(session.undo()).toBe(true);
        expect(await save()).toBe(original);
        expect(session.canUndo).toBe(false);
    });

    it('deletes the elements it was created for, even when an earlier command in the same compound renamed one', async () => {
        const { session, document, original } = await loaded();

        session.execute(compound([updateProperties(document, 'A', { id: 'Renamed' }), deleteElements(document, ['A'])]));

        for (const id of ['A', 'Renamed', 'Timer', 'Flow_1', 'Flow_2']) expect(document.element(id), id).toBeUndefined();
        expect((await session.save()).xml).not.toMatch(/"(Renamed|Flow_1|Flow_2)(_di)?"/);
        expect(session.undo()).toBe(true);
        expect((await session.save()).xml).toBe(original);
    });

    it('refuses what it cannot delete, and unknown elements', async () => {
        const { document } = await loaded();

        for (const id of ['Pool', 'Lane', 'Process_1', 'LaneSet_1']) expect(() => deleteElements(document, [id])).toThrow(`The element '${id}' cannot be deleted here.`);
        expect(() => deleteElements(document, ['Missing'])).toThrow("The document has no element 'Missing'.");
        expect(() => deleteElements(document, [])).toThrow('A delete needs at least one element.');
    });
});

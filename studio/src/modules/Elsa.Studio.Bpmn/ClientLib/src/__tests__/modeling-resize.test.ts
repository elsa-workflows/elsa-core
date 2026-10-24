// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { BpmnModelingSession, compound, minimumSize, moveShapes, resizeShape } from '../modeling/index.js';

const shape = (id: string, x: number, y: number, width: number, height: number, extra = '', label?: [number, number, number, number]) =>
    `<bpmndi:BPMNShape id="${id}_di" bpmnElement="${id}"${extra}><dc:Bounds x="${x}" y="${y}" width="${width}" height="${height}" />` +
    (label ? `<bpmndi:BPMNLabel><dc:Bounds x="${label[0]}" y="${label[1]}" width="${label[2]}" height="${label[3]}" /></bpmndi:BPMNLabel>` : '') +
    '</bpmndi:BPMNShape>';

const edge = (id: string, points: [number, number][]) =>
    `<bpmndi:BPMNEdge id="${id}_di" bpmnElement="${id}">${points.map(([x, y]) => `<di:waypoint x="${x}" y="${y}" />`).join('')}</bpmndi:BPMNEdge>`;

// A start event flowing into an expanded subprocess that holds a task, with a timer on its bottom border flowing out,
// a collapsed subprocess, and a text annotation associated with the task.
const XML = `<?xml version="1.0" encoding="UTF-8"?>
<bpmn:definitions xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL" xmlns:bpmndi="http://www.omg.org/spec/BPMN/20100524/DI" xmlns:dc="http://www.omg.org/spec/DD/20100524/DC" xmlns:di="http://www.omg.org/spec/DD/20100524/DI" id="Definitions_1" targetNamespace="http://bpmn.io/schema/bpmn">
  <bpmn:process id="Process_1" isExecutable="true">
    <bpmn:startEvent id="Start"><bpmn:outgoing>Flow_in</bpmn:outgoing></bpmn:startEvent>
    <bpmn:subProcess id="Sub"><bpmn:incoming>Flow_in</bpmn:incoming><bpmn:task id="Inner" /></bpmn:subProcess>
    <bpmn:boundaryEvent id="Timer" attachedToRef="Sub"><bpmn:outgoing>Flow_out</bpmn:outgoing><bpmn:timerEventDefinition /></bpmn:boundaryEvent>
    <bpmn:task id="Outside"><bpmn:incoming>Flow_out</bpmn:incoming></bpmn:task>
    <bpmn:subProcess id="Collapsed" />
    <bpmn:sequenceFlow id="Flow_in" sourceRef="Start" targetRef="Sub" />
    <bpmn:sequenceFlow id="Flow_out" sourceRef="Timer" targetRef="Outside" />
    <bpmn:textAnnotation id="Note"><bpmn:text>Why</bpmn:text></bpmn:textAnnotation>
    <bpmn:association id="Link" sourceRef="Outside" targetRef="Note" />
  </bpmn:process>
  <bpmndi:BPMNDiagram id="Diagram_1">
    <bpmndi:BPMNPlane id="Plane_1" bpmnElement="Process_1">
      ${shape('Start', 20, 182, 36, 36)}
      ${shape('Sub', 100, 100, 300, 200, ' isExpanded="true"')}
      ${shape('Inner', 150, 150, 100, 80)}
      ${shape('Timer', 282, 282, 36, 36, '', [280, 320, 40, 14])}
      ${shape('Outside', 500, 400, 100, 80)}
      ${shape('Collapsed', 700, 100, 100, 80, ' isExpanded="false"')}
      ${shape('Note', 700, 300, 100, 50)}
      ${edge('Flow_in', [[56, 200], [100, 200]])}
      ${edge('Flow_out', [[300, 318], [300, 440], [500, 440]])}
      ${edge('Link', [[600, 420], [700, 330]])}
    </bpmndi:BPMNPlane>
  </bpmndi:BPMNDiagram>
</bpmn:definitions>`;

async function loaded() {
    const session = new BpmnModelingSession();
    expect((await session.load(XML)).imported).toBe(true);
    const document = session.document!;
    const view = () => document.planeView('Plane_1')!;
    return {
        session,
        document,
        original: (await session.save()).xml,
        bounds: (id: string) => view().shapes.find(shape => shape.elementId === id)!.bounds,
        label: (id: string) => view().shapes.find(shape => shape.elementId === id)!.label,
        waypoints: (id: string) => view().edges.find(edge => edge.elementId === id)!.waypoints.map(({ x, y }) => [x, y])
    };
}

describe('resizeShape', () => {
    it('resizes an expanded subprocess, its boundary event and the edges on its border following, and undo puts it back', async () => {
        const { session, document, original, bounds, label, waypoints } = await loaded();
        const command = resizeShape(document, 'Plane_1', 'Sub', { x: 100, y: 100, width: 400, height: 260 });

        session.execute(command);

        expect(command.elementIds).toEqual(['Sub', 'Timer', 'Flow_in', 'Flow_out']);
        expect(bounds('Sub')).toEqual({ x: 100, y: 100, width: 400, height: 260 });
        // Nearer the right and bottom sides, so it follows them.
        expect(bounds('Timer')).toEqual({ x: 382, y: 342, width: 36, height: 36 });
        expect(label('Timer')).toEqual({ x: 380, y: 380, width: 40, height: 14 });
        expect(waypoints('Flow_out')).toEqual([[400, 378], [400, 440], [500, 440]]);
        // Nearer the left and top sides, which did not move.
        expect(waypoints('Flow_in')).toEqual([[56, 200], [100, 200]]);
        expect(bounds('Inner')).toEqual({ x: 150, y: 150, width: 100, height: 80 });

        expect(session.undo()).toBe(true);
        expect((await session.save()).xml).toBe(original);
    });

    it('moves an edge end on a side that moves', async () => {
        const { session, document, waypoints, bounds } = await loaded();

        session.execute(resizeShape(document, 'Plane_1', 'Sub', { x: 60, y: 100, width: 340, height: 200 }));

        expect(waypoints('Flow_in')).toEqual([[56, 200], [60, 200]]);
        expect(bounds('Timer')).toEqual({ x: 282, y: 282, width: 36, height: 36 });
    });

    it('resizes a text annotation, the end of its association following', async () => {
        const { session, document, waypoints } = await loaded();

        session.execute(resizeShape(document, 'Plane_1', 'Note', { x: 700, y: 300, width: 160, height: 80 }));

        // The end is nearer the bottom side, which moved down by 30, and nearer the left side, which did not move.
        expect(waypoints('Link')).toEqual([[600, 420], [700, 360]]);
        session.execute(resizeShape(document, 'Plane_1', 'Note', { x: 680, y: 300, width: 180, height: 80 }));
        expect(waypoints('Link')).toEqual([[600, 420], [680, 360]]);
    });

    it('resizes from the layout at the time it executes, so two resizes in one compound apply in turn', async () => {
        const { session, document, original, bounds } = await loaded();

        session.execute(compound([
            resizeShape(document, 'Plane_1', 'Sub', { x: 100, y: 100, width: 400, height: 260 }),
            resizeShape(document, 'Plane_1', 'Sub', { x: 100, y: 100, width: 300, height: 200 })
        ]));

        expect(bounds('Timer')).toEqual({ x: 282, y: 282, width: 36, height: 36 });
        expect(session.undo()).toBe(true);
        expect((await session.save()).xml).toBe(original);
    });

    it('refuses what cannot be resized, sizes under the minimum, and bounds that leave out what the shape contains', async () => {
        const { document } = await loaded();
        const resize = (id: string, bounds: { x: number; y: number; width: number; height: number }) => () => resizeShape(document, 'Plane_1', id, bounds);

        for (const id of ['Inner', 'Start', 'Timer', 'Collapsed']) {
            expect(resize(id, { x: 0, y: 0, width: 200, height: 200 })).toThrow(`The shape of '${id}' cannot be resized.`);
        }
        expect(resize('Sub', { x: 100, y: 100, width: 139, height: 200 })).toThrow("The shape of 'Sub' must be at least 140 by 120.");
        expect(resize('Note', { x: 700, y: 300, width: 50, height: 29 })).toThrow("The shape of 'Note' must be at least 50 by 30.");
        expect(resize('Sub', { x: 200, y: 100, width: 200, height: 200 })).toThrow("The shape of 'Sub' must still contain 'Inner'.");
        expect(resize('Sub', { x: 100, y: 100, width: Number.POSITIVE_INFINITY, height: 200 })).toThrow('A resize needs finite bounds.');
        expect(resize('Missing', { x: 0, y: 0, width: 200, height: 200 })).toThrow("The plane 'Plane_1' has no shape for 'Missing'.");
        expect(() => resizeShape(document, 'Nowhere', 'Sub', { x: 0, y: 0, width: 200, height: 200 })).toThrow("The document has no plane 'Nowhere'.");
    });

    it('checks the content again when it executes, so a compound that moved the content first is refused and rolled back', async () => {
        const { session, document, original } = await loaded();
        const both = compound([
            moveShapes(document, 'Plane_1', ['Inner'], { x: 20, y: 0 }),
            // Created while Inner still fits: it reaches x 250, inside the new right side at 260.
            resizeShape(document, 'Plane_1', 'Sub', { x: 100, y: 100, width: 160, height: 200 })
        ]);

        expect(() => session.execute(both)).toThrow("The shape of 'Sub' must still contain 'Inner'.");

        expect(session.canUndo).toBe(false);
        expect((await session.save()).xml).toBe(original);
    });

    it('applies the bounds given when the command was created, not later changes to that object', async () => {
        const { session, document, bounds } = await loaded();
        const target = { x: 700, y: 300, width: 160, height: 80 };
        const command = resizeShape(document, 'Plane_1', 'Note', target);

        target.width = 10;
        session.execute(command);

        expect(bounds('Note')).toEqual({ x: 700, y: 300, width: 160, height: 80 });
    });
});

describe('minimumSize', () => {
    it('names the minimum for what the bpmn-js designer resizes, and nothing for the rest', () => {
        expect(minimumSize({ type: 'bpmn:SubProcess', isExpanded: true })).toEqual({ width: 140, height: 120 });
        expect(minimumSize({ type: 'bpmn:SubProcess', isExpanded: false })).toBeUndefined();
        expect(minimumSize({ type: 'bpmn:TextAnnotation' })).toEqual({ width: 50, height: 30 });
        expect(minimumSize({ type: 'bpmn:Group' })).toEqual({ width: 50, height: 50 });
        expect(minimumSize({ type: 'bpmn:Task' })).toBeUndefined();
        expect(minimumSize({ type: 'bpmn:Participant', isExpanded: true })).toBeUndefined();
    });
});

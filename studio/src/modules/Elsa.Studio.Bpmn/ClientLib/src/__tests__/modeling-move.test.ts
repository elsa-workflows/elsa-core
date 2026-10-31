// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { BpmnModelingSession, compound, moveShapes, updateProperties } from '../modeling/index.js';

const shape = (id: string, x: number, y: number, width: number, height: number, extra = '', label?: [number, number, number, number]) =>
    `<bpmndi:BPMNShape id="${id}_di" bpmnElement="${id}"${extra}><dc:Bounds x="${x}" y="${y}" width="${width}" height="${height}" />` +
    (label ? `<bpmndi:BPMNLabel><dc:Bounds x="${label[0]}" y="${label[1]}" width="${label[2]}" height="${label[3]}" /></bpmndi:BPMNLabel>` : '') +
    '</bpmndi:BPMNShape>';

const edge = (id: string, points: [number, number][], label?: [number, number, number, number]) =>
    `<bpmndi:BPMNEdge id="${id}_di" bpmnElement="${id}">${points.map(([x, y]) => `<di:waypoint x="${x}" y="${y}" />`).join('')}` +
    (label ? `<bpmndi:BPMNLabel><dc:Bounds x="${label[0]}" y="${label[1]}" width="${label[2]}" height="${label[3]}" /></bpmndi:BPMNLabel>` : '') +
    '</bpmndi:BPMNEdge>';

const document = (semantic: string, di: string, root = '<bpmn:process id="Process_1" isExecutable="true">') => `<?xml version="1.0" encoding="UTF-8"?>
<bpmn:definitions xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL" xmlns:bpmndi="http://www.omg.org/spec/BPMN/20100524/DI" xmlns:dc="http://www.omg.org/spec/DD/20100524/DC" xmlns:di="http://www.omg.org/spec/DD/20100524/DI" id="Definitions_1" targetNamespace="http://bpmn.io/schema/bpmn">
  ${root}${semantic}</bpmn:process>
  <bpmndi:BPMNDiagram id="Diagram_1">
    <bpmndi:BPMNPlane id="Plane_1" bpmnElement="Process_1">${di}</bpmndi:BPMNPlane>
  </bpmndi:BPMNDiagram>
</bpmn:definitions>`;

// Start -> A -> B, in a row: Flow_1 is straight, Flow_2 has a bend and a label.
const ROW = document(
    `<bpmn:startEvent id="Start" name="Go"><bpmn:outgoing>Flow_1</bpmn:outgoing></bpmn:startEvent>
    <bpmn:task id="A"><bpmn:incoming>Flow_1</bpmn:incoming><bpmn:outgoing>Flow_2</bpmn:outgoing></bpmn:task>
    <bpmn:task id="B"><bpmn:incoming>Flow_2</bpmn:incoming></bpmn:task>
    <bpmn:sequenceFlow id="Flow_1" sourceRef="Start" targetRef="A" />
    <bpmn:sequenceFlow id="Flow_2" name="Then" sourceRef="A" targetRef="B" />`,
    shape('Start', 100, 102, 36, 36, '', [105, 145, 26, 14]) +
    shape('A', 200, 80, 100, 80) +
    shape('B', 400, 200, 100, 80) +
    edge('Flow_1', [[136, 120], [200, 120]]) +
    edge('Flow_2', [[300, 120], [350, 120], [350, 240], [400, 240]], [355, 170, 30, 14])
);

// An expanded subprocess holding a task, with a boundary event on the subprocess, and a task outside it.
const CONTAINER = document(
    `<bpmn:subProcess id="Sub"><bpmn:task id="Inner" /></bpmn:subProcess>
    <bpmn:boundaryEvent id="Timer" attachedToRef="Sub"><bpmn:outgoing>Flow_out</bpmn:outgoing><bpmn:timerEventDefinition /></bpmn:boundaryEvent>
    <bpmn:task id="Outside"><bpmn:incoming>Flow_out</bpmn:incoming></bpmn:task>
    <bpmn:sequenceFlow id="Flow_out" sourceRef="Timer" targetRef="Outside" />`,
    shape('Sub', 100, 100, 300, 200, ' isExpanded="true"') +
    shape('Inner', 150, 150, 100, 80) +
    shape('Timer', 282, 282, 36, 36, '', [280, 320, 40, 14]) +
    shape('Outside', 500, 400, 100, 80) +
    edge('Flow_out', [[300, 318], [300, 440], [500, 440]])
);

// A pool with one lane holding a task.
const POOL = `<?xml version="1.0" encoding="UTF-8"?>
<bpmn:definitions xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL" xmlns:bpmndi="http://www.omg.org/spec/BPMN/20100524/DI" xmlns:dc="http://www.omg.org/spec/DD/20100524/DC" xmlns:di="http://www.omg.org/spec/DD/20100524/DI" id="Definitions_1" targetNamespace="http://bpmn.io/schema/bpmn">
  <bpmn:collaboration id="Collaboration_1"><bpmn:participant id="Pool" processRef="Process_1" /></bpmn:collaboration>
  <bpmn:process id="Process_1" isExecutable="true">
    <bpmn:laneSet id="LaneSet_1"><bpmn:lane id="Lane"><bpmn:flowNodeRef>Work</bpmn:flowNodeRef></bpmn:lane></bpmn:laneSet>
    <bpmn:task id="Work" />
  </bpmn:process>
  <bpmndi:BPMNDiagram id="Diagram_1">
    <bpmndi:BPMNPlane id="Plane_1" bpmnElement="Collaboration_1">
      ${shape('Pool', 0, 0, 600, 200, ' isHorizontal="true"')}
      ${shape('Lane', 30, 0, 570, 200, ' isHorizontal="true"')}
      ${shape('Work', 100, 60, 100, 80)}
    </bpmndi:BPMNPlane>
  </bpmndi:BPMNDiagram>
</bpmn:definitions>`;

async function loaded(xml: string) {
    const session = new BpmnModelingSession();
    const result = await session.load(xml);
    expect(result.imported).toBe(true);
    const document = session.document!;
    const planeId = document.planes()[0].planeId;
    const view = () => document.planeView(planeId)!;
    return {
        session,
        document,
        planeId,
        original: (await session.save()).xml,
        bounds: (id: string) => view().shapes.find(shape => shape.elementId === id)!.bounds,
        label: (id: string) => [...view().shapes, ...view().edges].find(drawn => drawn.elementId === id)!.label,
        waypoints: (id: string) => view().edges.find(edge => edge.elementId === id)!.waypoints.map(({ x, y }) => [x, y])
    };
}

describe('moveShapes', () => {
    it('moves a shape, and bends a straight edge at its moved end so it stays orthogonal, and undo puts back the document', async () => {
        const { session, document, planeId, original, bounds, waypoints } = await loaded(ROW);

        session.execute(moveShapes(document, planeId, ['A'], { x: 20, y: 40 }));

        expect(bounds('A')).toEqual({ x: 220, y: 120, width: 100, height: 80 });
        expect(waypoints('Flow_1')).toEqual([[136, 120], [178, 120], [178, 160], [220, 160]]);
        expect(session.undo()).toBe(true);
        expect((await session.save()).xml).toBe(original);
        expect(session.redo()).toBe(true);
        expect(waypoints('Flow_1')).toEqual([[136, 120], [178, 120], [178, 160], [220, 160]]);
    });

    it('keeps a straight edge straight when the move runs along it', async () => {
        const { session, document, planeId, waypoints } = await loaded(ROW);

        session.execute(moveShapes(document, planeId, ['A'], { x: 30, y: 0 }));

        expect(waypoints('Flow_1')).toEqual([[136, 120], [230, 120]]);
    });

    it('moves the bend next to the moved end on the axis that keeps its segment orthogonal, and the label with the midpoint', async () => {
        const { session, document, planeId, waypoints, label } = await loaded(ROW);

        session.execute(moveShapes(document, planeId, ['A'], { x: 10, y: -20 }));

        // The first segment was horizontal, so the bend follows vertically only.
        expect(waypoints('Flow_2')).toEqual([[310, 100], [350, 100], [350, 240], [400, 240]]);
        // The path's midpoint went from (350, 180) to (350, 175): the label moves up by 5.
        expect(label('Flow_2')).toEqual({ x: 355, y: 165, width: 30, height: 14 });
    });

    it('moves an edge between two moved shapes, and their labels, as they are', async () => {
        const { session, document, planeId, waypoints, label, bounds } = await loaded(ROW);

        session.execute(moveShapes(document, planeId, ['Start', 'A'], { x: 5, y: 5 }));

        expect(bounds('Start')).toEqual({ x: 105, y: 107, width: 36, height: 36 });
        expect(label('Start')).toEqual({ x: 110, y: 150, width: 26, height: 14 });
        expect(waypoints('Flow_1')).toEqual([[141, 125], [205, 125]]);
    });

    it('moves what an expanded subprocess contains and the boundary events attached to it', async () => {
        const { session, document, planeId, bounds, label, waypoints } = await loaded(CONTAINER);
        const command = moveShapes(document, planeId, ['Sub'], { x: 50, y: 0 });

        session.execute(command);

        expect(command.elementIds).toEqual(['Sub', 'Inner', 'Timer', 'Flow_out']);
        expect(bounds('Inner')).toEqual({ x: 200, y: 150, width: 100, height: 80 });
        expect(bounds('Timer')).toEqual({ x: 332, y: 282, width: 36, height: 36 });
        expect(label('Timer')).toEqual({ x: 330, y: 320, width: 40, height: 14 });
        // The first segment is vertical, so its bend follows horizontally only.
        expect(waypoints('Flow_out')).toEqual([[350, 318], [350, 440], [500, 440]]);
        expect(bounds('Outside')).toEqual({ x: 500, y: 400, width: 100, height: 80 });
    });

    it('moves a participant with its lanes and what they hold, and leaves lane membership as it was', async () => {
        const { session, document, planeId, bounds } = await loaded(POOL);

        session.execute(moveShapes(document, planeId, ['Pool'], { x: 0, y: 100 }));

        expect(bounds('Lane')).toEqual({ x: 30, y: 100, width: 570, height: 200 });
        expect(bounds('Work')).toEqual({ x: 100, y: 160, width: 100, height: 80 });
        expect((await document.write())).toContain('<bpmn:flowNodeRef>Work</bpmn:flowNodeRef>');
    });

    it('refuses what cannot move on its own, and what is not on the plane', async () => {
        const row = await loaded(ROW);
        const container = await loaded(CONTAINER);
        const pool = await loaded(POOL);

        expect(() => moveShapes(container.document, container.planeId, ['Timer'], { x: 1, y: 0 })).toThrow("The boundary event 'Timer' moves with the activity it is attached to.");
        expect(() => moveShapes(pool.document, pool.planeId, ['Lane'], { x: 1, y: 0 })).toThrow("The lane 'Lane' moves with its participant.");
        expect(() => moveShapes(row.document, row.planeId, ['Missing'], { x: 1, y: 0 })).toThrow("The plane 'Plane_1' has no shape for 'Missing'.");
        expect(() => moveShapes(row.document, row.planeId, ['Flow_1'], { x: 1, y: 0 })).toThrow("The plane 'Plane_1' has no shape for 'Flow_1'.");
        expect(() => moveShapes(row.document, 'Nowhere', ['A'], { x: 1, y: 0 })).toThrow("The document has no plane 'Nowhere'.");
        expect(() => moveShapes(row.document, row.planeId, [], { x: 1, y: 0 })).toThrow('A move needs at least one element.');
        expect(() => moveShapes(row.document, row.planeId, ['A'], { x: Number.NaN, y: 0 })).toThrow('A move needs a finite delta.');
        // A boundary event moves when its activity moves with it.
        expect(() => moveShapes(container.document, container.planeId, ['Sub', 'Timer'], { x: 1, y: 0 })).not.toThrow();
    });

    it('moves from the layout at the time it executes, so two moves in one compound add up and undo as one', async () => {
        const { session, document, planeId, original, bounds } = await loaded(ROW);

        session.execute(compound([moveShapes(document, planeId, ['B'], { x: 10, y: 0 }), moveShapes(document, planeId, ['B'], { x: 10, y: 10 })]));

        expect(bounds('B')).toEqual({ x: 420, y: 210, width: 100, height: 80 });
        expect(session.undo()).toBe(true);
        expect((await session.save()).xml).toBe(original);
    });

    it('moves the shapes it was created for, even when an earlier command in the same compound renamed one', async () => {
        const { session, document, planeId, original, bounds } = await loaded(ROW);

        session.execute(compound([updateProperties(document, 'B', { id: 'Renamed' }), moveShapes(document, planeId, ['B'], { x: 10, y: 0 })]));

        expect(bounds('Renamed')).toEqual({ x: 410, y: 200, width: 100, height: 80 });
        expect(session.undo()).toBe(true);
        expect((await session.save()).xml).toBe(original);
    });

    it('keeps the foreign attributes of the waypoints it moves, and adds bend points without them', async () => {
        const xml = ROW
            .replace('xmlns:di="http://www.omg.org/spec/DD/20100524/DI"', 'xmlns:di="http://www.omg.org/spec/DD/20100524/DI" xmlns:vendor="http://vendor.example/schema"')
            .replace('<di:waypoint x="136" y="120" />', '<di:waypoint x="136" y="120" vendor:hint="start" />')
            .replace('<di:waypoint x="300" y="120" />', '<di:waypoint x="300" y="120" vendor:hint="first" />')
            .replace('<di:waypoint x="350" y="240" />', '<di:waypoint x="350" y="240" vendor:hint="bend" />');
        const { session, document, planeId } = await loaded(xml);

        session.execute(moveShapes(document, planeId, ['A'], { x: 10, y: 20 }));

        const saved = (await session.save()).xml;
        // Flow_1 gained two bend points; its ends keep their attributes.
        expect(saved).toContain('<di:waypoint x="136" y="120" vendor:hint="start" />');
        // Flow_2 kept its count, so every point keeps its own.
        expect(saved).toContain('<di:waypoint x="310" y="140" vendor:hint="first" />');
        expect(saved).toContain('<di:waypoint x="350" y="240" vendor:hint="bend" />');
        expect(saved.match(/vendor:hint=/g)).toHaveLength(3);
    });

    it('applies the elements given when the command was created, not later changes to that array', async () => {
        const { session, document, planeId, bounds } = await loaded(ROW);
        const elementIds = ['B'];
        const command = moveShapes(document, planeId, elementIds, { x: 10, y: 0 });

        elementIds.push('A');
        session.execute(command);

        expect(bounds('A')).toEqual({ x: 200, y: 80, width: 100, height: 80 });
    });
});

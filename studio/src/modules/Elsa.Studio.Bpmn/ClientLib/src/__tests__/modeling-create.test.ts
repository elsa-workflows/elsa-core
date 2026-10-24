// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { BpmnDocument, BpmnModelingSession, compound, CREATABLE_TYPES, createShape, moveShapes } from '../modeling/index.js';

const shape = (id: string, x: number, y: number, width: number, height: number, extra = '') =>
    `<bpmndi:BPMNShape id="${id}_di" bpmnElement="${id}"${extra}><dc:Bounds x="${x}" y="${y}" width="${width}" height="${height}" /></bpmndi:BPMNShape>`;

const definitions = (body: string) => `<?xml version="1.0" encoding="UTF-8"?>
<bpmn:definitions xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL" xmlns:bpmndi="http://www.omg.org/spec/BPMN/20100524/DI" xmlns:dc="http://www.omg.org/spec/DD/20100524/DC" xmlns:di="http://www.omg.org/spec/DD/20100524/DI" id="Definitions_1" targetNamespace="http://bpmn.io/schema/bpmn">
${body}
</bpmn:definitions>`;

// A process with an expanded subprocess holding a task, and a collapsed subprocess drawn on its own plane.
const PROCESS = definitions(`
  <bpmn:process id="Process_1" isExecutable="true">
    <bpmn:task id="A" />
    <bpmn:subProcess id="Sub"><bpmn:task id="Inner" /></bpmn:subProcess>
    <bpmn:subProcess id="Collapsed"><bpmn:task id="Hidden" /></bpmn:subProcess>
  </bpmn:process>
  <bpmndi:BPMNDiagram id="Diagram_1">
    <bpmndi:BPMNPlane id="Plane_1" bpmnElement="Process_1">
      ${shape('A', 100, 100, 100, 80)}
      ${shape('Sub', 300, 100, 300, 200, ' isExpanded="true"')}
      ${shape('Inner', 350, 150, 100, 80)}
      ${shape('Collapsed', 700, 100, 100, 80, ' isExpanded="false"')}
    </bpmndi:BPMNPlane>
  </bpmndi:BPMNDiagram>
  <bpmndi:BPMNDiagram id="Diagram_2">
    <bpmndi:BPMNPlane id="Plane_Collapsed" bpmnElement="Collapsed">${shape('Hidden', 100, 100, 100, 80)}</bpmndi:BPMNPlane>
  </bpmndi:BPMNDiagram>`);

// A pool with two lanes, and an empty process.
const COLLABORATION = definitions(`
  <bpmn:collaboration id="Collaboration_1"><bpmn:participant id="Pool" processRef="Process_1" /></bpmn:collaboration>
  <bpmn:process id="Process_1" isExecutable="true">
    <bpmn:laneSet id="LaneSet_1"><bpmn:lane id="Top" /><bpmn:lane id="Bottom" /></bpmn:laneSet>
  </bpmn:process>
  <bpmndi:BPMNDiagram id="Diagram_1">
    <bpmndi:BPMNPlane id="Plane_1" bpmnElement="Collaboration_1">
      ${shape('Pool', 0, 0, 600, 300, ' isHorizontal="true"')}
      ${shape('Top', 30, 0, 570, 150, ' isHorizontal="true"')}
      ${shape('Bottom', 30, 150, 570, 150, ' isHorizontal="true"')}
    </bpmndi:BPMNPlane>
  </bpmndi:BPMNDiagram>`);

/** The id of the element that holds the element with `id`. */
function parentOf(document: BpmnDocument, id: string): unknown {
    return (document.element(id)?.$parent as { id?: unknown } | undefined)?.id;
}

async function loaded(xml: string) {
    const session = new BpmnModelingSession();
    expect((await session.load(xml)).imported).toBe(true);
    const document = session.document!;
    return { session, document, original: (await session.save()).xml, save: async () => (await session.save()).xml };
}

describe('createShape', () => {
    it('creates a task centred on the point, in the process, and undo and redo take it out and put it back', async () => {
        const { session, document, original, save } = await loaded(PROCESS);
        const command = createShape(document, 'Plane_1', 'bpmn:Task', { x: 150.4, y: 400.6 });
        const [id] = command.elementIds;

        session.execute(command);

        expect(id).toMatch(/^Activity_[0-9a-z]{7}$/);
        expect(parentOf(document, id)).toBe('Process_1');
        expect(document.planeView('Plane_1')!.shapes.find(shape => shape.elementId === id)).toMatchObject({ diId: `${id}_di`, bounds: { x: 100, y: 361, width: 100, height: 80 } });
        const xml = await save();
        expect(xml).toContain(`<bpmn:task id="${id}" />\n  </bpmn:process>`);
        expect(xml).toContain(`<bpmndi:BPMNShape id="${id}_di" bpmnElement="${id}">\n        <dc:Bounds x="100" y="361" width="100" height="80" />`);

        expect(session.undo()).toBe(true);
        expect(await save()).toBe(original);
        expect(session.redo()).toBe(true);
        expect(await save()).toBe(xml);
    });

    it('creates inside the innermost expanded subprocess that holds the point, and on a collapsed subprocess\'s own plane', async () => {
        const { session, document } = await loaded(PROCESS);
        const inSub = createShape(document, 'Plane_1', 'bpmn:EndEvent', { x: 550, y: 250 });
        // A collapsed subprocess on the plane is not a container there.
        const onCollapsed = createShape(document, 'Plane_1', 'bpmn:StartEvent', { x: 750, y: 140 });
        const onItsPlane = createShape(document, 'Plane_Collapsed', 'bpmn:ExclusiveGateway', { x: 300, y: 140 });

        session.execute(compound([inSub, onCollapsed, onItsPlane]));

        expect(parentOf(document, inSub.elementIds[0])).toBe('Sub');
        expect(parentOf(document, onCollapsed.elementIds[0])).toBe('Process_1');
        expect(parentOf(document, onItsPlane.elementIds[0])).toBe('Collapsed');
        expect(document.planeOfElement(onItsPlane.elementIds[0])).toBe('Plane_Collapsed');
    });

    it('creates in a participant\'s process and its lane, and a text annotation outside every participant in the collaboration', async () => {
        const { session, document, original, save } = await loaded(COLLABORATION);
        const task = createShape(document, 'Plane_1', 'bpmn:Task', { x: 200, y: 220 });
        const note = createShape(document, 'Plane_1', 'bpmn:TextAnnotation', { x: 800, y: 50 });

        session.execute(compound([task, note]));

        const xml = await save();
        expect(parentOf(document, task.elementIds[0])).toBe('Process_1');
        expect(xml).toContain(`<bpmn:lane id="Bottom">\n        <bpmn:flowNodeRef>${task.elementIds[0]}</bpmn:flowNodeRef>`);
        expect(xml).toContain('<bpmn:lane id="Top" />');
        expect(parentOf(document, note.elementIds[0])).toBe('Collaboration_1');
        expect(xml).toContain(`<bpmn:textAnnotation id="${note.elementIds[0]}" />\n  </bpmn:collaboration>`);

        expect(session.undo()).toBe(true);
        expect(await save()).toBe(original);
    });

    it('creates a data object reference with a data object of its own', async () => {
        const { session, document, save } = await loaded(PROCESS);
        const command = createShape(document, 'Plane_1', 'bpmn:DataObjectReference', { x: 150, y: 400 });
        const [id, dataObjectId] = command.elementIds;

        session.execute(command);

        expect(id).toMatch(/^DataObjectReference_[0-9a-z]{7}$/);
        expect(dataObjectId).toMatch(/^DataObject_[0-9a-z]{7}$/);
        expect(await save()).toContain(`<bpmn:dataObjectReference id="${id}" dataObjectRef="${dataObjectId}" />\n    <bpmn:dataObject id="${dataObjectId}" />`);
    });

    it('gives each type the id prefix and size the bpmn-js designer gives it', async () => {
        const { document } = await loaded(PROCESS);
        const expected: Record<string, [RegExp, number, number]> = {
            'bpmn:StartEvent': [/^Event_/, 36, 36],
            'bpmn:IntermediateThrowEvent': [/^Event_/, 36, 36],
            'bpmn:EndEvent': [/^Event_/, 36, 36],
            'bpmn:ExclusiveGateway': [/^Gateway_/, 50, 50],
            'bpmn:Task': [/^Activity_/, 100, 80],
            'bpmn:DataObjectReference': [/^DataObjectReference_/, 36, 50],
            'bpmn:DataStoreReference': [/^DataStoreReference_/, 50, 50],
            'bpmn:TextAnnotation': [/^TextAnnotation_/, 100, 30]
        };
        expect([...CREATABLE_TYPES].sort()).toEqual(Object.keys(expected).sort());

        for (const [type, [prefix, width, height]] of Object.entries(expected)) {
            const session = new BpmnModelingSession();
            await session.load(PROCESS);
            const command = createShape(session.document!, 'Plane_1', type, { x: 1000, y: 1000 });
            session.execute(command);
            expect(command.elementIds[0], type).toMatch(prefix);
            expect(session.document!.planeView('Plane_1')!.shapes.at(-1)!.bounds, type).toEqual({ x: 1000 - width / 2, y: 1000 - height / 2, width, height });
        }
        expect(document.element('A')).toBeDefined();
    });

    it('works out where the element goes when it executes, so a container moved away earlier in a compound no longer holds it', async () => {
        const { session, document } = await loaded(PROCESS);
        const create = createShape(document, 'Plane_1', 'bpmn:Task', { x: 550, y: 250 });

        session.execute(compound([moveShapes(document, 'Plane_1', ['Sub'], { x: 0, y: 500 }), create]));

        expect(parentOf(document, create.elementIds[0])).toBe('Process_1');
    });

    it('picks the deepest container and lane by what contains what, not by the order the shapes are drawn in', async () => {
        // An inner subprocess and a child lane drawn before what contains them, as some tools order the DI.
        const { session, document } = await loaded(definitions(`
  <bpmn:collaboration id="Collaboration_1"><bpmn:participant id="Pool" processRef="Process_1" /></bpmn:collaboration>
  <bpmn:process id="Process_1" isExecutable="true">
    <bpmn:laneSet id="LaneSet_1"><bpmn:lane id="Parent"><bpmn:childLaneSet id="LaneSet_2"><bpmn:lane id="Child" /></bpmn:childLaneSet></bpmn:lane></bpmn:laneSet>
    <bpmn:subProcess id="Outer"><bpmn:subProcess id="Inner" /></bpmn:subProcess>
  </bpmn:process>
  <bpmndi:BPMNDiagram id="Diagram_1">
    <bpmndi:BPMNPlane id="Plane_1" bpmnElement="Collaboration_1">
      ${shape('Inner', 700, 100, 200, 150, ' isExpanded="true"')}
      ${shape('Child', 60, 0, 1140, 400, ' isHorizontal="true"')}
      ${shape('Pool', 0, 0, 1200, 400, ' isHorizontal="true"')}
      ${shape('Parent', 30, 0, 1170, 400, ' isHorizontal="true"')}
      ${shape('Outer', 600, 50, 400, 300, ' isExpanded="true"')}
    </bpmndi:BPMNPlane>
  </bpmndi:BPMNDiagram>`));
        const inInner = createShape(document, 'Plane_1', 'bpmn:Task', { x: 800, y: 175 });
        const inChild = createShape(document, 'Plane_1', 'bpmn:Task', { x: 300, y: 200 });

        session.execute(compound([inInner, inChild]));

        expect(parentOf(document, inInner.elementIds[0])).toBe('Inner');
        expect(parentOf(document, inChild.elementIds[0])).toBe('Process_1');
        const xml = (await session.save()).xml;
        expect(xml).toContain(`<bpmn:lane id="Child">\n            <bpmn:flowNodeRef>${inChild.elementIds[0]}</bpmn:flowNodeRef>`);
        expect(xml).toContain('<bpmn:lane id="Parent">\n        <bpmn:childLaneSet');
    });

    it('refuses an unknown plane or type, a position that is not finite, and a flow element outside every participant', async () => {
        const { document } = await loaded(PROCESS);
        expect(() => createShape(document, 'Nowhere', 'bpmn:Task', { x: 0, y: 0 })).toThrow("The document has no plane 'Nowhere'.");
        for (const type of ['bpmn:Participant', 'bpmn:SubProcess', 'bpmn:Group', 'bpmn:SequenceFlow']) {
            expect(() => createShape(document, 'Plane_1', type, { x: 0, y: 0 })).toThrow(`An element of type '${type}' cannot be created here.`);
        }
        expect(() => createShape(document, 'Plane_1', 'bpmn:Task', { x: Number.NaN, y: 0 })).toThrow('A create needs a finite position.');

        const collaboration = await loaded(COLLABORATION);
        expect(() => createShape(collaboration.document, 'Plane_1', 'bpmn:Task', { x: 800, y: 50 })).toThrow('On a collaboration, an element must be created inside a participant.');
    });
});

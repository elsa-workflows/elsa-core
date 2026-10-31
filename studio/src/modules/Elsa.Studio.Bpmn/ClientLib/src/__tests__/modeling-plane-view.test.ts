// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { parseXml } from '../load-warnings.js';
import { BpmnDocument, type BpmnPlaneView } from '../modeling/index.js';
import { fixture } from './modeling-fixtures.js';

const BPMNDI = 'http://www.omg.org/spec/BPMN/20100524/DI';

/** The ids of each plane's shapes and edges, read from the XML itself rather than through bpmn-moddle. */
function diIdsByPlane(xml: string): Record<string, { shapes: string[]; edges: string[] }> {
    const planes: Record<string, { shapes: string[]; edges: string[] }> = {};
    for (const plane of Array.from(parseXml(xml).getElementsByTagNameNS(BPMNDI, 'BPMNPlane'))) {
        const idsOf = (localName: string) => Array.from(plane.getElementsByTagNameNS(BPMNDI, localName)).map(element => element.getAttribute('id')!);
        planes[plane.getAttribute('id')!] = { shapes: idsOf('BPMNShape'), edges: idsOf('BPMNEdge') };
    }
    return planes;
}

async function views(file: string): Promise<{ document: BpmnDocument; byPlane: Record<string, BpmnPlaneView> }> {
    const { document } = await BpmnDocument.read(fixture(file));
    const byPlane = Object.fromEntries(document.planes().map(plane => [plane.planeId, document.planeView(plane.planeId)!]));
    return { document, byPlane };
}

describe('planeView', () => {
    it('shows every shape and edge of the preservation fixture exactly once, on the plane that holds it', async () => {
        const { byPlane } = await views('preservation.bpmn');
        const expected = diIdsByPlane(fixture('preservation.bpmn'));

        expect(Object.keys(byPlane)).toEqual(Object.keys(expected));
        for (const [planeId, view] of Object.entries(byPlane)) {
            expect(view.shapes.map(shape => shape.diId), planeId).toEqual(expected[planeId].shapes);
            expect(view.edges.map(edge => edge.diId), planeId).toEqual(expected[planeId].edges);
        }
    });

    it('keeps a collapsed subprocess on its parent plane, and its content on its own plane', async () => {
        const { document, byPlane } = await views('preservation.bpmn');

        expect(document.planes()).toEqual([
            { planeId: 'Plane_Main', diagramId: 'Diagram_Main', elementId: 'Collaboration_1', name: 'Main diagram' },
            { planeId: 'Plane_Collapsed', diagramId: 'Diagram_Collapsed', elementId: 'Sub_Collapsed' }
        ]);
        expect(byPlane.Plane_Main.shapes.find(shape => shape.elementId === 'Sub_Collapsed')).toMatchObject({ isExpanded: false, parentId: 'Participant_Main' });
        expect(byPlane.Plane_Main.shapes.map(shape => shape.elementId)).not.toContain('C_Task');
        expect(byPlane.Plane_Collapsed.shapes.map(shape => shape.elementId)).toEqual(['C_Start', 'C_Task', 'C_End']);
        expect(byPlane.Plane_Collapsed.shapes.every(shape => shape.parentId === undefined && shape.hiddenBy === undefined)).toBe(true);
        expect(document.planeOfElement('Sub_Collapsed')).toBe('Plane_Main');
        expect(document.planeOfElement('C_Task')).toBe('Plane_Collapsed');
    });

    it('gives each shape its bounds, label, container and attachment, and each edge its waypoints and ends', async () => {
        const { byPlane } = await views('preservation.bpmn');
        const shape = (id: string) => byPlane.Plane_Main.shapes.find(candidate => candidate.elementId === id);
        const edge = (id: string) => byPlane.Plane_Main.edges.find(candidate => candidate.elementId === id);

        expect(shape('Participant_Main')).toMatchObject({ type: 'bpmn:Participant', name: 'Order handling', isExpanded: true });
        expect(shape('Participant_Main')!.parentId).toBeUndefined();
        expect(shape('Participant_Partner')).toMatchObject({ isExpanded: false });
        expect(shape('Sub_L1')).toMatchObject({ isExpanded: true, parentId: 'Participant_Main' });
        expect(shape('Task_Deep')).toMatchObject({ type: 'bpmn:Task', name: 'Deep task', parentId: 'Sub_L3' });
        expect(shape('Task_Deep')!.hiddenBy).toBeUndefined();
        expect(shape('Boundary_Timer')).toMatchObject({ attachedToId: 'Sub_L2', parentId: 'Sub_L1' });
        expect(shape('Boundary_Timer')!.label).toEqual({ x: expect.any(Number), y: expect.any(Number), width: expect.any(Number), height: expect.any(Number) });

        expect(edge('L3_Flow_1')).toMatchObject({ type: 'bpmn:SequenceFlow', sourceId: 'L3_Start', targetId: 'Task_Deep' });
        expect(edge('L3_Flow_1')!.waypoints.length).toBeGreaterThanOrEqual(2);
        expect(edge('MessageFlow_1')).toMatchObject({ sourceId: 'Participant_Partner', targetId: 'Start_Msg' });
        expect(edge('Association_1')).toMatchObject({ sourceId: 'End_Main', targetId: 'Annotation_1' });
    });

    it('keeps the content an older tool drew on a collapsed subprocess\'s own plane, marked as hidden by it', async () => {
        const { byPlane } = await views('legacy-collapsed.bpmn');

        expect(byPlane.Plane_Legacy.shapes).toEqual([
            { diId: 'Sub_di', elementId: 'Sub', type: 'bpmn:SubProcess', name: 'Collapsed', bounds: { x: 100, y: 80, width: 100, height: 80 }, isExpanded: false },
            { diId: 'Inner_di', elementId: 'Inner', type: 'bpmn:Task', name: 'Inner', bounds: { x: 300, y: 300, width: 100, height: 80 }, parentId: 'Sub', hiddenBy: 'Sub' }
        ]);
    });

    it('returns undefined for an unknown plane', async () => {
        const { document } = await views('order.bpmn');

        expect(document.planeView('Nope')).toBeUndefined();
    });
});

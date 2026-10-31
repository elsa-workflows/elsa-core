// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { withGeneratedLayout } from '../layout.js';
import { computeLoadWarnings, lacksDiagramLayout } from '../load-warnings.js';

const BPMNDI = 'http://www.omg.org/spec/BPMN/20100524/DI';

function definitions(processes: string): string {
    return `<?xml version="1.0" encoding="UTF-8"?>
<bpmn:definitions xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL" xmlns:bpmndi="${BPMNDI}" xmlns:dc="http://www.omg.org/spec/DD/20100524/DC" id="Definitions_1" targetNamespace="https://example.com/process">${processes}</bpmn:definitions>`;
}

// Flows declared only on the sequence flow, with no incoming or outgoing on the nodes, as a generator might write them.
const process = (id: string) => `<bpmn:process id="${id}">
  <bpmn:startEvent id="${id}_Start" />
  <bpmn:task id="${id}_Task" />
  <bpmn:endEvent id="${id}_End" />
  <bpmn:sequenceFlow id="${id}_Flow1" sourceRef="${id}_Start" targetRef="${id}_Task" />
  <bpmn:sequenceFlow id="${id}_Flow2" sourceRef="${id}_Task" targetRef="${id}_End" />
</bpmn:process>`;

function drawnElements(xml: string): string[] {
    const document = new DOMParser().parseFromString(xml, 'application/xml');
    return ['BPMNShape', 'BPMNEdge']
        .flatMap(name => Array.from(document.getElementsByTagNameNS(BPMNDI, name)))
        .map(element => element.getAttribute('bpmnElement') ?? '')
        .sort();
}

describe('withGeneratedLayout', () => {
    it('returns a document that already draws something unchanged', async () => {
        const xml = definitions(`<bpmn:process id="P"><bpmn:task id="T" /></bpmn:process>
<bpmndi:BPMNDiagram id="D"><bpmndi:BPMNPlane id="DP" bpmnElement="P"><bpmndi:BPMNShape id="T_di" bpmnElement="T"><dc:Bounds x="0" y="0" width="100" height="80" /></bpmndi:BPMNShape></bpmndi:BPMNPlane></bpmndi:BPMNDiagram>`);

        expect(await withGeneratedLayout(xml)).toEqual({ xml, warnings: [] });
    });

    it('draws every node and flow of a process that has no layout, including flows its nodes do not reference', async () => {
        const original = definitions(process('P'));

        const result = await withGeneratedLayout(original);

        expect(lacksDiagramLayout(result.xml)).toBe(false);
        expect(drawnElements(result.xml)).toEqual(['P_End', 'P_Flow1', 'P_Flow2', 'P_Start', 'P_Task']);
        expect(result.warnings).toEqual([
            { kind: 'rewritten', message: "The document has no diagram layout. A layout for 'P' is generated, and a save writes it." }
        ]);
        expect(computeLoadWarnings(original, result.xml)).toEqual([]);
    });

    it('lays out the first process only, and says which others are not drawn', async () => {
        const result = await withGeneratedLayout(definitions(process('First') + process('Second')));

        expect(drawnElements(result.xml)).toEqual(['First_End', 'First_Flow1', 'First_Flow2', 'First_Start', 'First_Task']);
        expect(result.warnings).toEqual([
            { kind: 'rewritten', message: "The document has no diagram layout. A layout for 'First' is generated, and a save writes it." },
            { kind: 'rewritten', message: "Only the first process gets a generated layout, so 'Second' is not drawn. A save keeps it.", elementId: 'Second' }
        ]);
    });

    it('does not duplicate a reference a node already declares', async () => {
        const result = await withGeneratedLayout(definitions(`<bpmn:process id="P">
  <bpmn:startEvent id="S"><bpmn:outgoing>F</bpmn:outgoing></bpmn:startEvent>
  <bpmn:task id="T" />
  <bpmn:sequenceFlow id="F" sourceRef="S" targetRef="T" />
</bpmn:process>`));

        expect(result.xml.match(/<bpmn:outgoing>F<\/bpmn:outgoing>/g)).toHaveLength(1);
        expect(drawnElements(result.xml)).toEqual(['F', 'S', 'T']);
    });

    it('leaves a document without a process unchanged', async () => {
        const xml = definitions('<bpmn:message id="M" name="Order" />');

        expect(await withGeneratedLayout(xml)).toEqual({ xml, warnings: [] });
    });
});

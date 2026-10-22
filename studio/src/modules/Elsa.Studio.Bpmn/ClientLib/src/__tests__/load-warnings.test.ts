// @vitest-environment jsdom
import { readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { describe, expect, it } from 'vitest';
import { BPMN_ATTRIBUTE_DEFAULTS, computeLoadWarnings, lacksDiagramLayout } from '../load-warnings.js';
import { createModdle } from './moddle.js';

const NAMESPACES = 'xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL" xmlns:bpmndi="http://www.omg.org/spec/BPMN/20100524/DI" ' +
    'xmlns:dc="http://www.omg.org/spec/DD/20100524/DC" xmlns:di="http://www.omg.org/spec/DD/20100524/DI" ' +
    'xmlns:elsa="https://elsaworkflows.io/schemas/bpmn/v1" xmlns:vendor="https://example.com/schemas/vendor"';

function definitions(content: string, diagrams = ''): string {
    return `<?xml version="1.0" encoding="UTF-8"?>
<bpmn:definitions ${NAMESPACES} id="Definitions_1" targetNamespace="https://example.com/process">
  <bpmn:process id="Process_1">${content}</bpmn:process>${diagrams}
</bpmn:definitions>`;
}

/** What bpmn-js writes on save: the document read into bpmn-moddle and written back. */
async function roundTrip(xml: string): Promise<string> {
    const moddle = createModdle();
    const { rootElement } = await moddle.fromXML(xml);
    return (await moddle.toXML(rootElement, { format: true })).xml;
}

const shape = (id: string, x: number) => `<bpmndi:BPMNShape id="${id}_di" bpmnElement="${id}"><dc:Bounds x="${x}" y="80" width="100" height="80" /></bpmndi:BPMNShape>`;
const diagram = (id: string, plane: string, content: string) => `<bpmndi:BPMNDiagram id="${id}"><bpmndi:BPMNPlane id="${id}_plane" bpmnElement="${plane}">${content}</bpmndi:BPMNPlane></bpmndi:BPMNDiagram>`;

describe('computeLoadWarnings', () => {
    it('reports nothing for a document a save writes back unchanged', async () => {
        const original = definitions(`
            <bpmn:startEvent id="Start"><bpmn:outgoing>Flow</bpmn:outgoing></bpmn:startEvent>
            <bpmn:sequenceFlow id="Flow" sourceRef="Start" targetRef="Task" />
            <bpmn:task id="Task" name="Charge">
              <bpmn:extensionElements>
                <elsa:activityBinding activityType="Elsa.WriteLine"><elsa:input name="Text">"hello"</elsa:input></elsa:activityBinding>
                <vendor:setting key="retries">3</vendor:setting>
              </bpmn:extensionElements>
              <bpmn:incoming>Flow</bpmn:incoming>
            </bpmn:task>`,
            diagram('Diagram_1', 'Process_1', `${shape('Start', 100)}${shape('Task', 200)}
              <bpmndi:BPMNEdge id="Flow_di" bpmnElement="Flow"><di:waypoint x="136" y="120" /><di:waypoint x="200" y="120" /></bpmndi:BPMNEdge>`));

        expect(computeLoadWarnings(original, await roundTrip(original))).toEqual([]);
    });

    it('ignores prefixes, attribute order and whitespace', () => {
        const original = definitions('<bpmn:task id="Task" name="A" startQuantity="2" />');
        const saved = `<model:definitions xmlns:model="http://www.omg.org/spec/BPMN/20100524/MODEL" targetNamespace="https://example.com/process" id="Definitions_1">
  <model:process id="Process_1">

    <model:task startQuantity="2" name="A" id="Task"/>
  </model:process>
</model:definitions>`;

        expect(computeLoadWarnings(original, saved)).toEqual([]);
    });

    it('compares xsi:type by the type it names, not by its prefix', () => {
        const original = definitions('<bpmn:sequenceFlow id="Flow" sourceRef="A" targetRef="B"><bpmn:conditionExpression xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:type="bpmn:tFormalExpression">x</bpmn:conditionExpression></bpmn:sequenceFlow>');
        const saved = definitions('<bpmn:sequenceFlow id="Flow" sourceRef="A" targetRef="B"><bpmn:conditionExpression xmlns:i="http://www.w3.org/2001/XMLSchema-instance" xmlns:m="http://www.omg.org/spec/BPMN/20100524/MODEL" i:type="m:tFormalExpression">x</bpmn:conditionExpression></bpmn:sequenceFlow>');

        expect(computeLoadWarnings(original, saved)).toEqual([]);
    });

    it('reports a foreign element outside extensionElements as dropped', async () => {
        const original = definitions('<bpmn:task id="Task" /><vendor:annotation id="Note">kept by nobody</vendor:annotation>');

        const warnings = computeLoadWarnings(original, await roundTrip(original));

        expect(warnings).toEqual([{ kind: 'dropped', message: "'Note' (vendor:annotation) is lost.", elementId: 'Note' }]);
    });

    it('reports the second of two elements with the same id as dropped', () => {
        const original = definitions('<bpmn:task id="Task" name="first" /><bpmn:task id="Task" name="second" />');
        const saved = definitions('<bpmn:task id="Task" name="first" />');

        expect(computeLoadWarnings(original, saved)).toEqual([{ kind: 'dropped', message: "'Task' (bpmn:task) is lost.", elementId: 'Task' }]);
    });

    it('does not report an omitted attribute that held its BPMN default', () => {
        const original = definitions('<bpmn:task id="Task" startQuantity="1" isForCompensation="false" />');
        const saved = definitions('<bpmn:task id="Task" />');

        expect(computeLoadWarnings(original, saved)).toEqual([]);
    });

    it('reports an omitted attribute that held another value as dropped', () => {
        const original = definitions('<bpmn:task id="Task" startQuantity="2" vendor:priority="high" />');
        const saved = definitions('<bpmn:task id="Task" />');

        expect(computeLoadWarnings(original, saved)).toEqual([
            { kind: 'dropped', message: "The attribute 'startQuantity' of 'Task' is lost.", elementId: 'Task' },
            { kind: 'dropped', message: "The attribute 'vendor:priority' of 'Task' is lost.", elementId: 'Task' }
        ]);
    });

    it('applies BPMN defaults to BPMN elements only', () => {
        const original = definitions('<bpmn:task id="Task"><bpmn:extensionElements><vendor:limit minimum="0" /></bpmn:extensionElements></bpmn:task>');
        const saved = definitions('<bpmn:task id="Task"><bpmn:extensionElements><vendor:limit /></bpmn:extensionElements></bpmn:task>');

        expect(computeLoadWarnings(original, saved)).toEqual([
            { kind: 'dropped', message: "The attribute 'minimum' of a vendor:limit element inside 'Task' is lost.", elementId: 'Task' }
        ]);
    });

    it('reports a changed attribute or text as rewritten, and lost text as dropped', () => {
        const original = definitions('<bpmn:task id="A" name="Before"><bpmn:documentation>Docs</bpmn:documentation></bpmn:task><bpmn:task id="B"><bpmn:documentation>Docs</bpmn:documentation></bpmn:task>');
        const saved = definitions('<bpmn:task id="A" name="After"><bpmn:documentation>Other</bpmn:documentation></bpmn:task><bpmn:task id="B"><bpmn:documentation /></bpmn:task>');

        expect(computeLoadWarnings(original, saved)).toEqual([
            { kind: 'rewritten', message: "The attribute 'name' of 'A' changes from 'Before' to 'After'.", elementId: 'A' },
            { kind: 'rewritten', message: "The text of a bpmn:documentation element inside 'A' changes.", elementId: 'A' },
            { kind: 'dropped', message: "The text of a bpmn:documentation element inside 'B' is lost.", elementId: 'B' }
        ]);
    });

    it('reports lost comments as dropped', async () => {
        const original = definitions('<!-- one --><bpmn:task id="Task" /><!-- two -->');

        expect(computeLoadWarnings(original, await roundTrip(original))).toEqual([{ kind: 'dropped', message: '2 comments are lost.' }]);
    });

    it('does not report content the save adds', () => {
        const original = definitions('<bpmn:task id="Task" />');
        const saved = definitions('<bpmn:task id="Task" name="Added"><bpmn:incoming>Flow</bpmn:incoming></bpmn:task><bpmn:task id="Other" />');

        expect(computeLoadWarnings(original, saved)).toEqual([]);
    });

    describe('diagram layout', () => {
        const content = '<bpmn:subProcess id="Sub"><bpmn:task id="Inner" /></bpmn:subProcess>';

        it('reports layout that moves to another plane unchanged as rewritten, once', () => {
            const original = definitions(content, diagram('D1', 'Process_1', `${shape('Sub', 100)}${shape('Inner', 300)}`));
            const saved = definitions(content, diagram('D1', 'Process_1', shape('Sub', 100)) + diagram('D2', 'Sub', shape('Inner', 300)));

            expect(computeLoadWarnings(original, saved)).toEqual([
                { kind: 'rewritten', message: "The diagram layout of 'Inner' moves to the diagram of 'Sub', unchanged.", elementId: 'Inner' }
            ]);
        });

        it('reports layout that moves to another plane and changes as one rewritten warning', () => {
            const original = definitions(content, diagram('D1', 'Process_1', `${shape('Sub', 100)}${shape('Inner', 300)}`));
            const saved = definitions(content, diagram('D1', 'Process_1', shape('Sub', 100)) + diagram('D2', 'Sub', shape('Inner', 180)));

            expect(computeLoadWarnings(original, saved)).toEqual([
                { kind: 'rewritten', message: "The diagram layout of 'Inner' moves to the diagram of 'Sub', and its position or size changes.", elementId: 'Inner' }
            ]);
        });

        it('ignores the ids of diagram elements', () => {
            const original = definitions(content, diagram('D1', 'Process_1', `${shape('Sub', 100)}${shape('Inner', 300)}`));
            const saved = original.replaceAll('_di"', '_shape"').replace('D1', 'Diagram').replace('D1_plane', 'Plane');

            expect(computeLoadWarnings(original, saved)).toEqual([]);
        });

        it('reports changed geometry as rewritten, and lost layout as dropped', () => {
            const original = definitions(content, diagram('D1', 'Process_1', `${shape('Sub', 100)}${shape('Inner', 300)}`));
            const saved = definitions(content, diagram('D1', 'Process_1', shape('Sub', 150)));

            expect(computeLoadWarnings(original, saved)).toEqual([
                { kind: 'rewritten', message: "The diagram layout of 'Sub' changes.", elementId: 'Sub' },
                { kind: 'dropped', message: "The diagram layout of 'Inner' is lost.", elementId: 'Inner' }
            ]);
        });

        it('reports a lost or changed diagram attribute', () => {
            const original = definitions(content, diagram('D1', 'Process_1',
                '<bpmndi:BPMNShape id="Sub_di" bpmnElement="Sub" isExpanded="true" vendor:color="red"><dc:Bounds x="1" y="2" width="3" height="4" /></bpmndi:BPMNShape>'));
            const saved = definitions(content, diagram('D1', 'Process_1',
                '<bpmndi:BPMNShape id="Sub_di" bpmnElement="Sub" isExpanded="false"><dc:Bounds x="1" y="2" width="3" height="4" /></bpmndi:BPMNShape>'));

            expect(computeLoadWarnings(original, saved)).toEqual([
                { kind: 'rewritten', message: "The diagram attribute 'isExpanded' of 'Sub' changes.", elementId: 'Sub' },
                { kind: 'dropped', message: "The diagram attribute 'vendor:color' of 'Sub' is lost.", elementId: 'Sub' }
            ]);
        });
    });

    it('refuses a document that is not well-formed', () => {
        expect(() => computeLoadWarnings('<bpmn:definitions', definitions(''))).toThrow(/not well-formed/);
    });
});

describe('lacksDiagramLayout', () => {
    it('is true only for a document that draws nothing', () => {
        expect(lacksDiagramLayout(definitions('<bpmn:task id="Task" />'))).toBe(true);
        expect(lacksDiagramLayout(definitions('<bpmn:task id="Task" />', diagram('D1', 'Process_1', '')))).toBe(true);
        expect(lacksDiagramLayout(definitions('<bpmn:task id="Task" />', diagram('D1', 'Process_1', shape('Task', 1))))).toBe(false);
    });
});

describe('BPMN_ATTRIBUTE_DEFAULTS', () => {
    it("matches every attribute default in bpmn-moddle's BPMN schema", () => {
        const require = createRequire(import.meta.url);
        const schema = JSON.parse(readFileSync(require.resolve('bpmn-moddle/resources/bpmn/json/bpmn.json'), 'utf8')) as {
            types: { properties?: { name: string; default?: unknown }[] }[];
        };
        const defaults: Record<string, string> = {};
        for (const type of schema.types) {
            for (const property of type.properties ?? []) {
                if (property.default !== undefined) defaults[property.name] = String(property.default);
            }
        }

        expect(BPMN_ATTRIBUTE_DEFAULTS).toEqual(defaults);
    });
});

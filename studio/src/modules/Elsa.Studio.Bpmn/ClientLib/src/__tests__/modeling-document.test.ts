// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { BpmnDocument } from '../modeling/index.js';
import { bpmnFiles, CORE_ASSETS, FIXTURES, fixture } from './modeling-fixtures.js';

async function roundTrip(xml: string) {
    const { document, warnings } = await BpmnDocument.read(xml);
    return { document, warnings, saved: await document.write() };
}

describe('reading and writing', () => {
    it.each([...bpmnFiles(FIXTURES), ...bpmnFiles(CORE_ASSETS)])('%s saves what its read reported, and its save round-trips unchanged', async (_file, xml) => {
        const first = await roundTrip(xml);
        const second = await roundTrip(first.saved);

        expect(second.warnings).toEqual([]);
        expect(second.saved).toBe(first.saved);
    });

    // bpmn-js reports none for these (studio/test/browser/BpmnDesigner). The legacy layout stays where it is drawn.
    it.each(['order.bpmn', 'preservation.bpmn', 'foreign-attributes.bpmn', 'legacy-collapsed.bpmn'])('%s reads with no warnings', async file => {
        const xml = fixture(file);

        expect((await BpmnDocument.read(xml)).warnings).toEqual([]);
    });

    // The core assets that carry their own layout; bpmn-js reports none for them either.
    it.each(['camunda-order-process.bpmn', 'subprocess-boundary-events.bpmn', 'transaction-compensation.bpmn'])('core asset %s reads with no warnings', async file => {
        const xml = Object.fromEntries(bpmnFiles(CORE_ASSETS))[file];

        expect((await BpmnDocument.read(xml)).warnings).toEqual([]);
    });

    it('reports what a save loses', async () => {
        const xml = fixture('lossy.bpmn');

        const { warnings } = await BpmnDocument.read(xml);

        expect(warnings.some(warning => warning.kind === 'dropped')).toBe(true);
    });

    it('generates a layout for a document without one, and says so', async () => {
        const xml = fixture('no-layout.bpmn');

        const { document, warnings } = await BpmnDocument.read(xml);

        expect(warnings).toEqual([expect.objectContaining({ kind: 'rewritten' })]);
        expect(document.planes()).toHaveLength(1);
        for (const id of ['Start', 'Work', 'End', 'Flow_1', 'Flow_2']) expect(document.planeOfElement(id), id).toBe(document.planes()[0].planeId);
    });

    it.each([
        ['content that is not XML', '<bpmn:definitions'],
        ['a root that is not BPMN definitions', '<?xml version="1.0"?><bpmn:process xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL" id="P" />']
    ])('refuses %s', async (_case, xml) => {
        await expect(BpmnDocument.read(xml)).rejects.toThrow();
    });

    it('writes the tree as it is when write is called, not as a later edit leaves it', async () => {
        const { document } = await BpmnDocument.read(fixture('order.bpmn'));

        const writing = document.write();
        document.element('Review')!.name = 'Renamed';

        expect(await writing).toContain('name="Review order"');
    });
});

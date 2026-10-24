// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { BpmnModelingSession, compound, updateProperties } from '../modeling/index.js';
import { fixture } from './modeling-fixtures.js';

async function loaded(file = 'order.bpmn') {
    const session = new BpmnModelingSession();
    await session.load(fixture(file));
    return { session, document: session.document!, original: (await session.save()).xml };
}

describe('updateProperties', () => {
    it('sets and removes properties and foreign attributes, and undo puts back exactly what was there', async () => {
        const { session, document, original } = await loaded();

        session.execute(updateProperties(document, 'Review', { name: 'Check order', 'vendor:priority': 'high' }));
        session.execute(updateProperties(document, 'Start', { name: undefined }));

        const { xml } = await session.save();
        expect(xml).toContain('<bpmn:userTask id="Review" name="Check order" vendor:priority="high">');
        expect(xml).toContain('<bpmn:startEvent id="Start">');
        expect(session.undo() && session.undo()).toBe(true);
        expect((await session.save()).xml).toBe(original);
    });

    it('renames an element: references follow, the index finds it by its new id, and undo renames it back', async () => {
        const { session, document, original } = await loaded();
        const changes: string[][] = [];
        session.onChange(change => changes.push(change.elementIds));

        session.execute(updateProperties(document, 'Review', { id: 'Check' }));

        expect(changes).toEqual([['Review', 'Check']]);
        expect(document.element('Review')).toBeUndefined();
        expect(document.element('Check')?.name).toBe('Review order');
        expect(document.planeOfElement('Check')).toBe('Plane_Order');
        const { xml } = await session.save();
        expect(xml).toContain('<bpmn:sequenceFlow id="Flow_4" sourceRef="Notify" targetRef="Check" />');
        expect(xml).toContain('bpmnElement="Check"');

        session.undo();
        expect(document.element('Check')).toBeUndefined();
        expect(document.element('Review')).toBeDefined();
        expect((await session.save()).xml).toBe(original);
    });

    it('refuses an id another element has, a diagram element\'s included, and an unknown element', async () => {
        const { document } = await loaded();

        expect(() => updateProperties(document, 'Review', { id: 'Charge' })).toThrow(/already has/);
        expect(() => updateProperties(document, 'Review', { id: 'Review_di' })).toThrow(/already has/);
        expect(() => updateProperties(document, 'Review', { id: '' })).toThrow(/non-empty/);
        expect(() => updateProperties(document, 'Nope', { name: 'x' })).toThrow(/no element/);
    });
});

describe('compound', () => {
    it('executes in order, reverts in reverse, and undoes as one step reporting every element', async () => {
        const { session, document, original } = await loaded();
        const changes: string[][] = [];
        session.onChange(change => changes.push(change.elementIds));

        // Both set the same property: only executing in order and reverting in reverse ends at 'Second', then the original.
        session.execute(compound([
            updateProperties(document, 'Review', { name: 'First' }),
            updateProperties(document, 'Review', { name: 'Second' }),
            updateProperties(document, 'Charge', { name: 'Charge' })
        ]));

        expect(document.element('Review')?.name).toBe('Second');
        expect(changes).toEqual([['Review', 'Charge']]);
        expect(session.undo()).toBe(true);
        expect(session.undo()).toBe(false);
        expect((await session.save()).xml).toBe(original);
    });
});

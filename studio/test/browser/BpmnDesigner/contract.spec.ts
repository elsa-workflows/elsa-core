// BPMN designer contract, version 1 (studio/docs/specs/bpmn-designer-contract/contract.md). Every designer passes these.
import type { Page } from '@playwright/test';
import { expect, fixture, test } from './harness.js';

type Harness = {
    module: { designerId: string; contractVersion: number };
    designer: Record<string, (...args: unknown[]) => unknown>;
    events: { type: string; [key: string]: unknown }[];
    create(options?: { readOnly?: boolean; withProperties?: boolean }): void;
};

function create(page: Page, options: { readOnly?: boolean; withProperties?: boolean } = {}): Promise<void> {
    return page.evaluate(options => (window as unknown as { harness: Harness }).harness.create(options), options);
}

/** Calls a designer method in the page and returns its (awaited) result. */
function call<T = unknown>(page: Page, method: string, ...args: unknown[]): Promise<T> {
    return page.evaluate(([method, args]) => (window as unknown as { harness: Harness }).harness.designer[method as string](...(args as unknown[])), [method, args] as const) as Promise<T>;
}

function events(page: Page, type?: string): Promise<Harness['events']> {
    return page.evaluate(type => (window as unknown as { harness: Harness }).harness.events.filter(event => !type || event.type === type), type);
}

function clearEvents(page: Page): Promise<void> {
    return page.evaluate(() => {
        (window as unknown as { harness: Harness }).harness.events = [];
    });
}

async function loaded(page: Page, name: string, options: { readOnly?: boolean; withProperties?: boolean } = {}) {
    await create(page, options);
    return call<{ imported: boolean; error?: string; warnings: { kind: string; message: string; elementId?: string }[] }>(page, 'load', await fixture(name));
}

const CHARGE_BINDING = { activityType: 'Elsa.WriteLine', inputs: [{ name: 'Text', value: '"Charging"' }] };

test('the module declares its id and contract version 1', async ({ harness }) => {
    const module = await harness.evaluate(() => {
        const { designerId, contractVersion } = (window as unknown as { harness: Harness }).harness.module;
        return { designerId, contractVersion };
    });

    expect(module.designerId).toMatch(/^[a-z0-9-]+$/);
    expect(module.contractVersion).toBe(1);
});

test.describe('load', () => {
    test('imports a document it represents fully, with no warnings and no events', async ({ harness }) => {
        expect(await loaded(harness, 'order.bpmn')).toEqual({ imported: true, warnings: [] });
        expect(await events(harness)).toEqual([]);
    });

    test('reports a document it cannot show', async ({ harness }) => {
        await create(harness);

        const result = await call<{ imported: boolean; error?: string; warnings: unknown[] }>(harness, 'load', '<bpmn:definitions');

        expect(result.imported).toBe(false);
        expect(result.error).toBeTruthy();
        expect(result.warnings).toEqual([]);
    });

    test('reports content a save would lose as dropped', async ({ harness }) => {
        const result = await loaded(harness, 'lossy.bpmn');

        expect(result.imported).toBe(true);
        expect(result.warnings).toEqual(expect.arrayContaining([
            expect.objectContaining({ kind: 'dropped', elementId: 'Note' }),
            { kind: 'dropped', message: 'A comment is lost.' }
        ]));
    });

    test('reports layout that moves to its own diagram plane, unchanged, as rewritten', async ({ harness }) => {
        const result = await loaded(harness, 'legacy-collapsed.bpmn');

        expect(result.imported).toBe(true);
        expect(result.warnings).toEqual([expect.objectContaining({ kind: 'rewritten', elementId: 'Inner' })]);
    });

    test('draws a document without layout, and says the layout is generated', async ({ harness }) => {
        const result = await loaded(harness, 'no-layout.bpmn');

        expect(result.imported).toBe(true);
        expect(result.warnings).toEqual([expect.objectContaining({ kind: 'rewritten' })]);
        for (const id of ['Start', 'Work', 'End', 'Flow_1', 'Flow_2']) expect(await call(harness, 'select', id)).toBe(true);
    });
});

test.describe('save', () => {
    test('returns the document, keeping bindings and foreign extension content in place', async ({ harness }) => {
        await loaded(harness, 'order.bpmn');

        const { xml } = await call<{ xml: string }>(harness, 'save');

        expect(xml).toMatch(/<vendor:retry count="3"\s*\/>\s*<elsa:activityBinding activityType="Elsa.WriteLine">\s*<elsa:input name="Text">"Charging"<\/elsa:input>\s*<\/elsa:activityBinding>\s*<vendor:audit level="full"\s*\/>/);
    });

    test('a saved document loads again with no warnings', async ({ harness }) => {
        await loaded(harness, 'order.bpmn');
        const { xml } = await call<{ xml: string }>(harness, 'save');

        expect(await call(harness, 'load', xml)).toEqual({ imported: true, warnings: [] });
    });
});

test.describe('select', () => {
    test('selects an element by id, raises selection, and returns false for an unknown id', async ({ harness }) => {
        await loaded(harness, 'order.bpmn');

        expect(await call(harness, 'select', 'Charge')).toBe(true);
        expect(await call(harness, 'select', 'Nope')).toBe(false);
        expect(await call(harness, 'select', null)).toBe(true);

        expect(await events(harness, 'selection')).toEqual([
            { type: 'selection', elementIds: ['Charge'], primary: { id: 'Charge', type: 'bpmn:Task', name: 'Charge card' } },
            { type: 'selection', elementIds: [], primary: null }
        ]);
    });
});

test.describe('bindings', () => {
    test('getBinding reads a binding, and returns null for an element without one', async ({ harness }) => {
        await loaded(harness, 'order.bpmn');

        expect(await call(harness, 'getBinding', 'Charge')).toEqual(CHARGE_BINDING);
        expect(await call(harness, 'getBinding', 'Review')).toBeNull();
    });

    test('setBinding replaces a binding in place as one undo step, and raises change', async ({ harness }) => {
        await loaded(harness, 'order.bpmn');
        const binding = { activityType: 'Elsa.Http.SendHttpRequest', inputs: [{ name: 'Url', value: '"https://example.com"' }] };

        expect(await call(harness, 'setBinding', 'Charge', binding)).toEqual({ applied: true });

        expect(await call(harness, 'getBinding', 'Charge')).toEqual(binding);
        const { xml } = await call<{ xml: string }>(harness, 'save');
        expect(xml).toMatch(/<vendor:retry count="3"\s*\/>\s*<elsa:activityBinding activityType="Elsa.Http.SendHttpRequest">[\s\S]*?<\/elsa:activityBinding>\s*<vendor:audit level="full"\s*\/>/);
        expect(await events(harness, 'change')).toEqual([
            expect.objectContaining({ type: 'change', elementIds: expect.arrayContaining(['Charge']), isDirty: true, canUndo: true, canRedo: false })
        ]);

        expect(await call(harness, 'undo')).toBe(true);
        expect(await call(harness, 'getBinding', 'Charge')).toEqual(CHARGE_BINDING);
        expect(await call(harness, 'redo')).toBe(true);
        expect(await call(harness, 'getBinding', 'Charge')).toEqual(binding);
    });

    test('setBinding adds a binding to an element without extension content, and removes it again', async ({ harness }) => {
        await loaded(harness, 'order.bpmn');

        expect(await call(harness, 'setBinding', 'Review', CHARGE_BINDING)).toEqual({ applied: true });
        expect(await call(harness, 'getBinding', 'Review')).toEqual(CHARGE_BINDING);
        expect(await call(harness, 'setBinding', 'Review', null)).toEqual({ applied: true });
        expect(await call(harness, 'getBinding', 'Review')).toBeNull();

        const { xml } = await call<{ xml: string }>(harness, 'save');
        expect(xml).toMatch(/<bpmn:userTask id="Review" name="Review order">\s*<bpmn:incoming>/);
    });

    test('setBinding removing a binding keeps the foreign extension content', async ({ harness }) => {
        await loaded(harness, 'order.bpmn');

        expect(await call(harness, 'setBinding', 'Charge', null)).toEqual({ applied: true });

        const { xml } = await call<{ xml: string }>(harness, 'save');
        expect(xml).toMatch(/<bpmn:extensionElements>\s*<vendor:retry count="3"\s*\/>\s*<vendor:audit level="full"\s*\/>\s*<\/bpmn:extensionElements>/);
    });

    test('setBinding refuses an unknown element, an element that cannot carry a binding, and a message task', async ({ harness }) => {
        await loaded(harness, 'order.bpmn');

        expect(await call(harness, 'setBinding', 'Nope', CHARGE_BINDING)).toEqual({ applied: false, reason: 'unknown-element' });
        expect(await call(harness, 'setBinding', 'Gateway', CHARGE_BINDING)).toEqual({ applied: false, reason: 'not-bindable' });
        expect(await call(harness, 'setBinding', 'Start', CHARGE_BINDING)).toEqual({ applied: false, reason: 'not-bindable' });
        expect(await call(harness, 'setBinding', 'Notify', CHARGE_BINDING)).toEqual({ applied: false, reason: 'not-bindable' });
        expect(await events(harness, 'change')).toEqual([]);
    });

    test('setBinding throws for a binding Elsa would refuse', async ({ harness }) => {
        await loaded(harness, 'order.bpmn');

        await expect(call(harness, 'setBinding', 'Review', { activityType: '', inputs: [] })).rejects.toThrow(/activity type/);
        await expect(call(harness, 'setBinding', 'Review', { activityType: 'A', inputs: [{ name: 'X', value: '1' }, { name: 'X', value: '2' }] })).rejects.toThrow(/twice/);
    });
});

test.describe('change, undo and redo', () => {
    test('save marks the document clean, and undo past the save makes it dirty again', async ({ harness }) => {
        await loaded(harness, 'order.bpmn');
        await call(harness, 'setBinding', 'Review', CHARGE_BINDING);
        await call(harness, 'save');
        await clearEvents(harness);

        await call(harness, 'undo');
        await call(harness, 'redo');

        expect((await events(harness, 'change')).map(event => event.isDirty)).toEqual([true, false]);
    });

    test('an edit after an undo is dirty, though the history has the same length', async ({ harness }) => {
        await loaded(harness, 'order.bpmn');
        await call(harness, 'setBinding', 'Review', CHARGE_BINDING);
        await call(harness, 'save');
        await call(harness, 'undo');
        await clearEvents(harness);

        await call(harness, 'setBinding', 'Review', { activityType: 'Elsa.Other', inputs: [] });

        expect(await events(harness, 'change')).toEqual([expect.objectContaining({ isDirty: true, canUndo: true, canRedo: false })]);
    });

    test('undo and redo return false when there is nothing to do', async ({ harness }) => {
        await loaded(harness, 'order.bpmn');

        expect(await call(harness, 'undo')).toBe(false);
        expect(await call(harness, 'redo')).toBe(false);
    });
});

test.describe('read-only', () => {
    test('loads, saves and selects, but refuses every edit', async ({ harness }) => {
        expect(await loaded(harness, 'order.bpmn', { readOnly: true })).toEqual({ imported: true, warnings: [] });

        expect(await call(harness, 'getBinding', 'Charge')).toEqual(CHARGE_BINDING);
        expect(await call(harness, 'setBinding', 'Charge', null)).toEqual({ applied: false, reason: 'read-only' });
        expect(await call(harness, 'undo')).toBe(false);
        expect(await call(harness, 'select', 'Charge')).toBe(true);
        expect((await call<{ xml: string }>(harness, 'save')).xml).toContain('elsa:activityBinding');
        expect(await events(harness, 'change')).toEqual([]);
    });
});

test.describe('overlays', () => {
    test('applyOverlays replaces the overlay and reports unknown ids; null and clearOverlays clear it', async ({ harness }) => {
        await loaded(harness, 'order.bpmn');
        const badges = () => harness.locator('.elsa-bpmn-badge').count();

        expect(await call(harness, 'applyOverlays', { Charge: { started: 1, completed: 1 }, Review: { active: 1 }, Nope: { faulted: true } }))
            .toEqual({ applied: ['Charge', 'Review'], unknownElementIds: ['Nope'] });
        expect(await badges()).toBe(2);

        expect(await call(harness, 'applyOverlays', { Charge: { faulted: true } })).toEqual({ applied: ['Charge'], unknownElementIds: [] });
        expect(await badges()).toBe(1);

        expect(await call(harness, 'applyOverlays', null)).toEqual({ applied: [], unknownElementIds: [] });
        expect(await badges()).toBe(0);

        await call(harness, 'applyOverlays', { Charge: { completed: 2 } });
        expect(await call(harness, 'clearOverlays')).toEqual({ applied: [], unknownElementIds: [] });
        expect(await badges()).toBe(0);
    });
});

test.describe('dispose', () => {
    test('is idempotent, empties the hosts, and makes every other method throw', async ({ harness }) => {
        await loaded(harness, 'order.bpmn', { withProperties: true });

        await call(harness, 'dispose');
        await call(harness, 'dispose');

        expect(await harness.locator('#canvas').innerHTML()).toBe('');
        expect(await harness.locator('#properties').innerHTML()).toBe('');
        for (const [method, args] of [['load', ['<x/>']], ['save', []], ['select', [null]], ['getBinding', ['Charge']], ['setBinding', ['Charge', null]], ['undo', []], ['redo', []], ['applyOverlays', [null]], ['clearOverlays', []]] as const) {
            await expect(call(harness, method, ...args), method).rejects.toThrow(/disposed/);
        }
        await expect(harness.evaluate(() => (window as unknown as { harness: Harness }).harness.designer.on('change', () => {}))).rejects.toThrow(/disposed/);
    });
});

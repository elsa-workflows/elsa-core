// What the X6 designer draws, which the contract leaves to each designer. Runs only against the X6 designer.
import type { Page } from '@playwright/test';
import { expect, fixture, test } from './harness.js';

type Harness = {
    designer: Record<string, (...args: unknown[]) => unknown>;
    events: { type: string; [key: string]: unknown }[];
    create(options?: { readOnly?: boolean }): void;
};

async function loaded(page: Page, name: string, options: { readOnly?: boolean } = {}): Promise<void> {
    await page.evaluate(options => (window as unknown as { harness: Harness }).harness.create(options), options);
    const result = await page.evaluate(xml => (window as unknown as { harness: Harness }).harness.designer.load(xml), await fixture(name));
    expect(result).toMatchObject({ imported: true });
}

function call<T = unknown>(page: Page, method: string, ...args: unknown[]): Promise<T> {
    return page.evaluate(([method, args]) => (window as unknown as { harness: Harness }).harness.designer[method as string](...(args as unknown[])), [method, args] as const) as Promise<T>;
}

function selections(page: Page): Promise<unknown[]> {
    return page.evaluate(() => (window as unknown as { harness: Harness }).harness.events.filter(event => event.type === 'selection').map(({ type, ...event }) => event));
}

/** The cell that draws an element: its id is the element's id. */
const cell = (page: Page, id: string) => page.locator(`#canvas .x6-cell[data-cell-id="${id}"]`);

test('draws every shape and edge of a plane as a cell named by its element id, with names as labels', async ({ harness }) => {
    await loaded(harness, 'order.bpmn');
    for (const id of ['Start', 'Charge', 'Gateway', 'Notify', 'Review', 'End', 'Flow_1', 'Flow_2', 'Flow_3', 'Flow_4', 'Flow_5']) {
        await expect(cell(harness, id), id).toHaveCount(1);
    }
    await expect(cell(harness, 'Charge')).toContainText('Charge card');
    // An event's name is drawn as its own label, where the DI places it.
    await expect(cell(harness, 'Start::label')).toContainText('Order placed');
    await expect(cell(harness, 'Process_Order')).toHaveCount(0);
});

test('draws the content an older tool put on a collapsed subprocess\'s parent plane on a plane of its own', async ({ harness }) => {
    await loaded(harness, 'legacy-collapsed.bpmn');
    await expect(cell(harness, 'Sub')).toHaveCount(1);
    await expect(cell(harness, 'Inner')).toHaveCount(0);
    await expect(harness.locator('#canvas .elsa-x6-breadcrumbs')).toBeHidden();

    // Selecting hidden content opens the subprocess's plane.
    expect(await call(harness, 'select', 'Inner')).toBe(true);
    await expect(cell(harness, 'Inner')).toHaveCount(1);
    await expect(cell(harness, 'Sub')).toHaveCount(0);
    await expect(harness.locator('#canvas .elsa-x6-breadcrumb')).toHaveText(['Process_Legacy', 'Collapsed']);

    // The breadcrumb goes back, and the selection it no longer shows is cleared.
    await harness.locator('#canvas .elsa-x6-breadcrumb', { hasText: 'Process_Legacy' }).click();
    await expect(cell(harness, 'Sub')).toHaveCount(1);
    await expect(harness.locator('#canvas .elsa-x6-breadcrumbs')).toBeHidden();
    expect(await selections(harness)).toEqual([
        { elementIds: ['Inner'], primary: { id: 'Inner', type: 'bpmn:Task', name: 'Inner' } },
        { elementIds: [], primary: null }
    ]);
});

test('opens a collapsed subprocess\'s plane from its button', async ({ harness }) => {
    await loaded(harness, 'legacy-collapsed.bpmn');
    await cell(harness, 'Sub').locator('[event="node:drill"]').click();
    await expect(cell(harness, 'Inner')).toHaveCount(1);
});

test('opens a collapsed subprocess that has a plane of its own in the document', async ({ harness }) => {
    await loaded(harness, 'preservation.bpmn');
    const opened = await harness.evaluate(() => {
        const button = document.querySelector<SVGElement>('#canvas .x6-node [event="node:drill"]:not([display="none"])');
        return button?.closest('.x6-cell')?.getAttribute('data-cell-id') ?? null;
    });
    expect(opened).not.toBeNull();
    await cell(harness, opened!).locator('[event="node:drill"]').click();
    await expect(harness.locator('#canvas .elsa-x6-breadcrumb')).toHaveCount(2);
    await expect(cell(harness, opened!)).toHaveCount(0);
});

test('a click selects the element under the pointer, and a click on the canvas clears it', async ({ harness }) => {
    await loaded(harness, 'order.bpmn');
    await cell(harness, 'Charge').click();
    await expect(cell(harness, 'Charge')).toHaveClass(/elsa-selected/);
    await harness.mouse.click(5, 5);
    await expect(cell(harness, 'Charge')).not.toHaveClass(/elsa-selected/);
    expect(await selections(harness)).toEqual([
        { elementIds: ['Charge'], primary: { id: 'Charge', type: 'bpmn:Task', name: 'Charge card' } },
        { elementIds: [], primary: null }
    ]);
});

test('draws instance state as a badge with the count and a coloured outline, and clears it', async ({ harness }) => {
    await loaded(harness, 'order.bpmn');
    await call(harness, 'applyOverlays', { Charge: { started: 2, completed: 2 }, Review: { started: 1, active: 1 } });
    await expect(cell(harness, 'Charge::badge').locator('text')).toHaveText('2');
    await expect(cell(harness, 'Charge')).toHaveClass(/elsa-completed/);
    await expect(cell(harness, 'Review')).toHaveClass(/elsa-active/);
    await expect(cell(harness, 'Charge::badge').locator('title')).toHaveText('Started 2, completed 2, active 0');

    await call(harness, 'clearOverlays');
    await expect(cell(harness, 'Charge::badge')).toHaveCount(0);
    await expect(cell(harness, 'Charge')).not.toHaveClass(/elsa-completed/);
});

test('draws problem markers with every message, on shapes and edges, and keeps them through an edit', async ({ harness }) => {
    await loaded(harness, 'order.bpmn');
    await call(harness, 'applyMarkers', [
        { elementId: 'Review', severity: 'warning', message: 'Unbound.' },
        { elementId: 'Review', severity: 'error', message: 'Refused.' },
        { elementId: 'Flow_2', severity: 'warning', message: 'Never taken.' }
    ]);
    await expect(cell(harness, 'Review')).toHaveClass(/elsa-problem-error/);
    await expect(cell(harness, 'Review::problem').locator('title')).toHaveText('Unbound.\nRefused.');
    await expect(cell(harness, 'Flow_2')).toHaveClass(/elsa-problem-warning/);

    expect(await call(harness, 'setBinding', 'Review', { activityType: 'Elsa.WriteLine', inputs: [] })).toEqual({ applied: true });
    await expect(cell(harness, 'Review::problem')).toHaveCount(1);
    await call(harness, 'clearMarkers');
    await expect(cell(harness, 'Review::problem')).toHaveCount(0);
});

test('nothing can be moved, in either mode', async ({ harness }) => {
    await loaded(harness, 'order.bpmn');
    const before = await cell(harness, 'Charge').boundingBox();
    await cell(harness, 'Charge').hover();
    await harness.mouse.down();
    await harness.mouse.move(before!.x + 200, before!.y + 120, { steps: 5 });
    await harness.mouse.up();
    const { xml } = await call<{ xml: string }>(harness, 'save');
    expect(xml).toContain('<bpmndi:BPMNShape id="Charge_di" bpmnElement="Charge">');
    expect(await call(harness, 'undo')).toBe(false);
});

test('fits a document loaded while the container had no size once the container is shown', async ({ harness }) => {
    await harness.evaluate(() => {
        document.getElementById('canvas')!.style.display = 'none';
        (window as unknown as { harness: Harness }).harness.create({});
    });
    await harness.evaluate(xml => (window as unknown as { harness: Harness }).harness.designer.load(xml), await fixture('order.bpmn'));
    const viewport = harness.locator('#canvas .x6-graph-svg-viewport');
    const unfitted = await viewport.getAttribute('transform');

    await harness.evaluate(() => {
        document.getElementById('canvas')!.style.display = '';
    });
    await expect(viewport).not.toHaveAttribute('transform', unfitted ?? '');
    // Fitted: the diagram's centre is near the canvas's centre.
    const box = await cell(harness, 'Gateway').boundingBox();
    expect(Math.abs(box!.x + box!.width / 2 - 500)).toBeLessThan(150);
});

// What the X6 designer draws, which the contract leaves to each designer. Runs only against the X6 designer.
import type { Page } from '@playwright/test';
import { expect, fixture, test } from './harness.js';

type Harness = {
    designer: Record<string, (...args: unknown[]) => unknown>;
    events: { type: string; [key: string]: unknown }[];
    create(options?: { readOnly?: boolean; withProperties?: boolean }): void;
};

async function loaded(page: Page, name: string, options: { readOnly?: boolean; withProperties?: boolean } = {}): Promise<void> {
    await loadedXml(page, await fixture(name), options);
}

async function loadedXml(page: Page, xml: string, options: { readOnly?: boolean; withProperties?: boolean } = {}): Promise<void> {
    await page.evaluate(options => (window as unknown as { harness: Harness }).harness.create(options), options);
    const result = await page.evaluate(xml => (window as unknown as { harness: Harness }).harness.designer.load(xml), xml);
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

/** The path data of every glyph a cell draws: its paths that are shown, other than its outline. */
function glyphs(page: Page, id: string): Promise<string[]> {
    return cell(page, id).locator('path:not(.elsa-x6-outline)').evaluateAll(paths => paths
        .filter(path => path.getAttribute('display') !== 'none' && !path.closest('[display="none"]'))
        .map(path => path.getAttribute('d') ?? ''));
}

const shape = (id: string, x: number, y: number, width: number, height: number, extra = '') =>
    `<bpmndi:BPMNShape id="${id}_di" bpmnElement="${id}"${extra}><dc:Bounds x="${x}" y="${y}" width="${width}" height="${height}" /></bpmndi:BPMNShape>`;

// One of each element whose markers tell it apart from a look-alike.
const MARKERS = `<?xml version="1.0" encoding="UTF-8"?>
<bpmn:definitions xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL" xmlns:bpmndi="http://www.omg.org/spec/BPMN/20100524/DI" xmlns:dc="http://www.omg.org/spec/DD/20100524/DC" id="Definitions_Markers" targetNamespace="http://bpmn.io/schema/bpmn">
  <bpmn:process id="Process_Markers" isExecutable="true">
    <bpmn:task id="Loop" name="Loop"><bpmn:standardLoopCharacteristics /></bpmn:task>
    <bpmn:task id="Sequential" name="Sequential"><bpmn:multiInstanceLoopCharacteristics isSequential="true" /></bpmn:task>
    <bpmn:task id="Compensation" name="Compensation" isForCompensation="true" />
    <bpmn:task id="Plain" name="Plain" />
    <bpmn:adHocSubProcess id="AdHoc" name="Ad hoc" />
    <bpmn:complexGateway id="Complex" />
    <bpmn:parallelGateway id="Parallel" />
    <bpmn:intermediateThrowEvent id="Compensate"><bpmn:compensateEventDefinition /></bpmn:intermediateThrowEvent>
  </bpmn:process>
  <bpmndi:BPMNDiagram id="Diagram_Markers">
    <bpmndi:BPMNPlane id="Plane_Markers" bpmnElement="Process_Markers">
      ${shape('Loop', 0, 0, 100, 80)}
      ${shape('Sequential', 150, 0, 100, 80)}
      ${shape('Compensation', 300, 0, 100, 80)}
      ${shape('Plain', 450, 0, 100, 80)}
      ${shape('AdHoc', 0, 150, 200, 120, ' isExpanded="true"')}
      ${shape('Complex', 250, 150, 50, 50, ' isMarkerVisible="true"')}
      ${shape('Parallel', 350, 150, 50, 50)}
      ${shape('Compensate', 450, 150, 36, 36)}
    </bpmndi:BPMNPlane>
  </bpmndi:BPMNDiagram>
</bpmn:definitions>`;

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

test('draws the markers that tell an element from its look-alikes', async ({ harness }) => {
    await loadedXml(harness, MARKERS);
    // A standard loop is not drawn as a multi-instance activity, nor a complex gateway as a parallel one.
    expect(await glyphs(harness, 'Loop')).toHaveLength(1);
    expect(await glyphs(harness, 'Loop')).not.toEqual(await glyphs(harness, 'Sequential'));
    expect(await glyphs(harness, 'Complex')).toHaveLength(1);
    expect(await glyphs(harness, 'Complex')).not.toEqual(await glyphs(harness, 'Parallel'));
    // A compensation handler and an ad-hoc subprocess carry their markers; a plain task carries none.
    expect(await glyphs(harness, 'Plain')).toEqual([]);
    expect(await glyphs(harness, 'Compensation')).toHaveLength(1);
    expect(await glyphs(harness, 'AdHoc')).toHaveLength(1);
    expect(await glyphs(harness, 'AdHoc')).not.toEqual(await glyphs(harness, 'Compensation'));
    // A compensate event shows its icon.
    expect(await glyphs(harness, 'Compensate')).toHaveLength(1);
});

test('a click on a label selects the element it names, between its letters too', async ({ harness }) => {
    await loaded(harness, 'order.bpmn');
    await cell(harness, 'Start::label').click({ position: { x: 1, y: 1 } });
    await expect(cell(harness, 'Start')).toHaveClass(/elsa-selected/);
    expect(await selections(harness)).toEqual([
        { elementIds: ['Start'], primary: { id: 'Start', type: 'bpmn:StartEvent', name: 'Order placed' } }
    ]);
});

test('leaves the host\'s content in the properties container, through a dispose too', async ({ harness }) => {
    await harness.evaluate(() => {
        document.getElementById('properties')!.innerHTML = '<p id="host-content">Host</p>';
    });
    await loaded(harness, 'order.bpmn', { withProperties: true });
    await expect(harness.locator('#properties #host-content')).toHaveCount(1);
    await harness.evaluate(() => (window as unknown as { harness: Harness }).harness.designer.dispose());
    await expect(harness.locator('#properties #host-content')).toHaveCount(1);
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

/** The bounds the saved document gives the shape with `id`. */
async function savedBounds(page: Page, id: string): Promise<number[]> {
    const { xml } = await call<{ xml: string }>(page, 'save');
    const attributes = xml.match(new RegExp(`<bpmndi:BPMNShape id="${id}_di"[\\s\\S]*?<dc:Bounds([^>]*)>`))?.[1] ?? '';
    return [...attributes.matchAll(/"(-?\d+(?:\.\d+)?)"/g)].map(match => Number(match[1]));
}

test('dragging a shape moves it in the document, its label following, and the keyboard undoes and redoes it', async ({ harness }) => {
    await loaded(harness, 'order.bpmn');
    const original = await savedBounds(harness, 'Gateway');
    const label = await cell(harness, 'Gateway::label').boundingBox();
    const box = (await cell(harness, 'Gateway').boundingBox())!;

    await harness.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
    await harness.mouse.down();
    await harness.mouse.move(box.x + box.width / 2 + 60, box.y + box.height / 2 + 40, { steps: 5 });
    // The label follows while the shape is dragged.
    const [during, shape] = [(await cell(harness, 'Gateway::label').boundingBox())!, (await cell(harness, 'Gateway').boundingBox())!];
    expect(Math.abs(during.x - label!.x - (shape.x - box.x))).toBeLessThan(1);
    expect(Math.abs(during.y - label!.y - (shape.y - box.y))).toBeLessThan(1);
    await harness.mouse.up();

    const moved = await savedBounds(harness, 'Gateway');
    expect(moved[0]).toBeGreaterThan(original[0]);
    expect(moved[1]).toBeGreaterThan(original[1]);
    expect(moved.slice(2)).toEqual(original.slice(2));
    const changes = await harness.evaluate(() => (window as unknown as { harness: Harness }).harness.events.filter(event => event.type === 'change'));
    expect(changes).toHaveLength(1);
    expect(changes[0]).toMatchObject({ isDirty: true, canUndo: true });
    // The gateway, its label with it, and the edges at its ends.
    expect(changes[0].elementIds).toEqual(['Gateway', 'Flow_2', 'Flow_3']);
    expect(await selections(harness)).toEqual([{ elementIds: ['Gateway'], primary: { id: 'Gateway', type: 'bpmn:ExclusiveGateway', name: 'Paid?' } }]);

    await harness.keyboard.press('Control+z');
    expect(await savedBounds(harness, 'Gateway')).toEqual(original);
    await harness.keyboard.press('Control+Shift+z');
    expect(await savedBounds(harness, 'Gateway')).toEqual(moved);
    await harness.keyboard.press('Control+z');
    await harness.keyboard.press('Control+y');
    expect(await savedBounds(harness, 'Gateway')).toEqual(moved);
});

test('a label and an edge cannot be dragged on their own, on screen or in the document', async ({ harness }) => {
    await loaded(harness, 'order.bpmn');
    const edge = () => cell(harness, 'Flow_2').locator('path').first().getAttribute('d');
    // Where the label is relative to its gateway: a drag on the label pans the canvas, which moves both.
    const labelOffset = async () => {
        const [label, gateway] = [(await cell(harness, 'Gateway::label').boundingBox())!, (await cell(harness, 'Gateway').boundingBox())!];
        return { x: label.x - gateway.x, y: label.y - gateway.y };
    };
    const [edgeBefore, labelBefore] = [await edge(), await labelOffset()];
    for (const id of ['Gateway::label', 'Flow_2']) {
        const box = (await cell(harness, id).boundingBox())!;
        await harness.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
        await harness.mouse.down();
        await harness.mouse.move(box.x + 80, box.y + 60, { steps: 5 });
        await harness.mouse.up();
    }
    expect(await edge()).toBe(edgeBefore);
    expect(await labelOffset()).toEqual(labelBefore);
    expect(await call(harness, 'undo')).toBe(false);
});

test('an undo during a drag cancels the drag, so its drop is no edit', async ({ harness }) => {
    await loaded(harness, 'order.bpmn');
    const original = await savedBounds(harness, 'Gateway');
    const drag = async (finish: () => Promise<void>) => {
        const box = (await cell(harness, 'Gateway').boundingBox())!;
        await harness.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
        await harness.mouse.down();
        await harness.mouse.move(box.x + box.width / 2 + 60, box.y + box.height / 2 + 40, { steps: 5 });
        await finish();
        await harness.mouse.up();
    };
    await drag(async () => undefined);
    const moved = await savedBounds(harness, 'Gateway');
    expect(moved).not.toEqual(original);

    // Undo while the second drag is held: the drop that follows applies nothing, and redo is still there.
    await drag(() => harness.keyboard.press('Control+z'));
    expect(await savedBounds(harness, 'Gateway')).toEqual(original);
    expect(await call(harness, 'redo')).toBe(true);
    expect(await savedBounds(harness, 'Gateway')).toEqual(moved);
});

test('nothing can be moved when read-only, and the keyboard does not undo', async ({ harness }) => {
    await loaded(harness, 'order.bpmn', { readOnly: true });
    const original = await savedBounds(harness, 'Charge');
    // A drag pans the canvas, so the shape keeps its place relative to the others.
    const offset = async () => {
        const [charge, start] = [await cell(harness, 'Charge').boundingBox(), await cell(harness, 'Start').boundingBox()];
        return { x: charge!.x - start!.x, y: charge!.y - start!.y, width: charge!.width, height: charge!.height };
    };
    const before = await offset();
    const box = (await cell(harness, 'Charge').boundingBox())!;
    await harness.mouse.move(box.x + 10, box.y + 10);
    await harness.mouse.down();
    await harness.mouse.move(box.x + 210, box.y + 130, { steps: 5 });
    await harness.mouse.up();
    expect(await offset()).toEqual(before);
    expect(await savedBounds(harness, 'Charge')).toEqual(original);
    await harness.keyboard.press('Control+z');
    expect(await call(harness, 'undo')).toBe(false);
});

// An expanded subprocess holding a task, with a timer on its bottom border.
const CONTAINER = `<?xml version="1.0" encoding="UTF-8"?>
<bpmn:definitions xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL" xmlns:bpmndi="http://www.omg.org/spec/BPMN/20100524/DI" xmlns:dc="http://www.omg.org/spec/DD/20100524/DC" id="Definitions_Container" targetNamespace="http://bpmn.io/schema/bpmn">
  <bpmn:process id="Process_Container" isExecutable="true">
    <bpmn:subProcess id="Sub" name="Sub"><bpmn:task id="Inner" name="Inner" /></bpmn:subProcess>
    <bpmn:boundaryEvent id="Timer" attachedToRef="Sub"><bpmn:timerEventDefinition /></bpmn:boundaryEvent>
  </bpmn:process>
  <bpmndi:BPMNDiagram id="Diagram_Container">
    <bpmndi:BPMNPlane id="Plane_Container" bpmnElement="Process_Container">
      ${shape('Sub', 100, 100, 300, 200, ' isExpanded="true"')}
      ${shape('Inner', 150, 150, 100, 80)}
      ${shape('Timer', 282, 282, 36, 36)}
    </bpmndi:BPMNPlane>
  </bpmndi:BPMNDiagram>
</bpmn:definitions>`;

test('dragging a corner of a selected expanded subprocess resizes it, its boundary event following, and undo puts it back', async ({ harness }) => {
    await loadedXml(harness, CONTAINER);
    expect(await call(harness, 'select', 'Inner')).toBe(true);
    // A task cannot be resized, so it shows no handles.
    await expect(harness.locator('#canvas .x6-cell[data-cell-id^="Inner::resize-"]')).toHaveCount(0);

    expect(await call(harness, 'select', 'Sub')).toBe(true);
    await expect(harness.locator('#canvas .x6-cell[data-cell-id^="Sub::resize-"]')).toHaveCount(4);
    const original = await savedBounds(harness, 'Sub');
    const timer = await savedBounds(harness, 'Timer');
    const handle = (await cell(harness, 'Sub::resize-se').boundingBox())!;
    await harness.mouse.move(handle.x + handle.width / 2, handle.y + handle.height / 2);
    await harness.mouse.down();
    await harness.mouse.move(handle.x + 80, handle.y + 60, { steps: 5 });
    await harness.mouse.up();

    const resized = await savedBounds(harness, 'Sub');
    expect(resized.slice(0, 2)).toEqual(original.slice(0, 2));
    expect(resized[2]).toBeGreaterThan(original[2]);
    expect(resized[3]).toBeGreaterThan(original[3]);
    // The timer is nearer the right and bottom sides, so it follows them.
    const moved = await savedBounds(harness, 'Timer');
    expect(moved[0] - timer[0]).toBe(resized[2] - original[2]);
    expect(moved[1] - timer[1]).toBe(resized[3] - original[3]);
    // The handles are drawn at the new corners.
    await expect(harness.locator('#canvas .x6-cell[data-cell-id^="Sub::resize-"]')).toHaveCount(4);

    expect(await call(harness, 'undo')).toBe(true);
    expect(await savedBounds(harness, 'Sub')).toEqual(original);
    expect(await savedBounds(harness, 'Timer')).toEqual(timer);
});

test('every handle stays on its corner during a resize, and a resize that stops at fractional content is kept', async ({ harness }) => {
    await loadedXml(harness, CONTAINER.replace(shape('Inner', 150, 150, 100, 80), shape('Inner', 150.4, 150, 100, 80)));
    expect(await call(harness, 'select', 'Sub')).toBe(true);
    const handle = (await cell(harness, 'Sub::resize-se').boundingBox())!;
    await harness.mouse.move(handle.x + handle.width / 2, handle.y + handle.height / 2);
    await harness.mouse.down();
    await harness.mouse.move(handle.x + 60, handle.y + 40, { steps: 5 });
    const centre = async (corner: string) => {
        const box = (await cell(harness, `Sub::resize-${corner}`).boundingBox())!;
        return { x: box.x + box.width / 2, y: box.y + box.height / 2 };
    };
    const sub = (await cell(harness, 'Sub').locator('.elsa-x6-outline').boundingBox())!;
    for (const [corner, x, y] of [['ne', sub.x + sub.width, sub.y], ['sw', sub.x, sub.y + sub.height], ['se', sub.x + sub.width, sub.y + sub.height]] as const) {
        const at = await centre(corner);
        expect(Math.abs(at.x - x), corner).toBeLessThan(2);
        expect(Math.abs(at.y - y), corner).toBeLessThan(2);
    }
    // Past the task inside, which now ends at x 250.4: the resize stops there, fractional, and the drop keeps it.
    await harness.mouse.move(handle.x - 400, handle.y - 400, { steps: 5 });
    await harness.mouse.up();
    expect(await savedBounds(harness, 'Sub')).toEqual([100, 100, 150.4, 130]);
});

test('a resize stops where the shape still contains its content, and a read-only designer shows no handles', async ({ harness }) => {
    await loadedXml(harness, CONTAINER);
    expect(await call(harness, 'select', 'Sub')).toBe(true);
    const handle = (await cell(harness, 'Sub::resize-se').boundingBox())!;
    await harness.mouse.move(handle.x + handle.width / 2, handle.y + handle.height / 2);
    await harness.mouse.down();
    await harness.mouse.move(handle.x - 400, handle.y - 400, { steps: 5 });
    await harness.mouse.up();
    // It stops at the task inside, which reaches x 250 and y 230; that is larger than the 140 by 120 minimum.
    expect(await savedBounds(harness, 'Sub')).toEqual([100, 100, 150, 130]);
    expect(await call(harness, 'undo')).toBe(true);

    await loadedXml(harness, CONTAINER, { readOnly: true });
    expect(await call(harness, 'select', 'Sub')).toBe(true);
    await expect(harness.locator('#canvas .x6-cell[data-cell-id^="Sub::resize-"]')).toHaveCount(0);
});

test('Delete removes the selected element with its flows, clears the selection, and undo brings them back', async ({ harness }) => {
    await loaded(harness, 'order.bpmn');
    await cell(harness, 'Notify').click();
    await harness.keyboard.press('Delete');

    for (const id of ['Notify', 'Flow_3', 'Flow_4']) await expect(cell(harness, id), id).toHaveCount(0);
    await expect(cell(harness, 'Review')).toHaveCount(1);
    const { xml } = await call<{ xml: string }>(harness, 'save');
    expect(xml).not.toContain('Notify');
    expect(await selections(harness)).toEqual([
        { elementIds: ['Notify'], primary: { id: 'Notify', type: 'bpmn:SendTask', name: 'Notify shipping' } },
        { elementIds: [], primary: null }
    ]);

    await harness.keyboard.press('Control+z');
    for (const id of ['Notify', 'Flow_3', 'Flow_4']) await expect(cell(harness, id), id).toHaveCount(1);
    // Nothing is selected, so Delete does nothing.
    await harness.keyboard.press('Delete');
    await expect(cell(harness, 'Notify')).toHaveCount(1);
});

test('Delete does nothing when read-only', async ({ harness }) => {
    await loaded(harness, 'order.bpmn', { readOnly: true });
    await cell(harness, 'Notify').click();
    await harness.keyboard.press('Delete');
    await expect(cell(harness, 'Notify')).toHaveCount(1);
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

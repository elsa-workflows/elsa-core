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

/** The element the last selection event named, as the host is told it. */
async function lastPrimary(page: Page): Promise<unknown> {
    return ((await selections(page)) as { primary: unknown }[]).at(-1)?.primary;
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

const editor = (page: Page) => page.locator('#canvas textarea.elsa-x6-name-editor');

test('a double-click edits a name in place, saved as one undoable edit', async ({ harness }) => {
    await loaded(harness, 'order.bpmn');
    await cell(harness, 'Charge').dblclick();
    await expect(editor(harness)).toHaveValue('Charge card');
    await editor(harness).fill('Take payment');
    await editor(harness).press('Enter');

    await expect(editor(harness)).toHaveCount(0);
    await expect(cell(harness, 'Charge')).toContainText('Take payment');
    expect((await call<{ xml: string }>(harness, 'save')).xml).toContain('name="Take payment"');
    const changes = await harness.evaluate(() => (window as unknown as { harness: Harness }).harness.events.filter(event => event.type === 'change'));
    expect(changes.at(-1)).toMatchObject({ elementIds: ['Charge'], isDirty: true });
    // The host is told the selected element's new name, and its old one again on undo.
    expect(await lastPrimary(harness)).toEqual({ id: 'Charge', type: 'bpmn:Task', name: 'Take payment' });

    await harness.keyboard.press('Control+z');
    await expect(cell(harness, 'Charge')).toContainText('Charge card');
    expect(await lastPrimary(harness)).toEqual({ id: 'Charge', type: 'bpmn:Task', name: 'Charge card' });
});

test('Escape gives up an edit, Backspace in it edits the text, and an empty name removes the name', async ({ harness }) => {
    await loaded(harness, 'order.bpmn');
    await cell(harness, 'Charge').dblclick();
    await editor(harness).press('End');
    await editor(harness).press('Backspace');
    await expect(editor(harness)).toHaveValue('Charge car');
    await editor(harness).press('Escape');
    await expect(editor(harness)).toHaveCount(0);
    await expect(cell(harness, 'Charge')).toHaveCount(1);
    expect(await call(harness, 'undo')).toBe(false);

    await cell(harness, 'Charge').dblclick();
    await editor(harness).fill('');
    await editor(harness).press('Enter');
    expect((await call<{ xml: string }>(harness, 'save')).xml).toContain('<bpmn:task id="Charge">');
});

test('F2 edits the selected element, an edge gets a label when named, and read-only opens no editor', async ({ harness }) => {
    await loaded(harness, 'order.bpmn');
    await cell(harness, 'Gateway').click();
    await harness.keyboard.press('F2');
    await expect(editor(harness)).toHaveValue('Paid?');
    await editor(harness).press('Escape');

    // A straight edge has no height, so it is double-clicked at its midpoint rather than through its locator.
    const flow = (await cell(harness, 'Flow_2').locator('path').first().boundingBox())!;
    await harness.mouse.dblclick(flow.x + flow.width / 2, flow.y + flow.height / 2);
    await editor(harness).fill('Charged');
    // Enter that confirms a text composition, as with an IME, does not save.
    await editor(harness).dispatchEvent('keydown', { key: 'Enter', isComposing: true });
    await expect(editor(harness)).toHaveCount(1);
    await editor(harness).press('Enter');
    await expect(cell(harness, 'Flow_2::label')).toContainText('Charged');

    await loaded(harness, 'order.bpmn', { readOnly: true });
    await cell(harness, 'Charge').dblclick();
    await expect(editor(harness)).toHaveCount(0);
});

test('a breadcrumb clicked while a name is being edited saves the name and goes back in one click', async ({ harness }) => {
    await loaded(harness, 'legacy-collapsed.bpmn');
    expect(await call(harness, 'select', 'Inner')).toBe(true);
    await cell(harness, 'Inner').dblclick();
    await editor(harness).fill('Renamed');

    // Pressing the breadcrumb leaves the box, which saves the name; the breadcrumb must still take the click.
    await harness.locator('#canvas .elsa-x6-breadcrumb', { hasText: 'Process_Legacy' }).click();

    await expect(cell(harness, 'Sub')).toHaveCount(1);
    await expect(harness.locator('#canvas .elsa-x6-breadcrumbs')).toBeHidden();
    const { xml } = await call<{ xml: string }>(harness, 'save');
    expect(xml).toContain('name="Renamed"');
});

test('an edge without a label is edited in a compact box on its midpoint, not one spanning the edge', async ({ harness }) => {
    // Flow_2 rerouted around a long detour, so its bounding box is large and its centre is not its midpoint.
    const xml = (await fixture('order.bpmn')).replace(
        '<di:waypoint x="340" y="120" />\n        <di:waypoint x="395" y="120" />',
        '<di:waypoint x="340" y="120" />\n        <di:waypoint x="340" y="400" />\n        <di:waypoint x="700" y="400" />\n        <di:waypoint x="700" y="300" />'
    );
    expect(xml).toContain('<di:waypoint x="700" y="300" />');
    await loadedXml(harness, xml);
    const path = cell(harness, 'Flow_2').locator('path').first();
    const midpoint = await path.evaluate((element: SVGPathElement) => {
        const point = element.getPointAtLength(element.getTotalLength() / 2);
        const screen = new DOMPoint(point.x, point.y).matrixTransform(element.getScreenCTM()!);
        return { x: screen.x, y: screen.y };
    });

    await harness.mouse.dblclick(midpoint.x, midpoint.y);

    const box = (await editor(harness).boundingBox())!;
    expect(box.width).toBe(120);
    expect(box.height).toBe(40);
    expect(Math.abs(box.x + box.width / 2 - midpoint.x)).toBeLessThan(2);
    expect(Math.abs(box.y + box.height / 2 - midpoint.y)).toBeLessThan(2);
});

const entry = (page: Page, title: string) => page.getByRole('toolbar', { name: 'Create' }).getByRole('button', { name: title });

/** A point on the canvas, below the order fixture's shapes, where nothing is drawn. */
async function blankPoint(page: Page): Promise<{ x: number; y: number }> {
    const box = (await page.locator('#canvas .elsa-x6-canvas').boundingBox())!;
    return { x: box.x + box.width / 2, y: box.y + box.height - 60 };
}

/** `point`, a page position, relative to the canvas, as a drag's target position takes it. */
async function relativeTo(page: Page, point: { x: number; y: number }): Promise<{ x: number; y: number }> {
    const box = (await page.locator('#canvas .elsa-x6-canvas').boundingBox())!;
    return { x: point.x - box.x, y: point.y - box.y };
}

/** The id of the element the last selection event named. */
async function lastSelected(page: Page): Promise<string> {
    const events = (await selections(page)) as { primary: { id: string; type: string } | null }[];
    return events.at(-1)!.primary!.id;
}

test('an element dragged from the palette is created centred where it is dropped, selected, and undone in one step', async ({ harness }) => {
    await loaded(harness, 'order.bpmn');
    const before = (await call<{ xml: string }>(harness, 'save')).xml;
    const target = await blankPoint(harness);
    const button = (await entry(harness, 'Create end event').boundingBox())!;

    await harness.mouse.move(button.x + button.width / 2, button.y + button.height / 2);
    await harness.mouse.down();
    await harness.mouse.move(target.x - 50, target.y - 20, { steps: 4 });
    await expect(harness.locator('#canvas .elsa-x6-create-preview')).toBeVisible();
    await harness.mouse.move(target.x, target.y, { steps: 4 });
    await harness.mouse.up();

    const id = await lastSelected(harness);
    expect(id).toMatch(/^Event_[0-9a-z]{7}$/);
    const created = (await cell(harness, id).boundingBox())!;
    expect(Math.abs(created.x + created.width / 2 - target.x)).toBeLessThan(2);
    expect(Math.abs(created.y + created.height / 2 - target.y)).toBeLessThan(2);
    await expect(harness.locator('#canvas .elsa-x6-create-preview')).toHaveCount(0);
    expect((await call<{ xml: string }>(harness, 'save')).xml).toContain(`<bpmn:endEvent id="${id}" />`);

    await harness.keyboard.press('Control+z');
    await expect(cell(harness, id)).toHaveCount(0);
    expect((await call<{ xml: string }>(harness, 'save')).xml).toBe(before);
});

test('a click on the palette arms it, the next click places the element, and a new task opens for its name', async ({ harness }) => {
    await loaded(harness, 'order.bpmn');
    const target = await blankPoint(harness);

    await entry(harness, 'Create task').click();
    await expect(entry(harness, 'Create task')).toHaveAttribute('aria-pressed', 'true');
    await harness.mouse.click(target.x, target.y);

    await expect(entry(harness, 'Create task')).not.toHaveAttribute('aria-pressed');
    await editor(harness).fill('Pay');
    await editor(harness).press('Enter');
    const id = await lastSelected(harness);
    expect((await call<{ xml: string }>(harness, 'save')).xml).toContain(`<bpmn:task id="${id}" name="Pay" />`);
    // The click that placed the task lands on it, so it stays selected.
    expect(((await selections(harness)) as { primary: { id: string } | null }[]).at(-1)!.primary?.id).toBe(id);
});

test('a name being edited is saved when an element is dragged from the palette', async ({ harness }) => {
    await loaded(harness, 'order.bpmn');
    await cell(harness, 'Charge').dblclick();
    await editor(harness).fill('Charged');
    const target = await blankPoint(harness);

    await entry(harness, 'Create end event').dragTo(harness.locator('#canvas .elsa-x6-canvas'), { targetPosition: await relativeTo(harness, target) });

    const { xml } = await call<{ xml: string }>(harness, 'save');
    expect(xml).toContain('name="Charged"');
    expect(xml).toContain(`<bpmn:endEvent id="${await lastSelected(harness)}" />`);
});

test('a create armed before another document is loaded does not place anything in it', async ({ harness }) => {
    await loaded(harness, 'order.bpmn');
    await entry(harness, 'Create end event').click();
    await expect(entry(harness, 'Create end event')).toHaveAttribute('aria-pressed', 'true');

    const xml = await fixture('order.bpmn');
    expect(await harness.evaluate(xml => (window as unknown as { harness: Harness }).harness.designer.load(xml), xml)).toMatchObject({ imported: true });
    const before = (await call<{ xml: string }>(harness, 'save')).xml;
    const target = await blankPoint(harness);
    await harness.mouse.click(target.x, target.y);

    await expect(entry(harness, 'Create end event')).not.toHaveAttribute('aria-pressed');
    expect((await call<{ xml: string }>(harness, 'save')).xml).toBe(before);
});

test('a cancelled drag from the palette is given up, so a later release on the canvas creates nothing', async ({ harness }) => {
    await loaded(harness, 'order.bpmn');
    const before = (await call<{ xml: string }>(harness, 'save')).xml;
    const button = (await entry(harness, 'Create end event').boundingBox())!;
    const press = { pointerId: 7, isPrimary: true, button: 0, clientX: button.x + 5, clientY: button.y + 5, bubbles: true };

    await entry(harness, 'Create end event').dispatchEvent('pointerdown', press);
    await expect(entry(harness, 'Create end event')).toHaveAttribute('aria-pressed', 'true');
    await harness.evaluate(press => window.dispatchEvent(new PointerEvent('pointercancel', press)), press);
    const target = await blankPoint(harness);
    await harness.mouse.click(target.x, target.y);

    await expect(entry(harness, 'Create end event')).not.toHaveAttribute('aria-pressed');
    await expect(harness.locator('#canvas .elsa-x6-create-preview')).toHaveCount(0);
    expect((await call<{ xml: string }>(harness, 'save')).xml).toBe(before);
});

test('Escape gives up an armed create, a read-only designer has no palette, and a page an older tool drew disables it', async ({ harness }) => {
    await loaded(harness, 'order.bpmn');
    const before = (await call<{ xml: string }>(harness, 'save')).xml;
    await entry(harness, 'Create gateway').click();
    await harness.keyboard.press('Escape');
    await expect(entry(harness, 'Create gateway')).not.toHaveAttribute('aria-pressed');
    const target = await blankPoint(harness);
    await harness.mouse.click(target.x, target.y);
    expect((await call<{ xml: string }>(harness, 'save')).xml).toBe(before);

    await loaded(harness, 'legacy-collapsed.bpmn');
    await expect(entry(harness, 'Create task')).toBeEnabled();
    expect(await call(harness, 'select', 'Inner')).toBe(true);
    await expect(entry(harness, 'Create task')).toBeDisabled();

    await loaded(harness, 'order.bpmn', { readOnly: true });
    await expect(harness.getByRole('toolbar', { name: 'Create' })).toHaveCount(0);
});

const replaceMenu = (page: Page) => page.getByRole('menu', { name: 'Change type' });

test('the wrench beside a selected task opens its replace menu, and an entry replaces it, keeping its name, in one undoable step', async ({ harness }) => {
    await loaded(harness, 'order.bpmn');
    const before = (await call<{ xml: string }>(harness, 'save')).xml;
    await cell(harness, 'Charge').click();

    await expect(harness.locator('#canvas [role="button"][aria-label="Change type"]')).toHaveCount(1);
    await cell(harness, 'Charge::replace').click();
    await expect(replaceMenu(harness).getByRole('menuitem').first()).toBeFocused();
    await expect(replaceMenu(harness).getByRole('menuitem').first()).toHaveText('User task');
    await replaceMenu(harness).getByRole('menuitem', { name: 'Service task' }).click();

    await expect(replaceMenu(harness)).toHaveCount(0);
    expect((await call<{ xml: string }>(harness, 'save')).xml).toContain('<bpmn:serviceTask id="Charge" name="Charge card">');
    expect(await lastPrimary(harness)).toMatchObject({ id: 'Charge', type: 'bpmn:ServiceTask' });
    await harness.keyboard.press('Control+z');
    expect((await call<{ xml: string }>(harness, 'save')).xml).toBe(before);
    // Undo and redo tell the host the selected element's type again.
    expect(await lastPrimary(harness)).toMatchObject({ id: 'Charge', type: 'bpmn:Task' });
    await harness.keyboard.press('Control+y');
    expect(await lastPrimary(harness)).toMatchObject({ id: 'Charge', type: 'bpmn:ServiceTask' });
});

test('R opens the replace menu, arrow keys and Enter pick an entry, Escape closes it, and a read-only designer has no wrench', async ({ harness }) => {
    await loaded(harness, 'order.bpmn');
    await cell(harness, 'Gateway').click();
    await harness.keyboard.press('r');
    await harness.keyboard.press('ArrowDown');
    await expect(replaceMenu(harness).getByRole('menuitem', { name: 'Inclusive gateway' })).toBeFocused();
    // The focused entry shows an outline, not only a faint background.
    expect(await replaceMenu(harness).getByRole('menuitem', { name: 'Inclusive gateway' }).evaluate(item => getComputedStyle(item).outlineStyle)).toBe('solid');
    await harness.keyboard.press('Enter');
    expect((await call<{ xml: string }>(harness, 'save')).xml).toContain('<bpmn:inclusiveGateway id="Gateway" name="Paid?">');

    const after = (await call<{ xml: string }>(harness, 'save')).xml;
    await harness.keyboard.press('r');
    // The menu is beside the canvas, so its keys do not reach the canvas: Delete does not delete the gateway.
    await harness.keyboard.press('Delete');
    await harness.keyboard.press('Escape');
    await expect(replaceMenu(harness)).toHaveCount(0);
    expect((await call<{ xml: string }>(harness, 'save')).xml).toBe(after);

    await loaded(harness, 'order.bpmn', { readOnly: true });
    await cell(harness, 'Charge').click();
    await expect(cell(harness, 'Charge::replace')).toHaveCount(0);
});

test('the replace menu of an element at the bottom of the canvas opens above it, inside the designer', async ({ harness }) => {
    // Two tasks far apart, so the lower one is drawn at the bottom of the fitted canvas.
    await loadedXml(harness, `<?xml version="1.0" encoding="UTF-8"?>
<bpmn:definitions xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL" xmlns:bpmndi="http://www.omg.org/spec/BPMN/20100524/DI" xmlns:dc="http://www.omg.org/spec/DD/20100524/DC" id="Definitions_1" targetNamespace="http://bpmn.io/schema/bpmn">
  <bpmn:process id="Process_1"><bpmn:task id="Top" /><bpmn:task id="Bottom" /></bpmn:process>
  <bpmndi:BPMNDiagram id="Diagram_1"><bpmndi:BPMNPlane id="Plane_1" bpmnElement="Process_1">
    ${shape('Top', 900, 0, 100, 80)}${shape('Bottom', 900, 520, 100, 80)}
  </bpmndi:BPMNPlane></bpmndi:BPMNDiagram>
</bpmn:definitions>`);
    await cell(harness, 'Bottom').click();
    await harness.keyboard.press('r');

    const menu = (await replaceMenu(harness).boundingBox())!;
    const stage = (await harness.locator('#canvas .elsa-x6-stage').boundingBox())!;
    const task = (await cell(harness, 'Bottom').boundingBox())!;
    expect(menu.y + menu.height).toBeLessThanOrEqual(stage.y + stage.height);
    expect(menu.x + menu.width).toBeLessThanOrEqual(stage.x + stage.width);
    expect(menu.y + menu.height).toBeLessThanOrEqual(task.y);
});

test('the replace menu in a designer too short for it below or above an element is shortened to fit beside it, and scrolls', async ({ harness }) => {
    await harness.evaluate(() => {
        document.getElementById('canvas')!.style.height = '240px';
    });
    await loadedXml(harness, `<?xml version="1.0" encoding="UTF-8"?>
<bpmn:definitions xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL" xmlns:bpmndi="http://www.omg.org/spec/BPMN/20100524/DI" xmlns:dc="http://www.omg.org/spec/DD/20100524/DC" id="Definitions_1" targetNamespace="http://bpmn.io/schema/bpmn">
  <bpmn:process id="Process_1"><bpmn:task id="Only" /></bpmn:process>
  <bpmndi:BPMNDiagram id="Diagram_1"><bpmndi:BPMNPlane id="Plane_1" bpmnElement="Process_1">${shape('Only', 100, 100, 100, 80)}</bpmndi:BPMNPlane></bpmndi:BPMNDiagram>
</bpmn:definitions>`);
    await cell(harness, 'Only').click();
    await harness.keyboard.press('r');

    const menu = (await replaceMenu(harness).boundingBox())!;
    const stage = (await harness.locator('#canvas .elsa-x6-stage').boundingBox())!;
    const wrench = (await cell(harness, 'Only::replace').boundingBox())!;
    expect(menu.y).toBeGreaterThanOrEqual(stage.y);
    expect(menu.y + menu.height).toBeLessThanOrEqual(stage.y + stage.height);
    // It does not cover the wrench it opened from.
    expect(menu.y + menu.height <= wrench.y || menu.y >= wrench.y + wrench.height).toBe(true);
    expect(await replaceMenu(harness).evaluate(element => element.scrollHeight > element.clientHeight)).toBe(true);
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

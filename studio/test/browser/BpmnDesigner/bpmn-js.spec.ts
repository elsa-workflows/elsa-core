// What the bpmn-js designer draws beyond the contract. The contract leaves drawing to each designer, so these run only
// against bpmn-js (see playwright.config.ts).
import { expect, fixture, test } from './harness.js';

test('overlays draw one badge per element with stats, and clearing removes them', async ({ harness }) => {
    const xml = await fixture('order.bpmn');
    await harness.evaluate(async xml => {
        const harness = (window as any).harness;
        harness.create({});
        await harness.designer.load(xml);
    }, xml);
    const apply = (stats: unknown) => harness.evaluate(stats => (window as any).harness.designer.applyOverlays(stats), stats);
    const badges = () => harness.locator('.elsa-bpmn-badge').count();

    await apply({ Charge: { started: 1, completed: 1 }, Review: { active: 1 } });
    expect(await badges()).toBe(2);
    await expect(harness.locator('.elsa-bpmn-badge-active')).toHaveCount(1);
    await apply({ Charge: { faulted: true } });
    expect(await badges()).toBe(1);
    await apply(null);
    expect(await badges()).toBe(0);
});

test('a badge shows a cancellation over blocked and active, and a fault over everything', async ({ harness }) => {
    const xml = await fixture('order.bpmn');
    await harness.evaluate(async xml => {
        const harness = (window as any).harness;
        harness.create({});
        await harness.designer.load(xml);
    }, xml);
    const apply = (stats: unknown) => harness.evaluate(stats => (window as any).harness.designer.applyOverlays(stats), stats);

    await apply({ Charge: { canceled: true, blocked: true }, Review: { canceled: true, active: 2 } });
    await expect(harness.locator('.elsa-bpmn-badge-canceled')).toHaveCount(2);
    await apply({ Charge: { faulted: true, canceled: true } });
    await expect(harness.locator('.elsa-bpmn-badge-faulted')).toHaveCount(1);
});

test('a load that fails after bpmn-js replaced the document puts the earlier one back, still dirty', async ({ harness }) => {
    // bpmn-js accepts these definitions, then fails to draw them: they hold no diagram.
    const undrawable = '<bpmn:definitions xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL" id="D" targetNamespace="x"><bpmn:message id="M" name="Only" /></bpmn:definitions>';
    const result = await harness.evaluate(async ([order, undrawable]) => {
        const harness = (window as any).harness;
        harness.create({});
        await harness.designer.load(order);
        harness.designer.setBinding('Charge', { activityType: 'Elsa.Delay', inputs: [] });
        harness.events = [];
        const load = await harness.designer.load(undrawable);
        return { load, xml: (await harness.designer.save()).xml, changes: harness.events.filter((event: { type: string }) => event.type === 'change') };
    }, [await fixture('order.bpmn'), undrawable] as const);

    expect(result.load).toEqual({ imported: false, error: 'no diagram to display', warnings: [] });
    expect(result.xml).toContain('activityType="Elsa.Delay"');
    expect(result.xml).not.toContain('Only');
    expect(result.changes).toEqual([expect.objectContaining({ isDirty: true, canUndo: false })]);
});

test('a read-only designer keeps its document when a load fails after bpmn-js replaced it', async ({ harness }) => {
    const undrawable = '<bpmn:definitions xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL" id="D" targetNamespace="x"><bpmn:message id="M" name="Only" /></bpmn:definitions>';
    const result = await harness.evaluate(async ([order, undrawable]) => {
        const harness = (window as any).harness;
        harness.create({ readOnly: true });
        await harness.designer.load(order);
        const load = await harness.designer.load(undrawable);
        return { load, xml: (await harness.designer.save()).xml, shapes: document.querySelectorAll('.djs-shape').length };
    }, [await fixture('order.bpmn'), undrawable] as const);

    expect(result.load.imported).toBe(false);
    expect(result.xml).toContain('id="Charge"');
    expect(result.shapes).toBeGreaterThan(0);
});

test('a badge shows the count of its state', async ({ harness }) => {
    await harness.evaluate(async xml => {
        const harness = (window as any).harness;
        harness.create({});
        await harness.designer.load(xml);
    }, await fixture('order.bpmn'));

    await harness.evaluate(stats => (window as any).harness.designer.applyOverlays(stats), {
        Charge: { started: 3, completed: 1, active: 2 },
        Review: { started: 4, completed: 4 }
    });
    await expect(harness.locator('.elsa-bpmn-badge-active')).toHaveText('2');
    await expect(harness.locator('.elsa-bpmn-badge-completed')).toHaveText('4');
});

test('markers draw one problem badge per element, in its most severe colour, with every message', async ({ harness }) => {
    await harness.evaluate(async xml => {
        const harness = (window as any).harness;
        harness.create({});
        await harness.designer.load(xml);
    }, await fixture('order.bpmn'));
    const apply = (markers: unknown) => harness.evaluate(markers => (window as any).harness.designer.applyMarkers(markers), markers);

    await apply([
        { elementId: 'Charge', severity: 'warning', message: 'First.' },
        { elementId: 'Charge', severity: 'error', message: 'Second.' },
        { elementId: 'Flow_2', severity: 'warning', message: 'Never taken.' },
        { elementId: 'Review', severity: 'fatal', message: 'Unknown severity.' }
    ]);

    await expect(harness.locator('.elsa-bpmn-problem')).toHaveCount(3);
    await expect(harness.locator('.elsa-bpmn-problem-error')).toHaveCount(2);
    await expect(harness.locator('.elsa-bpmn-problem-warning')).toHaveAttribute('title', 'Never taken.');
    await expect(harness.locator('.djs-element[data-element-id="Charge"].elsa-problem-error')).toHaveCount(1);
    await expect(harness.locator('.djs-element[data-element-id="Flow_2"].elsa-problem-warning')).toHaveCount(1);
    expect(await harness.locator('.elsa-bpmn-problem-error').first().getAttribute('title')).toBe('First.\nSecond.');

    await apply([{ elementId: 'Review', severity: 'warning', message: 'Only this.' }]);
    await expect(harness.locator('.elsa-bpmn-problem')).toHaveCount(1);
    await expect(harness.locator('.djs-element.elsa-problem-error')).toHaveCount(0);
    await harness.evaluate(() => (window as any).harness.designer.clearMarkers());
    await expect(harness.locator('.elsa-bpmn-problem')).toHaveCount(0);
    await expect(harness.locator('.djs-element.elsa-problem-warning')).toHaveCount(0);
});

test('markers and overlays do not clear each other, and a load clears both', async ({ harness }) => {
    const xml = await fixture('order.bpmn');
    await harness.evaluate(async xml => {
        const harness = (window as any).harness;
        harness.create({});
        await harness.designer.load(xml);
        harness.designer.applyOverlays({ Charge: { faulted: true } });
        harness.designer.applyMarkers([{ elementId: 'Charge', severity: 'error', message: 'Unbound.' }]);
    }, xml);

    await harness.evaluate(() => (window as any).harness.designer.clearOverlays());
    await expect(harness.locator('.elsa-bpmn-problem')).toHaveCount(1);
    await harness.evaluate(() => {
        const designer = (window as any).harness.designer;
        designer.applyOverlays({ Charge: { faulted: true } });
        designer.clearMarkers();
    });
    await expect(harness.locator('.elsa-bpmn-badge')).toHaveCount(1);
    await expect(harness.locator('.djs-element[data-element-id="Charge"].elsa-faulted')).toHaveCount(1);

    await harness.evaluate(async xml => {
        const designer = (window as any).harness.designer;
        designer.applyMarkers([{ elementId: 'Charge', severity: 'error', message: 'Unbound.' }]);
        await designer.load(xml);
    }, xml);
    await expect(harness.locator('.elsa-bpmn-badge')).toHaveCount(0);
    await expect(harness.locator('.elsa-bpmn-problem')).toHaveCount(0);
    await expect(harness.locator('.djs-element.elsa-problem-error')).toHaveCount(0);
});

test('deleting a marked element removes its marker', async ({ harness }) => {
    await harness.evaluate(async xml => {
        const harness = (window as any).harness;
        harness.create({});
        await harness.designer.load(xml);
        harness.designer.applyMarkers([{ elementId: 'Charge', severity: 'error', message: 'Unbound.' }]);
    }, await fixture('order.bpmn'));

    await harness.locator('.djs-element[data-element-id="Charge"]').click();
    await harness.keyboard.press('Delete');

    await expect(harness.locator('.djs-element[data-element-id="Charge"]')).toHaveCount(0);
    await expect(harness.locator('.elsa-bpmn-problem')).toHaveCount(0);
});

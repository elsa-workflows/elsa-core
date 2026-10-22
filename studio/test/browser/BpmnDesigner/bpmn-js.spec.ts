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

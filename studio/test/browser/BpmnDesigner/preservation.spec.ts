// Round-trip preservation (studio/docs/specs/bpmn-designer-contract/contract.md). A save keeps everything a load did not
// report, and saving is stable. With BPMN_DESIGNER_OUTPUT set, every round trip is written there, so the Elsa import
// equivalence check (BpmnImportEquivalence) can import each original and its saved copy.
import type { Page } from '@playwright/test';
import { mkdir, readdir, readFile, writeFile } from 'node:fs/promises';
import { dirname, join, relative } from 'node:path';
import { fileURLToPath } from 'node:url';
import { expect, test } from './harness.js';

const here = dirname(fileURLToPath(import.meta.url));
const repositoryRoot = join(here, '../../../..');
const coreAssets = join(repositoryRoot, 'core/test/integration/Elsa.Bpmn.Interchange.IntegrationTests/Assets');
const output = process.env.BPMN_DESIGNER_OUTPUT;

type Warning = { kind: string; message: string; elementId?: string };
type LoadResult = { imported: boolean; error?: string; warnings: Warning[] };
type Binding = { activityType: string; inputs: { name: string; value: string }[] };

/** Loads `xml` into a new designer and returns the load result and what a save right after writes. */
function roundTrip(page: Page, xml: string): Promise<{ result: LoadResult; saved: string }> {
    return page.evaluate(async xml => {
        const harness = (window as any).harness;
        harness.create({});
        const result = await harness.designer.load(xml);
        return { result, saved: result.imported ? (await harness.designer.save()).xml : '' };
    }, xml);
}

function getBinding(page: Page, elementId: string): Promise<Binding | null> {
    return page.evaluate(elementId => (window as any).harness.designer.getBinding(elementId), elementId);
}

async function record(name: string, source: string, result: LoadResult, saved: string): Promise<void> {
    if (!output) return;
    await mkdir(output, { recursive: true });
    await writeFile(join(output, `${name}.saved.bpmn`), saved);
    await writeFile(join(output, `${name}.json`), JSON.stringify({
        name,
        source: relative(repositoryRoot, source).replaceAll('\\', '/'),
        saved: `${name}.saved.bpmn`,
        warnings: result.warnings
    }, null, 2));
}

/**
 * How many elements, and how many namespaced attributes, each namespace contributes to `xml`. It does not rely on the
 * designer's own load warnings, so a save that loses content those warnings miss still changes it.
 */
function inventory(page: Page, xml: string): Promise<Record<string, number>> {
    return page.evaluate(xml => {
        const counts: Record<string, number> = {};
        const add = (key: string) => (counts[key] = (counts[key] ?? 0) + 1);
        const ignored = ['http://www.w3.org/2000/xmlns/', 'http://www.w3.org/2001/XMLSchema-instance'];
        for (const element of Array.from(new DOMParser().parseFromString(xml, 'application/xml').getElementsByTagName('*'))) {
            add(`{${element.namespaceURI}}${element.localName}`);
            for (const attribute of Array.from(element.attributes)) {
                if (attribute.namespaceURI && !ignored.includes(attribute.namespaceURI)) add(`@{${attribute.namespaceURI}}${attribute.localName}`);
            }
        }
        return counts;
    }, xml);
}

/** A saved document opens with no warnings, and saving it again writes the same document. */
async function expectStable(page: Page, saved: string): Promise<void> {
    const again = await roundTrip(page, saved);
    expect(again.result).toEqual({ imported: true, warnings: [] });
    expect(again.saved).toBe(saved);
}

test.describe('the preservation fixture', () => {
    const source = join(here, 'fixtures/preservation.bpmn');

    test('round-trips with no warnings, stably', async ({ harness }) => {
        const { result, saved } = await roundTrip(harness, await readFile(source, 'utf8'));

        expect(result).toEqual({ imported: true, warnings: [] });
        expect(await inventory(harness, saved)).toEqual(await inventory(harness, await readFile(source, 'utf8')));
        await expectStable(harness, saved);
        await record('preservation', source, result, saved);
    });

    test('keeps every binding exactly, at every depth and on every plane', async ({ harness }) => {
        await roundTrip(harness, await readFile(source, 'utf8'));
        const before = {
            Task_Bound: await getBinding(harness, 'Task_Bound'),
            Task_Deep: await getBinding(harness, 'Task_Deep'),
            C_Task: await getBinding(harness, 'C_Task'),
            E_Task: await getBinding(harness, 'E_Task')
        };
        expect(before.Task_Bound?.inputs[0].value).toBe('{"typeName":"String","expression":{"type":"JavaScript","value":"a < b && c > d ? \\"caf\\u00e9 \\\\n\\" : \'x\'"}}');
        expect(before.E_Task?.inputs[0].value).toContain('naïve — ✓ <&> cancelled');

        const { saved } = await roundTrip(harness, (await harness.evaluate(() => (window as any).harness.designer.save())).xml);
        await roundTrip(harness, saved);

        for (const [id, binding] of Object.entries(before)) expect(await getBinding(harness, id), id).toEqual(binding);
    });

    test('an edit changes only what was edited', async ({ harness }) => {
        await roundTrip(harness, await readFile(source, 'utf8'));
        const edited: Binding = { activityType: 'Elsa.Delay', inputs: [{ name: 'timeSpan', value: '{"typeName":"TimeSpan","expression":{"type":"Literal","value":"00:01:00"}}' }] };
        const untouched = await getBinding(harness, 'Task_Bound');

        expect(await harness.evaluate(edited => (window as any).harness.designer.setBinding('Task_Deep', edited), edited)).toEqual({ applied: true });
        const { xml } = await harness.evaluate(() => (window as any).harness.designer.save());
        const reloaded = await roundTrip(harness, xml);

        expect(reloaded.result).toEqual({ imported: true, warnings: [] });
        // The edit replaces a binding with another of the same shape, so every element and foreign attribute remains.
        expect(await inventory(harness, xml)).toEqual(await inventory(harness, await readFile(source, 'utf8')));
        expect(await getBinding(harness, 'Task_Deep')).toEqual(edited);
        expect(await getBinding(harness, 'Task_Bound')).toEqual(untouched);
        expect(xml).toMatch(/<elsa:activityBinding activityType="Elsa.Delay">[\s\S]*?<\/elsa:activityBinding>\s*<acme:note>deep foreign content<\/acme:note>/);
    });
});

test.describe('Elsa test assets', () => {
    test('each one saves what its load reported, stably', async ({ harness }) => {
        const files = (await readdir(coreAssets)).filter(file => file.endsWith('.bpmn')).sort();
        expect(files.length).toBeGreaterThan(0);

        for (const file of files) {
            const source = join(coreAssets, file);
            const { result, saved } = await roundTrip(harness, await readFile(source, 'utf8'));

            expect(result.imported, file).toBe(true);
            await expectStable(harness, saved);
            await record(`core-assets.${file.replace(/\.bpmn$/, '')}`, source, result, saved);
        }
    });
});

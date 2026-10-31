// The Blazor host's JS shim (studio/src/modules/Elsa.Studio.Bpmn/wwwroot/bpmn-canvas-host.js), driven with the designer
// under test the way BpmnCanvasHost drives it. Every designer passes these, since a host must be able to load any of them.
import { test as base, expect, type Page } from '@playwright/test';
import { readFile } from 'node:fs/promises';
import { dirname, extname, join, normalize } from 'node:path';
import { fileURLToPath } from 'node:url';
import { CONTENT_TYPES, designerDirectory, designerModule, designerStylesheet, fixture } from './harness.js';

const here = dirname(fileURLToPath(import.meta.url));
const shim = join(here, '../../../src/modules/Elsa.Studio.Bpmn/wwwroot/bpmn-canvas-host.js');
const ORIGIN = 'http://host.test';

// Studio serves a module's static web assets under _content/<assembly>/, relative to the page's <base href>, which is
// not the site root here so that a URL resolved against the shim's own location would miss.
const SHIM_URL = '_content/Elsa.Studio.Bpmn/bpmn-canvas-host.js';
const DESIGNER_URL = `_content/designer/${designerModule}`;
const STYLESHEET_URL = `_content/designer/${designerStylesheet}`;
const OTHER_VERSION_URL = 'other-version.js';
const NO_MARKERS_URL = 'no-markers.js';

const PAGE = '<!doctype html><html><head><meta charset="utf-8"><base href="/app/"></head><body style="margin:0"><div id="canvas" style="width:1000px;height:600px"></div></body></html>';

type ShimResult = Record<string, unknown>;

async function openHost(page: Page): Promise<void> {
    await page.route(`${ORIGIN}/**`, async route => {
        const path = new URL(route.request().url()).pathname;
        if (path === '/app/') return route.fulfill({ contentType: 'text/html', body: PAGE });
        if (path === `/app/${SHIM_URL}`) return route.fulfill({ contentType: 'text/javascript', body: await readFile(shim) });
        if (path === `/app/${OTHER_VERSION_URL}`) {
            return route.fulfill({ contentType: 'text/javascript', body: 'export default { designerId: "other", contractVersion: 2, createDesigner() { throw new Error("A host must not call this."); } };' });
        }
        if (path === `/app/${NO_MARKERS_URL}`) {
            // A designer without the optional problem markers, reduced to what the shim calls while creating it.
            return route.fulfill({ contentType: 'text/javascript', body: 'export default { designerId: "plain", contractVersion: 1, createDesigner() { return { on() { return () => {}; }, dispose() {} }; } };' });
        }
        const prefix = '/app/_content/designer/';
        if (!path.startsWith(prefix)) return route.fulfill({ status: 404 });
        const file = normalize(join(designerDirectory, decodeURIComponent(path.slice(prefix.length))));
        if (!file.startsWith(designerDirectory)) return route.fulfill({ status: 403 });
        try {
            return await route.fulfill({ contentType: CONTENT_TYPES[extname(file)] ?? 'application/octet-stream', body: await readFile(file) });
        } catch {
            return route.fulfill({ status: 404 });
        }
    });
    await page.goto(`${ORIGIN}/app/`);
}

const test = base.extend<{ host: Page }>({
    host: async ({ page }, use) => {
        await openHost(page);
        await use(page);
    }
});

/** Imports the designer module the way a host would not, only to learn the id it declares. */
function designerId(page: Page): Promise<string> {
    return page.evaluate(async url => (await import(new URL(url, document.baseURI).href)).default.designerId, DESIGNER_URL);
}

test('creates the designer from URLs relative to the base URL, links its stylesheet once and forwards its events', async ({ host }) => {
    const id = await designerId(host);
    const xml = await fixture('order.bpmn');

    const result = await host.evaluate(async ({ id, xml, shimUrl, designerUrl, stylesheetUrl }) => {
        const { createDesigner } = await import(new URL(shimUrl, document.baseURI).href);
        const calls: { method: string; event: unknown }[] = [];
        const listener = { invokeMethodAsync: (method: string, event: unknown) => { calls.push({ method, event }); return Promise.resolve(); } };
        const canvas = document.getElementById('canvas');
        const out: ShimResult = {};

        const designer = await createDesigner(id, designerUrl, stylesheetUrl, canvas, false, listener);
        out.imported = (await designer.load(xml)).imported;
        out.selected = designer.select('Charge');
        out.applied = designer.setBinding('Charge', { activityType: 'Elsa.WriteLine', inputs: [{ name: 'text', value: '"hi"' }] });
        out.binding = designer.getBinding('Charge');
        out.saved = (await designer.save()).xml.includes('Elsa.WriteLine');
        out.methods = [...new Set(calls.map(call => call.method))].sort();
        out.lastChange = calls.filter(call => call.method === 'OnDesignerChanged').at(-1)?.event;
        const callsBeforeDispose = calls.length;
        designer.dispose();
        out.callsAfterDispose = calls.length - callsBeforeDispose;

        const readOnly = await createDesigner(id, designerUrl, stylesheetUrl, canvas, true, listener);
        await readOnly.load(xml);
        out.readOnlySetBinding = readOnly.setBinding('Charge', null);
        readOnly.dispose();

        out.stylesheets = Array.from(document.querySelectorAll<HTMLLinkElement>('link[rel="stylesheet"]')).map(link => new URL(link.href).pathname);
        return out;
    }, { id, xml, shimUrl: SHIM_URL, designerUrl: DESIGNER_URL, stylesheetUrl: STYLESHEET_URL });

    expect(result).toEqual({
        imported: true,
        selected: true,
        applied: { applied: true },
        binding: { activityType: 'Elsa.WriteLine', inputs: [{ name: 'text', value: '"hi"' }] },
        saved: true,
        methods: ['OnDesignerChanged', 'OnDesignerSelectionChanged'],
        lastChange: expect.objectContaining({ elementIds: ['Charge'], isDirty: true, canUndo: true }),
        callsAfterDispose: 0,
        readOnlySetBinding: { applied: false, reason: 'read-only' },
        stylesheets: [`/app/${STYLESHEET_URL}`]
    });
});

test('refuses a module that declares another id than the one the host chose', async ({ host }) => {
    const message = await host.evaluate(async ({ shimUrl, designerUrl }) => {
        const { createDesigner } = await import(new URL(shimUrl, document.baseURI).href);
        return createDesigner('another-designer', designerUrl, null, document.getElementById('canvas'), false, { invokeMethodAsync: () => Promise.resolve() })
            .then(() => 'created', (error: Error) => error.message);
    }, { shimUrl: SHIM_URL, designerUrl: DESIGNER_URL });

    expect(message).toContain("not 'another-designer'");
});

test('refuses a module that implements a contract version the host does not support, without calling it', async ({ host }) => {
    const message = await host.evaluate(async ({ shimUrl, otherVersionUrl }) => {
        const { createDesigner } = await import(new URL(shimUrl, document.baseURI).href);
        return createDesigner('other', otherVersionUrl, null, document.getElementById('canvas'), false, { invokeMethodAsync: () => Promise.resolve() })
            .then(() => 'created', (error: Error) => error.message);
    }, { shimUrl: SHIM_URL, otherVersionUrl: OTHER_VERSION_URL });

    expect(message).toBe(`The BPMN designer module '${OTHER_VERSION_URL}' implements contract version 2, but this host supports version 1.`);
});

test('passes problem markers to a designer that has them, and answers null for one that does not', async ({ host }) => {
    const id = await designerId(host);
    const xml = await fixture('order.bpmn');

    const result = await host.evaluate(async ({ id, xml, shimUrl, designerUrl, noMarkersUrl }) => {
        const { createDesigner } = await import(new URL(shimUrl, document.baseURI).href);
        const listener = { invokeMethodAsync: () => Promise.resolve() };
        const canvas = document.getElementById('canvas');

        const designer = await createDesigner(id, designerUrl, null, canvas, false, listener);
        await designer.load(xml);
        // Whether the designer under test has the optional markers, asked of a designer of its own.
        const probe = (await import(new URL(designerUrl, document.baseURI).href)).default.createDesigner(document.createElement('div'));
        const hasMarkers = typeof probe.applyMarkers === 'function';
        probe.dispose();
        const applied = designer.applyMarkers([{ elementId: 'Charge', severity: 'error', message: 'Unbound.' }]);
        const cleared = designer.clearMarkers();
        designer.dispose();

        const plain = await createDesigner('plain', noMarkersUrl, null, canvas, false, listener);
        const plainResult = { applied: plain.applyMarkers([{ elementId: 'Charge', severity: 'error', message: 'Unbound.' }]), cleared: plain.clearMarkers() };
        plain.dispose();

        return { hasMarkers, applied, cleared, plain: plainResult };
    }, { id, xml, shimUrl: SHIM_URL, designerUrl: DESIGNER_URL, noMarkersUrl: NO_MARKERS_URL });

    expect(result.plain).toEqual({ applied: null, cleared: null });
    if (result.hasMarkers) {
        expect(result.applied).toEqual({ applied: ['Charge'], unknownElementIds: [] });
        expect(result.cleared).toEqual({ applied: [], unknownElementIds: [] });
    } else {
        expect(result).toEqual(expect.objectContaining({ applied: null, cleared: null }));
    }
});

import { test as base, type Page } from '@playwright/test';
import { readFile } from 'node:fs/promises';
import { dirname, extname, join, normalize, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));

/**
 * The designer under test: a directory holding its module and stylesheet. Defaults to the bpmn-js designer's build
 * output. Every designer passes the same suite, so another designer runs it by pointing these at its own build.
 */
const designerDirectory = resolve(process.env.BPMN_DESIGNER_DIRECTORY ?? join(here, '../../../src/modules/Elsa.Studio.Bpmn.BpmnJs/wwwroot'));
const designerModule = process.env.BPMN_DESIGNER_MODULE ?? 'bpmn-js-designer.js';
const designerStylesheet = process.env.BPMN_DESIGNER_STYLESHEET ?? 'bpmn-js-designer.css';

const ORIGIN = 'http://designer.test';
const CONTENT_TYPES: Record<string, string> = {
    '.js': 'text/javascript', '.css': 'text/css', '.woff2': 'font/woff2', '.woff': 'font/woff', '.ttf': 'font/ttf', '.eot': 'application/vnd.ms-fontobject', '.svg': 'image/svg+xml'
};

// The page keeps one designer and records every event it raises, so tests can assert on what a host would receive.
const PAGE = `<!doctype html>
<html>
<head><meta charset="utf-8"><link rel="stylesheet" href="/${designerStylesheet}"></head>
<body style="margin:0">
<div id="canvas" style="width:1000px;height:600px"></div>
<div id="properties" style="width:280px"></div>
<script type="module">
import module from '/${designerModule}';
window.harness = {
    module,
    designer: null,
    events: [],
    create(options) {
        this.designer?.dispose();
        this.events = [];
        const properties = document.getElementById('properties');
        this.designer = module.createDesigner(document.getElementById('canvas'), { ...options, propertiesContainer: options?.withProperties ? properties : null });
        this.designer.on('change', event => this.events.push({ type: 'change', ...event }));
        this.designer.on('selection', event => this.events.push({ type: 'selection', ...event }));
    }
};
window.harnessReady = true;
</script>
</body>
</html>`;

export async function fixture(name: string): Promise<string> {
    return readFile(join(here, 'fixtures', name), 'utf8');
}

async function openHarness(page: Page): Promise<void> {
    const errors: string[] = [];
    page.on('pageerror', error => errors.push(error.message));
    await page.route(`${ORIGIN}/**`, async route => {
        const path = new URL(route.request().url()).pathname;
        if (path === '/') return route.fulfill({ contentType: 'text/html', body: PAGE });
        const file = normalize(join(designerDirectory, decodeURIComponent(path)));
        if (!file.startsWith(designerDirectory)) return route.fulfill({ status: 403 });
        try {
            return await route.fulfill({ contentType: CONTENT_TYPES[extname(file)] ?? 'application/octet-stream', body: await readFile(file) });
        } catch {
            return route.fulfill({ status: 404 });
        }
    });
    await page.goto(`${ORIGIN}/`);
    await page.waitForFunction(() => (window as any).harnessReady === true).catch(error => {
        throw new Error(`The designer module did not load from ${designerDirectory}: ${errors.join('; ') || error}`);
    });
}

export const test = base.extend<{ harness: Page }>({
    harness: async ({ page }, use) => {
        await openHarness(page);
        await use(page);
    }
});

export { expect } from '@playwright/test';

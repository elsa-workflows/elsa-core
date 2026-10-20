// SPIKE (#8901): drives the harness in headless Chromium and records evidence for checks 1-6.
// Usage: node build.mjs && SPIKE_OUT=/some/dir node run-spike.mjs
import { createServer } from 'node:http';
import { createRequire } from 'node:module';
import { execSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import { mkdirSync, readFileSync, readdirSync, writeFileSync, existsSync, statSync } from 'node:fs';
import { extname, join, resolve } from 'node:path';

const here = new URL('.', import.meta.url).pathname;
const repo = resolve(here, '../../..');
const out = process.env.SPIKE_OUT ?? join(here, 'out');
mkdirSync(out, { recursive: true });

function loadPlaywright() {
    try {
        return createRequire(import.meta.url)('playwright');
    } catch {
        return createRequire(execSync('npm root -g').toString().trim() + '/')('playwright');
    }
}
const { chromium } = loadPlaywright();

// ---- static server for the spike folder ---------------------------------------------------------------------------
const types = { '.html': 'text/html', '.js': 'text/javascript', '.css': 'text/css', '.json': 'application/json', '.map': 'application/json', '.woff2': 'font/woff2', '.woff': 'font/woff', '.ttf': 'font/ttf', '.svg': 'image/svg+xml', '.eot': 'application/vnd.ms-fontobject', '.wasm': 'application/wasm', '.dll': 'application/octet-stream', '.dat': 'application/octet-stream', '.blat': 'application/octet-stream', '.bpmn': 'application/xml', '.ico': 'image/x-icon', '.pdb': 'application/octet-stream' };
const blazorRoot = join(here, 'blazor-harness/bin/publish/wwwroot');
function serve(spikeRoot) {
    const server = createServer((request, response) => {
        let path = decodeURIComponent(new URL(request.url, 'http://x').pathname);
        let root = spikeRoot;
        if (path.startsWith('/blazor/')) { root = blazorRoot; path = path.slice('/blazor'.length); }
        let file = join(root, path);
        if (existsSync(file) && statSync(file).isDirectory()) file = join(file, 'index.html');
        if (!file.startsWith(root) || !existsSync(file)) { response.writeHead(404); response.end(); return; }
        response.writeHead(200, { 'content-type': types[extname(file)] ?? 'application/octet-stream', 'cache-control': 'no-store' });
        response.end(readFileSync(file));
    });
    return new Promise(done => server.listen(0, '127.0.0.1', () => done(server)));
}

// ---- fixtures -----------------------------------------------------------------------------------------------------
function fixtures() {
    const dirs = [
        ['core-assets', join(repo, 'core/test/integration/Elsa.Bpmn.Interchange.IntegrationTests/Assets')],
        ['clientlib-fixtures', join(repo, 'studio/src/modules/Elsa.Studio.Workflows.Designer/ClientLib/src/bpmn/__fixtures__')],
        ['spike', join(here, 'fixtures')]
    ];
    const seen = new Map();
    const list = [];
    for (const [source, dir] of dirs) {
        for (const name of readdirSync(dir).filter(f => f.endsWith('.bpmn')).sort()) {
            const xml = readFileSync(join(dir, name), 'utf8');
            const hash = createHash('sha256').update(xml).digest('hex').slice(0, 12);
            if (seen.has(hash)) { seen.get(hash).alsoIn.push(`${source}/${name}`); continue; }
            const entry = { id: `${source}/${name}`, xml, alsoIn: [] };
            seen.set(hash, entry);
            list.push(entry);
        }
    }
    return list;
}

const results = { environment: {}, checks: {} };
const server = await serve(here);
const base = `http://127.0.0.1:${server.address().port}`;
const browser = await chromium.launch();
results.environment = { chromium: browser.version(), node: process.version, playwright: createRequire(import.meta.url).resolve ? 'global' : '' };

async function openHarness(context, viewport = { width: 1440, height: 900 }) {
    const page = await context.newPage();
    await page.setViewportSize(viewport);
    const errors = [];
    page.on('pageerror', error => errors.push(String(error)));
    page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
    await page.goto(`${base}/harness/index.html`);
    await page.waitForFunction(() => window.spikeReady === true);
    return { page, errors };
}

const shot = (page, name) => page.screenshot({ path: join(out, name) });
const crafted = readFileSync(join(here, 'fixtures/foreign-unknown-deep.bpmn'), 'utf8');

// ---- check 1: load time in a representative static page ------------------------------------------------------------
async function measureLoad(label, { cpu = 1, network = null } = {}) {
    const runs = [];
    for (let i = 0; i < 5; i++) {
        const context = await browser.newContext();
        const page = await context.newPage();
        const cdp = await context.newCDPSession(page);
        if (cpu > 1) await cdp.send('Emulation.setCPUThrottlingRate', { rate: cpu });
        if (network) { await cdp.send('Network.enable'); await cdp.send('Network.emulateNetworkConditions', network); }
        await page.setViewportSize({ width: 1440, height: 900 });
        await page.goto(`${base}/harness/index.html`);
        await page.waitForFunction(() => window.spikeReady === true, null, { timeout: 120000 });
        const timing = await page.evaluate(async xml => {
            const importMs = performance.measure('import', 'module-import-start', 'module-import-end').duration;
            const t0 = performance.now();
            const handle = window.spike.module.createModeler(document.getElementById('canvas'), { propertiesContainer: document.getElementById('properties') });
            const t1 = performance.now();
            const result = await handle.load(xml);
            await new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r)));
            const t2 = performance.now();
            const navigation = performance.getEntriesByType('navigation')[0];
            return { importMs, createMs: t1 - t0, loadAndPaintMs: t2 - t1, imported: result.imported, sinceNavigationStartMs: t2, domContentLoadedMs: navigation.domContentLoadedEventEnd };
        }, crafted);
        runs.push(timing);
        await context.close();
    }
    const median = key => { const v = runs.map(r => r[key]).sort((a, b) => a - b); return Math.round(v[Math.floor(v.length / 2)]); };
    return { label, runs: runs.length, medianMs: { moduleDownloadParseEval: median('importMs'), createModeler: median('createMs'), loadCraftedDocAndPaint: median('loadAndPaintMs'), canvasInteractiveSinceNavigationStart: median('sinceNavigationStartMs') } };
}

results.checks.loadTime = {
    note: 'Static harness page, not the Studio WASM host: these are the costs the module adds on top of Studio boot. Localhost serving; network row emulates a 10 Mbps / 40 ms link.',
    rows: [
        await measureLoad('no throttling'),
        await measureLoad('4x CPU slowdown', { cpu: 4 }),
        await measureLoad('4x CPU + 10 Mbps/40 ms', { cpu: 4, network: { offline: false, latency: 40, downloadThroughput: 10 * 1024 * 1024 / 8, uploadThroughput: 5 * 1024 * 1024 / 8 } })
    ]
};
results.checks.bundleSize = JSON.parse(readFileSync(join(here, 'dist/sizes.json'), 'utf8'));

// ---- check 2: the module API, events, overlays, markers, dispose ---------------------------------------------------
{
    const context = await browser.newContext();
    const { page, errors } = await openHarness(context);
    const cdp = await context.newCDPSession(page);
    await cdp.send('Performance.enable');

    async function listenerCounts() {
        const counts = {};
        for (const [name, expression] of Object.entries({ window: 'window', document: 'document', body: 'document.body', canvasHost: 'document.getElementById("canvas")', propertiesHost: 'document.getElementById("properties")' })) {
            const { result } = await cdp.send('Runtime.evaluate', { expression, objectGroup: 'listener-check' });
            const { listeners } = await cdp.send('DOMDebugger.getEventListeners', { objectId: result.objectId, depth: 0 });
            counts[name] = listeners.length;
            await cdp.send('Runtime.releaseObjectGroup', { objectGroup: 'listener-check' });
        }
        return counts;
    }
    async function metrics() {
        await cdp.send('HeapProfiler.enable');
        await cdp.send('HeapProfiler.collectGarbage');
        const { metrics } = await cdp.send('Performance.getMetrics');
        const pick = n => metrics.find(m => m.name === n)?.value;
        return { jsEventListeners: pick('JSEventListeners'), nodes: pick('Nodes'), jsHeapUsedKiB: Math.round(pick('JSHeapUsedSize') / 1024) };
    }
    async function liveModelers() {
        await cdp.send('HeapProfiler.collectGarbage');
        // Remote objects are put in a group and released: a held query result would itself keep instances alive.
        const { result: proto } = await cdp.send('Runtime.evaluate', { expression: 'window.spike.module._ModelerPrototype', objectGroup: 'leak-check' });
        const { objects } = await cdp.send('Runtime.queryObjects', { prototypeObjectId: proto.objectId, objectGroup: 'leak-check' });
        const { result } = await cdp.send('Runtime.callFunctionOn', { objectId: objects.objectId, functionDeclaration: 'function () { return this.length; }', returnByValue: true });
        await cdp.send('Runtime.releaseObjectGroup', { objectGroup: 'leak-check' });
        await cdp.send('HeapProfiler.collectGarbage');
        return result.value;
    }

    const before = await listenerCounts();
    const api = await page.evaluate(async xml => {
        const { createModeler } = window.spike.module;
        const handle = window.handle = createModeler(document.getElementById('canvas'), { propertiesContainer: document.getElementById('properties') });
        const events = window.events = { change: [], selection: [] };
        const offChange = handle.on('change', e => events.change.push(e));
        handle.on('selection', e => events.selection.push(e));
        const load = await handle.load(xml);
        const selectKnown = handle.select('Task_Deep');
        const selectUnknown = handle.select('Does_Not_Exist');
        const modeling = handle._bpmnjs.get('modeling');
        const registry = handle._bpmnjs.get('elementRegistry');
        modeling.updateLabel(registry.get('Task_Bound'), 'Notify warehouse (renamed)');
        const afterEdit = events.change.at(-1);
        const saved = await handle.save();
        modeling.moveElements([registry.get('Task_Deep')], { x: 10, y: 0 });
        const afterSaveEdit = events.change.at(-1);
        handle._bpmnjs.get('commandStack').undo();
        const afterUndo = events.change.at(-1);
        const overlayResult = handle.applyOverlays({ Task_Bound: { started: 3, completed: 3 }, Task_Deep: { started: 1, faulted: true }, Sub_L2: { started: 2, active: 1 }, Nope: { started: 1 } });
        const overlayDom = { overlays: document.querySelectorAll('.djs-overlay-elsa-element-state').length, faulted: document.querySelectorAll('.djs-element.elsa-faulted').length, completed: document.querySelectorAll('.djs-element.elsa-completed').length, active: document.querySelectorAll('.djs-element.elsa-active').length };
        return { load: { imported: load.imported, warningCount: load.warnings.length, warnings: load.warnings }, selectKnown, selectUnknown, selectionEvents: events.selection.slice(), afterEdit, afterSaveEdit, afterUndo, savedLength: saved.length, overlayResult, overlayDom, offChangeType: typeof offChange };
    }, crafted);
    await page.waitForTimeout(200);
    await shot(page, '2-overlays-and-markers.png');
    const live = await listenerCounts();
    const cleared = await page.evaluate(() => {
        window.handle.clearOverlays();
        return { overlays: document.querySelectorAll('.djs-overlay-elsa-element-state').length, markers: document.querySelectorAll('.elsa-faulted,.elsa-completed,.elsa-active').length };
    });
    const disposed = await page.evaluate(() => {
        const before = window.events.change.length;
        window.handle.dispose();
        window.handle.dispose(); // idempotent
        let threw = false;
        try { window.handle.select('Task_Deep'); } catch { threw = true; }
        return { callAfterDisposeThrows: threw, canvasChildren: document.getElementById('canvas').children.length, propertiesChildren: document.getElementById('properties').children.length, changeEventsAfterDispose: window.events.change.length - before };
    });
    delete api.selectionEvents?.[0]?.raw;
    const afterDispose = await listenerCounts();
    await page.evaluate(() => { window.handle = null; window.events = null; });

    // Repeated mount/unmount: a host that opens and closes the designer 20 times.
    const baseline = await metrics();
    const baselineModelers = await liveModelers();
    const cycle = n => page.evaluate(async ([xml, n]) => {
        for (let i = 0; i < n; i++) {
            const handle = window.spike.module.createModeler(document.getElementById('canvas'), { propertiesContainer: document.getElementById('properties') });
            handle.on('change', () => {});
            handle.on('selection', () => {});
            await handle.load(xml);
            handle.select('Task_Bound');
            handle.applyOverlays({ Task_Bound: { completed: 1 } });
            handle.dispose();
        }
    }, [crafted, n]);
    await cycle(20);
    await page.waitForTimeout(500);
    const after20 = await metrics();
    const after20Modelers = await liveModelers();
    await cycle(40);
    await page.waitForTimeout(500);
    const after60 = await metrics();
    const after60Modelers = await liveModelers();
    results.checks.api = { ...api, cleared, disposed, listeners: { beforeCreate: before, whileMounted: live, afterDispose }, mountUnmount: { baseline, after20, after60, liveModelerInstances: { baseline: baselineModelers, after20: after20Modelers, after60: after60Modelers } }, pageErrors: errors };
    await context.close();
}


// ---- check 2 again, through Blazor WebAssembly JS interop -----------------------------------------------------------
if (!existsSync(blazorRoot)) {
    results.checks.blazor = { skipped: 'blazor-harness not published (dotnet publish blazor-harness -c Release -o blazor-harness/bin/publish)' };
} else {
    const marks = [];
    for (let i = 0; i < 3; i++) {
        const context = await browser.newContext();
        const page = await context.newPage();
        await page.setViewportSize({ width: 1440, height: 900 });
        await page.goto(`${base}/blazor/`);
        await page.waitForFunction(() => document.getElementById('status')?.textContent.startsWith('loaded'), null, { timeout: 120000 });
        marks.push(await page.evaluate(() => Object.fromEntries(performance.getEntriesByType('mark').map(m => [m.name, Math.round(m.startTime)]))));
        await context.close();
    }
    const context = await browser.newContext();
    const page = await context.newPage();
    const errors = [];
    page.on('pageerror', error => errors.push(String(error)));
    page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto(`${base}/blazor/`);
    await page.waitForFunction(() => document.getElementById('status')?.textContent.startsWith('loaded'), null, { timeout: 120000 });
    const cdp = await context.newCDPSession(page);
    await cdp.send('Performance.enable');
    const gcMetrics = async () => {
        await cdp.send('HeapProfiler.enable');
        await cdp.send('HeapProfiler.collectGarbage');
        const { metrics } = await cdp.send('Performance.getMetrics');
        return { jsEventListeners: metrics.find(m => m.name === 'JSEventListeners').value, nodes: metrics.find(m => m.name === 'Nodes').value };
    };
    const liveModelers = async () => {
        await cdp.send('HeapProfiler.collectGarbage');
        const { result: proto } = await cdp.send('Runtime.evaluate', { expression: "import('/blazor/bpmn/elsa-bpmn-modeler.js').then(m => m._ModelerPrototype)", awaitPromise: true, objectGroup: 'leak-check' });
        const { objects } = await cdp.send('Runtime.queryObjects', { prototypeObjectId: proto.objectId, objectGroup: 'leak-check' });
        const { result } = await cdp.send('Runtime.callFunctionOn', { objectId: objects.objectId, functionDeclaration: 'function () { return this.length; }', returnByValue: true });
        await cdp.send('Runtime.releaseObjectGroup', { objectGroup: 'leak-check' });
        await cdp.send('HeapProfiler.collectGarbage');
        return result.value;
    };
    const logLines = () => page.evaluate(() => document.getElementById('log').textContent.split('\n').filter(Boolean));
    const waitForLog = prefix => page.waitForFunction(p => document.getElementById('log').textContent.split('\n').some(l => l.startsWith(p)), prefix);
    const mounted = { metrics: await gcMetrics(), liveModelers: await liveModelers() };
    await page.click('#select');
    await waitForLog('selection');
    await page.click('#select-missing');
    await waitForLog('select-missing');
    await page.click('#overlays');
    await waitForLog('overlays');
    const overlayDom = await page.evaluate(() => ({ overlays: document.querySelectorAll('.djs-overlay-elsa-element-state').length, faulted: document.querySelectorAll('.djs-element.elsa-faulted').length }));
    await page.screenshot({ path: join(out, '2-blazor-wasm-host.png') });
    await page.click('#clear-overlays');
    await waitForLog('clear-overlays');
    const afterClear = await page.evaluate(() => document.querySelectorAll('.djs-overlay-elsa-element-state').length);
    // An edit made in the properties panel reaches .NET as a change event.
    // Canvas click by coordinates and panel typing from inside the page: Playwright element handles on the modeler's DOM
    // would keep the disposed DOM (and through it the modeler) alive and spoil the leak measurement below.
    const target = await page.evaluate(() => { const r = document.querySelector('[data-element-id="Task_Bound"]').getBoundingClientRect(); return { x: r.x + r.width / 2, y: r.y + r.height / 2 }; });
    await page.mouse.click(target.x, target.y);
    await page.waitForFunction(() => !!document.querySelector('[data-group-id="group-elsa-activity"]'));
    await page.evaluate(async () => {
        const header = document.querySelector('[data-group-id="group-elsa-activity"] .bio-properties-panel-group-header');
        if (!header.classList.contains('open')) header.click();
        await new Promise(r => setTimeout(r, 100));
        const input = document.querySelector('[data-entry-id="elsa-activityType"] input');
        input.focus();
        input.value = 'Elsa.WriteLine2';
        input.dispatchEvent(new Event('input', { bubbles: true }));
        input.blur();
    });
    await waitForLog('change');
    await page.click('#save');
    await waitForLog('save');
    const beforeDispose = await gcMetrics();
    await page.click('#dispose');
    await waitForLog('dispose');
    await page.waitForTimeout(300);
    const disposed = {
        metrics: await gcMetrics(),
        liveModelers: await liveModelers(),
        canvasChildren: await page.evaluate(() => document.querySelector('.canvas').children.length),
        propertiesChildren: await page.evaluate(() => document.querySelector('.properties').children.length)
    };
    const median = key => { const v = marks.map(m => m[key]).sort((a, b) => a - b); return v[Math.floor(v.length / 2)]; };
    const frameworkFiles = readdirSync(join(blazorRoot, '_framework'));
    const brotliBytes = frameworkFiles.filter(f => f.endsWith('.br')).reduce((sum, f) => sum + statSync(join(blazorRoot, '_framework', f)).size, 0);
    results.checks.blazor = {
        host: 'Minimal Blazor WebAssembly app (net10.0, Release publish), not the Studio host. Served uncompressed from localhost.',
        frameworkPayloadBrotliKiB: Math.round(brotliBytes / 1024),
        medianMarksMsSinceNavigation: { blazorFirstRender: median('blazor-first-render'), moduleImported: median('module-imported'), canvasInteractive: median('canvas-interactive') },
        runs: marks,
        dotnetReceived: await logLines(),
        overlayDom, overlaysAfterClear: afterClear,
        mounted, beforeDispose, disposed,
        pageErrors: errors
    };
    await context.close();
}

// ---- check 3: theming ---------------------------------------------------------------------------------------------
{
    const context = await browser.newContext();
    const { page, errors } = await openHarness(context);
    await page.evaluate(async xml => {
        const handle = window.handle = window.spike.module.createModeler(document.getElementById('canvas'), { propertiesContainer: document.getElementById('properties') });
        await handle.load(xml);
        handle.applyOverlays({ Task_Bound: { completed: 3 }, Task_Deep: { faulted: true, started: 1 } });
        handle.select('Task_Bound');
    }, crafted);
    await page.waitForTimeout(300);
    const sample = () => page.evaluate(() => {
        const css = el => el ? getComputedStyle(el) : null;
        const task = document.querySelector('[data-element-id="E_Task"] .djs-visual > rect');
        const label = document.querySelector('[data-element-id="E_Task"] .djs-visual text');
        const flow = document.querySelector('[data-element-id="E_Flow_1"] .djs-visual > path');
        const colours = new Set();
        for (const el of document.querySelectorAll('.djs-visual *')) {
            const s = getComputedStyle(el);
            if (s.fill && s.fill !== 'none') colours.add(`fill ${s.fill}`);
            if (s.stroke && s.stroke !== 'none') colours.add(`stroke ${s.stroke}`);
        }
        return {
            taskInlineFill: task?.style.fill, taskFill: css(task)?.fill, taskStroke: css(task)?.stroke, labelFill: css(label)?.fill, flowStroke: css(flow)?.stroke,
            flowMarker: flow?.style.markerEnd,
            canvasBackground: css(document.querySelector('#canvas'))?.backgroundColor,
            palette: css(document.querySelector('.djs-palette'))?.backgroundColor,
            panelHeader: css(document.querySelector('.bio-properties-panel-header'))?.backgroundColor,
            panelGroupHeader: css(document.querySelector('.bio-properties-panel-group-header'))?.backgroundColor,
            panelText: css(document.querySelector('.bio-properties-panel-group-header-title'))?.color,
            panelInput: css(document.querySelector('.bio-properties-panel-input'))?.backgroundColor,
            contextPadEntry: css(document.querySelector('.djs-context-pad .entry'))?.backgroundColor,
            distinctCanvasColours: [...colours].sort()
        };
    });
    const light = await sample();
    await shot(page, '3-theme-light.png');
    await page.click('#theme');
    await page.waitForTimeout(200);
    const dark = await sample();
    await shot(page, '3-theme-dark.png');
    // Popup menu (replace menu) and direct editing in dark mode.
    await page.evaluate(() => {
        const bpmnjs = window.handle._bpmnjs;
        const element = bpmnjs.get('elementRegistry').get('E_Task');
        const gfx = bpmnjs.get('elementRegistry').getGraphics(element).getBoundingClientRect();
        bpmnjs.get('popupMenu').open(element, 'bpmn-replace', { x: gfx.right, y: gfx.bottom });
    });
    await page.waitForTimeout(200);
    const popup = await page.evaluate(() => ({ background: getComputedStyle(document.querySelector('.djs-popup')).backgroundColor, entryColor: getComputedStyle(document.querySelector('.djs-popup .entry')).color }));
    await shot(page, '3-theme-dark-popup.png');
    await page.keyboard.press('Escape');
    await page.evaluate(() => window.handle._bpmnjs.get('directEditing').activate(window.handle._bpmnjs.get('elementRegistry').get('E_Task')));
    await page.waitForTimeout(100);
    const directEditing = await page.evaluate(() => { const box = document.querySelector('.djs-direct-editing-parent'); const s = getComputedStyle(box); return { background: s.backgroundColor, color: s.color, inlineStyle: box.getAttribute('style') }; });
    await shot(page, '3-theme-dark-direct-editing.png');
    results.checks.theming = { light, dark, darkPopup: popup, darkDirectEditing: directEditing, pageErrors: errors };
    await context.close();
}

// ---- checks 4 and 5: round trip and properties-panel editing ------------------------------------------------------
async function roundTrip(page, xml, options = {}) {
    return page.evaluate(async ([xml, options]) => {
        const { createModeler } = window.spike.module;
        const handle = createModeler(document.getElementById('canvas'), options);
        try {
            const load = await handle.load(xml);
            if (!load.imported) return { load };
            const saved = await handle.save();
            const load2 = await handle.load(saved);
            const saved2 = await handle.save();
            return { load, saved, diff: window.spike.tools.canonicalDiff(xml, saved), stable: window.spike.tools.canonicalDiff(saved, saved2).differences.length === 0, secondCycleDifferences: window.spike.tools.canonicalDiff(saved, saved2).differences, secondLoadWarnings: load2.warnings.length };
        } finally {
            handle.dispose();
        }
    }, [xml, options]);
}

{
    const context = await browser.newContext();
    const { page, errors } = await openHarness(context);
    const perFixture = [];
    for (const fixture of fixtures()) {
        const result = await roundTrip(page, fixture.xml);
        const entry = { fixture: fixture.id, alsoIn: fixture.alsoIn, imported: result.load.imported, error: result.load.error, warnings: result.load.warnings };
        if (result.load.imported) {
            entry.differences = result.diff.differences;
            entry.lexical = result.diff.lexical;
            entry.stableOnSecondCycle = result.stable;
            entry.secondCycleDifferences = result.secondCycleDifferences;
            writeFileSync(join(out, `roundtrip-${fixture.id.replace(/[/]/g, '__')}`), result.saved);
            const preserved = await roundTrip(page, fixture.xml, { preserveDiOrder: true });
            entry.withPreserveDiOrder = { differences: preserved.diff.differences, stableOnSecondCycle: preserved.stable };
        } else {
            // No DI: bpmn-js cannot draw it. Try generating DI with bpmn-auto-layout first, then compare everything but DI.
            const layout = await page.evaluate(async xml => {
                try {
                    const laidOut = await window.spike.tools.layoutProcess(xml);
                    const handle = window.spike.module.createModeler(document.getElementById('canvas'));
                    const load = await handle.load(laidOut);
                    const saved = load.imported ? await handle.save() : null;
                    handle.dispose();
                    return { saved, imported: load.imported, error: load.error, warnings: load.warnings, semanticDiff: saved ? window.spike.tools.canonicalDiff(xml, saved, { ignoreDi: true }).differences : null, layoutDiff: window.spike.tools.canonicalDiff(xml, laidOut, { ignoreDi: true }).differences };
                } catch (error) {
                    return { layoutError: String(error) };
                }
            }, fixture.xml);
            if (layout.saved) writeFileSync(join(out, `autolayout-roundtrip-${fixture.id.replace(/[/]/g, '__')}`), layout.saved);
            entry.autoLayout = { ...layout, saved: undefined };
        }
        perFixture.push(entry);
    }
    results.checks.roundTrip = { perFixture, pageErrors: errors.slice() };

    // With BpmnDiOrdering switched off, does an edit that creates shapes still write their DI?
    results.checks.roundTrip.preserveDiOrderEdit = await page.evaluate(async xml => {
        const handle = window.spike.module.createModeler(document.getElementById('canvas'), { preserveDiOrder: true });
        await handle.load(xml);
        const bpmnjs = handle._bpmnjs;
        const registry = bpmnjs.get('elementRegistry');
        const modeling = bpmnjs.get('modeling');
        const parent = registry.get('Sub_L3');
        const created = modeling.createShape({ type: 'bpmn:Task' }, { x: 820, y: 360 }, parent);
        modeling.connect(registry.get('Task_Deep'), created);
        const saved = await handle.save();
        handle.dispose();
        const diff = window.spike.tools.canonicalDiff(xml, saved).differences;
        return { createdId: created.businessObject.id, added: diff.filter(d => d.kind === 'added').map(d => d.path), reordered: diff.filter(d => d.kind === 'reordered').map(d => d.path) };
    }, crafted);

    // Edited round trip of the crafted document, with the elsa edits made through the properties panel (check 5).
    await page.evaluate(async xml => {
        const handle = window.handle = window.spike.module.createModeler(document.getElementById('canvas'), { propertiesContainer: document.getElementById('properties') });
        window.changes = [];
        window.originalXml = xml;
        handle.on('change', e => window.changes.push(e));
        await handle.load(xml);
        const bpmnjs = handle._bpmnjs;
        const registry = bpmnjs.get('elementRegistry');
        const modeling = bpmnjs.get('modeling');
        modeling.updateLabel(registry.get('Task_Deep'), 'Deep task (renamed)');
        modeling.moveElements([registry.get('Task_Deep')], { x: 0, y: 20 });
        handle.select('Task_Bound');
    }, crafted);
    await page.waitForTimeout(300);

    const panel = {};
    const group = id => page.locator(`[data-group-id="group-${id}"]`);
    async function openGroup(id) {
        const header = group(id).locator('.bio-properties-panel-group-header').first();
        if (!(await header.getAttribute('class')).includes('open')) await header.click();
    }
    await openGroup('elsa-activity');
    const activityType = group('elsa-activity').locator('[data-entry-id="elsa-activityType"] input');
    panel.initialActivityType = await activityType.inputValue();
    await openGroup('elsa-inputs');
    panel.inputItems = await group('elsa-inputs').locator('.bio-properties-panel-collapsible-entry-header-title').allInnerTexts();
    // Expand the "text" input and replace its JSON value.
    await group('elsa-inputs').locator('.bio-properties-panel-collapsible-entry-header').first().click();
    const textValue = group('elsa-inputs').locator('[data-entry-id="Task_Bound-elsa-input-0-value"] textarea');
    panel.initialTextValue = await textValue.inputValue();
    await textValue.fill('{"typeName":"String","expression":{"type":"Literal","value":"edited in the panel <&>"}}');
    await textValue.blur();
    await page.waitForTimeout(600);
    // Invalid JSON is flagged and not written.
    await textValue.fill('{not json');
    await page.waitForTimeout(600);
    panel.invalidJsonError = await group('elsa-inputs').locator('.bio-properties-panel-error').allInnerTexts();
    await textValue.fill('{"typeName":"String","expression":{"type":"Literal","value":"edited in the panel <&>"}}');
    await textValue.blur();
    await page.waitForTimeout(600);
    await shot(page, '5-properties-panel-elsa-inputs.png');

    // Bind a task created in this session (no extensionElements at all), then give it an input, all in the panel.
    // Insert it on the gateway's default path: Gateway_Route -> new task -> End_Default.
    const newTaskId = await page.evaluate(() => {
        const bpmnjs = window.handle._bpmnjs;
        const registry = bpmnjs.get('elementRegistry');
        const modeling = bpmnjs.get('modeling');
        const created = modeling.createShape({ type: 'bpmn:Task' }, { x: 300, y: 680 }, registry.get('Participant_Main'));
        modeling.reconnectEnd(registry.get('Flow_Default'), created, { x: 300, y: 680 });
        modeling.connect(created, registry.get('End_Default'));
        window.handle.select(created.id);
        return created.id;
    });
    await page.waitForTimeout(300);
    await openGroup('elsa-activity');
    await group('elsa-activity').locator('[data-entry-id="elsa-activityType"] input').fill('Elsa.WriteLine');
    await group('elsa-activity').locator('[data-entry-id="elsa-activityType"] input').blur();
    await page.waitForTimeout(600);
    panel.inputsGroupAppearsAfterBinding = await group('elsa-inputs').count() === 1;
    await openGroup('elsa-inputs');
    await group('elsa-inputs').locator('.bio-properties-panel-add-entry').click();
    await page.waitForTimeout(300);
    const newName = page.locator(`[data-entry-id="${newTaskId}-elsa-input-0-name"] input`);
    await newName.fill('text');
    await newName.blur();
    await page.waitForTimeout(600);
    const newValue = page.locator(`[data-entry-id="${newTaskId}-elsa-input-0-value"] textarea`);
    if (!(await newValue.isVisible())) await page.locator(`[data-entry-id="${newTaskId}-elsa-input-0-name"]`).locator('xpath=ancestor::*[contains(@class,"bio-properties-panel-collapsible-entry")][1]').locator('.bio-properties-panel-collapsible-entry-header').click();
    await newValue.fill('{"typeName":"String","expression":{"type":"Literal","value":"added in the panel"}}');
    await newValue.blur();
    await page.waitForTimeout(600);
    await shot(page, '5-properties-panel-new-binding.png');
    panel.newTaskId = newTaskId;

    await page.evaluate(id => { window.newTaskIdForCheck = id; }, newTaskId);
    const edited = await page.evaluate(async () => {
        const saved = await window.handle.save();
        const commandStack = window.handle._bpmnjs.get('commandStack');
        const doc = new DOMParser().parseFromString(saved, 'application/xml');
        const ELSA = 'https://elsaworkflows.io/schemas/bpmn/v1';
        const bindingOf = id => {
            const element = [...doc.getElementsByTagNameNS('*', '*')].find(e => e.getAttribute('id') === id);
            const binding = element?.getElementsByTagNameNS(ELSA, 'activityBinding')[0];
            if (!binding) return null;
            return { activityType: binding.getAttribute('activityType'), inputs: [...binding.getElementsByTagNameNS(ELSA, 'input')].map(i => ({ name: i.getAttribute('name'), value: i.textContent })), siblings: [...binding.parentElement.children].map(c => c.tagName) };
        };
        const diff = window.spike.tools.canonicalDiff(window.originalXml, saved);
        return { saved, taskBound: bindingOf('Task_Bound'), newTask: bindingOf(window.newTaskIdForCheck), eTask: bindingOf('E_Task'), gateway: bindingOf('Gateway_Route'), taskDeep: bindingOf('Task_Deep'), diff, changeEvents: window.changes.length, undoDepth: commandStack._stack.length };
    }).catch(async error => ({ error: String(error) }));
    writeFileSync(join(out, 'roundtrip-edited-crafted.bpmn'), edited.saved ?? '');
    results.checks.editedRoundTrip = { panel, ...edited, saved: undefined, pageErrors: errors };
    await context.close();
}

// ---- check 6: watermark -------------------------------------------------------------------------------------------
{
    const context = await browser.newContext();
    const watermark = [];
    for (const viewport of [{ width: 1440, height: 900 }, { width: 1024, height: 768 }, { width: 800, height: 600 }]) {
        const { page } = await openHarness(context, viewport);
        await page.evaluate(async xml => {
            const handle = window.handle = window.spike.module.createModeler(document.getElementById('canvas'), { propertiesContainer: document.getElementById('properties') });
            await handle.load(xml);
            handle.applyOverlays({ Task_Bound: { completed: 3 }, End_Main: { completed: 1 }, Sub_Collapsed: { faulted: true } });
            handle.select('Task_Bound');
        }, crafted);
        await page.waitForTimeout(200);
        const probe = () => page.evaluate(() => {
            const link = document.querySelector('.bjs-powered-by');
            if (!link) return { present: false };
            const rect = link.getBoundingClientRect();
            const canvas = document.getElementById('canvas').getBoundingClientRect();
            const style = getComputedStyle(link);
            const points = [];
            for (const fx of [0.1, 0.5, 0.9]) for (const fy of [0.1, 0.5, 0.9]) {
                const x = rect.left + rect.width * fx, y = rect.top + rect.height * fy;
                const hit = document.elementFromPoint(x, y);
                points.push({ x: Math.round(x), y: Math.round(y), onWatermark: !!hit && (hit === link || link.contains(hit)), hit: hit ? `${hit.tagName}.${hit.className?.baseVal ?? hit.className}` : null });
            }
            return {
                present: true, href: link.getAttribute('href'), rect: { x: rect.x, y: rect.y, width: rect.width, height: rect.height },
                insideCanvas: rect.left >= canvas.left && rect.right <= canvas.right && rect.top >= canvas.top && rect.bottom <= canvas.bottom,
                insideViewport: rect.left >= 0 && rect.top >= 0 && rect.right <= innerWidth && rect.bottom <= innerHeight,
                opacity: style.opacity, visibility: style.visibility, display: style.display, zIndex: style.zIndex,
                allSamplePointsHitWatermark: points.every(p => p.onWatermark), points
            };
        });
        const atRest = await probe();
        await shot(page, `6-watermark-${viewport.width}x${viewport.height}.png`);
        // Drag the canvas so a shape with a Studio overlay badge sits under the watermark.
        await page.evaluate(() => {
            const bpmnjs = window.handle._bpmnjs;
            const canvas = bpmnjs.get('canvas');
            const link = document.querySelector('.bjs-powered-by').getBoundingClientRect();
            const gfx = bpmnjs.get('elementRegistry').getGraphics('Task_Bound').getBoundingClientRect();
            canvas.scroll({ dx: link.left + link.width / 2 - (gfx.right - 10), dy: link.top + link.height / 2 - (gfx.top - 10) });
        });
        await page.waitForTimeout(200);
        const withBadgeUnderneath = await probe();
        await shot(page, `6-watermark-over-badge-${viewport.width}x${viewport.height}.png`);
        watermark.push({ viewport, atRest, withBadgeUnderneath });
        await page.close();
    }
    // Dark theme: the logo colour is fixed by bpmn-js; measure its contrast with and without the opt-in plate.
    {
        const { page } = await openHarness(context, { width: 1440, height: 900 });
        await page.evaluate(async xml => {
            window.handle = window.spike.module.createModeler(document.getElementById('canvas'), { propertiesContainer: document.getElementById('properties') });
            await window.handle.load(xml);
        }, crafted);
        const contrast = () => page.evaluate(() => {
            const parse = c => c.match(/[\d.]+/g).map(Number);
            const lum = ([r, g, b]) => { const f = v => { v /= 255; return v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4; }; return 0.2126 * f(r) + 0.7152 * f(g) + 0.0722 * f(b); };
            const ratio = (a, b) => { const [x, y] = [lum(parse(a)), lum(parse(b))].sort((p, q) => q - p); return Math.round(((x + 0.05) / (y + 0.05)) * 100) / 100; };
            const link = document.querySelector('.bjs-powered-by');
            const logo = getComputedStyle(link).color;
            const plate = getComputedStyle(document.querySelector('.bjs-container'), '::after');
            const behind = plate.content !== 'none' && plate.content !== 'normal' ? plate.backgroundColor : getComputedStyle(document.getElementById('canvas')).backgroundColor;
            const rect = link.getBoundingClientRect();
            const hit = document.elementFromPoint(rect.left + rect.width / 2, rect.top + rect.height / 2);
            return { logoColour: logo, background: behind, contrastRatio: ratio(logo, behind), centreHitsWatermark: link.contains(hit) };
        });
        const light = await contrast();
        await page.click('#theme');
        const dark = await contrast();
        await page.evaluate(() => document.getElementById('canvas').classList.add('elsa-bpmn-watermark-plate'));
        const darkWithPlate = await contrast();
        await page.screenshot({ path: join(out, '6-watermark-dark-with-plate.png'), clip: { x: 900, y: 780, width: 180, height: 120 } });
        await page.evaluate(() => document.getElementById('canvas').classList.remove('elsa-bpmn-watermark-plate'));
        await page.screenshot({ path: join(out, '6-watermark-dark-no-plate.png'), clip: { x: 900, y: 780, width: 180, height: 120 } });
        results.checks.watermarkContrast = { light, dark, darkWithPlate };
        await page.close();
    }
    results.checks.watermark = watermark;
    await context.close();
}

await browser.close();
server.close();
writeFileSync(join(out, 'results.json'), JSON.stringify(results, null, 2));
console.log(`Wrote ${join(out, 'results.json')}`);

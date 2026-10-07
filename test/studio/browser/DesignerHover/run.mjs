import {chromium, expect as playwrightExpect} from '@playwright/test';
import {build} from 'esbuild';
import {createHash} from 'node:crypto';
import {readFile, rename, writeFile} from 'node:fs/promises';
import {createServer} from 'node:http';
import {createRequire} from 'node:module';
import {basename, dirname, join, relative, resolve} from 'node:path';
import {fileURLToPath} from 'node:url';
import {assertSourceHead, captureSourceBinding, verifySourceBinding} from './source-binding.mjs';

const fixtureDirectory = dirname(fileURLToPath(import.meta.url));
const expect = playwrightExpect.configure({timeout: 10000});
const root = resolve(fixtureDirectory, '../../../..');
const clientPath = 'src/studio/modules/Elsa.Studio.Workflows.Designer/ClientLib';
const helperPath = `${clientPath}/src/designer/api/edge-hover-tools.ts`;
const reviewedLockPath = 'scripts/integration-program/consolidated-build/studio-clientlib-lockfiles/designer.package-lock.json';
const installedLockPath = `${clientPath}/package-lock.json`;
const assertions = [
    'real-x6-rendered', 'repeated-edge-hover', 'edge-to-edge-transfer',
    'node-clears-hover', 'blank-clears-hover', 'outside-clears-hover',
    'remove-tool-entry', 'remove-tool-click', 'noninteractive-no-tools',
];
const receipt = {
    schema: 1,
    kind: 'designer-hover-source-browser',
    passed: false,
    failure_category: null,
    source: null,
    dependencies: null,
    browser: null,
    bundle_sha256: null,
    assertions: assertions.map(name => ({name, passed: false, reason_category: 'not_run', observation: null})),
    events: null,
    browser_errors: 0,
    blocked_requests: 0,
};
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const json = async path => JSON.parse(await readFile(path, 'utf8'));
let stage = 'arguments';
let browser;
let context;
let server;
let output;
let interrupted = false;

// The outer hosted runner also has a timeout. These handlers close native resources on cancellation.
const stop = () => {
    interrupted = true;
    void browser?.close().catch(() => {});
    server?.closeAllConnections();
    server?.close();
};
process.once('SIGTERM', stop);
process.once('SIGINT', stop);

try {
    if (process.argv.length !== 6 || process.argv[2] !== '--expected-head' || process.argv[4] !== '--output') {
        throw new Error('arguments');
    }
    const expectedHead = process.argv[3];
    output = resolve(process.argv[5]);
    stage = 'source_binding_initial';
    assertSourceHead(root, expectedHead);
    stage = 'dependency_binding';
    const reviewedLock = await readFile(join(root, reviewedLockPath));
    const installedLock = await readFile(join(root, installedLockPath));
    expect(hash(installedLock)).toBe(hash(reviewedLock));
    const x6Lock = JSON.parse(reviewedLock).packages['node_modules/@antv/x6'];
    const x6Directory = join(root, clientPath, 'node_modules/@antv/x6');
    const x6Package = await json(join(x6Directory, 'package.json'));
    expect(x6Package.version).toBe(x6Lock.version);
    expect(x6Lock.integrity).toMatch(/^sha512-[A-Za-z0-9+/]+=*$/);
    const fixtureRequire = createRequire(join(fixtureDirectory, 'package.json'));
    receipt.dependencies = {
        x6: {version: x6Package.version, integrity: x6Lock.integrity},
        playwright: (await json(fixtureRequire.resolve('@playwright/test/package.json'))).version,
        esbuild: (await json(fixtureRequire.resolve('esbuild/package.json'))).version,
    };
    expect(receipt.dependencies.playwright).toBe('1.61.1');
    expect(receipt.dependencies.esbuild).toBe('0.28.2');

    stage = 'bundle';
    const bundle = await build({
        absWorkingDir: root,
        entryPoints: [join(fixtureDirectory, 'fixture.ts')],
        outfile: 'fixture.js',
        bundle: true,
        write: false,
        metafile: true,
        platform: 'browser',
        format: 'iife',
        target: 'chrome120',
        logLevel: 'silent',
        // The fixture and production helper must share the mapped ClientLib's restored X6.
        alias: {'@antv/x6': x6Directory},
    });
    expect(Object.keys(bundle.metafile.inputs)).toContain(helperPath);
    const bundledPaths = Object.keys(bundle.metafile.inputs);
    expect(bundledPaths.some(path => path.startsWith(`${clientPath}/node_modules/@antv/x6/`))).toBe(true);
    expect(bundledPaths.filter(path => path.includes('/node_modules/@antv/x6/'))
        .every(path => path.startsWith(`${clientPath}/node_modules/@antv/x6/`))).toBe(true);
    const inputPaths = new Set([
        ...bundledPaths,
        reviewedLockPath,
        `${clientPath}/package.json`,
        installedLockPath,
        `${clientPath}/src/designer/api/create-graph.ts`,
        `${clientPath}/src/designer/api/__tests__/edge-hover-tools.test.ts`,
        `${clientPath}/vitest.config.ts`,
        '.github/workflows/studio-clientlib-build.yml',
        ...['run.mjs', 'fixture.ts', 'source-binding.mjs', 'source-binding.test.mjs', 'package.json', 'package-lock.json', 'README.md', '.gitignore'].map(name => relative(root, join(fixtureDirectory, name))),
    ]);
    receipt.source = await captureSourceBinding(root, [...inputPaths], expectedHead, [installedLockPath]);
    const files = new Map(bundle.outputFiles.map(file => [`/${basename(file.path)}`, file.contents]));
    expect(files.has('/fixture.js') && files.has('/fixture.css')).toBe(true);
    receipt.bundle_sha256 = hash(files.get('/fixture.js'));
    const html = Buffer.from('<!doctype html><html><head><meta charset="utf-8"><link rel="stylesheet" href="/fixture.css"><style>body{margin:0}#graph{position:absolute;left:40px;top:40px;width:800px;height:440px}</style></head><body><div id="graph"></div><script src="/fixture.js"></script></body></html>');
    files.set('/', html);
    server = createServer((request, response) => {
        const body = files.get(request.url);
        response.setHeader('Cache-Control', 'no-store');
        if (request.url === '/favicon.ico') {
            response.writeHead(204).end();
        } else if (body) {
            response.setHeader('Content-Type', request.url === '/' ? 'text/html' : request.url.endsWith('.css') ? 'text/css' : 'text/javascript');
            response.writeHead(200).end(body);
        } else {
            response.writeHead(404).end();
        }
    });
    await new Promise((resolveListen, reject) => {
        server.once('error', reject);
        server.listen(0, '127.0.0.1', resolveListen);
    });
    const origin = `http://127.0.0.1:${server.address().port}`;

    stage = 'browser_launch';
    browser = await chromium.launch({headless: true, timeout: 30000});
    receipt.browser = {engine: 'chromium', version: browser.version()};
    context = await browser.newContext({viewport: {width: 1000, height: 700}, serviceWorkers: 'block'});
    await context.route('**/*', route => {
        if (new URL(route.request().url()).origin === origin) {
            return route.continue();
        }
        receipt.blocked_requests += 1;
        return route.abort('blockedbyclient');
    });
    const page = await context.newPage();
    page.on('pageerror', () => { receipt.browser_errors += 1; });
    page.on('console', message => {
        if (message.type() === 'error') {
            receipt.browser_errors += 1;
        }
    });
    page.setDefaultTimeout(10000);
    stage = 'page_load';
    await page.goto(origin, {waitUntil: 'load'});
    await page.waitForFunction(() => Boolean(window.hoverFixture));
    const snapshot = () => page.evaluate(() => window.hoverFixture.snapshot());
    const eventCount = async name => (await snapshot()).events[name] ?? 0;
    let subcheck = 'action';
    const move = async (x, y) => {
        subcheck = 'pointer-move';
        const box = await page.locator('#graph').boundingBox();
        expect(box).not.toBeNull();
        await page.mouse.move(box.x + x, box.y + y, {steps: 8});
        // Flush rendering/event effects before checking absence; no timing sleeps.
        await page.evaluate(() => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done))));
    };
    const edgePoint = id => id === 'edge-a' ? [320, 120] : [320, 240];
    const tool = (id, name) => page.locator(`.x6-cell-tools[data-cell-id="${id}"] [data-tool-name="${name}"]`);
    const allButtons = page.locator('.x6-cell-tools [data-tool-name="button-remove"]');
    const checkEdge = async (id, buttons, vertices = 1) => {
        subcheck = 'rendered-edge-buttons';
        await expect(tool(id, 'button-remove')).toHaveCount(buttons);
        subcheck = 'rendered-edge-vertices';
        await expect(tool(id, 'vertices')).toHaveCount(vertices);
        subcheck = 'model-edge-tools';
        await expect.poll(async () => {
            const edge = (await snapshot()).edges.find(edge => edge.id === id);
            return {buttons: edge.buttons, vertices: edge.vertices};
        }).toEqual({buttons, vertices});
    };
    const hover = async id => {
        await move(...edgePoint(id));
        await checkEdge(id, 1);
        subcheck = 'single-remove-button';
        await expect(allButtons).toHaveCount(1);
    };
    const observation = async () => {
        const state = await snapshot();
        const [a, b] = state.edges;
        return {
            subcheck,
            pointer_inside: state.pointerInside,
            edge_a_exists: a.exists,
            edge_b_exists: b.exists,
            model_a_buttons: a.buttons,
            model_b_buttons: b.buttons,
            model_a_vertices: a.vertices,
            model_b_vertices: b.vertices,
            rendered_buttons: await allButtons.count(),
            rendered_a_buttons: await tool('edge-a', 'button-remove').count(),
            rendered_b_buttons: await tool('edge-b', 'button-remove').count(),
            rendered_a_vertices: await tool('edge-a', 'vertices').count(),
            rendered_b_vertices: await tool('edge-b', 'vertices').count(),
            native_container_enters: state.events['container:mouseenter'] ?? 0,
            native_container_leaves: state.events['container:mouseleave'] ?? 0,
            x6_a_enters: state.events['edge:mouseenter:edge-a'] ?? 0,
            x6_b_enters: state.events['edge:mouseenter:edge-b'] ?? 0,
            x6_a_leaves: state.events['edge:mouseleave:edge-a'] ?? 0,
            x6_b_leaves: state.events['edge:mouseleave:edge-b'] ?? 0,
            x6_graph_leaves: state.events['graph:mouseleave'] ?? 0,
            x6_node_enters: state.events['node:mouseenter'] ?? 0,
            x6_blank_overs: state.events['blank:mouseover'] ?? 0,
        };
    };
    const run = async (name, action) => {
        stage = name;
        subcheck = 'action';
        const result = receipt.assertions.find(result => result.name === name);
        result.reason_category = 'assertion_failed';
        try {
            await action();
            subcheck = 'browser-errors';
            expect(receipt.browser_errors).toBe(0);
            subcheck = 'external-requests';
            expect(receipt.blocked_requests).toBe(0);
            result.passed = true;
            result.reason_category = null;
            subcheck = 'completed';
        } finally {
            try {
                result.observation = await observation();
            } catch {
                result.passed = false;
                result.reason_category = 'observation_failed';
                throw new Error('observation_failed');
            }
        }
    };

    await run('real-x6-rendered', async () => {
        await expect(page.locator('#graph .x6-graph-svg')).toBeVisible();
        await expect(page.locator('#graph .x6-edge')).toHaveCount(2);
        expect((await snapshot()).edges.every(edge => edge.exists)).toBe(true);
    });
    await run('repeated-edge-hover', async () => {
        for (let repetition = 0; repetition < 5; repetition += 1) {
            await move(400, 390);
            await hover('edge-a');
        }
        expect(await eventCount('edge:mouseenter:edge-a')).toBeGreaterThanOrEqual(5);
    });
    await run('edge-to-edge-transfer', async () => {
        await hover('edge-a');
        await hover('edge-b');
        await checkEdge('edge-a', 0);
        expect(await eventCount('edge:mouseenter:edge-b')).toBeGreaterThan(0);
    });
    for (const [name, point, event] of [
        ['node-clears-hover', [740, 120], 'node:mouseenter'],
        ['blank-clears-hover', [400, 390], 'blank:mouseover'],
        ['outside-clears-hover', [850, 240], 'container:mouseleave'],
    ]) {
        await run(name, async () => {
            await hover('edge-b');
            const before = await eventCount(event);
            await move(...point);
            subcheck = 'all-remove-buttons-cleared';
            await expect(allButtons).toHaveCount(0);
            await checkEdge('edge-b', 0);
            subcheck = name === 'outside-clears-hover' ? 'native-container-leave' : 'native-event-count';
            expect(await eventCount(event)).toBeGreaterThan(before);
            if (name === 'outside-clears-hover') {
                subcheck = 'pointer-outside-container';
                expect((await snapshot()).pointerInside).toBe(false);
            }
        });
    }
    await run('remove-tool-entry', async () => {
        await hover('edge-a');
        const button = tool('edge-a', 'button-remove').locator('circle');
        await expect(button).toBeVisible();
        const box = await button.boundingBox();
        expect(box).not.toBeNull();
        // Follow the edge to its remove control so entering the actual tool is exercised.
        await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2, {steps: 16});
        await checkEdge('edge-a', 1);
        expect(await page.evaluate(({x, y}) => document.elementFromPoint(x, y)?.closest('[data-tool-name]')?.getAttribute('data-tool-name'),
            {x: box.x + box.width / 2, y: box.y + box.height / 2})).toBe('button-remove');
    });
    await run('remove-tool-click', async () => {
        // Click at the pointer already on the real control; do not call model removal or synthetic events.
        await page.mouse.down();
        await page.mouse.up();
        await expect.poll(async () => (await snapshot()).edges.find(edge => edge.id === 'edge-a').exists).toBe(false);
        await expect(page.locator('.x6-edge[data-cell-id="edge-a"]')).toHaveCount(0);
        await expect(allButtons).toHaveCount(0);
        expect((await snapshot()).edges.find(edge => edge.id === 'edge-b').exists).toBe(true);
    });
    const interactiveEvents = (await snapshot()).events;
    await run('noninteractive-no-tools', async () => {
        // A fresh graph prevents residual vertices from the interactive case from masking the boundary.
        await move(850, 480);
        await page.evaluate(() => window.hoverFixture.reset(false));
        for (let repetition = 0; repetition < 3; repetition += 1) {
            await move(400, 390);
            await move(...edgePoint('edge-a'));
            await checkEdge('edge-a', 0, 0);
            await move(...edgePoint('edge-b'));
            await checkEdge('edge-b', 0, 0);
            await expect(page.locator('.x6-cell-tool')).toHaveCount(0);
        }
        const state = await snapshot();
        expect(state.edges.every(edge => edge.exists)).toBe(true);
        expect(await eventCount('edge:mouseenter:edge-a')).toBeGreaterThanOrEqual(3);
        expect(await eventCount('edge:mouseenter:edge-b')).toBeGreaterThanOrEqual(3);
        const leavesBefore = await eventCount('container:mouseleave');
        await move(850, 240);
        subcheck = 'native-listener-cleanup';
        // The previous graph's native listeners must have been aborted during reset.
        expect(await eventCount('container:mouseleave')).toBe(leavesBefore + 1);
        expect((await snapshot()).pointerInside).toBe(false);
    });
    receipt.events = {interactive: interactiveEvents, noninteractive: (await snapshot()).events};
    expect(interrupted).toBe(false);
    receipt.passed = receipt.assertions.every(result => result.passed);
} catch {
    receipt.failure_category = interrupted ? 'interrupted' : stage;
} finally {
    // Teardown failures are acceptance failures too, without publishing native exception text.
    for (const resource of [context, browser]) {
        try {
            await resource?.close();
        } catch {
            receipt.passed = false;
            receipt.failure_category = 'cleanup';
        }
    }
    try {
        if (server?.listening) {
            server.closeAllConnections();
            await new Promise((resolveClose, reject) => server.close(error => error ? reject(error) : resolveClose()));
        }
    } catch {
        receipt.passed = false;
        receipt.failure_category = 'cleanup';
    }
    if (interrupted) {
        receipt.passed = false;
        receipt.failure_category = 'interrupted';
    }
    if (receipt.passed) {
        try {
            await verifySourceBinding(root, receipt.source, [installedLockPath]);
        } catch {
            receipt.passed = false;
            receipt.failure_category = 'source_binding_final';
        }
    }
    process.exitCode = receipt.passed ? 0 : 1;
    if (output) {
        try {
            const temporary = `${output}.${process.pid}.tmp`;
            await writeFile(temporary, `${JSON.stringify(receipt, null, 2)}\n`, {flag: 'wx', mode: 0o600});
            await rename(temporary, output);
        } catch {
            process.exitCode = 1;
        }
    }
}

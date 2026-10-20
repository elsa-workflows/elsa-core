// SPIKE (#8901): bundles the module, the harness tooling and the size-comparison variants, then reports sizes.
import { build } from 'esbuild';
import { cpSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { gzipSync, brotliCompressSync } from 'node:zlib';

const here = new URL('.', import.meta.url).pathname;
mkdirSync(`${here}dist/size`, { recursive: true });

const common = { bundle: true, minify: true, format: 'esm', target: 'es2020', loader: { '.json': 'json' }, legalComments: 'none', logLevel: 'warning' };

// The module a host would ship.
await build({ ...common, entryPoints: [`${here}src/elsa-bpmn-modeler.js`], outfile: `${here}dist/elsa-bpmn-modeler.js`, sourcemap: true });
await build({ ...common, entryPoints: [`${here}src/elsa-bpmn-modeler.css`], outfile: `${here}dist/elsa-bpmn-modeler.css`, loader: { '.woff': 'file', '.woff2': 'file', '.ttf': 'file', '.eot': 'file', '.svg': 'file' }, assetNames: 'fonts/[name]' });

// Harness-only tooling (semantic XML diff, auto layout experiment). Not part of the module.
await build({ ...common, minify: false, entryPoints: [`${here}harness/harness-tools.js`], outfile: `${here}dist/harness-tools.js` });

// Size variants, for the findings table only.
const variants = {
    'viewer': "export { default } from 'bpmn-js/lib/NavigatedViewer';",
    'modeler': "export { default } from 'bpmn-js/lib/Modeler';",
    'modeler-and-panel': "export { default } from 'bpmn-js/lib/Modeler'; export { BpmnPropertiesPanelModule, BpmnPropertiesProviderModule } from 'bpmn-js-properties-panel';",
    'auto-layout': "export { layoutProcess } from 'bpmn-auto-layout';"
};
for (const [name, contents] of Object.entries(variants)) {
    await build({ ...common, stdin: { contents, resolveDir: here, loader: 'js' }, outfile: `${here}dist/size/${name}.js` });
}

// The Blazor WebAssembly harness serves the module from its own wwwroot.
cpSync(`${here}dist/elsa-bpmn-modeler.js`, `${here}blazor-harness/wwwroot/bpmn/elsa-bpmn-modeler.js`);
cpSync(`${here}dist/elsa-bpmn-modeler.css`, `${here}blazor-harness/wwwroot/bpmn/elsa-bpmn-modeler.css`);
cpSync(`${here}dist/fonts`, `${here}blazor-harness/wwwroot/bpmn/fonts`, { recursive: true });
cpSync(`${here}fixtures/foreign-unknown-deep.bpmn`, `${here}blazor-harness/wwwroot/fixtures/foreign-unknown-deep.bpmn`);

const files = {
    'module JS (bpmn-js Modeler + properties panel + elsa provider + moddle)': 'dist/elsa-bpmn-modeler.js',
    'module CSS (diagram-js, bpmn-js, bpmn-font, properties panel, theme)': 'dist/elsa-bpmn-modeler.css',
    'bpmn-font woff2 (the only font file a current browser fetches)': 'dist/fonts/bpmn.woff2',
    'variant: bpmn-js NavigatedViewer only': 'dist/size/viewer.js',
    'variant: bpmn-js Modeler only': 'dist/size/modeler.js',
    'variant: Modeler + BPMN properties panel (no elsa provider)': 'dist/size/modeler-and-panel.js',
    'variant: bpmn-auto-layout (for DI-less documents)': 'dist/size/auto-layout.js'
};
const sizes = {};
for (const [label, file] of Object.entries(files)) {
    const bytes = readFileSync(`${here}${file}`);
    sizes[label] = { file, minified: bytes.length, gzip: gzipSync(bytes, { level: 9 }).length, brotli: brotliCompressSync(bytes).length };
}
writeFileSync(`${here}dist/sizes.json`, JSON.stringify(sizes, null, 2));
const kb = n => `${(n / 1024).toFixed(1)} KiB`;
for (const [label, s] of Object.entries(sizes)) console.log(`${label}: min ${kb(s.minified)}, gzip ${kb(s.gzip)}, brotli ${kb(s.brotli)}`);

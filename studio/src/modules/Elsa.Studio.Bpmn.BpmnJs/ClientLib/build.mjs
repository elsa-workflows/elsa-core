// Bundles the bpmn-js designer into the module's wwwroot, and fails when the JavaScript outgrows its size budget.
import { build } from 'esbuild';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { brotliCompressSync } from 'node:zlib';

const here = fileURLToPath(new URL('.', import.meta.url));
const output = `${here}../wwwroot`;

// The module is loaded on demand when a BPMN canvas opens, never at Studio start, but it still has to stay small. The
// bpmn-js spike measured 366 KiB brotli with the properties panel (#8901).
const BUDGET_BROTLI_KIB = 400;

const common = { bundle: true, minify: true, format: 'esm', target: 'es2020', legalComments: 'linked', logLevel: 'warning' };

await build({ ...common, entryPoints: [`${here}src/index.ts`], outfile: `${output}/bpmn-js-designer.js` });
await build({
    ...common,
    entryPoints: [`${here}src/bpmn-js-designer.css`],
    outfile: `${output}/bpmn-js-designer.css`,
    loader: { '.woff': 'file', '.woff2': 'file', '.ttf': 'file', '.eot': 'file', '.svg': 'file' },
    assetNames: 'fonts/[name]'
});

const brotliKib = brotliCompressSync(readFileSync(`${output}/bpmn-js-designer.js`)).length / 1024;
console.log(`bpmn-js-designer.js: ${brotliKib.toFixed(1)} KiB brotli (budget ${BUDGET_BROTLI_KIB} KiB).`);
if (brotliKib > BUDGET_BROTLI_KIB) {
    console.error(`The bpmn-js designer is ${brotliKib.toFixed(1)} KiB brotli, over its ${BUDGET_BROTLI_KIB} KiB budget.`);
    process.exit(1);
}

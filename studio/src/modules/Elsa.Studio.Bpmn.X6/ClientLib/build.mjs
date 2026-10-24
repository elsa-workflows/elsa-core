// Bundles the X6 designer into the module's wwwroot, and fails when the JavaScript outgrows its size budget.
import { build } from 'esbuild';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { brotliCompressSync } from 'node:zlib';

const here = fileURLToPath(new URL('.', import.meta.url));
const output = `${here}../wwwroot`;

// The module is loaded on demand when a BPMN canvas opens, never at Studio start. It carries X6, bpmn-moddle and the
// layout generator; the editing steps (#8928) add to it, so the budget leaves room for them.
const BUDGET_BROTLI_KIB = 250;

const common = { bundle: true, minify: true, format: 'esm', target: 'es2020', legalComments: 'linked', logLevel: 'warning' };

await build({ ...common, entryPoints: [`${here}src/index.ts`], outfile: `${output}/x6-designer.js` });
await build({ ...common, entryPoints: [`${here}src/x6-designer.css`], outfile: `${output}/x6-designer.css` });

const brotliKib = brotliCompressSync(readFileSync(`${output}/x6-designer.js`)).length / 1024;
console.log(`x6-designer.js: ${brotliKib.toFixed(1)} KiB brotli (budget ${BUDGET_BROTLI_KIB} KiB).`);
if (brotliKib > BUDGET_BROTLI_KIB) {
    console.error(`The X6 designer is ${brotliKib.toFixed(1)} KiB brotli, over its ${BUDGET_BROTLI_KIB} KiB budget.`);
    process.exit(1);
}

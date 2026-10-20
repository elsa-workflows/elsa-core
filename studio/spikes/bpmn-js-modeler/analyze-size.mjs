// SPIKE (#8901): where the module's bytes come from, grouped by npm package (minified output bytes).
import { build } from 'esbuild';

const here = new URL('.', import.meta.url).pathname;
const entries = {
    'full module': `${here}src/elsa-bpmn-modeler.js`
};

async function analyze(label, options) {
    const result = await build({ bundle: true, minify: true, format: 'esm', target: 'es2020', loader: { '.json': 'json' }, write: false, metafile: true, outdir: `${here}dist/analyze`, legalComments: 'none', ...options });
    const output = Object.values(result.metafile.outputs).find(o => o.entryPoint || o.inputs);
    const byPackage = {};
    for (const [input, info] of Object.entries(output.inputs)) {
        const match = /node_modules\/((?:@[^/]+\/)?[^/]+)/.exec(input);
        const name = match ? match[1] : 'spike source';
        byPackage[name] = (byPackage[name] ?? 0) + info.bytesInOutput;
    }
    const total = Object.values(byPackage).reduce((a, b) => a + b, 0);
    console.log(`\n${label}: ${(total / 1024).toFixed(1)} KiB minified`);
    for (const [name, bytes] of Object.entries(byPackage).sort((a, b) => b[1] - a[1]).slice(0, 18)) {
        console.log(`  ${name.padEnd(40)} ${(bytes / 1024).toFixed(1).padStart(8)} KiB`);
    }
}

await analyze('full module', { entryPoints: [entries['full module']] });
await analyze('@bpmn-io/properties-panel alone (PropertiesPanel + entries a custom-only provider would use)', {
    stdin: { contents: "export { PropertiesPanel, TextFieldEntry, TextAreaEntry, ListGroup } from '@bpmn-io/properties-panel';", resolveDir: here, loader: 'js' }
});

// What the module would weigh if the FEEL / CodeMirror editors (unused by Elsa) were stubbed out: measured by marking
// them external, so this is the size of everything else, not a working build.
const unused = ['@codemirror/*', '@lezer/*', '@bpmn-io/feel-editor', '@bpmn-io/feelers-editor', '@bpmn-io/feel-lint', '@bpmn-io/feelers-lint', '@bpmn-io/lang-feel', '@bpmn-io/lang-feelers', '@bpmn-io/lezer-feel', '@bpmn-io/lezer-feelers', '@bpmn-io/cm-theme', '@camunda/feel-builtins', '@bpmn-io/feel-analyzer', '@bpmn-io/extract-process-variables', '@bpmn-io/extract-process-variables/*'];
{
    const { gzipSync, brotliCompressSync } = await import('node:zlib');
    const result = await build({ bundle: true, minify: true, format: 'esm', target: 'es2020', loader: { '.json': 'json' }, write: false, legalComments: 'none', external: unused, entryPoints: [entries['full module']], outdir: `${here}dist/analyze` });
    const bytes = result.outputFiles[0].contents;
    const kib = n => `${(n / 1024).toFixed(1)} KiB`;
    console.log(`\nfull module without FEEL/CodeMirror (external, not a working build): min ${kib(bytes.length)}, gzip ${kib(gzipSync(bytes).length)}, brotli ${kib(brotliCompressSync(bytes).length)}`);
}

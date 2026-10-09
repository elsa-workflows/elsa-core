// Resolve from this installed package, including npm's normal hoisted layout.
import {cpSync, mkdirSync, rmSync} from 'node:fs';
import {createRequire} from 'node:module';
import {dirname, join, sep} from 'node:path';
import {fileURLToPath} from 'node:url';

const require = createRequire(import.meta.url);
const source = dirname(require.resolve('@elsa-workflows/elsa-studio-wasm/package.json'));
const packageRoot = fileURLToPath(new URL('../', import.meta.url));
// npm runs an installed package's lifecycle in node_modules, while Vite serves
// the invoking application's public/. Keep local wrapper development unchanged.
const installed = packageRoot.split(sep).includes('node_modules');
const destination = join(installed ? (process.env.INIT_CWD || process.cwd()) : packageRoot, 'public');
mkdirSync(destination, {recursive: true});

for (const asset of ['_content', '_framework', 'appsettings.json', 'Elsa.Studio.Host.CustomElements.styles.css']) {
    // Replace rather than merge to remove assets no longer shipped by WASM.
    const target = join(destination, asset);
    rmSync(target, {recursive: true, force: true});
    cpSync(join(source, asset), target, {recursive: true});
}

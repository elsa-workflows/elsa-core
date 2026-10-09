// Resolve from this installed package, including npm's normal hoisted layout.
import {lstatSync, mkdirSync, readFileSync, readdirSync, renameSync, unlinkSync, writeFileSync} from 'node:fs';
import {createHash, randomUUID} from 'node:crypto';
import {createRequire} from 'node:module';
import {dirname, join, parse, resolve, sep} from 'node:path';
import {fileURLToPath} from 'node:url';

const require = createRequire(import.meta.url);
const source = dirname(require.resolve('@elsa-workflows/elsa-studio-wasm/package.json'));
const packageRoot = fileURLToPath(new URL('../', import.meta.url));
// npm runs an installed package's lifecycle in node_modules, while Vite serves
// the invoking application's public/. Keep local wrapper development unchanged.
const installed = packageRoot.split(sep).includes('node_modules');
const destination = join(installed ? (process.env.INIT_CWD || process.cwd()) : packageRoot, 'public');
const ownershipPath = join(destination, '.elsa-studio-wasm-assets.json');
const assets = ['_content', '_framework', 'appsettings.json', 'Elsa.Studio.Host.CustomElements.styles.css'];
const hash = bytes => createHash('sha256').update(bytes).digest('hex');

function ensure(condition, code) {
    if (!condition) throw new Error(`Elsa Studio asset copy: ${code}`);
}

function stat(path) {
    try { return lstatSync(path); }
    catch (error) {
        if (error.code === 'ENOENT') return undefined;
        throw error;
    }
}

function safePath(path) {
    return typeof path === 'string' && !/[\\:\0]/.test(path) &&
        path.split('/').every(part => part !== '' && part !== '.' && part !== '..') &&
        (assets.slice(2).includes(path) || assets.slice(0, 2).some(asset => path.startsWith(`${asset}/`)));
}

function checkDirectories(path) {
    const absolute = resolve(path);
    let current = parse(absolute).root;
    for (const part of absolute.slice(current.length).split(sep).filter(Boolean)) {
        current = join(current, part);
        const info = stat(current);
        ensure(!info || (info.isDirectory() && !info.isSymbolicLink()), 'unsafe-directory');
    }
}

// Read and hash every incoming file before touching the application. Links and
// special files are not an asset-copy contract, either as inputs or targets.
const incoming = new Map();
function collect(relative) {
    const path = join(source, relative);
    const info = stat(path);
    ensure(info && !info.isSymbolicLink(), 'unsafe-source');
    if (info.isDirectory()) {
        for (const name of readdirSync(path).sort()) collect(`${relative}/${name}`);
    } else {
        ensure(info.isFile() && safePath(relative), 'unsafe-source');
        const bytes = readFileSync(path);
        incoming.set(relative, {bytes, sha256: hash(bytes)});
    }
}
for (const asset of assets) {
    const info = stat(join(source, asset));
    ensure(info && (assets.slice(0, 2).includes(asset) ? info.isDirectory() : info.isFile()), 'missing-source');
    collect(asset);
}

checkDirectories(destination);
const previous = new Map();
const ownershipInfo = stat(ownershipPath);
if (ownershipInfo) {
    ensure(ownershipInfo.isFile() && !ownershipInfo.isSymbolicLink() && ownershipInfo.size <= 2 * 1024 * 1024,
        'invalid-ownership');
    let ledger;
    try { ledger = JSON.parse(readFileSync(ownershipPath, 'utf8')); }
    catch { throw new Error('Elsa Studio asset copy: invalid-ownership'); }
    ensure(ledger && Object.keys(ledger).sort().join(',') === 'files,package,schema' && ledger.schema === 1 &&
        ledger.package === '@elsa-workflows/elsa-studio-wasm' && Array.isArray(ledger.files) && ledger.files.length <= 10000,
        'invalid-ownership');
    for (const record of ledger.files) {
        ensure(record && Object.keys(record).sort().join(',') === 'path,sha256' && safePath(record.path) &&
            typeof record.sha256 === 'string' && /^[a-f0-9]{64}$/.test(record.sha256) && !previous.has(record.path),
            'invalid-ownership');
        previous.set(record.path, record.sha256);
    }
}

const aliases = new Map();
for (const relative of new Set([...previous.keys(), ...incoming.keys()])) {
    const alias = relative.normalize('NFC').toLowerCase();
    ensure(!aliases.has(alias) || aliases.get(alias) === relative, 'ambiguous-asset-path');
    aliases.set(alias, relative);
}
const ledger = {schema: 1, package: '@elsa-workflows/elsa-studio-wasm',
    files: [...incoming.keys()].sort().map(path => ({path, sha256: incoming.get(path).sha256}))};
const serialized = `${JSON.stringify(ledger, null, 2)}\n`;
ensure(incoming.size <= 10000 && Buffer.byteLength(serialized) <= 2 * 1024 * 1024, 'invalid-ownership');

// Complete preflight first. An unowned collision is rejected even when its
// bytes happen to match: silently adopting it could delete it on a later update.
for (const relative of new Set([...previous.keys(), ...incoming.keys()])) {
    const target = join(destination, relative);
    checkDirectories(dirname(target));
    const info = stat(target);
    if (!info) continue;
    ensure(info.isFile() && !info.isSymbolicLink() && info.nlink === 1, 'unsafe-target');
    ensure(previous.has(relative), 'unmanaged-conflict');
    ensure(hash(readFileSync(target)) === previous.get(relative), 'modified-owned-file');
}

// Only files recorded as ours are replaced/deleted. Shared directories and all
// unmanaged siblings are preserved. This is a serial refresh, not a filesystem
// transaction; I/O failure can require repair of owned files before retrying.
for (const [relative, file] of incoming) {
    const target = join(destination, relative);
    mkdirSync(dirname(target), {recursive: true});
    writeFileSync(target, file.bytes);
}
for (const relative of previous.keys()) {
    if (!incoming.has(relative) && stat(join(destination, relative))) unlinkSync(join(destination, relative));
}
const temporary = `${ownershipPath}.${randomUUID()}.tmp`;
let staged = false;
try {
    writeFileSync(temporary, serialized, {flag: 'wx'});
    staged = true;
    renameSync(temporary, ownershipPath);
} finally {
    if (staged && stat(temporary)) unlinkSync(temporary);
}

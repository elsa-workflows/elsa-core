// Observe Vite's actual release build without changing the selected product or its bytes.
import { appendFileSync, readFileSync, existsSync, realpathSync } from 'node:fs';
import { dirname, join, relative, resolve } from 'node:path';
import { createHash } from 'node:crypto';
import { createRequire, registerHooks } from 'node:module';
import { pathToFileURL } from 'node:url';

const root = process.env.ELSA_VITE_ROOT && realpathSync(process.env.ELSA_VITE_ROOT);
const ledger = process.env.ELSA_VITE_LEDGER;
if (root && ledger) {
  const builder = pathToFileURL(join(root, 'scripts/build-assets.mjs')).href;
  registerHooks({ resolve(specifier, context, nextResolve) {
    if (specifier !== 'vite' || context.parentURL !== builder) return nextResolve(specifier, context);
    const vite = nextResolve(specifier, context).url;
    const facade = `import { build as viteBuild } from ${JSON.stringify(vite)};
      import { capture } from ${JSON.stringify(import.meta.url)};
      export const build = (config) => viteBuild({...config, plugins: [...(config.plugins || []), capture()]});`;
    return { url: 'data:text/javascript,' + encodeURIComponent(facade), shortCircuit: true };
  } });
}

const packageCache = new Map();
function packageIdentity(moduleId) {
  const id = moduleId.replace(/^\0/, '').split('?')[0];
  if (!id.startsWith('/') || id.includes('/node_modules/.cache/')) return undefined;
  let directory = dirname(id);
  const visited = [];
  while (directory !== root && directory !== dirname(directory)) {
    if (packageCache.has(directory)) break;
    const manifest = join(directory, 'package.json');
    if (existsSync(manifest)) {
      const { name, version } = JSON.parse(readFileSync(manifest));
      if (name && version) {
        packageCache.set(directory, [name, version]);
        break;
      }
    }
    visited.push(directory);
    directory = dirname(directory);
  }
  const identity = packageCache.get(directory);
  for (const path of visited) packageCache.set(path, identity);
  return identity;
}

export async function cssComponents(entries, resolveStyle, parse, identity) {
  const visited = new Set();
  const components = new Map();
  const add = value => { if (value) components.set(value.join('\0'), value); };
  async function visit(source) {
    if (visited.has(source)) return;
    visited.add(source);
    add(identity(source));
    const css = readFileSync(source, 'utf8');
    const tree = parse(css, { from: source });
    const imports = [];
    tree.walkAtRules('import', rule => {
      const match = /^(?:url\(\s*)?["']([^"']+)["']/.exec(rule.params.trim());
      if (!match) throw new Error('Unknown CSS import in ' + source + ': ' + rule.params);
      imports.push(match[1]);
    });
    for (const specifier of imports) {
      const path = await resolveStyle(specifier, source);
      if (!path) throw new Error('Unresolved CSS import ' + specifier + ' in ' + source);
      await visit(realpathSync(path));
    }
    // The UI's vendored stylesheet carries its upstream name, version and notice.
    tree.walkComments(comment => {
      const notice = /Source: ([\w@./-]+) (\d[\w.+-]*), [^\n]+\. Vendored/.exec(comment.text);
      if (notice) add([notice[1], notice[2]]);
    });
  }
  for (const source of entries) await visit(source);
  return [...components.values()];
}

export function capture() {
  const workspace = createRequire(join(root, 'package.json'));
  const vitePath = workspace.resolve('vite');
  const vite = createRequire(vitePath);
  const virtual = { vite: vitePath, rolldown: vite.resolve('rolldown') };
  const postcss = vite('postcss');
  let resolveStyle;
  return {
    name: 'elsa-release-sbom',
    configResolved(config) {
      resolveStyle = config.createResolver({ extensions: ['.css'], mainFields: ['style'], conditions: ['style'], preferRelative: true });
    },
    async writeBundle(options, bundle) {
      for (const item of Object.values(bundle)) {
        if (!/\.(?:js|css)$/.test(item.fileName)) continue;
        const file = resolve(options.dir, item.fileName);
        const ids = item.type === 'chunk'
          ? Object.entries(item.modules).filter(([, info]) => info.renderedLength !== 0).map(([id]) => id)
          : item.originalFileNames ?? [];
        const components = new Map();
        for (const id of ids) {
          const generated = /^\0(vite|rolldown)\//.exec(id);
          const identity = packageIdentity(generated ? virtual[generated[1]] : id);
          if (identity) components.set(identity.join('\0'), identity);
        }
        if (item.fileName.endsWith('.css')) {
          const outputs = Object.values(bundle).filter(asset => asset.fileName.endsWith('.css'));
          if (outputs.length !== 1) throw new Error('Ambiguous CSS asset attribution in ' + options.dir);
          const sources = [...this.getModuleIds()].filter(id => id.endsWith('.css') && existsSync(id));
          for (const identity of await cssComponents(sources, resolveStyle, postcss.parse, packageIdentity)) {
            components.set(identity.join('\0'), identity);
          }
        }
        const record = { file: relative(root, file), sha256: createHash('sha256').update(readFileSync(file)).digest('hex'),
          components: [...components.values()].sort(), imports: item.type === 'chunk' ? item.imports : [] };
        appendFileSync(ledger, JSON.stringify(record) + '\n');
      }
    },
  };
}

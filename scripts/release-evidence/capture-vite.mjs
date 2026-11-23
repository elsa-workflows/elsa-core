// Observe Vite's actual release build without changing the selected product or its bytes.
import { appendFileSync, readFileSync, existsSync, realpathSync } from 'node:fs';
import { dirname, join, relative, resolve } from 'node:path';
import { createHash } from 'node:crypto';
import { registerHooks } from 'node:module';
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

export function capture() {
  return {
    name: 'elsa-release-sbom',
    writeBundle(options, bundle) {
      for (const item of Object.values(bundle)) {
        if (!/\.(?:js|css)$/.test(item.fileName)) continue;
        const file = resolve(options.dir, item.fileName);
        const ids = item.type === 'chunk'
          ? Object.entries(item.modules).filter(([, info]) => info.renderedLength !== 0).map(([id]) => id)
          : item.originalFileNames ?? [];
        const components = new Map();
        for (const id of ids) {
          const identity = packageIdentity(id);
          if (identity) components.set(identity.join('\0'), identity);
        }
        const record = { file: relative(root, file), sha256: createHash('sha256').update(readFileSync(file)).digest('hex'),
          components: [...components.values()].sort(), imports: item.type === 'chunk' ? item.imports : [] };
        appendFileSync(ledger, JSON.stringify(record) + '\n');
      }
    },
  };
}

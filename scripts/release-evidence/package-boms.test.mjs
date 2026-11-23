import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { mkdirSync, mkdtempSync, readFileSync, realpathSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { spawnSync } from 'node:child_process';
import { test } from 'node:test';

test('different target frameworks keep their own resolved version and packed range', () => {
  const dir = mkdtempSync(join(tmpdir(), 'elsa-bom-'));
  try {
    mkdirSync(join(dir, 'release-evidence'));
    const resolved = { 'net8.0': '8.1.1', 'net10.0': '10.1.1' };
    const assets = { project: { restore: { projectName: 'Elsa.Example' }, frameworks: {} }, targets: {} };
    const row = { id: 'Elsa.Example', nupkg_sha256: '0'.repeat(64), frameworks: Object.keys(resolved),
      shared_frameworks: [], dependency_groups: [], restore_assets: [], payload: { 'example.dll': '1'.repeat(64) } };
    for (const [framework, version] of Object.entries(resolved)) {
      assets.project.frameworks[framework] = { downloadDependencies: [{ name: 'Schema.Input', version: '[3.2.1, 3.2.1]' }] };
      assets.targets[framework] = { ['Lib/' + version]: { type: 'package' }, 'Build.Input/1': { type: 'package' } };
      row.dependency_groups.push({ framework, dependencies: [{ id: 'Lib', version: '[' + version + ', )' }] });
    }
    row.shipped = { 'example.dll': { method: 'CLR resource', resources: [
      { name: 'vendor.js', sha256: '2'.repeat(64), components: [['lodash', '4.18.1']] }] } };
    const bytes = JSON.stringify(assets);
    const sha256 = createHash('sha256').update(bytes).digest('hex');
    writeFileSync(join(dir, 'restore.json'), bytes);
    row.restore_assets = row.frameworks.map(framework => ({ framework, retained_path: 'restore.json', sha256 }));
    const input = join(dir, 'input.json');
    writeFileSync(input, JSON.stringify({ version: '3.10.3', packages: [row], browser: [] }));
    const run = spawnSync(process.execPath, [join(import.meta.dirname, 'package-boms.mjs'), dir, input], { encoding: 'utf8' });
    assert.equal(run.status, 0, run.stderr);
    for (const [framework, version] of Object.entries(resolved)) {
      const bom = JSON.parse(readFileSync(join(dir, 'release-evidence/boms/Elsa.Example', framework + '.cdx.json')));
      const component = bom.components.find(item => item.name === 'Lib');
      assert.equal(component.version, version);
      assert.ok(component.properties.some(p => p.name === 'elsa:nuspec-version' && p.value === '[' + version + ', )'));
      assert.equal(bom.components.find(item => item.name === 'Build.Input').scope, 'excluded');
      const download = bom.components.find(item => item.name === 'Schema.Input');
      assert.equal(download.version, '3.2.1');
      assert.equal(download.scope, 'excluded');
      const resource = bom.metadata.component.components.find(file => file.name === 'example.dll').components[0];
      assert.equal(resource.hashes[0].content, '2'.repeat(64));
      assert.equal(resource.components[0].purl, 'pkg:npm/lodash@4.18.1');
      assert.ok(bom.metadata.component.components.some(file => file.name === 'example.dll' && file.hashes[0].content === '1'.repeat(64)));
    }
    row.restore_assets.pop();
    rmSync(join(dir, 'release-evidence/boms'), { recursive: true });
    writeFileSync(input, JSON.stringify({ version: '3.10.3', packages: [row], browser: [] }));
    const incomplete = spawnSync(process.execPath, [join(import.meta.dirname, 'package-boms.mjs'), dir, input], { encoding: 'utf8' });
    assert.notEqual(incomplete.status, 0);
    assert.match(incomplete.stderr, /Missing target framework/);
  } finally {
    rmSync(dir, { recursive: true, force: true });
  }
});

test('CSS evidence follows resolved imports, peer styles and the vendored source notice', async () => {
  const { cssComponents } = await import('./capture-vite.mjs');
  const { parse } = await import('postcss');
  const dir = realpathSync(mkdtempSync(join(tmpdir(), 'elsa-css-')));
  try {
    const entry = join(dir, 'entry.css');
    const theme = join(dir, 'theme.css');
    const animation = join(dir, 'animation.css');
    const vendor = join(dir, 'vendor.css');
    writeFileSync(entry, '@import "theme" source(none); @import "animation";');
    writeFileSync(theme, '@import "./vendor.css"; .theme { display: block }');
    writeFileSync(animation, '.animate { display: block }');
    writeFileSync(vendor, '/* Source: shadcn 4.21.4, dist/tailwind.css. Vendored with original notice. */');
    const paths = { theme, animation, './vendor.css': vendor };
    const versions = new Map([[theme, ['tailwindcss', '4.3.3']], [animation, ['tw-animate-css', '1.4.0']]]);
    const result = await cssComponents([entry], name => paths[name], parse, path => versions.get(path));
    assert.deepEqual(result.sort(), [['shadcn', '4.21.4'], ['tailwindcss', '4.3.3'], ['tw-animate-css', '1.4.0']]);
  } finally {
    rmSync(dir, { recursive: true, force: true });
  }
});

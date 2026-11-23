// Read only the build's retained restore snapshots and the pack verifier's metadata.
// No second MSBuild evaluation, restore, or package-feed access during SBOM generation.
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { join } from 'node:path';
import { Models, Serialize, Spec, Validation } from '@cyclonedx/cyclonedx-library';
import { PackageURL } from 'packageurl-js';

const serializer = new Serialize.JsonSerializer(new Serialize.JSON.Normalize.Factory(Spec.Spec1dot6));
const validator = new Validation.JsonStrictValidator(Spec.Version.v1dot6);
const digest = data => createHash('sha256').update(data).digest('hex');

function library(id, version) {
  const purl = new PackageURL('nuget', undefined, id.toLowerCase(), version, undefined, undefined).toString();
  return new Models.Component('library', id, { version, purl, bomRef: purl });
}

function frameworkBom(row, version, framework, assets, payload) {
  assert.equal(assets.project.restore.projectName, row.id);
  const target = assets.targets[framework];
  const declared = row.dependency_groups.find(group => group.framework === framework);
  assert.ok(target && declared, `Missing resolved or declared target: ${row.id}/${framework}`);

  const root = library(row.id, version);
  root.hashes.set('SHA-256', row.nupkg_sha256);
  const bom = new Models.Bom({ metadata: new Models.Metadata({ component: root }) });
  bom.metadata.properties.add(new Models.Property('elsa:target-framework', framework));
  bom.metadata.properties.add(new Models.Property('elsa:scope', 'NuGet producer restore, packed dependency declarations and shipped files'));
  const shared = row.shared_frameworks.find(group => group.framework === framework);
  for (const name of shared?.references ?? []) {
    root.properties.add(new Models.Property('elsa:shared-framework', name));
  }
  const resolved = new Map();
  for (const [key, value] of Object.entries(target)) {
    const [id, resolvedVersion] = key.split('/');
    assert.ok(id && resolvedVersion && !resolved.has(id.toLowerCase()), `Ambiguous target library: ${key}`);
    const component = library(id, resolvedVersion);
    component.properties.add(new Models.Property('elsa:restore-type', value.type));
    resolved.set(id.toLowerCase(), component);
    bom.components.add(component);
  }
  function dependency(id) {
    const component = resolved.get(id.toLowerCase());
    assert.ok(component, `Missing resolved dependency: ${row.id}/${framework}/${id}`);
    return component;
  }
  for (const item of declared.dependencies) {
    const component = dependency(item.id);
    root.dependencies.add(component.bomRef);
    component.properties.add(new Models.Property('elsa:nuspec-version', item.version));
    for (const field of ['include', 'exclude']) {
      if (item[field]) component.properties.add(new Models.Property('elsa:nuspec-' + field, item[field]));
    }
  }
  for (const [key, value] of Object.entries(target)) {
    const component = dependency(key.split('/')[0]);
    for (const id of Object.keys(value.dependencies ?? {})) component.dependencies.add(dependency(id).bomRef);
  }
  const byRef = new Map([...resolved.values()].map(component => [component.bomRef, component]));
  const direct = new Set(root.dependencies);
  const reachable = new Set(direct);
  for (const ref of reachable) {
    for (const child of byRef.get(ref).dependencies) reachable.add(child);
  }
  for (const component of resolved.values()) {
    let role = 'producer-only';
    if (direct.has(component.bomRef)) role = 'declared';
    else if (reachable.has(component.bomRef)) role = 'producer-resolved transitive';
    component.properties.add(new Models.Property('elsa:dependency-scope', role));
    if (role === 'producer-only') component.scope = 'excluded';
  }
  // These are contained files, distinct from the above producer-resolved components.
  for (const [name, sha256] of Object.entries(payload)) {
    const file = new Models.Component('file', name, { bomRef: 'file:' + name });
    file.hashes.set('SHA-256', sha256);
    root.components.add(file);
  }
  return bom;
}

const [candidate, inputFile] = process.argv.slice(2);
const input = JSON.parse(readFileSync(inputFile));
const output = join(candidate, 'release-evidence');
const browserValidator = new Validation.JsonStrictValidator(Spec.Version.v1dot5);
for (const record of input.browser) {
  const content = readFileSync(join(output, record.path));
  assert.equal(digest(content), record.sha256);
  const errors = await browserValidator.validate(content.toString());
  assert.equal(errors, null, JSON.stringify(errors));
}
const files = {};
for (const row of input.packages) {
  const seen = new Set();
  for (const record of row.restore_assets) {
    const framework = record.framework;
    assert.ok(row.frameworks.includes(framework) && !seen.has(framework), `Unexpected framework: ${row.id}/${framework}`);
    seen.add(framework);
    const bytes = readFileSync(join(candidate, record.retained_path));
    assert.equal(digest(bytes), record.sha256, `Restore snapshot changed: ${row.id}/${framework}`);
    const bom = frameworkBom(row, input.version, framework, JSON.parse(bytes), row.payload);
    const content = serializer.serialize(bom, { sortLists: true }) + '\n';
    const errors = await validator.validate(content);
    assert.equal(errors, null, JSON.stringify(errors));
    const name = `boms/${row.id}/${framework}.cdx.json`;
    mkdirSync(join(output, 'boms', row.id), { recursive: true });
    writeFileSync(join(output, name), content, { flag: 'wx' });
    (files[row.id] ??= {})[framework] = { path: name, sha256: digest(content) };
  }
  assert.equal(seen.size, row.frameworks.length, `Missing target framework: ${row.id}`);
}
writeFileSync(join(output, 'frameworks.json'), JSON.stringify(files) + '\n', { flag: 'wx' });

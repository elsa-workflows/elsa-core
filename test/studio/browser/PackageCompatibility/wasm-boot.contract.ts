// Synthetic parser/observer contracts only; no downloads, runtime or browser proof.
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { bootPolicy, parseWasmBootstrap, platformRole, WasmBootObserver, wasmBootPolicy, type ObservedBootResource } from './wasm-boot.js';

type Resource = ConstructorParameters<typeof WasmBootObserver>[0][number];
const hash = (body: Buffer | string) => createHash('sha256').update(body).digest('hex');
const sri = (digest: string) => 'sha256-' + Buffer.from(digest, 'hex').toString('base64');
const platform: Resource[] = wasmBootPolicy.platform.map(role => ({
  path: '/_framework/' + role.served.replace('{fingerprint}', 'a'.repeat(10)), sha256: hash(role.role), bytes: role.role.length,
  content_type: role.content_type, owner: 'platform'
}));
const managed: Resource[] = [...wasmBootPolicy.mandatory_managed, wasmBootPolicy.main_assembly, 'Elsa.Studio.Optional'].map(name => ({
  path: '/_framework/' + name + '.' + 'b'.repeat(10) + '.wasm', sha256: hash(name), bytes: name.length,
  content_type: 'application/wasm', owner: name === wasmBootPolicy.main_assembly ? 'fixture' : 'package'
}));
const config: any = Object.fromEntries(wasmBootPolicy.configuration_fields.map(name => [name, null]));
config.mainAssemblyName = wasmBootPolicy.main_assembly;
config.resources = Object.fromEntries(wasmBootPolicy.resource_groups.map(name => [name, null]));
config.resources.assembly = managed.map(row => ({ name: row.path.slice('/_framework/'.length),
  virtualPath: row.path.slice('/_framework/'.length).replace(/\.[a-z0-9]{10}\.wasm$/, '.wasm'), hash: sri(row.sha256), cache: 'force-cache' }));
config.resources.coreAssembly = [{ name: 'System.Core.aaaaaaaaaa.wasm', virtualPath: 'System.Core.wasm', hash: sri('c'.repeat(64)), cache: 'force-cache' }];
for (const role of wasmBootPolicy.platform) {
  if (!role.group) continue;
  const binding = platform.find(row => platformRole(row.path) === role.role)!;
  config.resources[role.group] = [{ name: binding.path.slice('/_framework/'.length),
    ...(role.group === 'wasmNative' ? { hash: sri(binding.sha256), cache: 'force-cache' } : {}) }];
}
const script = (value: any): Buffer => Buffer.from('globalThis.__bootstrapMustNotExecute=true;runtime.withConfig(/*json-start*/' + JSON.stringify(value) + '/*json-end*/);');
const body = script(config);
const manifest = platform.find(row => platformRole(row.path) === 'manifest')!;
manifest.sha256 = hash(body); manifest.bytes = body.length;
const platformBindings = platform.map(row => ({ role: platformRole(row.path)!, path: row.path, sha256: row.sha256 }));
const managedBindings = managed.map(row => ({ path: row.path, sha256: row.sha256, owner: row.owner === 'fixture' ? 'fixture' as const : 'package' as const }));
manifest.boot_configuration_sha256 = parseWasmBootstrap(body, platformBindings, managedBindings);
assert.equal('__bootstrapMustNotExecute' in globalThis, false);
assert.equal(manifest.boot_configuration_sha256, hash(JSON.stringify(config)));

const expected = platform.concat(managed);
const observed: ObservedBootResource[] = expected.map(({ boot_configuration_sha256: _, ...row }) => ({ ...row, status: 200, requested: true }));
const complete = (proof: ReturnType<WasmBootObserver['proof']>) => Object.values(proof.checks).every(Boolean);
const observer = new WasmBootObserver(expected, 'net10.0');
observer.observe(observed.find(row => platformRole(row.path) === 'manifest')!, body);
const proof = observer.proof(observed, true);
assert.equal(complete(proof), true);
assert.equal(proof.managed_bindings.length, managed.length);
assert.equal(complete(observer.proof(observed, false)), false); // Downloads without an executed managed callback are insufficient.
assert.equal(complete(observer.proof([], true)), false); // Rendering/callback alone is insufficient.
assert.equal(complete(new WasmBootObserver(expected, 'net10.0').proof(observed, true)), false); // No parsed observed loader body.
assert.equal(complete(observer.proof(observed.filter(row => row.owner !== 'fixture'), true)), false);
assert.equal(complete(observer.proof(observed.filter(row => !row.path.includes('Optional')), true)), false);
for (const role of wasmBootPolicy.platform)
  assert.equal(complete(observer.proof(observed.filter(row => platformRole(row.path) !== role.role), true)), false);
for (const field of ['sha256', 'bytes', 'content_type', 'owner', 'status', 'requested'] as const) {
  const changed = structuredClone(observed);
  Object.assign(changed[0], { [field]: { sha256: '0'.repeat(64), bytes: 0, content_type: 'text/html', owner: 'package', status: 404, requested: false }[field] });
  assert.equal(complete(observer.proof(changed, true)), false);
}
const changedBody = new WasmBootObserver(expected, 'net10.0');
changedBody.observe(observed.find(row => platformRole(row.path) === 'manifest')!, Buffer.from('changed source'));
assert.equal(complete(changedBody.proof(observed, true)), false);
const wrongOriginalConfiguration = structuredClone(expected);
wrongOriginalConfiguration.find(row => platformRole(row.path) === 'manifest')!.boot_configuration_sha256 = '0'.repeat(64);
const mismatchedConfiguration = new WasmBootObserver(wrongOriginalConfiguration, 'net10.0');
mismatchedConfiguration.observe(observed.find(row => platformRole(row.path) === 'manifest')!, body);
assert.equal(complete(mismatchedConfiguration.proof(observed, true)), false);

for (const framework of ['net8.0', 'net9.0']) assert.throws(() => new WasmBootObserver(expected, framework));
assert.throws(() => new WasmBootObserver(expected.filter(row => row.owner !== 'platform'), 'net10.0'));
assert.throws(() => new WasmBootObserver(expected.concat(platform[0]), 'net10.0'));
assert.throws(() => new WasmBootObserver(expected.map(({ boot_configuration_sha256: _, ...row }) => row), 'net10.0'));

const mutations: Array<(value: any) => void> = [
  value => { value.mainAssemblyName = 'Other.Host'; }, value => { value.extra = true; },
  value => { value.resources.extra = []; }, value => { value.resources.wasmNative[0].hash = sri('0'.repeat(64)); },
  value => { value.resources.jsModuleNative[0].name = '../escape.js'; },
  value => { value.resources.assembly[0].hash = sri('0'.repeat(64)); },
  value => { value.resources.assembly[0].virtualPath = 'Other.wasm'; },
  value => { value.resources.coreAssembly[0].virtualPath = value.resources.assembly[0].virtualPath; },
  value => { value.resources.assembly.pop(); }, value => { value.resources.assembly.push(structuredClone(value.resources.assembly[0])); },
  value => { value.resources.assembly.push({ name: 'Elsa.Unknown.aaaaaaaaaa.wasm', virtualPath: 'Elsa.Unknown.wasm', hash: sri('0'.repeat(64)), cache: 'force-cache' }); },
  value => { value.resources.assembly[0].cache = 'other'; }, value => { value.resources.coreAssembly = []; },
];
for (const mutate of mutations) {
  const changed = structuredClone(config); mutate(changed);
  assert.throws(() => parseWasmBootstrap(script(changed), platformBindings, managedBindings));
}
for (const raw of [Buffer.from('{}'), Buffer.from('runtime.withConfig(/*json-start*/{}/*json-end*/);'),
  Buffer.concat([body, Buffer.from('/*json-start*/')]), Buffer.alloc(wasmBootPolicy.maximum_boot_bytes + 1),
  Buffer.from(body.toString().replace('/*json-start*/', '/*unknown-start*/'))])
  assert.throws(() => parseWasmBootstrap(raw, platformBindings, managedBindings));
assert.equal('__bootstrapMustNotExecute' in globalThis, false);
// Source-derived JSON fixtures remain synthetic; they are not observed runtime proof.
for (const framework of ['net8.0', 'net9.0']) {
  const policy = bootPolicy(framework);
  const platform: Resource[] = policy.platform.map(role => ({
    path: '/_framework/' + role.served.replace('{fingerprint}', 'a'.repeat(10)), sha256: hash(role.role), bytes: role.role.length,
    content_type: role.content_type, owner: 'platform'
  }));
  const managed: Resource[] = [...policy.mandatory_managed, policy.main_assembly, 'Elsa.Studio.Optional'].map(name => ({
    path: '/_framework/' + name + (framework === 'net8.0' ? '' : '.' + 'b'.repeat(10)) + '.wasm', sha256: hash(name), bytes: name.length,
    content_type: 'application/wasm', owner: name === policy.main_assembly ? 'fixture' : 'package'
  }));
  const config: any = { mainAssemblyName: policy.main_assembly, debugLevel: 0, globalizationMode: 'sharded', resources: {
    hash: sri('c'.repeat(64)), assembly: Object.fromEntries(managed.map(row => [row.path.slice('/_framework/'.length), sri(row.sha256)])),
    coreAssembly: {}
  } };
  if (framework === 'net9.0') config.resources.fingerprinting = Object.fromEntries(managed.map(row => [
    row.path.slice('/_framework/'.length), row.path.slice('/_framework/'.length).replace(/\.[a-z0-9]{10}\.wasm$/, '.wasm')
  ]));
  for (const role of policy.platform) {
    if (!role.group) continue;
    const row = platform.find(row => platformRole(row.path, framework) === role.role)!;
    const name = row.path.slice('/_framework/'.length);
    config.resources[role.group] = { [name]: sri(row.sha256) };
    if (framework === 'net9.0') config.resources.fingerprinting[name] = role.virtual;
  }
  const body = Buffer.from(JSON.stringify(config));
  const manifest = platform.find(row => platformRole(row.path, framework) === 'manifest')!;
  manifest.sha256 = hash(body); manifest.bytes = body.length;
  const platformBindings = platform.map(row => ({ role: platformRole(row.path, framework)!, path: row.path, sha256: row.sha256 }));
  const managedBindings = managed.map(row => ({ path: row.path, sha256: row.sha256, owner: row.owner === 'fixture' ? 'fixture' as const : 'package' as const }));
  manifest.boot_configuration_sha256 = parseWasmBootstrap(body, platformBindings, managedBindings, framework);
  assert.equal(manifest.boot_configuration_sha256, hash(body));
  const expected = platform.concat(managed);
  const observed: ObservedBootResource[] = expected.map(({ boot_configuration_sha256: _, ...row }) => ({ ...row, status: 200, requested: true }));
  const observer = new WasmBootObserver(expected, framework);
  observer.observe(observed.find(row => platformRole(row.path, framework) === 'manifest')!, body);
  assert.equal(complete(observer.proof(observed, true)), true);
  assert.equal(observer.proof(observed, true).format, policy.format);
  assert.equal(complete(observer.proof(observed, false)), false);
  assert.equal(complete(new WasmBootObserver(expected, framework).proof(observed, true)), false);
  for (const row of expected) assert.equal(complete(observer.proof(observed.filter(item => item.path !== row.path), true)), false);
  for (const wrong of ['net10.0', framework === 'net8.0' ? 'net9.0' : 'net8.0', 'net11.0'])
    assert.throws(() => parseWasmBootstrap(body, platformBindings, managedBindings, wrong));
  const mutations: Array<(value: any) => void> = [
    value => { value.extra = true; }, value => { value.mainAssemblyName = 'Other.Host'; },
    value => { value.debugLevel = true; }, value => { value.resources.assembly = []; },
    value => { delete value.resources.assembly[managed[0].path.slice('/_framework/'.length)]; },
    value => { value.resources.assembly['Elsa.Unknown.wasm'] = sri('0'.repeat(64)); },
    value => { value.resources.wasmNative[Object.keys(value.resources.wasmNative)[0]] = sri('0'.repeat(64)); }
  ];
  if (framework === 'net9.0') mutations.push(
    value => { delete value.resources.fingerprinting; },
    value => { value.resources.fingerprinting[managed[0].path.slice('/_framework/'.length)] = 'Other.wasm'; },
    value => { value.resources.fingerprinting[Object.keys(value.resources.wasmNative)[0]] = 'other.wasm'; }
  );
  else mutations.push(value => { value.resources.fingerprinting = {}; });
  for (const mutate of mutations) {
    const changed = structuredClone(config); mutate(changed);
    assert.throws(() => parseWasmBootstrap(Buffer.from(JSON.stringify(changed)), platformBindings, managedBindings, framework));
  }
}
process.stdout.write('WASM bootstrap parser and observer contracts passed\n');

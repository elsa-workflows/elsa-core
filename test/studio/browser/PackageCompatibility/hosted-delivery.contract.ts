// Synthetic observer contracts only; no browser, native requests, or runtime proof.
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { HostedDeliveryObserver, emptyHostedDocument, failedHostedDelivery, hostedBaseHash,
  hostedDocumentResponse, type HostedResource, type HostedRoutePrefix } from './hosted-delivery.js';
import { bootPolicy, parseWasmBootstrap, platformRole } from './wasm-boot.js';
import type { RawResourceObservation } from './raw-resources.js';

const hash = (body: Buffer | string) => createHash('sha256').update(body).digest('hex');
const sri = (digest: string) => 'sha256-' + Buffer.from(digest, 'hex').toString('base64');
const origin = 'http://127.0.0.1:5011';
function fixture(framework: string) {
  const policy = bootPolicy(framework), bodies = new Map<string, Buffer>();
  const resource = (path: string, body: Buffer, content_type: string, owner: HostedResource['owner']): HostedResource => {
    bodies.set(path, body);
    return { path, sha256: hash(body), bytes: body.length, content_type, owner };
  };
  const platform = policy.platform.map(role => resource('/_framework/' + role.served.replace('{fingerprint}', 'a'.repeat(10)),
    Buffer.from(role.role), role.content_type, 'platform'));
  const managed = [...policy.mandatory_managed, policy.main_assembly].map(name => resource(
    '/_framework/' + name + (framework === 'net8.0' ? '' : '.' + 'b'.repeat(10)) + '.wasm',
    Buffer.from(name), 'application/wasm', name === policy.main_assembly ? 'fixture' : 'package'));
  const config: any = Object.fromEntries(policy.configuration_fields.map(name => [name, null]));
  config.mainAssemblyName = policy.main_assembly;
  config.resources = Object.fromEntries(policy.resource_groups.map(name => [name, null]));
  const managedName = (row: HostedResource) => row.path.slice('/_framework/'.length);
  if (framework === 'net10.0') {
    config.resources.assembly = managed.map(row => ({ name: managedName(row),
      virtualPath: managedName(row).replace(/\.[a-z0-9]{10}\.wasm$/, '.wasm'), hash: sri(row.sha256), cache: 'force-cache' }));
    config.resources.coreAssembly = [{ name: 'System.Core.aaaaaaaaaa.wasm', virtualPath: 'System.Core.wasm', hash: sri('c'.repeat(64)), cache: 'force-cache' }];
  } else {
    config.debugLevel = 0; config.globalizationMode = 'sharded';
    config.resources.assembly = Object.fromEntries(managed.map(row => [managedName(row), sri(row.sha256)]));
    config.resources.coreAssembly = {};
    if (framework === 'net9.0') config.resources.fingerprinting = Object.fromEntries(managed.map(row =>
      [managedName(row), managedName(row).replace(/\.[a-z0-9]{10}\.wasm$/, '.wasm')]));
  }
  for (const role of policy.platform) {
    if (!role.group) continue;
    const row = platform.find(row => platformRole(row.path, framework) === role.role)!;
    const name = managedName(row);
    config.resources[role.group] = framework === 'net10.0'
      ? [{ name, ...(role.group === 'wasmNative' ? { hash: sri(row.sha256), cache: 'force-cache' } : {}) }]
      : { [name]: sri(row.sha256) };
    if (framework === 'net9.0') config.resources.fingerprinting[name] = role.virtual;
  }
  const body = Buffer.from(framework === 'net10.0'
    ? 'globalThis.__hostedBootMustNotExecute=true;runtime.withConfig(/*json-start*/' + JSON.stringify(config) + '/*json-end*/);'
    : JSON.stringify(config));
  const manifest = platform.find(row => platformRole(row.path, framework) === 'manifest')!;
  manifest.sha256 = hash(body); manifest.bytes = body.length; bodies.set(manifest.path, body);
  manifest.boot_configuration_sha256 = parseWasmBootstrap(body,
    platform.map(row => ({ role: platformRole(row.path, framework)!, path: row.path, sha256: row.sha256 })),
    managed.map(row => ({ path: row.path, sha256: row.sha256, owner: row.owner as 'package' | 'fixture' })), framework);
  const extra = resource('/_content/Elsa.Studio.Test/localization.js', Buffer.from([0xef, 0xbb, 0xbf, 0xff, 0x61]), 'text/javascript', 'package');
  return { resources: [...platform, ...managed, extra], bodies };
}
function document(prefix: HostedRoutePrefix) {
  const path = prefix === '' ? '/login' : '/compat/login';
  return { ...hostedDocumentResponse(origin, prefix, { url: origin + path, main_frame_navigation: true,
    status: 200, content_type: 'Text/HTML; charset=utf-8' }), base_href_sha256: hostedBaseHash(prefix === '' ? '/' : '/compat/') };
}
function observed(row: HostedResource, body: Buffer, path = row.path): RawResourceObservation {
  return { record: { path, status: 200, requested: true, owner: row.owner,
    content_type: row.content_type, sha256: row.sha256, bytes: row.bytes }, body };
}

for (const framework of ['net8.0', 'net9.0', 'net10.0']) {
  const { resources, bodies } = fixture(framework);
  for (const prefix of ['', 'compat'] as HostedRoutePrefix[]) {
    const observer = new HostedDeliveryObserver(resources, framework, prefix);
    assert.equal(observer.expectedResources().size, resources.length * (prefix === '' ? 1 : 2));
    const actualPaths: string[] = [];
    resources.forEach((row, index) => {
      const path = prefix === 'compat' && index % 2 === 0 ? '/compat' + row.path : row.path;
      actualPaths.push(path);
      observer.observe(observed(row, bodies.get(row.path)!, path));
      observer.observe(observed(row, bodies.get(row.path)!, path)); // Same exact path is retained once.
    });
    if (prefix === 'compat') observer.observe(observed(resources[0], bodies.get(resources[0].path)!));
    assert.equal(observer.proof(document(prefix), true, true, true).checks.observations, false);
    observer.finishObservations(true);
    const proof = observer.proof(document(prefix), true, true, true);
    assert.equal(proof.result, 'passed');
    assert.equal(Object.values(proof.checks).every(Boolean), true);
    assert.deepEqual(proof.resources.map(row => row.path), [...new Set([...actualPaths, ...(prefix === 'compat' ? [resources[0].path] : [])])]);
    assert.equal(proof.boot?.managed_bindings.every(row => row.path.startsWith('/_framework/')), true);
    assert.equal(proof.resources.some(row => row.path.startsWith('/compat/')), prefix === 'compat');
    assert.equal('__hostedBootMustNotExecute' in globalThis, false);
    assert.equal(observer.proof(document(prefix), false, true, true).result, 'failed');
    assert.equal(observer.proof(document(prefix), true, false, true).result, 'failed');
    assert.equal(observer.proof(document(prefix), true, true, false).result, 'failed');
    assert.equal(observer.proof({ ...document(prefix), base_href_sha256: hostedBaseHash('/login/') }, true, true, true).result, 'failed');
    assert.equal(observer.proof({ ...document(prefix), path: null }, true, true, true).result, 'failed');
    const frozen = JSON.stringify(proof);
    assert.throws(() => observer.observe(observed(resources[0], bodies.get(resources[0].path)!)));
    assert.equal(JSON.stringify(proof), frozen);
    assert.throws(() => observer.finishObservations(true));
  }
  for (const mutate of [
    (row: RawResourceObservation) => { row.record.path = '/other' + row.record.path; },
    (row: RawResourceObservation) => { row.record.path = '/compat/compat' + row.record.path; },
    (row: RawResourceObservation) => { row.record.sha256 = '0'.repeat(64); },
    (row: RawResourceObservation) => { row.record.owner = 'package'; },
    (row: RawResourceObservation) => { row.record.content_type = 'text/html'; },
    (row: RawResourceObservation) => { row.record.bytes++; },
    (row: RawResourceObservation) => { row.record.status = 304; },
    (row: RawResourceObservation) => { row.body = Buffer.from('changed body'); },
  ]) {
    const observer = new HostedDeliveryObserver(resources, framework, 'compat');
    const invalid = observed(resources[0], bodies.get(resources[0].path)!); mutate(invalid);
    observer.observe(invalid);
    resources.forEach(row => observer.observe(observed(row, bodies.get(row.path)!)));
    observer.finishObservations(true);
    const proof = observer.proof(document('compat'), true, true, true);
    assert.equal(proof.checks.observations, false);
    assert.equal(proof.result, 'failed');
    assert.equal(proof.resources.length, resources.length); // Invalid observation itself never enters the receipt.
  }
  const aliasAtRoot = new HostedDeliveryObserver(resources, framework, '');
  aliasAtRoot.observe(observed(resources[0], bodies.get(resources[0].path)!, '/compat' + resources[0].path));
  aliasAtRoot.finishObservations(true);
  assert.equal(aliasAtRoot.proof(document(''), true, true, true).checks.observations, false);
  const noBody = new HostedDeliveryObserver(resources, framework, '');
  noBody.finishObservations(true);
  assert.equal(noBody.proof(document(''), true, true, true).result, 'failed');
  const noStop = new HostedDeliveryObserver(resources, framework, '');
  resources.forEach(row => noStop.observe(observed(row, bodies.get(row.path)!)));
  noStop.finishObservations(false);
  assert.equal(noStop.proof(document(''), true, true, true).checks.observations, false);
  assert.throws(() => new HostedDeliveryObserver([...resources, resources[0]], framework, ''));
  assert.throws(() => new HostedDeliveryObserver(resources.map(row => ({ ...row, path: '/compat' + row.path })), framework, 'compat'));
}

{
  const { resources, bodies } = fixture('net8.0');
  const template = resources.at(-1)!, body = bodies.get(template.path)!;
  const expanded = [...resources];
  while (expanded.length < 1025) expanded.push({ ...template, path: '/_content/Elsa.Studio.Test/extra-' + expanded.length + '.js' });
  const observer = new HostedDeliveryObserver(expanded, 'net8.0', 'compat');
  for (const row of expanded) {
    const bytes = bodies.get(row.path) ?? body;
    observer.observe(observed(row, bytes));
    observer.observe(observed(row, bytes, '/compat' + row.path));
  }
  observer.finishObservations(true);
  const proof = observer.proof(document('compat'), true, true, true);
  assert.equal(proof.resources.length, 2048);
  assert.equal(proof.checks.observations, false);
  assert.equal(proof.result, 'failed');
  assert.throws(() => new HostedDeliveryObserver(Array.from({ length: 2049 }, () => template), 'net8.0', ''));
  const interrupted = new HostedDeliveryObserver(resources, 'net8.0', '');
  resources.forEach(row => interrupted.observe(observed(row, bodies.get(row.path)!)));
  interrupted.failObservations();
  interrupted.finishObservations(true);
  assert.equal(interrupted.proof(document(''), true, true, true).result, 'failed');
  const wrongConfiguration = resources.map(row => ({ ...row }));
  wrongConfiguration.find(row => platformRole(row.path, 'net8.0') === 'manifest')!.boot_configuration_sha256 = '0'.repeat(64);
  const unbound = new HostedDeliveryObserver(wrongConfiguration, 'net8.0', '');
  resources.forEach(row => unbound.observe(observed(row, bodies.get(row.path)!)));
  unbound.finishObservations(true);
  assert.equal(unbound.proof(document(''), true, true, true).checks.configuration, false);
}

for (const url of [origin + '/other', origin + '/login/', origin + '/login?token=private', origin + '/login#private', 'http://127.0.0.1:5012/login']) {
  const observed = hostedDocumentResponse(origin, '', { url, main_frame_navigation: true, status: 200, content_type: 'text/html' });
  assert.equal(observed.path, null);
  assert.equal(JSON.stringify(observed).includes('private'), false);
}
assert.deepEqual(hostedDocumentResponse(origin, '', { url: origin + '/login', main_frame_navigation: false,
  status: 200, content_type: 'text/html' }), emptyHostedDocument());
assert.equal(hostedDocumentResponse(origin, '', { url: origin + '/login', main_frame_navigation: true,
  status: 500, content_type: 'application/json' }).content_type, 'other');
assert.equal(hostedBaseHash('x'.repeat(2049)), null);
assert.equal(hostedBaseHash(null), null);
assert.equal(failedHostedDelivery('').result, 'failed');
assert.equal(failedHostedDelivery('compat').entry_path, '/compat/login');
process.stdout.write('Hosted delivery observer contracts passed\n');

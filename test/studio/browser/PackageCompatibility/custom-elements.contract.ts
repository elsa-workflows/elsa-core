import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { bindNativeCallback, elementCallbacks, embeddingChecks, nativeAuthenticationRequest, nativeCallbackValue, type EmbeddingProof } from './custom-elements.js';

const hash = (value: string) => createHash('sha256').update(value).digest('hex');
const fresh = (): EmbeddingProof => ({ checks: Object.fromEntries(embeddingChecks.map(key => [key, false])) as EmbeddingProof['checks'] });
const proof = fresh();
const definition = '0123abcd', activity = 'a123', version = 'b123', instance = 'c123';
for (const [kind, payload, expected] of [
  ['definition', definition, { id: definition }],
  ['activity', { id: activity, outputValue: { expression: { value: 'private value' } } }, { id: activity }],
  ['version', { id: version, definitionId: definition, name: 'private name' }, { id: version, definitionId: definition }],
  ['execution', instance, { id: instance }],
  ['instance', instance, { id: instance }]
] as const) bindNativeCallback(kind, nativeCallbackValue(kind, payload), expected, proof);
assert.equal(proof.definition_id_sha256, hash(definition));
assert.equal(proof.activity_id_sha256, hash(activity));
assert.equal(proof.version_id_sha256, hash(version));
assert.notEqual(proof.version_id_sha256, proof.definition_id_sha256);
assert.equal(proof.instance_id_sha256, hash(instance));
assert.equal(proof.checks.backend_configured, false);
assert.equal(proof.checks.native_authentication, false);
assert.equal(proof.checks.instance_viewer, false);
assert.ok(!JSON.stringify(proof).includes('private'));
assert.deepEqual(Object.keys(proof.checks), embeddingChecks);
assert.deepEqual(elementCallbacks['definition-editor'], {
  activitySelectionChanged: 'activity', definitionVersionSelected: 'version', definitionExecuted: 'execution'
});
assert.equal(elementCallbacks['definition-list'].editWorkflowDefinition, 'definition');
assert.equal(elementCallbacks['instance-list'].viewWorkflowInstance, 'instance');
for (const value of [null, '', '../secret', 'https://127.0.0.1', 'x'.repeat(65), 12, {}, ['a']])
  assert.throws(() => nativeCallbackValue('definition', value));
for (const value of [null, [], { id: activity, definitionId: '../bad' }, { id: version }, { definitionId: definition }])
  assert.throws(() => nativeCallbackValue('version', value));
for (const [kind, value, expected] of [
  ['definition', { id: definition }, { id: activity }],
  ['activity', { id: activity }, { id: definition }],
  ['version', { id: version, definitionId: activity }, { id: version, definitionId: definition }],
  ['version', { id: definition, definitionId: definition }, { id: version, definitionId: definition }],
  ['execution', { id: instance }, { id: definition }],
  ['instance', { id: definition }, { id: instance }]
] as const) {
  const failed = fresh();
  assert.throws(() => bindNativeCallback(kind, value, expected, failed));
  assert.deepEqual(failed, fresh());
}

const backend = 'http://127.0.0.1:12345/elsa/api';
const token = 'private-token';
const observation = { nativeFrame: true, configured: true, url: backend + '/workflow-definitions?versionOptions=Latest',
  status: 200, method: 'GET', authorization: 'Bearer ' + token };
assert.equal(nativeAuthenticationRequest(observation, backend, token), true);
for (const change of [{ nativeFrame: false }, { configured: false }, { status: 401 }, { method: 'POST' },
  { authorization: null }, { authorization: 'Bearer wrong' }, { url: backend + '/workflow-definitions-foreign' },
  { url: 'http://127.0.0.1:54321/elsa/api/workflow-definitions' }, { url: backend + '/identity/login' }])
  assert.equal(nativeAuthenticationRequest({ ...observation, ...change }, backend, token), false);

console.log("CustomElements callback contracts passed");

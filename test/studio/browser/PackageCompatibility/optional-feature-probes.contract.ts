// Pure synthetic parser/profile contracts, never native browser proof.
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { classifyOptionalFeatureBody, optionalFeatureProfile, optionalFeatureScenarios } from './optional-feature-probes.js';

const empty = classifyOptionalFeatureBody('secrets-descriptors', Buffer.alloc(0))!;
assert.equal(empty.bytes, 0);
assert.equal(empty.sha256, createHash('sha256').update(Buffer.alloc(0)).digest('hex'));
assert.equal(empty.sensitive_items_present, false);
for (const value of [{}, { secret: 'synthetic private data' }, { items: { nested: [] } },
  { nested: { items: [] } }, { types: [], stores: [], secret: 'synthetic private data' }, 'scalar', null]) {
  assert.equal(classifyOptionalFeatureBody('secrets-descriptors', Buffer.from(JSON.stringify(value)))!.sensitive_items_present, null);
}
for (const [target, value] of [['secrets-descriptors', { types: [], stores: [] }],
  ['workflow-context-descriptors', { items: [], count: 0 }], ['secrets-picker', { items: [], canCreateInline: false }]] as const) {
  assert.equal(classifyOptionalFeatureBody(target, Buffer.from(JSON.stringify(value)))!.sensitive_items_present, false);
}
for (const [target, value] of [['secrets-descriptors', { types: [{}], stores: [] }],
  ['workflow-context-descriptors', { items: [{}], count: 1 }], ['secrets-picker', { items: [{}], canCreateInline: false }]] as const) {
  assert.equal(classifyOptionalFeatureBody(target, Buffer.from(JSON.stringify(value)))!.sensitive_items_present, true);
}
assert.equal(classifyOptionalFeatureBody('workflow-context-descriptors', Buffer.from('{"items":{"nested":[]},"count":0}'))!.sensitive_items_present, null);
assert.equal(classifyOptionalFeatureBody('secrets-descriptors', Buffer.from('<html>failure</html>'))!.sensitive_items_present, null);
assert.throws(() => classifyOptionalFeatureBody('secrets-descriptors', Buffer.alloc(65537)));
assert.equal(optionalFeatureScenarios.length, 5);
for (const scenario of optionalFeatureScenarios) {
  const profile = optionalFeatureProfile(scenario);
  assert.deepEqual(profile.backend_features, scenario === 'without-secrets' ? ['workflow-contexts'] :
    scenario === 'without-workflow-contexts' ? ['secrets'] : ['workflow-contexts', 'secrets']);
  assert.equal(profile.permission_profile, scenario.startsWith('deny-') ? scenario : 'full');
}
process.stdout.write('optional feature response contracts passed\n');

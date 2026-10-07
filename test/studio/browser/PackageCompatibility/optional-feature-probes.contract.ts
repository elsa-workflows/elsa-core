// Pure synthetic observation/parser/profile contracts, never native browser proof.
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { classifyOptionalFeatureBody, NativeCircuitObservation, NativeRequestOrigins, optionalFeatureProfile, optionalFeatureScenarios } from './optional-feature-probes.js';

const loginSocket = {}, editorSocket = {}, reconnectSocket = {};
const circuit = new NativeCircuitObservation();
assert.equal(circuit.closed(), null);
circuit.connected(loginSocket);
assert.equal(circuit.closed(), false);
circuit.disconnected(loginSocket);
assert.equal(circuit.closed(), true);
circuit.connected(editorSocket); // Legitimate navigation replaces the retired login circuit.
assert.equal(circuit.closed(), false);
circuit.disconnected(loginSocket); // A late old-socket close cannot close the current circuit.
assert.equal(circuit.closed(), false);
circuit.beginAction();
circuit.disconnected(loginSocket);
assert.equal(circuit.closed(), false);
circuit.disconnected(editorSocket);
assert.equal(circuit.closed(), true);
circuit.connected(reconnectSocket); // Reconnect cannot erase genuine action-time loss.
assert.equal(circuit.closed(), true);
const closedBeforeAction = new NativeCircuitObservation();
closedBeforeAction.connected(loginSocket);
closedBeforeAction.disconnected(loginSocket);
closedBeforeAction.beginAction();
closedBeforeAction.connected(editorSocket);
assert.equal(closedBeforeAction.closed(), true);
const firstConnectionAfterAction = new NativeCircuitObservation();
firstConnectionAfterAction.beginAction();
assert.equal(firstConnectionAfterAction.closed(), null);
firstConnectionAfterAction.connected(editorSocket);
assert.equal(firstConnectionAfterAction.closed(), false);
firstConnectionAfterAction.disconnected(editorSocket);
assert.equal(firstConnectionAfterAction.closed(), true);

// The same terminal bookkeeping is used for response and requestfailed events.
for (const terminal of ['response', 'requestfailed']) {
  for (const started of [{ after_action: false, after_disconnect_ack: false },
    { after_action: true, after_disconnect_ack: false }, { after_action: false, after_disconnect_ack: true },
    { after_action: true, after_disconnect_ack: true }]) {
    const origins = new NativeRequestOrigins(), request = { terminal };
    const flags = { ...started };
    origins.started(request, flags);
    // Complete after both boundaries; the request's earlier origin is fixed.
    flags.after_action = true;
    flags.after_disconnect_ack = true;
    assert.deepEqual(origins.completed(request), started);
    assert.equal(origins.failed(), false);
  }
}

{
  const origins = new NativeRequestOrigins(), before = {}, afterAction = {}, afterDisconnect = {};
  origins.started(before, { after_action: false, after_disconnect_ack: false });
  origins.started(afterAction, { after_action: true, after_disconnect_ack: false });
  origins.started(afterDisconnect, { after_action: true, after_disconnect_ack: true });
  // Terminal events can arrive in a different order from starts.
  assert.deepEqual(origins.completed(afterDisconnect), { after_action: true, after_disconnect_ack: true });
  assert.deepEqual(origins.completed(before), { after_action: false, after_disconnect_ack: false });
  assert.deepEqual(origins.completed(afterAction), { after_action: true, after_disconnect_ack: false });
  assert.equal(origins.failed(), false);
}
for (const order of [['response', 'response'], ['response', 'requestfailed'], ['requestfailed', 'response']]) {
  const origins = new NativeRequestOrigins(), request = {};
  origins.started(request, { after_action: true, after_disconnect_ack: true });
  assert.notEqual(origins.completed(request), undefined, order[0]);
  assert.equal(origins.completed(request), undefined, order[1]);
  assert.equal(origins.failed(), true); // No duplicate rows or second body read may certify this request.
}
{
  const origins = new NativeRequestOrigins(), request = { url: 'synthetic native endpoint' };
  origins.started(request, { after_action: false, after_disconnect_ack: false });
  assert.equal(origins.completed({ ...request }), undefined); // Matching URL is not Request identity.
  assert.equal(origins.failed(), true);
  assert.deepEqual(origins.completed(request), { after_action: false, after_disconnect_ack: false });
}
{
  const origins = new NativeRequestOrigins(), request = {};
  origins.started(request, { after_action: false, after_disconnect_ack: false });
  origins.started(request, { after_action: true, after_disconnect_ack: true });
  assert.equal(origins.failed(), true);
  assert.deepEqual(origins.completed(request), { after_action: false, after_disconnect_ack: false });
}
{
  const origins = new NativeRequestOrigins();
  for (let index = 0; index < 64; index++) {
    const early = {};
    origins.started(early, { after_action: false, after_disconnect_ack: false });
    assert.notEqual(origins.completed(early), undefined);
  }
  assert.equal(origins.failed(), false);
  const overflow = {};
  origins.started(overflow, { after_action: true, after_disconnect_ack: true });
  assert.equal(origins.completed(overflow), undefined);
  assert.equal(origins.failed(), true); // Early traffic cannot grow an unbounded origin inventory.
}
{
  const origins = new NativeRequestOrigins(), request = {};
  origins.started(request, { after_action: true, after_disconnect_ack: true });
  assert.notEqual(origins.completed(request), undefined);
  origins.stop();
  origins.started({}, { after_action: true, after_disconnect_ack: true });
  assert.equal(origins.completed(request), undefined);
  assert.equal(origins.completed({}), undefined);
  assert.equal(origins.failed(), false); // Detached late events cannot mutate a finished receipt.
}
{
  const origins = new NativeRequestOrigins(), incomplete = {};
  origins.started(incomplete, { after_action: false, after_disconnect_ack: false });
  origins.stop();
  assert.equal(origins.failed(), true); // An in-flight descriptor cannot disappear during teardown.
  assert.equal(origins.completed(incomplete), undefined);
  assert.equal(origins.failed(), true);
}
{
  const origins = new NativeRequestOrigins(), before = {}, after = {};
  origins.started(before, { after_action: false, after_disconnect_ack: false });
  origins.started(after, { after_action: true, after_disconnect_ack: true });
  const early = { ...origins.completed(before)!, body: null as string | null };
  const late = { ...origins.completed(after)!, body: null as string | null };
  let finishEarly!: (body: string) => void, finishLate!: (body: string) => void;
  const pending = [new Promise<string>(resolve => { finishEarly = resolve; }).then(body => { early.body = body; }),
    new Promise<string>(resolve => { finishLate = resolve; }).then(body => { late.body = body; })];
  finishLate('synthetic late body');
  await pending[1];
  finishEarly('synthetic early body');
  await Promise.all(pending);
  assert.deepEqual(early, { after_action: false, after_disconnect_ack: false, body: 'synthetic early body' });
  assert.deepEqual(late, { after_action: true, after_disconnect_ack: true, body: 'synthetic late body' });
  assert.equal(origins.failed(), false);
}

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

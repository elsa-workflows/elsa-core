// Synthetic CDP contracts only. No requests, browser, or runtime proof.
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import type { CDPSession } from '@playwright/test';
import { startRawResources, type PausedResource, type RawResourceAsset, type RawResourceObservation,
  type RawResourceOptions, type RawResourceSession, type ResourceFailure } from './raw-resources.js';

// Hosted typecheck verifies compatibility with Playwright's actual session.
const actualSession = (session: CDPSession): RawResourceSession => session;
void actualSession;
type Method = Parameters<RawResourceSession['send']>[0];
const studio = 'http://127.0.0.1:5011';
const path = '/_content/Package/localization.js';
const hash = (body: Buffer) => createHash('sha256').update(body).digest('hex');
// Exactly 149 entity bytes, including a UTF-8 BOM and an invalid UTF-8 byte.
const original = Buffer.concat([Buffer.from([0xef, 0xbb, 0xbf, 0xff]), Buffer.alloc(145, 0x61)]);
const tick = () => new Promise<void>(resolve => setImmediate(resolve));
function deferred<T>() {
  let resolve!: (value: T) => void;
  let reject!: (error: Error) => void;
  const promise = new Promise<T>((yes, no) => { resolve = yes; reject = no; });
  return { promise, resolve, reject };
}

class FakeSession implements RawResourceSession {
  readonly calls: Array<{ method: string; parameters?: any }> = [];
  readonly listeners = new Set<(event: PausedResource) => void>();
  readonly errors = new Set<string>();
  body: any = { body: original.toString('base64'), base64Encoded: true };
  hook?: (method: Method, parameters?: any) => Promise<any> | undefined;
  async send(method: Method, parameters?: any): Promise<any> {
    this.calls.push({ method, parameters: structuredClone(parameters) });
    if (this.errors.has(method)) throw new Error('private protocol error');
    const result = this.hook?.(method, parameters);
    if (result) return result;
    return method === 'Fetch.getResponseBody' ? this.body : {};
  }
  on(_event: 'Fetch.requestPaused', listener: (event: PausedResource) => void): void { this.listeners.add(listener); }
  off(_event: 'Fetch.requestPaused', listener: (event: PausedResource) => void): void { this.listeners.delete(listener); }
  async detach(): Promise<void> {
    this.calls.push({ method: 'detach' });
    if (this.errors.has('detach')) throw new Error('private detach error');
  }
  emit(event: PausedResource): void { for (const listener of this.listeners) listener(event); }
}

function paused(id = 'native-request'): PausedResource {
  return { requestId: id, request: { url: studio + path }, responseStatusCode: 200,
    responseHeaders: [{ name: 'Content-Type', value: 'text/javascript; charset=utf-8' },
      { name: 'Content-Length', value: String(original.length) }] };
}
async function fixture(body = original, options: RawResourceOptions = {}, session = new FakeSession()) {
  const asset: RawResourceAsset = { path, sha256: hash(body), bytes: body.length, content_type: 'text/javascript', owner: 'package' };
  session.body = { body: body.toString('base64'), base64Encoded: true };
  const observations: RawResourceObservation[] = [], failures: ResourceFailure[] = [];
  const observer = await startRawResources(session, studio, new Map([[path, asset]]),
    value => observations.push(value), value => failures.push(value), options);
  return { asset, session, observations, failures, observer };
}
function noOverrides(session: FakeSession): void {
  const allowed = new Set(['Network.enable', 'Network.setCacheDisabled', 'Fetch.enable', 'Fetch.getResponseBody', 'Fetch.continueResponse', 'Fetch.disable', 'detach']);
  for (const call of session.calls) {
    assert(allowed.has(call.method));
    if (call.method === 'Fetch.continueResponse') assert.deepEqual(call.parameters, { requestId: call.parameters.requestId });
  }
}

{
  const value = await fixture();
  assert.deepEqual(value.session.calls.slice(0, 3), [
    { method: 'Network.enable', parameters: undefined },
    { method: 'Network.setCacheDisabled', parameters: { cacheDisabled: true } },
    { method: 'Fetch.enable', parameters: { patterns: [
      { urlPattern: studio + path, requestStage: 'Response' },
      { urlPattern: studio + path + '\\?*', requestStage: 'Response' }
    ] } }
  ]);
  value.session.emit(paused());
  await tick();
  assert.equal(value.failures.length, 0);
  assert.equal(value.observations.length, 1);
  assert.deepEqual(value.observations[0].body, original);
  assert.equal(value.observations[0].body.length, 149);
  assert.deepEqual(value.observations[0].record, { ...value.asset, status: 200, requested: true });
  await value.observer.stop();
  assert.deepEqual(value.session.calls.slice(-2).map(call => call.method), ['Fetch.disable', 'detach']);
  assert.equal(value.session.listeners.size, 0);
  await value.observer.stop(); // Idempotent: no second disable or detach.
  assert.equal(value.session.calls.filter(call => call.method === 'detach').length, 1);
  noOverrides(value.session);
}

type Mutation = (value: Awaited<ReturnType<typeof fixture>>, event: PausedResource) => void;
const invalid: Array<[Mutation, ResourceFailure['reason']]> = [
  [(value, _) => { value.session.body.body = Buffer.alloc(original.length, 0x62).toString('base64'); }, 'response_read_failed'],
  [(value, _) => { value.session.body.body = original.subarray(0, original.length - 1).toString('base64'); }, 'resource_body_size'],
  [(value, _) => { value.session.body.base64Encoded = false; }, 'response_read_failed'],
  [(value, _) => { value.session.body.body = '!' + value.session.body.body.slice(1); }, 'response_read_failed'],
  [(_, event) => { event.responseStatusCode = 304; }, 'response_read_failed'],
  [(_, event) => { event.responseStatusCode = 404; }, 'response_read_failed'],
  [(_, event) => { delete event.responseStatusCode; event.responseErrorReason = 'ConnectionClosed'; }, 'response_read_failed'],
  [(_, event) => { event.request.url += '#' + 'private-fragment'; }, 'response_read_failed'],
  [(_, event) => { event.request.url += '?' + 'q'.repeat(2049); }, 'response_read_failed'],
  [(_, event) => { event.responseHeaders![1].value = '33554433'; }, 'resource_body_limit'],
  [(_, event) => { event.responseHeaders![1].value = '-1'; }, 'resource_body_limit'],
  [(_, event) => { event.responseHeaders!.push({ name: 'Content-Encoding', value: 'deflate' }); }, 'resource_body_limit'],
  [(_, event) => { event.responseHeaders!.push({ name: 'content-length', value: '149' }); }, 'resource_body_limit'],
  [(_, event) => { event.responseHeaders![0].value = 'text/html'; }, 'response_read_failed'],
  [(_, event) => { event.responseHeaders!.push(...Array.from({ length: 129 }, () => ({ name: 'x-header', value: '' }))); }, 'resource_body_limit'],
  [(_, event) => { event.responseHeaders!.push({ name: 'x-header', value: 'x'.repeat(65537) }); }, 'resource_body_limit'],
  [(value, _) => { value.session.errors.add('Fetch.getResponseBody'); }, 'response_read_failed'],
];
for (const [mutate, reason] of invalid) {
  const value = await fixture(), event = paused();
  mutate(value, event);
  value.session.emit(event);
  await tick();
  assert.equal(value.observations.length, 0);
  assert.deepEqual(value.failures, [{ path_sha256: hash(Buffer.from(path)), status: event.responseStatusCode ?? null, phase: 'body', reason }]);
  assert.deepEqual(value.session.calls.at(-1), { method: 'Fetch.continueResponse', parameters: { requestId: event.requestId } });
  await value.observer.stop();
  noOverrides(value.session);
}
{
  const value = await fixture(Buffer.from('f'));
  value.session.body.body = 'Zh=='; // Decodes to f, but has nonzero padding bits.
  const event = paused(); event.responseHeaders![1].value = '1';
  value.session.emit(event); await tick();
  assert.equal(value.observations.length, 0);
  assert.equal(value.failures[0].reason, 'response_read_failed');
  await value.observer.stop();
}
for (const encoding of [undefined, 'identity', 'gzip', 'br']) {
  const value = await fixture(), event = paused();
  if (encoding) event.responseHeaders!.push({ name: 'Content-Encoding', value: encoding });
  if (encoding === 'gzip' || encoding === 'br') event.responseHeaders![1].value = '200';
  else event.responseHeaders = event.responseHeaders!.filter(header => header.name !== 'Content-Length');
  event.request.url += '?v=cachebuster&other=bounded';
  value.session.emit(event); await tick();
  assert.equal(value.observations.length, 1);
  assert.equal(value.observations[0].record.path, path);
  assert.deepEqual(value.observations[0].body, original);
  await value.observer.stop();
}
for (const url of ['http://127.0.0.1:5012' + path, studio + path + '.sibling', studio + '/unlisted.js']) {
  const value = await fixture(), event = paused(); event.request.url = url;
  value.session.emit(event); await tick();
  assert.equal(value.observations.length, 0);
  assert.equal(value.failures.length, 0);
  assert.equal(value.session.calls.filter(call => call.method === 'Fetch.getResponseBody').length, 0);
  assert.equal(value.session.calls.at(-1)?.method, 'Fetch.continueResponse');
  await value.observer.stop();
}
{
  const value = await fixture();
  value.session.errors.add('Fetch.continueResponse');
  value.session.emit(paused()); await tick();
  assert.equal(value.observations.length, 0); // Bytes alone cannot prove a served response.
  assert.equal(value.failures[0].reason, 'response_read_failed');
  await assert.rejects(value.observer.stop(), /raw_resources_cleanup_failed/);
  assert.equal(value.session.calls.at(-1)?.method, 'detach');
  noOverrides(value.session);
}
{
  const session = new FakeSession(), observations: RawResourceObservation[] = [], failures: ResourceFailure[] = [];
  const asset: RawResourceAsset = { path, bytes: original.length, sha256: hash(original), content_type: 'text/javascript', owner: 'package' };
  const observer = await startRawResources(session, studio, new Map([[path, asset]]), value => {
    observations.push(value); assert.equal(session.calls.at(-1)?.method, 'Fetch.continueResponse');
    throw new Error('private observer failure');
  }, failure => failures.push(failure));
  session.emit(paused()); await tick();
  assert.equal(observations.length, 1);
  assert.equal(failures[0].phase, 'observation');
  assert.equal(failures[0].reason, 'resource_observation_failed');
  await observer.stop();
}
{
  const value = await fixture(), reading = deferred<any>();
  value.session.hook = method => method === 'Fetch.getResponseBody' ? reading.promise : undefined;
  value.session.emit(paused('first'));
  const stopping = value.observer.stop();
  value.session.emit(paused('late'));
  await tick();
  assert.equal(value.session.calls.filter(call => call.method === 'Fetch.getResponseBody').length, 1);
  reading.resolve(value.session.body);
  await stopping;
  assert.equal(value.observations.length, 1);
  const count = value.session.calls.length;
  value.session.emit(paused('after-stop')); await tick();
  assert.equal(value.session.calls.length, count);
  noOverrides(value.session);
}
{
  // A pause delivered while disable is pending must be resumed and drained too.
  const value = await fixture(), continuing = deferred<any>();
  value.session.hook = method => {
    if (method === 'Fetch.disable') { value.session.emit(paused('during-disable')); return Promise.resolve({}); }
    return method === 'Fetch.continueResponse' ? continuing.promise : undefined;
  };
  let stopped = false;
  const stopping = value.observer.stop().then(() => { stopped = true; });
  await tick(); assert.equal(stopped, false);
  continuing.resolve({}); await stopping;
  assert.equal(value.observations.length, 0);
  assert.equal(value.session.calls.filter(call => call.method === 'Fetch.getResponseBody').length, 0);
  noOverrides(value.session);
}
for (const method of ['Fetch.disable', 'detach']) {
  const value = await fixture(); value.session.errors.add(method);
  await assert.rejects(value.observer.stop(), /raw_resources_cleanup_failed/);
  assert.equal(value.session.listeners.size, 0);
}
for (const method of ['Network.enable', 'Network.setCacheDisabled', 'Fetch.enable'] as const) {
  const session = new FakeSession(); session.errors.add(method);
  await assert.rejects(fixture(original, {}, session), /raw_resources_setup_failed/);
  assert.equal(session.listeners.size, 0);
  assert.equal(session.calls.at(-1)?.method, 'detach');
}
{
  const session = new FakeSession();
  const asset: RawResourceAsset = { path, bytes: 33554433, sha256: hash(original), content_type: 'text/javascript', owner: 'package' };
  await assert.rejects(startRawResources(session, studio, new Map([[path, asset]]), () => {}, () => {}), /raw_resources_setup_failed/);
  assert.deepEqual(session.calls, [{ method: 'detach' }]); // Reject overbound inventory before enabling interception.
}
{
  const value = await fixture(original, { stopTimeoutMs: 5 }), reading = deferred<any>();
  value.session.hook = method => method === 'Fetch.getResponseBody' ? reading.promise : undefined;
  value.session.emit(paused());
  await assert.rejects(value.observer.stop(), /raw_resources_cleanup_failed/);
  // An unresolved read is left for owned context/browser close. CDP forbids
  // continuing or disabling Fetch during the read; no cleanup claim is made.
  assert.equal(value.session.calls.filter(call => ['Fetch.continueResponse', 'Fetch.disable', 'detach'].includes(call.method)).length, 0);
  const count = value.session.calls.length;
  reading.resolve(value.session.body); value.session.emit(paused('after-timeout')); await tick();
  assert.equal(value.session.calls.length, count);
  assert.equal(value.observations.length, 0);
  assert.equal(value.session.listeners.size, 0);
}

{
  const value = await fixture(), reading = deferred<any>();
  value.session.hook = method => method === 'Fetch.getResponseBody' ? reading.promise : undefined;
  for (let i = 0; i < 129; i++) value.session.emit(paused('concurrent-' + i));
  await tick();
  assert.equal(value.session.calls.filter(call => call.method === 'Fetch.getResponseBody').length, 128);
  assert.equal(value.failures[0].reason, 'resource_body_limit');
  assert.equal(value.session.calls.at(-1)?.method, 'Fetch.continueResponse');
  reading.resolve(value.session.body); await value.observer.stop();
  assert.equal(value.observations.length, 128);
}
{
  const session = new FakeSession(), reading = deferred<any>(), failures: ResourceFailure[] = [];
  const asset: RawResourceAsset = { path, bytes: 32 * 1024 * 1024, sha256: hash(original), content_type: 'text/javascript', owner: 'package' };
  session.hook = method => method === 'Fetch.getResponseBody' ? reading.promise : undefined;
  const observer = await startRawResources(session, studio, new Map([[path, asset]]), () => assert.fail('Unverified bytes'), failure => failures.push(failure));
  for (let i = 0; i < 5; i++) {
    const event = paused('aggregate-' + i); event.responseHeaders![1].value = String(asset.bytes); session.emit(event);
  }
  await tick();
  assert.equal(session.calls.filter(call => call.method === 'Fetch.getResponseBody').length, 4);
  assert.equal(failures[0].reason, 'resource_body_limit');
  reading.reject(new Error('synthetic read failure')); await observer.stop();
  noOverrides(session);
}
{
  const value = await fixture();
  for (let i = 0; i < 2049; i++) { value.session.emit(paused('repeated-' + i)); await tick(); }
  assert.equal(value.observations.length, 2048);
  assert.equal(value.failures.length, 1);
  assert.equal(value.failures[0].reason, 'resource_body_limit');
  await value.observer.stop(); noOverrides(value.session);
}
{
  const value = await fixture(original, { stopTimeoutMs: 5 }), holding = deferred<any>();
  value.session.hook = method => ['Fetch.getResponseBody', 'Fetch.continueResponse'].includes(method) ? holding.promise : undefined;
  for (let i = 0; i < 1024; i++) value.session.emit(paused('flood-' + i));
  assert.equal(value.session.calls.length, 515); // Three setup calls, at most 512 tracked requests.
  await assert.rejects(value.observer.stop(), /raw_resources_cleanup_failed/);
  holding.resolve(value.session.body); await tick();
  assert.equal(value.observations.length, 0);
  assert.equal(value.session.listeners.size, 0);
}

process.stdout.write('raw resource response contracts passed\n');

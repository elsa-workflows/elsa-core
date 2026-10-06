// Synthetic observer contracts only. These fixtures are not browser/runtime evidence.
import assert from 'node:assert/strict';
import { DirectBackendObserver, directBackendChecks } from './direct-backend.js';

const studio = 'http://127.0.0.1:5011';
const backend = 'http://127.0.0.1:5012/elsa/api';
const username = 'private-observer-user', password = 'private-observer-password';
const token = 'private_header.private_payload.private_signature';
type Fixture = {
  url: string; method: string; started: number; requestHeaders: Record<string, string>;
  responseHeaders: Record<string, string>; status: number; payload: unknown;
  fromMainFrame: boolean; resourceType: string; postData: string | null; body?: () => Promise<Buffer>;
};
const fixtures = (): [Fixture, Fixture] => [{
  url: backend + '/identity/login', method: 'POST', started: 1,
  requestHeaders: { origin: studio, 'content-type': 'application/json' },
  responseHeaders: { 'access-control-allow-origin': studio, 'content-type': 'application/json' },
  status: 200, payload: { isAuthenticated: true, accessToken: token }, fromMainFrame: true,
  resourceType: 'fetch', postData: JSON.stringify({ username, password })
}, {
  url: backend + '/descriptors/activities?refresh=true', method: 'GET', started: 2,
  requestHeaders: { origin: studio, authorization: 'Bearer ' + token },
  responseHeaders: { 'access-control-allow-origin': studio, 'content-type': 'application/json' },
  status: 200, payload: { count: 2, items: ['SetOutput', 'Flowchart'].map(name =>
    ({ typeName: 'Elsa.' + name, namespace: 'Elsa', name, version: 1 })) },
  fromMainFrame: true, resourceType: 'fetch', postData: null
}];

function response(fixture: Fixture) {
  return {
    url: () => fixture.url, status: () => fixture.status, allHeaders: async () => fixture.responseHeaders,
    body: fixture.body ?? (async () => Buffer.from(JSON.stringify(fixture.payload))),
    request: () => ({ method: () => fixture.method, allHeaders: async () => fixture.requestHeaders,
      resourceType: () => fixture.resourceType, postData: () => fixture.postData,
      timing: () => ({ startTime: fixture.started, domainLookupStart: -1, domainLookupEnd: -1,
        connectStart: -1, secureConnectionStart: -1, connectEnd: -1, requestStart: -1, responseStart: -1, responseEnd: -1 }) })
  };
}

async function observe(rows: Fixture[], origin = studio) {
  const observer = new DirectBackendObserver(origin, backend, username, password);
  await Promise.all(rows.map(row => observer.observe(response(row), row.fromMainFrame)));
  return observer.proof();
}
const complete = (proof: ReturnType<DirectBackendObserver['proof']>) => Object.values(proof.checks).every(Boolean);
const positive = await observe(fixtures());
assert.equal(complete(positive), true);
assert.equal(positive.descriptor_count, 2);
const refitCasing = fixtures();
refitCasing[1].url = backend + '/descriptors/activities?Refresh=True';
assert.equal(complete(await observe(refitCasing)), true);
assert.deepEqual(Object.keys(positive.checks).sort(), [...directBackendChecks].sort());
for (const secret of [username, password, token, studio, backend]) assert.equal(JSON.stringify(positive).includes(secret), false);
assert.equal(complete(await observe([])), false); // Rendering is not network evidence.
assert.equal(complete(await observe([fixtures()[0]])), false);
assert.equal(complete(await observe([fixtures()[1]])), false); // Independent metadata/API login cannot satisfy this observer.
assert.equal(complete(await observe(fixtures(), 'http://127.0.0.1:5012')), false);
for (const url of ['https://127.0.0.1:5011', 'http://example.com:5011', 'http://user@127.0.0.1:5011', studio + '?private=value'])
  assert.throws(() => new DirectBackendObserver(url, backend, username, password));

const mutations: Array<(rows: [Fixture, Fixture]) => void> = [
  rows => { rows[0].requestHeaders.origin = backend; },
  rows => { rows[1].requestHeaders.origin = backend; },
  rows => { rows[0].responseHeaders['access-control-allow-origin'] = '*'; },
  rows => { rows[1].responseHeaders['access-control-allow-origin'] = backend; },
  rows => { delete rows[1].responseHeaders['access-control-allow-origin']; },
  rows => { rows[0].requestHeaders.authorization = 'Bearer ' + token; },
  rows => { rows[1].requestHeaders.authorization = 'Bearer different.token.signature'; },
  rows => { delete rows[1].requestHeaders.authorization; },
  rows => { rows[0].postData = JSON.stringify({ username, password: 'wrong' }); },
  rows => { rows[0].postData = '{malformed'; },
  rows => { rows[0].postData = 'x'.repeat(4097); },
  rows => { rows[0].payload = { isAuthenticated: false, accessToken: token }; },
  rows => { rows[0].payload = { isAuthenticated: true, accessToken: 'not-a-jwt' }; },
  rows => { rows[0].status = 401; },
  rows => { rows[1].status = 403; },
  rows => { rows[1].payload = { count: 1, items: [{ typeName: 'Unrelated.Activity', version: 1 }] }; },
  rows => { rows[1].payload = { count: 2, items: ['SetOutput', 'Flowchart'].map(name =>
    ({ typeName: 'Elsa.' + name, namespace: 'Elsa', name, version: 2 })) }; },
  rows => { rows[1].payload = { count: 2, items: ['SetOutput', 'Flowchart'].map(name =>
    ({ typeName: 'Elsa.' + name, namespace: 'Other', name, version: 1 })) }; },
  rows => { rows[1].payload = { count: 3, items: [] }; },
  rows => { rows[1].payload = { count: true, items: [] }; },
  rows => { rows[1].body = async () => Buffer.from('{invalid'); },
  rows => { rows[1].responseHeaders['content-type'] = 'text/html'; },
  rows => { rows[1].responseHeaders['content-length'] = '4194305'; },
  rows => { rows[1].body = async () => Buffer.alloc(4194305); },
  rows => { rows[1].started = 0; },
  rows => { rows[1].url = backend + '/descriptors/activities?refresh=true&refresh=true'; },
  rows => { rows[1].url = studio + '/descriptors/activities?refresh=true'; },
  rows => { rows[1].method = 'POST'; },
  rows => { rows[0].fromMainFrame = false; },
  rows => { rows[1].resourceType = 'document'; },
];
for (const mutate of mutations) {
  const rows = fixtures(); mutate(rows);
  assert.equal(complete(await observe(rows)), false);
}

// Descriptor body parsing can complete before login body parsing. Relate private tokens after settling both.
const rows = fixtures();
let releaseLogin: () => void = () => {};
const gate = new Promise<void>(resolve => { releaseLogin = resolve; });
rows[0].body = async () => { await gate; return Buffer.from(JSON.stringify(rows[0].payload)); };
const observer = new DirectBackendObserver(studio, backend, username, password);
const loginRead = observer.observe(response(rows[0]), true);
await observer.observe(response(rows[1]), true);
assert.equal(complete(observer.proof()), false);
releaseLogin(); await loginRead;
assert.equal(complete(observer.proof()), true);
process.stdout.write('direct backend observer contracts passed\n');

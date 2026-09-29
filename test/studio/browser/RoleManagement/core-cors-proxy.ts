import { existsSync, lstatSync, readFileSync } from 'node:fs';
import { createServer, request as requestHttps } from 'node:https';

const allowedOrigin = required('ROLE_E2E_PROXY_ALLOWED_ORIGIN');
const upstream = localHttpsUrl(required('ROLE_E2E_PROXY_UPSTREAM'), 'ROLE_E2E_PROXY_UPSTREAM');
const listenUrl = localHttpsUrl(required('ROLE_E2E_PROXY_URL'), 'ROLE_E2E_PROXY_URL');
const certificatePath = required('ROLE_E2E_PROXY_PFX_PATH');
const certificatePassword = required('ROLE_E2E_PROXY_PFX_PASSWORD');
const upstreamCertificatePath = required('ROLE_E2E_PROXY_UPSTREAM_CERT_PATH');

assertRegularFile(certificatePath, 'ROLE_E2E_PROXY_PFX_PATH');
assertRegularFile(upstreamCertificatePath, 'ROLE_E2E_PROXY_UPSTREAM_CERT_PATH');
if (new URL(allowedOrigin).origin !== allowedOrigin || !isLoopback(new URL(allowedOrigin)))
  throw new Error('ROLE_E2E_PROXY_ALLOWED_ORIGIN must be an exact loopback origin.');

const server = createServer({ pfx: readFileSync(certificatePath), passphrase: certificatePassword }, (incoming, outgoing) => {
  const origin = incoming.headers.origin;
  if (origin && origin !== allowedOrigin) {
    outgoing.writeHead(403).end();
    return;
  }
  const incomingUrl = new URL(incoming.url ?? '/', 'https://loopback.invalid');
  if (!incomingUrl.pathname.startsWith('/elsa/api/')) {
    outgoing.writeHead(404).end();
    return;
  }
  if (incoming.method === 'OPTIONS') {
    outgoing.writeHead(204, corsHeaders(incoming.headers['access-control-request-headers'])).end();
    return;
  }

  const headers = { ...incoming.headers, host: upstream.host };
  delete headers.origin;
  const forwarded = requestHttps({
    hostname: upstream.hostname,
    port: Number(upstream.port),
    path: `${incomingUrl.pathname}${incomingUrl.search}`,
    method: incoming.method,
    headers,
    ca: readFileSync(upstreamCertificatePath)
  }, response => {
    const responseHeaders = { ...response.headers, ...corsHeaders() };
    outgoing.writeHead(response.statusCode ?? 502, responseHeaders);
    response.pipe(outgoing);
  });
  forwarded.on('error', () => outgoing.writeHead(502).end());
  incoming.pipe(forwarded);
});

server.listen(Number(listenUrl.port), listenUrl.hostname, () => {
  console.log(`RoleManagement HTTPS CORS proxy listening on ${listenUrl.origin}`);
});

function corsHeaders(requestHeaders?: string): Record<string, string> {
  return {
    'Access-Control-Allow-Origin': allowedOrigin,
    'Access-Control-Allow-Credentials': 'true',
    'Access-Control-Allow-Headers': requestHeaders ?? 'Authorization, Content-Type',
    'Access-Control-Allow-Methods': 'DELETE, GET, OPTIONS, POST, PUT',
    'Access-Control-Expose-Headers': '*',
    'Vary': 'Origin'
  };
}

function required(name: string): string {
  const value = process.env[name];
  if (!value)
    throw new Error(`Missing required proxy setting: ${name}`);
  return value;
}

function assertRegularFile(value: string, name: string): void {
  if (!existsSync(value) || !lstatSync(value).isFile() || lstatSync(value).isSymbolicLink())
    throw new Error(`${name} must point to a regular, non-symlink file.`);
}

function localHttpsUrl(value: string, name: string): URL {
  const parsed = new URL(value);
  if (parsed.protocol !== 'https:' || !parsed.port || !isLoopback(parsed))
    throw new Error(`${name} must be an HTTPS loopback URL with an explicit port.`);
  return parsed;
}

function isLoopback(url: URL): boolean {
  return ['localhost', '127.0.0.1', '[::1]', '::1'].includes(url.hostname);
}

import { createHash } from 'node:crypto';
import type { Request, Response } from '@playwright/test';

export const directBackendChecks = ['distinct_origins', 'login_request', 'login_cors', 'login_authenticated',
  'descriptor_request', 'descriptor_cors', 'bearer_matches_login', 'login_precedes_descriptor', 'activity_identity'] as const;
export type DirectBackendProof = {
  checks: Record<typeof directBackendChecks[number], boolean>;
  login_status?: number;
  descriptor_status?: number;
  descriptor_count?: number;
  descriptor_body_sha256?: string;
};
type Observation = { kind: 'login' | 'descriptors'; status: number; requestValid: boolean; cors: boolean;
  started: number; authorization: string; body: Buffer | undefined };
type BrowserResponse = Pick<Response, 'url' | 'status' | 'allHeaders' | 'body'> & {
  request(): Pick<Request, 'timing' | 'allHeaders' | 'resourceType' | 'postData' | 'method'>;
};
const maximumBodyBytes = 4 * 1024 * 1024;

function loopback(url: string): URL {
  const parsed = new URL(url);
  if (parsed.protocol !== 'http:' || parsed.hostname !== '127.0.0.1' || !parsed.port || parsed.username || parsed.password || parsed.search || parsed.hash)
    throw new Error('invalid_direct_backend_origin');
  return parsed;
}

// Only the caller's page response listener supplies observations. This class sends no requests.
// Keep credentials, tokens and raw bodies private; reduce them only after all body reads settle.
export class DirectBackendObserver {
  private readonly studio: URL;
  private readonly backend: URL;
  private readonly observations: Observation[] = [];

  constructor(studioUrl: string, backendUrl: string, private readonly username: string, private readonly password: string) {
    this.studio = loopback(studioUrl);
    this.backend = loopback(backendUrl);
  }

  async observe(response: BrowserResponse, fromMainFrame: boolean): Promise<void> {
    const url = new URL(response.url());
    if (url.origin !== this.backend.origin || url.hash) return;
    const prefix = this.backend.pathname.replace(/\/$/, '');
    const kind = url.pathname === prefix + '/identity/login' && !url.search ? 'login'
      : url.pathname === prefix + '/descriptors/activities' ? 'descriptors' : undefined;
    if (!kind || this.observations.length >= 64) return;
    const request = response.request();
    // Insert synchronously before reading headers/body: completion order is not request order.
    const row: Observation = { kind, status: response.status(), requestValid: false, cors: false,
      started: request.timing().startTime, authorization: '', body: undefined };
    this.observations.push(row);
    try {
      const headers = await request.allHeaders();
      const responseHeaders = await response.allHeaders();
      row.authorization = headers.authorization ?? '';
      row.cors = responseHeaders['access-control-allow-origin'] === this.studio.origin;
      const browserRequest = fromMainFrame && ['fetch', 'xhr'].includes(request.resourceType()) && headers.origin === this.studio.origin;
      if (kind === 'login') {
        const raw = request.postData();
        const credentials = raw && raw.length <= 4096 ? JSON.parse(raw) : undefined;
        row.requestValid = browserRequest && request.method() === 'POST' && !row.authorization &&
          headers['content-type']?.split(';')[0].trim() === 'application/json' &&
          credentials?.username === this.username && credentials?.password === this.password;
      } else {
        // Refit's request property is Refresh; backend query binding accepts its casing.
        const query = [...url.searchParams];
        row.requestValid = browserRequest && request.method() === 'GET' &&
          query.length === 1 && query[0][0].toLowerCase() === 'refresh' && query[0][1].toLowerCase() === 'true';
      }
      const length = responseHeaders['content-length'];
      if (responseHeaders['content-type']?.split(';')[0].trim() !== 'application/json' ||
          (length !== undefined && (!/^[0-9]+$/.test(length) || Number(length) > maximumBodyBytes))) return;
      const body = await response.body();
      if (body.length > 0 && body.length <= maximumBodyBytes) row.body = body;
    } catch { /* A failed, blocked or malformed response remains incomplete evidence. */ }
  }

  proof(): DirectBackendProof {
    const checks = Object.fromEntries(directBackendChecks.map(name => [name, false])) as DirectBackendProof['checks'];
    checks.distinct_origins = this.studio.origin !== this.backend.origin;
    const proof: DirectBackendProof = { checks };
    for (const login of this.observations.filter(row => row.kind === 'login')) {
      proof.login_status = login.status;
      checks.login_request = login.requestValid;
      checks.login_cors = login.cors;
      let token: string | undefined;
      try {
        const body = login.body && JSON.parse(login.body.toString('utf8'));
        if (login.status === 200 && body?.isAuthenticated === true && typeof body.accessToken === 'string' &&
            body.accessToken.length <= 16_384 && /^[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$/.test(body.accessToken))
          token = body.accessToken;
      } catch { /* Invalid JSON cannot authenticate the browser. */ }
      checks.login_authenticated = token !== undefined;
      proof.checks = { ...checks };
      for (const descriptor of this.observations.filter(row => row.kind === 'descriptors')) {
        const candidate = { ...checks, descriptor_request: descriptor.requestValid, descriptor_cors: descriptor.cors,
          bearer_matches_login: token !== undefined && descriptor.authorization === 'Bearer ' + token,
          login_precedes_descriptor: Number.isFinite(login.started) && login.started > 0 && descriptor.started > login.started,
          activity_identity: false };
        let count: number | undefined;
        try {
          const body = descriptor.body && JSON.parse(descriptor.body.toString('utf8'));
          if (descriptor.status === 200 && Array.isArray(body?.items) && Number.isSafeInteger(body.count) &&
              body.count === body.items.length && body.count > 0 && body.count <= 10_000) {
            count = body.count;
            candidate.activity_identity = ['SetOutput', 'Flowchart'].every(name => body.items.some((item: any) =>
              item?.typeName === 'Elsa.' + name && item.namespace === 'Elsa' && item.name === name && item.version === 1));
          }
        } catch { /* Invalid descriptor JSON cannot prove the registered activities. */ }
        delete proof.descriptor_count;
        delete proof.descriptor_body_sha256;
        Object.assign(proof, { checks: candidate, descriptor_status: descriptor.status });
        if (count !== undefined) proof.descriptor_count = count;
        if (descriptor.body) proof.descriptor_body_sha256 = createHash('sha256').update(descriptor.body).digest('hex');
        if (Object.values(candidate).every(Boolean)) return proof;
      }
    }
    return proof;
  }
}

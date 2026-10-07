import { createHash } from 'node:crypto';
import { WasmBootObserver, type ObservedBootResource, type WasmBootProof } from './wasm-boot.js';
import type { RawResourceAsset, RawResourceObservation } from './raw-resources.js';

export type HostedRoutePrefix = '' | 'compat';
export type HostedResource = ConstructorParameters<typeof WasmBootObserver>[0][number];
export type HostedDocument = {
  path: '/login' | '/compat/login' | null; status: number | null;
  content_type: 'text/html' | 'other' | null; base_href_sha256: string | null;
};
export type HostedDelivery = {
  route_prefix: HostedRoutePrefix; entry_path: '/login' | '/compat/login'; document: HostedDocument;
  resources: ObservedBootResource[]; boot: WasmBootProof | null; interactive_validation_observed: boolean;
  checks: { document: boolean; base: boolean; platform_resources: boolean; configuration: boolean;
    managed_resources: boolean; managed_callback: boolean; observations: boolean; navigation: boolean; cleanup: boolean };
  result: 'passed' | 'failed';
};
const hash = (body: Buffer | string) => createHash('sha256').update(body).digest('hex');
const require = (condition: unknown): void => { if (!condition) throw new Error('hosted_delivery_observation_invalid'); };
export const hostedEntryPath = (prefix: HostedRoutePrefix): '/login' | '/compat/login' => prefix === '' ? '/login' : '/compat/login';
export const emptyHostedDocument = (): HostedDocument => ({ path: null, status: null, content_type: null, base_href_sha256: null });
export function hostedBaseHash(value: string | null): string | null {
  return typeof value === 'string' && Buffer.byteLength(value, 'utf8') <= 2048 ? hash(value) : null;
}

// This projection accepts only an actual main-frame navigation response. Unknown
// origins/routes are represented as null, never serialized as arbitrary URLs.
export function hostedDocumentResponse(studioOrigin: string, prefix: HostedRoutePrefix, response: {
  url: string; main_frame_navigation: boolean; status: number; content_type: string | undefined;
}): HostedDocument {
  const document = emptyHostedDocument();
  if (!response.main_frame_navigation) return document;
  require(Number.isSafeInteger(response.status) && response.status >= 100 && response.status <= 599);
  document.status = response.status;
  const mime = response.content_type?.split(';')[0].trim().toLowerCase();
  document.content_type = mime === undefined ? null : mime === 'text/html' ? mime : 'other';
  const url = new URL(response.url);
  if (url.origin === studioOrigin && !url.username && !url.password && !url.search && !url.hash && url.pathname === hostedEntryPath(prefix))
    document.path = hostedEntryPath(prefix);
  return document;
}

export function failedHostedDelivery(prefix: HostedRoutePrefix): HostedDelivery {
  return { route_prefix: prefix, entry_path: hostedEntryPath(prefix), document: emptyHostedDocument(), resources: [], boot: null,
    interactive_validation_observed: false, checks: { document: false, base: false, platform_resources: false, configuration: false,
      managed_resources: false, managed_callback: false, observations: false, navigation: false, cleanup: false }, result: 'failed' };
}

// Actual paths stay in the portable rows. Only an explicitly inventoried exact
// root path or its single /compat alias maps to the parser's logical root path.
export class HostedDeliveryObserver {
  private readonly expected = new Map<string, RawResourceAsset>();
  private readonly canonical = new Map<string, HostedResource>();
  private readonly actualToCanonical = new Map<string, string>();
  private readonly actual = new Map<string, ObservedBootResource>();
  private readonly normalized = new Map<string, ObservedBootResource>();
  private readonly boot: WasmBootObserver;
  private invalid = false;
  private sealed = false;
  private observationsCompleted = false;

  constructor(resources: HostedResource[], framework: string, private readonly prefix: HostedRoutePrefix) {
    require((prefix === '' || prefix === 'compat') && Array.isArray(resources) && resources.length > 0 && resources.length <= 2048);
    for (const resource of resources) {
      require(/^\/[A-Za-z0-9_./-]+$/.test(resource.path) && resource.path.length <= 2048 &&
        !resource.path.split('/').includes('..') && !resource.path.startsWith('/compat/') && !this.canonical.has(resource.path) &&
        /^[a-f0-9]{64}$/.test(resource.sha256) && Number.isSafeInteger(resource.bytes) && resource.bytes >= 0 && resource.bytes <= 32 * 1024 * 1024 &&
        ['package', 'platform', 'fixture'].includes(resource.owner) && /^[a-z0-9.+-]+\/[a-z0-9.+-]+$/.test(resource.content_type));
      const canonical = { ...resource };
      this.canonical.set(resource.path, canonical);
      for (const path of prefix === '' ? [resource.path] : [resource.path, '/compat' + resource.path]) {
        this.expected.set(path, { path, sha256: canonical.sha256, bytes: canonical.bytes,
          content_type: canonical.content_type, owner: canonical.owner });
        this.actualToCanonical.set(path, resource.path);
      }
    }
    this.boot = new WasmBootObserver([...this.canonical.values()], framework);
  }

  expectedResources(): ReadonlyMap<string, RawResourceAsset> { return new Map(this.expected); }

  observe({ record, body }: RawResourceObservation): void {
    if (this.sealed) throw new Error('hosted_delivery_observation_closed');
    try {
      const logicalPath = this.actualToCanonical.get(record.path);
      const expected = logicalPath === undefined ? undefined : this.canonical.get(logicalPath);
      require(expected && record.status === 200 && record.requested === true && record.owner === expected.owner &&
        record.content_type === expected.content_type && record.sha256 === expected.sha256 && record.bytes === expected.bytes &&
        Buffer.isBuffer(body) && body.length === expected.bytes && hash(body) === expected.sha256);
      if (this.actual.has(record.path)) return; // Every duplicate was verified above.
      require(this.actual.size < 2048);
      const safe: ObservedBootResource = { path: record.path, status: 200, requested: true, owner: expected!.owner,
        content_type: expected!.content_type, sha256: expected!.sha256, bytes: expected!.bytes };
      const normalized = { ...safe, path: logicalPath! };
      this.actual.set(record.path, safe);
      this.normalized.set(logicalPath!, normalized);
      this.boot.observe(normalized, body); // Original entity bytes, never decoded or reconstructed.
    } catch { this.invalid = true; }
  }

  failObservations(): void { if (!this.sealed) this.invalid = true; }
  finishObservations(stopSucceeded: boolean): void {
    if (this.sealed) throw new Error('hosted_delivery_observation_closed');
    this.observationsCompleted = stopSucceeded === true && !this.invalid;
    this.sealed = true;
  }

  proof(document: HostedDocument, interactive: boolean, navigation: boolean, cleanup: boolean): HostedDelivery {
    const result = failedHostedDelivery(this.prefix);
    result.document = { ...document };
    result.resources = [...this.actual.values()].map(row => ({ ...row }));
    result.boot = this.boot.proof([...this.normalized.values()], interactive === true);
    result.interactive_validation_observed = interactive === true;
    const checks = result.checks;
    checks.document = document.path === result.entry_path && document.status === 200 && document.content_type === 'text/html';
    checks.base = document.base_href_sha256 === hash(this.prefix === '' ? '/' : '/compat/');
    checks.platform_resources = result.boot.checks.platform_resources === true;
    checks.configuration = result.boot.checks.configuration === true;
    checks.managed_resources = result.boot.checks.managed_resources === true;
    checks.managed_callback = result.boot.checks.managed_callback === true;
    checks.observations = this.sealed && this.observationsCompleted;
    checks.navigation = navigation === true;
    checks.cleanup = cleanup === true;
    result.result = Object.values(checks).every(Boolean) ? 'passed' : 'failed';
    return result;
  }
}

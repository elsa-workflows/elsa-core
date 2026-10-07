import { createHash } from 'node:crypto';

export type ResourceFailure = {
  path_sha256: string; status: number | null; phase: 'body' | 'observation';
  reason: 'resource_body_limit' | 'resource_body_size' | 'response_read_failed' | 'resource_observation_failed';
};
export type RawResourceAsset = {
  path: string; sha256: string; bytes: number; content_type: string; owner: 'package' | 'fixture' | 'platform';
};
export type RawResourceObservation = {
  record: RawResourceAsset & { status: number; requested: true }; body: Buffer;
};
export type PausedResource = {
  requestId: string; request: { url: string }; responseStatusCode?: number;
  responseErrorReason?: string; responseHeaders?: Array<{ name: string; value: string }>;
};
type Command = 'Network.setCacheDisabled' | 'Fetch.enable' | 'Fetch.getResponseBody' | 'Fetch.continueResponse' | 'Fetch.disable';
// A dedicated Playwright CDPSession satisfies this interface. Contracts inject a
// fake transport; the module never creates requests or fulfills responses.
export interface RawResourceSession {
  send(method: Command, parameters?: any): Promise<any>;
  on(event: 'Fetch.requestPaused', listener: (event: PausedResource) => void): unknown;
  off(event: 'Fetch.requestPaused', listener: (event: PausedResource) => void): unknown;
  detach(): Promise<void>;
}
export type RawResourceOptions = { commandTimeoutMs?: number; stopTimeoutMs?: number };
export type RawResourceObserver = { stop(): Promise<void> };
const maximumBytes = 32 * 1024 * 1024;
const hash = (value: Buffer | string) => createHash('sha256').update(value).digest('hex');
class Deadline extends Error {}

class Capture implements RawResourceObserver {
  private accepting = false;
  private closed = false;
  private enabled = false;
  private requiresContextCleanup = false;
  private cleanupFailed = false;
  private activeBodies = 0;
  private activeBytes = 0;
  private capturedResponses = 0;
  private stopping?: Promise<void>;
  private readonly pending = new Set<Promise<void>>();
  private readonly cancelTimers = new Set<() => void>();
  private readonly commandTimeout: number;
  private readonly stopTimeout: number;
  private origin = '';

  constructor(private session: RawResourceSession, private studioUrl: string,
    private assets: ReadonlyMap<string, RawResourceAsset>,
    private observed: (value: RawResourceObservation) => void,
    private failed: (value: ResourceFailure) => void, options: RawResourceOptions) {
    this.commandTimeout = options.commandTimeoutMs ?? 20_000;
    this.stopTimeout = options.stopTimeoutMs ?? 5_000;
  }

  async initialize(): Promise<void> {
    const studio = new URL(this.studioUrl);
    if (!['http:', 'https:'].includes(studio.protocol) || studio.username || studio.password || studio.search || studio.hash ||
        this.assets.size < 1 || this.assets.size > 4096 ||
        ![this.commandTimeout, this.stopTimeout].every(value => Number.isSafeInteger(value) && value > 0 && value <= 20_000))
      throw new Error('raw_resources_setup_failed');
    this.origin = studio.origin;
    const patterns: Array<{ urlPattern: string; requestStage: 'Response' }> = [];
    for (const [path, asset] of this.assets) {
      if (path !== asset.path || path.length > 2048 || !/^\/[A-Za-z0-9_./-]+$/.test(path) || path.split('/').includes('..') ||
          !Number.isSafeInteger(asset.bytes) || asset.bytes < 0 || asset.bytes > maximumBytes ||
          !/^[0-9a-f]{64}$/.test(asset.sha256) || !['package', 'fixture', 'platform'].includes(asset.owner) ||
          !/^[a-z0-9.+-]+\/[a-z0-9.+-]+$/.test(asset.content_type)) throw new Error('raw_resources_setup_failed');
      // '?' is a glob metacharacter in Fetch patterns; escape its literal form.
      // Query variants retain the previous observer's exact origin/path scope.
      for (const suffix of ['', '\\?*']) patterns.push({ urlPattern: this.origin + path + suffix, requestStage: 'Response' });
    }
    this.session.on('Fetch.requestPaused', this.onPaused);
    await this.command('Network.setCacheDisabled', { cacheDisabled: true });
    this.accepting = true;
    await this.command('Fetch.enable', { patterns });
    this.enabled = true;
  }

  private bounded<T>(operation: Promise<T>, milliseconds: number): Promise<T> {
    let cancel: () => void;
    let timer: ReturnType<typeof setTimeout>;
    const result = new Promise<T>((resolve, reject) => {
      cancel = () => reject(new Deadline('raw_resources_deadline'));
      timer = setTimeout(cancel, milliseconds);
      this.cancelTimers.add(cancel);
      operation.then(resolve, reject); // Also consume a late protocol rejection.
    });
    return result.finally(() => { clearTimeout(timer!); this.cancelTimers.delete(cancel!); });
  }

  private async command(method: Command, parameters?: any, timeout = this.commandTimeout): Promise<any> {
    try { return await this.bounded(this.session.send(method, parameters), timeout); }
    catch (error) {
      if (error instanceof Deadline) this.requiresContextCleanup = true;
      throw error;
    }
  }

  private readonly onPaused = (event: PausedResource): void => {
    if (this.closed || this.requiresContextCleanup) return;
    // Even continuation commands need a bound. An event flood is left to the
    // caller's owned context close, never converted into successful evidence.
    if (this.pending.size >= 512) { this.requiresContextCleanup = true; return; }
    const capture = this.accepting;
    const task = this.handle(event, capture).catch(() => { this.cleanupFailed = true; });
    this.pending.add(task);
    void task.then(() => this.pending.delete(task));
  };

  private report(path: string, status: number | null, reason: ResourceFailure['reason'], phase: ResourceFailure['phase'] = 'body'): void {
    if (this.closed) return;
    try { this.failed({ path_sha256: hash(path), status, phase, reason }); }
    catch { this.cleanupFailed = true; }
  }

  private async handle(event: PausedResource, capture: boolean): Promise<void> {
    let asset: RawResourceAsset | undefined;
    let body: Buffer | undefined;
    let contentType = '';
    let bodyPending = false;
    let continued = false;
    let reserved = false;
    const status = Number.isInteger(event.responseStatusCode) && event.responseStatusCode! >= 100 && event.responseStatusCode! <= 599
      ? event.responseStatusCode! : null;
    try {
      const url = new URL(event.request.url);
      asset = url.origin === this.origin ? this.assets.get(url.pathname) : undefined;
      if (!capture || !asset) return;
      if (++this.capturedResponses > 2048 || this.activeBodies >= 128 || this.activeBytes + asset.bytes > 128 * 1024 * 1024)
        throw new Error('resource_body_limit');
      if (url.username || url.password || url.hash || url.search.length > 2048 || status !== 200 || event.responseErrorReason)
        throw new Error('response_read_failed');
      const headers = event.responseHeaders ?? [];
      if (headers.length > 128 || headers.reduce((size, item) => size + item.name.length + item.value.length, 0) > 64 * 1024)
        throw new Error('resource_body_limit');
      const header = (name: string): string | undefined => {
        const entries = headers.filter(item => item.name.toLowerCase() === name);
        if (entries.length > 1) throw new Error('resource_body_limit');
        return entries[0]?.value;
      };
      const encoding = header('content-encoding');
      const declared = header('content-length');
      const wireLimit = encoding && encoding !== 'identity' ? maximumBytes : asset.bytes;
      if (encoding !== undefined && !['gzip', 'br', 'identity'].includes(encoding) ||
          declared !== undefined && (!/^[0-9]+$/.test(declared) || !Number.isSafeInteger(Number(declared)) || Number(declared) > wireLimit))
        throw new Error('resource_body_limit');
      contentType = header('content-type')?.split(';')[0].trim().toLowerCase() ?? '';
      if (contentType !== asset.content_type) throw new Error('response_read_failed');
      this.activeBodies++; this.activeBytes += asset.bytes; reserved = true;
      const operation = this.session.send('Fetch.getResponseBody', { requestId: event.requestId });
      bodyPending = true;
      const reading = operation.then(
        value => { bodyPending = false; return value; }, error => { bodyPending = false; throw error; });
      let result: any;
      try { result = await this.bounded(reading, this.commandTimeout); }
      catch (error) { if (bodyPending) this.requiresContextCleanup = true; throw error; }
      if (result?.base64Encoded !== true || typeof result.body !== 'string') throw new Error('response_read_failed');
      if (result.body.length > 4 * Math.ceil(maximumBytes / 3)) throw new Error('resource_body_limit');
      if (result.body.length !== 4 * Math.ceil(asset.bytes / 3)) throw new Error('resource_body_size');
      if (/[^A-Za-z0-9+/=]/.test(result.body))
        throw new Error('response_read_failed');
      // Fetch's BodyReader returns HTTP content-decoded entity bytes as base64.
      // No character decoder, BOM repair, decompression or response replacement.
      body = Buffer.from(result.body, 'base64');
      if (body.toString('base64') !== result.body) throw new Error('response_read_failed');
      if (body.length !== asset.bytes) throw new Error('resource_body_size');
      if (hash(body) !== asset.sha256) throw new Error('response_read_failed');
    } catch (error) {
      if (asset) {
        const reason = error instanceof Error && ['resource_body_limit', 'resource_body_size'].includes(error.message)
          ? error.message as ResourceFailure['reason'] : 'response_read_failed';
        this.report(asset.path, status, reason);
      }
      body = undefined;
    } finally {
      // A timed-out unresolved body read cannot safely be continued or disabled.
      // stop() rejects and the caller closes its owned context/browser instead.
      if (!bodyPending && !this.closed && !this.requiresContextCleanup) {
        try { await this.command('Fetch.continueResponse', { requestId: event.requestId }); continued = true; }
        catch { this.cleanupFailed = true; if (asset) this.report(asset.path, status, 'response_read_failed'); }
      }
      if (reserved) { this.activeBodies--; this.activeBytes -= asset!.bytes; }
    }
    if (asset && body && continued && !this.closed) {
      try {
        this.observed({ record: { path: asset.path, status: status!, content_type: contentType,
          sha256: hash(body), bytes: body.length, owner: asset.owner, requested: true }, body });
      } catch { this.report(asset.path, status, 'resource_observation_failed', 'observation'); }
    }
  }

  stop(): Promise<void> { return this.stopping ??= this.shutdown(); }

  private close(): void {
    if (this.closed) return;
    this.closed = true;
    try { this.session.off('Fetch.requestPaused', this.onPaused); }
    catch { this.cleanupFailed = true; }
    for (const cancel of this.cancelTimers) cancel();
  }

  private async drain(deadline: number): Promise<void> {
    while (this.pending.size) {
      const remaining = deadline - Date.now();
      if (remaining <= 0) throw new Deadline('raw_resources_deadline');
      await this.bounded(Promise.all([...this.pending]), remaining);
    }
  }

  private async shutdown(): Promise<void> {
    this.accepting = false;
    const deadline = Date.now() + this.stopTimeout;
    try {
      await this.drain(deadline);
    } catch { this.requiresContextCleanup = true; }
    // Never disable Fetch/detach while a body API may still be reading. That is
    // undefined by CDP; a failed drain requires the caller's context close.
    if (!this.requiresContextCleanup) {
      try { if (this.enabled) await this.command('Fetch.disable', undefined, Math.max(1, deadline - Date.now())); }
      catch { this.cleanupFailed = true; }
      try {
        await this.drain(deadline);
      } catch { this.requiresContextCleanup = true; }
      this.close();
      if (!this.requiresContextCleanup) {
        try { await this.bounded(this.session.detach(), Math.max(1, deadline - Date.now())); }
        catch { this.cleanupFailed = true; }
      }
    }
    this.close();
    if (this.requiresContextCleanup || this.cleanupFailed) throw new Error('raw_resources_cleanup_failed');
  }
}

/** Observe allowlisted native responses with an owned, dedicated CDP session. */
export async function startRawResources(session: RawResourceSession, studioUrl: string,
  assets: ReadonlyMap<string, RawResourceAsset>, observed: (value: RawResourceObservation) => void,
  failed: (value: ResourceFailure) => void, options: RawResourceOptions = {}): Promise<RawResourceObserver> {
  const observer = new Capture(session, studioUrl, assets, observed, failed, options);
  try { await observer.initialize(); return observer; }
  catch {
    try { await observer.stop(); } catch { /* Caller still closes its context on setup failure. */ }
    throw new Error('raw_resources_setup_failed');
  }
}

import { expect as playwrightExpect, type Page, type Response } from '@playwright/test';
import { createHash } from 'node:crypto';

const expect = playwrightExpect.configure({ timeout: 20_000 });
const hash = (value: string) => createHash('sha256').update(value).digest('hex');
export const embeddingChecks = ['backend_configured', 'native_authentication', 'list_callback', 'activity_callback',
  'version_callback', 'execution_callback', 'instance_list_callback', 'instance_viewer'] as const;
export type EmbeddingProof = { checks: Record<typeof embeddingChecks[number], boolean>; definition_id_sha256?: string;
  activity_id_sha256?: string; version_id_sha256?: string; instance_id_sha256?: string };
type CallbackKind = 'definition' | 'activity' | 'version' | 'execution' | 'instance';
type CallbackValue = { id: string; definitionId?: string };
type ElementKind = 'definition-list' | 'definition-editor' | 'instance-list' | 'instance-viewer';

// These are native wrapper parameter names. The three additive EventCallbacks
// bridge the package editor's public Func callbacks without changing its bytes.
export const elementCallbacks: Record<ElementKind, Partial<Record<string, CallbackKind>>> = {
  'definition-list': { editWorkflowDefinition: 'definition' },
  'definition-editor': { activitySelectionChanged: 'activity', definitionVersionSelected: 'version', definitionExecuted: 'execution' },
  'instance-list': { viewWorkflowInstance: 'instance' },
  'instance-viewer': { editWorkflowDefinition: 'definition' }
};

export function nativeCallbackValue(kind: CallbackKind, value: unknown): CallbackValue {
  const id = (item: unknown): string => {
    if (typeof item !== 'string' || !/^[0-9a-f]{1,64}$/.test(item)) throw new Error('native_callback_identity_invalid');
    return item;
  };
  if (['definition', 'execution', 'instance'].includes(kind)) return { id: id(value) };
  if (!value || typeof value !== 'object' || Array.isArray(value)) throw new Error('native_callback_payload_invalid');
  const object = value as Record<string, unknown>;
  return kind === 'activity' ? { id: id(object.id) } : { id: id(object.id), definitionId: id(object.definitionId) };
}

export function bindNativeCallback(kind: CallbackKind, value: CallbackValue, expected: { id: string; definitionId?: string }, proof: EmbeddingProof): void {
  if (value.id !== expected.id || (kind === 'version' && value.definitionId !== expected.definitionId))
    throw new Error('native_callback_backend_mismatch');
  const check = { definition: 'list_callback', activity: 'activity_callback', version: 'version_callback', execution: 'execution_callback', instance: 'instance_list_callback' } as const;
  proof.checks[check[kind]] = true;
  if (kind === 'definition') proof.definition_id_sha256 = hash(value.id);
  if (kind === 'activity') proof.activity_id_sha256 = hash(value.id);
  if (kind === 'version') proof.version_id_sha256 = hash(value.id);
  if (kind === 'execution') proof.instance_id_sha256 = hash(value.id);
}

export function nativeAuthenticationRequest(observation: { nativeFrame: boolean; configured: boolean; url: string; status: number; method: string; authorization: string | null }, backendUrl: string, token: string): boolean {
  const url = new URL(observation.url), base = new URL(backendUrl);
  const relative = url.pathname.startsWith(base.pathname + '/') ? url.pathname.slice(base.pathname.length) : '';
  return observation.nativeFrame && observation.configured && observation.status === 200 && observation.method === 'GET' &&
    url.origin === base.origin && (relative === '/workflow-definitions' || relative.startsWith('/workflow-definitions/')) &&
    observation.authorization === 'Bearer ' + token;
}

/** An embedding consumer of real registered custom elements; all callback payloads remain private. */
export class NativeCustomElements {
  readonly proof: EmbeddingProof = { checks: Object.fromEntries(embeddingChecks.map(key => [key, false])) as EmbeddingProof['checks'] };
  private generation = 0;
  private values = new Map<CallbackKind, CallbackValue>();
  private invalidCallback = false;
  private definitionId?: string;
  constructor(private page: Page, private studioUrl: string, private backendUrl: string, private accessToken: string) {}

  async initialize(): Promise<void> {
    await this.page.exposeBinding('__pairedNativeCallback', (source, generation: number, kind: CallbackKind, payload: unknown) => {
      if (source.frame !== this.page.mainFrame() || generation !== this.generation) return;
      try {
        if (!['definition', 'activity', 'version', 'execution', 'instance'].includes(kind)) throw new Error('native_callback_kind');
        this.values.set(kind, nativeCallbackValue(kind, payload));
      } catch { this.invalidCallback = true; }
    });
    await this.page.goto(this.studioUrl);
    await expect(this.page.locator('#paired-custom-elements-root')).toHaveCount(1);
  }

  async observeResponse(response: Response): Promise<void> {
    let native = false;
    try { native = response.request().frame() === this.page.mainFrame(); } catch { return; }
    if (nativeAuthenticationRequest({ nativeFrame: native, configured: this.proof.checks.backend_configured,
      url: response.url(), status: response.status(), method: response.request().method(),
      authorization: await response.request().headerValue('authorization') }, this.backendUrl, this.accessToken))
      this.proof.checks.native_authentication = true;
  }

  async mount(kind: ElementKind, id?: string): Promise<void> {
    this.values.clear();
    const generation = ++this.generation;
    const tag = 'elsa-workflow-' + kind;
    await this.page.waitForFunction(tag => customElements.get(tag) !== undefined, tag);
    await this.page.evaluate(({ tag, kind, id, endpoint, token, callbacks, generation }) => {
      const root = document.querySelector('#paired-custom-elements-root');
      if (!root || document.querySelector('elsa-studio-workflow-definition-editor')) throw new Error('native_mount_root_invalid');
      const element = document.createElement(tag) as HTMLElement & Record<string, unknown>;
      // CustomElements queues its root creation as a microtask. Supply public
      // properties synchronously, before append, so OnInitialized sees auth.
      element.remoteEndpoint = endpoint;
      element.accessToken = token;
      if (kind === 'definition-editor') element.definitionId = id;
      if (kind === 'instance-viewer') element.instanceId = id;
      for (const [parameter, callback] of Object.entries(callbacks))
        element[parameter] = (value: unknown) => (window as any).__pairedNativeCallback(generation, callback, value);
      root.replaceChildren(element);
    }, { tag, kind, id, endpoint: this.backendUrl, token: this.accessToken, callbacks: elementCallbacks[kind], generation });
    this.proof.checks.backend_configured = true;
    if (kind === 'definition-editor') this.definitionId = id;
  }

  async definitions(): Promise<void> {
    await this.mount('definition-list');
    await expect(this.page.getByRole('button', { name: 'Create workflow', exact: true })).toBeVisible();
    // The real table performs paging normalization after its awaited API reads.
    await this.page.waitForURL(url => url.searchParams.get('page') === '1' && url.searchParams.get('pageSize') === '10');
    await expect.poll(() => this.proof.checks.native_authentication).toBe(true);
  }

  forgetCallback(kind: CallbackKind): void { this.values.delete(kind); }

  async callback(kind: CallbackKind): Promise<CallbackValue> {
    await expect.poll(() => this.invalidCallback ? 'invalid' : this.values.has(kind) ? 'observed' : 'pending').toBe('observed');
    return this.values.get(kind)!;
  }

  async editedDefinition(expectedId?: string): Promise<string> {
    const value = await this.callback('definition');
    if (expectedId !== undefined && value.id !== expectedId) throw new Error('native_definition_callback_mismatch');
    await this.mount('definition-editor', value.id);
    return value.id;
  }

  async reloadEditor(): Promise<void> {
    if (!this.definitionId) throw new Error('native_editor_identity_missing');
    await this.page.reload();
    await this.mount('definition-editor', this.definitionId);
  }

  async runInstance(): Promise<string> {
    return (await this.callback('execution')).id;
  }

  async viewer(instanceId: string): Promise<void> { await this.mount('instance-viewer', instanceId); }

  async instanceList(instanceId: string): Promise<void> {
    await this.mount('instance-list');
    const row = this.page.locator('.instances-table tbody tr').filter({ has: this.page.getByText(instanceId, { exact: true }) });
    await expect(row).toHaveCount(1);
    await row.getByText(instanceId, { exact: true }).click();
    bindNativeCallback('instance', await this.callback('instance'), { id: instanceId }, this.proof);
    await this.viewer(instanceId);
    await expect(this.page.getByText('Finished', { exact: true }).first()).toBeVisible();
    this.proof.checks.instance_viewer = true;
  }
}

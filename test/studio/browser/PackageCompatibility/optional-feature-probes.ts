import { expect as playwrightExpect, type Locator, type Page, type Request, type Response, type WebSocket } from '@playwright/test';
import { createHash } from 'node:crypto';

const expect = playwrightExpect.configure({ timeout: 20_000 });
const hash = (value: string | Buffer) => createHash('sha256').update(value).digest('hex');
const forbidden = "You don't have permission to do this. Ask an administrator for access.";
export const optionalFeatureScenarios = ['without-secrets', 'without-workflow-contexts', 'deny-secrets',
  'deny-workflow-contexts', 'disconnect'] as const;
export type OptionalFeatureScenario = typeof optionalFeatureScenarios[number];
export type ProbeCell = { version: string; framework: string; host: 'server' | 'wasm' | 'hosted-wasm' | 'custom-elements' };
export type ProbeInput = { cell: ProbeCell; scenario: OptionalFeatureScenario; backend_features: string[];
  permission_profile: string; definition_name: string; backend_url: string };
export type NativeProbeAdapter = {
  authenticateAndList(): Promise<void>;
  createWorkflow(name: string): Promise<string>;
  addSetOutput(): Promise<unknown>;
  openOutputSyntaxMenu(): Promise<void>;
  inputControl(label: RegExp): Locator;
  /** Parent observes its native-traffic boundary before allowing the actual UI action. */
  beforeNativeAction?(): Promise<void>;
  /** Resolves only after the parent validates readiness, stops its owned backend and acknowledges. */
  disconnect?(): Promise<void>;
};
type Endpoint = 'secrets-descriptors' | 'secrets-picker' | 'workflow-context-descriptors';
export type ProbeRequest = { endpoint: Endpoint; method: 'GET' | 'POST'; source: 'browser-native' | 'backend-native';
  status: number | null; transport_failed: boolean; after_action: boolean; after_disconnect_ack: boolean;
  body: { bytes: number; sha256: string; sensitive_items_present: boolean | null } | null };
const uiFlags = ['secret_syntax_visible', 'secret_navigation_visible', 'context_heading_visible',
  'synthetic_checkbox_visible', 'picker_empty', 'inline_create_visible', 'authorization_guidance_visible',
  'error_visible', 'editor_visible', 'server_circuit_closed'] as const;
type Ui = Record<typeof uiFlags[number], boolean | null> & { page_closed: boolean; page_error_count: number };
export type OptionalFeatureReceipt = {
  schema: 1; cell: ProbeCell; scenario: OptionalFeatureScenario; backend_features: string[]; permission_profile: string;
  checks: { authenticated: boolean; native_workflow_created: boolean; native_action: boolean; observation_completed: boolean };
  hashes: { probe_definition_id_sha256?: string }; ui: Ui; requests: ProbeRequest[];
  disconnect: { child_ready: boolean; parent_acknowledged: boolean; native_action_after_ack: boolean } | null;
  failure_category: 'probe_execution_failed' | null;
};

/** Ignore retired navigation sockets; retain loss of the current circuit during the probe action. */
export class NativeCircuitObservation {
  private current?: object;
  private currentClosed = false;
  private actionStarted = false;
  private lostDuringAction = false;

  connected(socket: object): void {
    this.current = socket;
    this.currentClosed = false;
  }

  disconnected(socket: object): void {
    if (socket !== this.current) return;
    this.currentClosed = true;
    if (this.actionStarted) this.lostDuringAction = true;
  }

  beginAction(): void {
    this.actionStarted = true;
    if (this.currentClosed) this.lostDuringAction = true;
  }

  closed(): boolean | null {
    return this.current === undefined ? null : this.currentClosed || this.lostDuringAction;
  }
}

export function optionalFeatureProfile(scenario: OptionalFeatureScenario): { backend_features: string[]; permission_profile: string } {
  if (!optionalFeatureScenarios.includes(scenario)) throw new Error('invalid_optional_feature_scenario');
  return { backend_features: scenario === 'without-secrets' ? ['workflow-contexts'] :
    scenario === 'without-workflow-contexts' ? ['secrets'] : ['workflow-contexts', 'secrets'],
  permission_profile: scenario === 'deny-secrets' || scenario === 'deny-workflow-contexts' ? scenario : 'full' };
}

function endpoint(url: string, backend: URL, method: string): Endpoint | undefined {
  const value = new URL(url);
  if (value.origin !== backend.origin || value.username || value.password || value.search || value.hash) return;
  const routes: [string, string, Endpoint][] = [['/secrets/descriptors', 'GET', 'secrets-descriptors'],
    ['/secrets/picker', 'POST', 'secrets-picker'], ['/workflow-contexts/provider-descriptors', 'GET', 'workflow-context-descriptors']];
  return routes.find(([path, verb]) => value.pathname === backend.pathname.replace(/\/$/, '') + path && method === verb)?.[2];
}

export function classifyOptionalFeatureBody(target: Endpoint, bytes: Buffer): ProbeRequest['body'] {
  if (!Number.isSafeInteger(bytes.length) || bytes.length > 65536) throw new Error('optional_feature_body_limit');
  let items: boolean | null = bytes.length === 0 ? false : null;
  try {
    const value: unknown = JSON.parse(bytes.toString('utf8'));
    if (value && typeof value === 'object' && !Array.isArray(value)) {
      const body = value as Record<string, unknown>;
      const fields = target === 'secrets-descriptors' ? ['types', 'stores'] : ['items'];
      if (fields.some(field => Array.isArray(body[field]) && (body[field] as unknown[]).length > 0)) items = true;
      else {
        // Only exact empty descriptor envelopes prove absence. An unknown error
        // object, including {}, cannot certify the absence of sensitive payload.
        const keys = Object.keys(body).sort().join(',');
        if (target === 'secrets-descriptors' && keys === 'stores,types' && Array.isArray(body.types) &&
            Array.isArray(body.stores) && body.types.length === 0 && body.stores.length === 0) items = false;
        if (target === 'workflow-context-descriptors' && keys === 'count,items' && Array.isArray(body.items) &&
            body.items.length === 0 && body.count === 0) items = false;
        if (target === 'secrets-picker' && keys === 'canCreateInline,items' && Array.isArray(body.items) &&
            body.items.length === 0 && typeof body.canCreateInline === 'boolean') items = false;
      }
    }
  } catch { /* No raw response body or exception text enters evidence. */ }
  return { bytes: bytes.length, sha256: hash(bytes), sensitive_items_present: items };
}

/** Native UI only. Server HTTP evidence and process ownership remain with the parent. */
export async function runOptionalFeatureProbe(page: Page, input: ProbeInput, adapter: NativeProbeAdapter): Promise<OptionalFeatureReceipt> {
  const profile = optionalFeatureProfile(input.scenario);
  if (input.cell.version !== '3.10.0' || !['net8.0', 'net9.0', 'net10.0'].includes(input.cell.framework) ||
      !['server', 'wasm', 'hosted-wasm', 'custom-elements'].includes(input.cell.host) ||
      JSON.stringify(input.backend_features) !== JSON.stringify(profile.backend_features) ||
      input.permission_profile !== profile.permission_profile || !/^paired-browser-[0-9a-f]{12}$/.test(input.definition_name))
    throw new Error('invalid_optional_feature_profile');
  const backend = new URL(input.backend_url);
  if (backend.protocol !== 'http:' || !['127.0.0.1', 'localhost', '[::1]'].includes(backend.hostname) ||
      backend.username || backend.password || backend.search || backend.hash || backend.pathname !== '/elsa/api')
    throw new Error('invalid_optional_feature_backend');
  const receipt: OptionalFeatureReceipt = { schema: 1,
    cell: { version: input.cell.version, framework: input.cell.framework, host: input.cell.host }, scenario: input.scenario,
    ...profile, checks: { authenticated: false, native_workflow_created: false, native_action: false, observation_completed: false },
    hashes: {}, ui: { ...Object.fromEntries(uiFlags.map(name => [name, null])) as Record<typeof uiFlags[number], null>,
      page_closed: false, page_error_count: 0 }, requests: [], disconnect: input.scenario === 'disconnect' ?
      { child_ready: false, parent_acknowledged: false, native_action_after_ack: false } : null, failure_category: null };
  let active = true, overflow = false;
  const circuit = new NativeCircuitObservation();
  const pending: Promise<void>[] = [];
  // Only actual pageerror events increment this count; navigation/socket close events do not.
  const errors = () => { if (active) { if (receipt.ui.page_error_count < 64) receipt.ui.page_error_count++; else overflow = true; } };
  const closed = () => { if (active) receipt.ui.page_closed = true; };
  const sockets = new Map<WebSocket, () => void>();
  const websocket = (socket: WebSocket) => {
    try {
      const url = new URL(socket.url());
      if (input.cell.host === 'server' && url.origin.replace(/^ws/, 'http') === new URL(page.url()).origin && url.pathname.endsWith('/_blazor')) {
        circuit.connected(socket);
        const socketClosed = () => { if (active) circuit.disconnected(socket); };
        socket.on('close', socketClosed);
        sockets.set(socket, socketClosed);
      }
    } catch { /* An unrelated or malformed socket cannot establish a Studio circuit. */ }
  };
  const response = (value: Response) => {
    if (!active || input.cell.host === 'server') return;
    let target: Endpoint | undefined;
    try { if (value.request().frame() !== page.mainFrame()) return; target = endpoint(value.url(), backend, value.request().method()); } catch { return; }
    if (!target) return;
    if (receipt.requests.length >= 64) { overflow = true; return; }
    const row: ProbeRequest = { endpoint: target, method: target === 'secrets-picker' ? 'POST' : 'GET', source: 'browser-native',
      status: value.status(), transport_failed: false, after_action: receipt.checks.native_action,
      after_disconnect_ack: receipt.disconnect?.parent_acknowledged === true, body: null };
    receipt.requests.push(row);
    const read = (async () => {
      try {
        const length = value.headers()['content-length'];
        if (length !== undefined && (!/^\d+$/.test(length) || Number(length) > 65536)) { overflow = true; return; }
        const bytes = await value.body();
        if (bytes.length > 65536) { overflow = true; return; }
        if (active) row.body = classifyOptionalFeatureBody(target!, bytes);
      } catch { /* Preserve actual response status; unreadable bodies cannot prove payload absence. */ }
    })();
    pending.push(read);
  };
  const requestFailed = (value: Request) => {
    if (!active || input.cell.host === 'server') return;
    let target: Endpoint | undefined;
    try { if (value.frame() !== page.mainFrame()) return; target = endpoint(value.url(), backend, value.method()); } catch { return; }
    if (!target) return;
    if (receipt.requests.length >= 64) { overflow = true; return; }
    receipt.requests.push({ endpoint: target, method: target === 'secrets-picker' ? 'POST' : 'GET', source: 'browser-native',
      status: null, transport_failed: true, after_action: receipt.checks.native_action,
      after_disconnect_ack: receipt.disconnect?.parent_acknowledged === true, body: null });
  };
  const editor = page.locator('.flowchart-diagram-designer-wrapper .x6-graph-svg');
  const guidance = page.getByText(forbidden, { exact: true });
  const error = page.locator('.mud-alert-error, .mud-alert-warning, .mud-snackbar-error');
  const visible = async (locator: Locator) => (await locator.count()) > 0 && await locator.first().isVisible();
  const snapshot = async () => {
    receipt.ui.page_closed = page.isClosed();
    receipt.ui.server_circuit_closed = input.cell.host === 'server' ? circuit.closed() : null;
    if (page.isClosed()) return;
    receipt.ui.authorization_guidance_visible = await visible(guidance);
    receipt.ui.error_visible = await visible(error);
    receipt.ui.editor_visible = await visible(editor);
  };
  page.on('pageerror', errors); page.on('close', closed); page.on('websocket', websocket);
  page.on('response', response); page.on('requestfailed', requestFailed);
  const beginAction = async () => {
    await adapter.beforeNativeAction?.();
    receipt.checks.native_action = true;
    circuit.beginAction();
  };
  try {
    await adapter.authenticateAndList();
    receipt.checks.authenticated = true;
    if (input.cell.host !== 'custom-elements') receipt.ui.secret_navigation_visible = await visible(page.getByRole('link', { name: 'Secrets', exact: true }));
    // Candidate Properties is the initial tab; contexts may fail during editor entry itself.
    if (input.scenario === 'deny-workflow-contexts') await beginAction();
    const id = await adapter.createWorkflow(input.definition_name + '-' + input.scenario);
    if (typeof id !== 'string' || !/^[0-9a-f]{1,64}$/.test(id)) throw new Error('invalid_optional_feature_workflow');
    receipt.hashes.probe_definition_id_sha256 = hash(id);
    receipt.checks.native_workflow_created = true;
    if (input.scenario === 'deny-workflow-contexts') {
      await expect.poll(async () => page.isClosed() || receipt.ui.page_error_count > 0 || circuit.closed() === true || await visible(guidance) || await visible(error)).toBe(true);
      if (!page.isClosed()) {
        receipt.ui.context_heading_visible = await visible(page.getByText('Workflow Context', { exact: true }));
        receipt.ui.synthetic_checkbox_visible = await visible(page.getByRole('checkbox', { name: 'Synthetic', exact: true }));
      }
    } else {
      await expect(editor).toBeVisible();
      await page.getByRole('tab', { name: 'Properties', exact: true }).click();
      const present = input.scenario !== 'without-workflow-contexts';
      const checkbox = page.getByRole('checkbox', { name: 'Synthetic', exact: true });
      const heading = page.getByText('Workflow Context', { exact: true });
      if (present) { await expect(heading).toBeVisible(); await expect(checkbox).toBeVisible(); }
      else { await expect(heading).toHaveCount(0); await expect(checkbox).toHaveCount(0); }
      receipt.ui.context_heading_visible = present;
      receipt.ui.synthetic_checkbox_visible = present;
      await adapter.addSetOutput();
      await adapter.openOutputSyntaxMenu();
      const secret = page.locator('.studio-expression-input-menu-item:visible').filter({ hasText: /^Secret$/ });
      const secretPresent = input.scenario !== 'without-secrets';
      await expect(secret).toHaveCount(secretPresent ? 1 : 0);
      receipt.ui.secret_syntax_visible = secretPresent;
      if (receipt.disconnect) {
        if (!adapter.disconnect) throw new Error('missing_owned_disconnect');
        receipt.disconnect.child_ready = true;
        await adapter.disconnect();
        receipt.disconnect.parent_acknowledged = true;
      }
      await beginAction();
      if (secretPresent) {
        if (receipt.disconnect) receipt.disconnect.native_action_after_ack = true;
        await secret.click();
        const picker = adapter.inputControl(/^Output Value$/i);
        const create = picker.locator('xpath=ancestor::div[contains(concat(" ", normalize-space(@class), " "), " mud-stack ")][1]').locator('.mud-tooltip-root button');
        if (input.scenario === 'deny-secrets') await expect(guidance.first()).toBeVisible();
        else if (input.scenario === 'disconnect') await expect(error.first()).toBeVisible();
        else await expect(create).toHaveCount(1);
        await expect(picker).toBeVisible();
        receipt.ui.inline_create_visible = await visible(create);
        // Opening the actual dropdown tests its items, rather than equating an unselected value with an empty inventory.
        await picker.click();
        receipt.ui.picker_empty = await page.locator('[role="option"]:visible').count() === 0;
        await page.keyboard.press('Escape');
      } else await page.keyboard.press('Escape');
    }
    await snapshot();
    receipt.checks.observation_completed = true;
  } catch {
    receipt.failure_category = 'probe_execution_failed';
    await snapshot().catch(() => {});
    // An adapter may observe an editor failure before returning its definition ID.
    // Retain the native denial outcome; it is assessed as a defect, never silently passed.
    if (input.scenario === 'deny-workflow-contexts' && receipt.checks.authenticated && receipt.checks.native_action &&
        (receipt.ui.error_visible || receipt.ui.page_closed || receipt.ui.page_error_count > 0 || circuit.closed() === true)) {
      if (!page.isClosed()) {
        receipt.ui.context_heading_visible = await visible(page.getByText('Workflow Context', { exact: true })).catch(() => null);
        receipt.ui.synthetic_checkbox_visible = await visible(page.getByRole('checkbox', { name: 'Synthetic', exact: true })).catch(() => null);
      }
      receipt.checks.observation_completed = true;
      receipt.failure_category = null;
    }
  } finally {
    page.off('response', response); page.off('requestfailed', requestFailed);
    let timer: ReturnType<typeof setTimeout> | undefined;
    const drained = await Promise.race([Promise.all(pending).then(() => true), new Promise<false>(resolve => { timer = setTimeout(() => resolve(false), 5000); })]);
    if (timer !== undefined) clearTimeout(timer);
    if (!drained || overflow) { receipt.failure_category = 'probe_execution_failed'; receipt.checks.observation_completed = false; }
    active = false;
    page.off('pageerror', errors); page.off('close', closed); page.off('websocket', websocket);
    for (const [socket, socketClosed] of sockets) socket.off('close', socketClosed);
  }
  return receipt;
}

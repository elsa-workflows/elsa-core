import { chromium, expect, request, type Page, type APIRequestContext } from '@playwright/test';
import { createHash } from 'node:crypto';
import { readFileSync } from 'node:fs';
const policy = JSON.parse(readFileSync(new URL('./coverage-policy.json', import.meta.url), 'utf8'));

type Cell = { host: 'server' | 'wasm' | 'hosted-wasm' | 'custom-elements'; framework: string; version: string; route_prefix?: string };
type Resource = { path: string; sha256: string; bytes: number; content_type: string; owner: 'package' | 'fixture' };
type PrivateInput = { request: Cell; studio_url: string; backend_url: string; username: string; password: string; safe_ids: Record<string, string>; resources: Resource[] };
type Assertion = { name: string; passed: boolean; reason_category: string | null };
const baseline: string[] = policy.baseline;
const candidate: string[] = policy.candidate;
const hostAssertions: Record<Cell['host'], string[]> = policy.host_assertions;
const hash = (data: Buffer | string): string => createHash('sha256').update(data).digest('hex');

function loopback(value: string): string {
  const url = new URL(value);
  if (!['http:', 'https:'].includes(url.protocol) || !['localhost', '127.0.0.1', '[::1]'].includes(url.hostname) || url.username || url.password || url.search || url.hash)
    throw new Error('invalid_runtime_url');
  return value.replace(/\/+$/, '');
}

async function toolbar(page: Page, title: string): Promise<void> {
  // MudTooltip gives the package toolbar icon its accessible description.
  const button = page.getByRole('button', { name: title, exact: true });
  if (await button.count() === 1) {
    await button.click();
    return;
  }
  const titles = page.locator(`[title="${title}"], [aria-label="${title}"]`);
  if (await titles.count() === 1) {
    await titles.click();
    return;
  }
  // Package versions without accessible icon labels still expose an actual tooltip.
  for (const icon of await page.locator('.mud-tooltip-root button').all()) {
    await icon.hover();
    const text = page.locator('.mud-tooltip').filter({ hasText: new RegExp('^' + title + '$') });
    if (await text.isVisible()) {
      await icon.click();
      return;
    }
  }
  throw new Error('toolbar_control_unavailable');
}

class Backend {
  private constructor(private api: APIRequestContext, private base: string, private token: string) {}
  static async login(base: string, actor: PrivateInput): Promise<Backend> {
    const api = await request.newContext();
    const response = await api.post(base + '/identity/login', { data: { username: actor.username, password: actor.password } });
    const body = await response.json();
    if (response.status() !== 200 || body.isAuthenticated !== true || typeof body.accessToken !== 'string') {
      await api.dispose();
      throw new Error('backend_authentication_failed');
    }
    return new Backend(api, base, body.accessToken);
  }
  async get(path: string): Promise<any> {
    const response = await this.api.get(this.base + path, { headers: { Authorization: 'Bearer ' + this.token } });
    if (response.status() !== 200)
      throw new Error('backend_read_failed');
    return response.json();
  }
  async dispose(): Promise<void> { await this.api.dispose(); }
}

async function fullShell(page: Page, input: PrivateInput, backend: Backend, passed: (name: string) => void, proof: Record<string, unknown>, circuitFrames: () => number): Promise<void> {
  await page.goto(input.studio_url + '/login');
  proof.last_completed_stage = 'login_navigation';
  const username = page.getByLabel(input.request.version === '3.8.4' ? 'Username' : 'User name', { exact: true });
  await expect(username).toBeVisible();
  if (input.request.host === 'server') await expect.poll(circuitFrames).toBeGreaterThanOrEqual(2);
  await username.fill(input.username);
  await page.getByLabel('Password', { exact: true }).fill(input.password);
  proof.last_completed_stage = 'login_form';
  await page.getByRole('button', { name: input.request.version === '3.8.4' ? 'Login' : 'Sign in', exact: true }).click();
  proof.last_completed_stage = 'login_submitted';
  await expect(page).not.toHaveURL(/\/login(?:$|[?#])/);
  passed('authentication');
  await page.goto(input.studio_url + '/workflows/definitions');
  await expect(page.getByRole('button', { name: 'Create workflow', exact: true })).toBeVisible();
  passed('shell_or_embedding');
  const name = input.safe_ids.definition_name ?? 'package-browser-workflow';
  const sentinel = input.safe_ids.activity_value ?? 'package-browser-output';
  proof.last_completed_stage = 'workflow_list';
  await page.getByRole('button', { name: 'Create workflow', exact: true }).click();
  let dialog = page.getByRole('dialog');
  await dialog.getByLabel('Name', { exact: true }).fill(name);
  await dialog.getByRole('button', { name: 'Ok', exact: true }).click();
  await expect(page).toHaveURL(/\/workflows\/definitions\/[^/]+\/edit/);
  proof.last_completed_stage = 'workflow_created';
  const definitionId = new URL(page.url()).pathname.split('/').at(-2)!;
  const getDefinition = () => backend.get('/workflow-definitions/by-definition-id/' + encodeURIComponent(definitionId) + '?versionOptions=Latest');
  // A declared output is authored through the real package UI, not seeded via HTTP.
  await page.getByRole('tab', { name: /Input.*Output/i }).click();
  await page.getByRole('button', { name: 'Add output', exact: true }).click();
  dialog = page.getByRole('dialog');
  await dialog.getByLabel('Name', { exact: true }).fill('sentinel');
  await dialog.getByLabel('Type', { exact: true }).click();
  await page.getByText('String', { exact: true }).last().click();
  await dialog.getByRole('button', { name: 'Ok', exact: true }).click();
  proof.last_completed_stage = 'output_declared';
  const search = page.getByPlaceholder('Search', { exact: true });
  await search.fill('Set output');
  const category = page.locator('.mud-expand-panel-header').filter({ hasText: 'Composition' });
  if (await category.count()) await category.click();
  const activity = page.locator('[draggable="true"]').filter({ hasText: /^Set output$/i });
  await expect(activity).toHaveCount(1);
  passed('activity_registry');
  proof.last_completed_stage = 'activity_registry';
  const canvas = page.locator('.flowchart-diagram-designer-wrapper').first();
  await activity.dragTo(canvas, { targetPosition: { x: 260, y: 180 } });
  const node = page.locator('.x6-node').filter({ hasText: /Set output/i });
  await expect(node).toHaveCount(1);
  await node.click();
  passed('editor_smoke');
  proof.last_completed_stage = 'activity_inserted';
  await page.getByLabel('Output', { exact: true }).click();
  await page.getByText('sentinel', { exact: true }).last().click();
  const value = page.getByLabel(/^Output value$/i);
  await value.fill(sentinel);
  await value.blur();
  await page.keyboard.press('ControlOrMeta+s');
  await expect.poll(async () => JSON.stringify(await getDefinition())).toContain(sentinel);
  const before = await getDefinition();
  const child = before.root.activities.find((a: any) => a.type === 'Elsa.SetOutput');
  if (!child || typeof child.id !== 'string') throw new Error('saved_activity_identity_missing');
  proof.last_completed_stage = 'property_saved';
  await page.reload();
  await expect(page.locator('.x6-node').filter({ hasText: /Set output/i })).toHaveCount(1);
  const reloaded = await getDefinition();
  const after = reloaded.root.activities.find((a: any) => a.id === child.id);
  if (!after || !JSON.stringify(after).includes(sentinel) || reloaded.definitionId !== definitionId)
    throw new Error('saved_activity_identity_or_value_changed');
  if (input.request.version === '3.10.0') { passed('x6_edit_save_reload'); passed('identity_preserved'); }
  proof.last_completed_stage = 'edit_reloaded';
  proof.definition_id_sha256 = hash(definitionId);
  proof.activity_id_sha256 = hash(child.id);
  proof.value_sha256 = hash(sentinel);
  proof.synthetic_document_sha256 = hash(JSON.stringify(reloaded));
  if (input.request.version !== '3.10.0') return;
  await toolbar(page, 'Publish workflow');
  await expect.poll(async () => (await getDefinition()).isPublished).toBe(true);
  await toolbar(page, 'Run Workflow');
  await expect(page).toHaveURL(/\/workflows\/instances\/[^/]+\/view/);
  proof.last_completed_stage = 'workflow_run';
  const instanceId = new URL(page.url()).pathname.split('/').at(-2)!;
  passed('publish_run');
  await expect.poll(async () => (await backend.get('/workflow-instances/' + instanceId)).status).toBe('Finished');
  const instance = await backend.get('/workflow-instances/' + instanceId);
  if (instance.workflowState?.output?.sentinel !== sentinel) throw new Error('backend_output_mismatch');
  passed('backend_output');
  await expect(page.getByText('Finished', { exact: true }).first()).toBeVisible();
  passed('studio_terminal');
  proof.instance_id_sha256 = hash(instanceId);
}

async function main(): Promise<void> {
  const parts: Buffer[] = [];
  let length = 0;
  for await (const chunk of process.stdin) {
    length += chunk.length;
    if (length > 2 * 1024 * 1024) throw new Error('private_input_limit');
    parts.push(chunk);
  }
  const input = JSON.parse(Buffer.concat(parts).toString('utf8')) as PrivateInput;
  input.studio_url = loopback(input.studio_url); input.backend_url = loopback(input.backend_url);
  if (!hostAssertions[input.request.host] || !['3.8.4', '3.9.0', '3.10.0'].includes(input.request.version) || !['net8.0', 'net9.0', 'net10.0'].includes(input.request.framework))
    throw new Error('invalid_cell');
  const required = [...baseline, ...(input.request.version === '3.10.0' ? candidate : []), ...hostAssertions[input.request.host]];
  const assertions: Assertion[] = required.map(name => ({ name, passed: false, reason_category: 'not_implemented' }));
  const passed = (name: string) => {
    const assertion = assertions.find(item => item.name === name);
    if (!assertion || assertion.passed) throw new Error('duplicate_or_unknown_assertion');
    assertion.passed = true; assertion.reason_category = null;
  };
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ serviceWorkers: 'block' });
  const page = await context.newPage();
  page.setDefaultTimeout(20_000);
  const resources: Array<Record<string, unknown>> = [];
  const pending: Promise<void>[] = [];
  const expected = new Map(input.resources.map(asset => [asset.path, asset]));
  let failed = false;
  let serverCircuit = false;
  let serverFrames = 0;
  page.on('websocket', socket => {
    const url = new URL(socket.url());
    if (url.origin.replace(/^ws/, 'http') === new URL(input.studio_url).origin && url.pathname.endsWith('/_blazor')) {
      serverCircuit = true;
      socket.on('framereceived', () => { serverFrames++; });
    }
  });
  const proof: Record<string, unknown> = {};
  page.on('response', response => {
    const url = new URL(response.url());
    const asset = expected.get(url.pathname);
    if (!asset || url.origin !== new URL(input.studio_url).origin) return;
    pending.push((async () => {
      const headers = response.headers();
      const declared = Number(headers['content-length']);
      if (!Number.isFinite(declared) || declared > asset.bytes || asset.bytes > 32 * 1024 * 1024) throw new Error('resource_body_limit');
      const body = await response.body();
      if (body.length !== asset.bytes) throw new Error('resource_body_size');
      resources.push({ path: url.pathname, status: response.status(), content_type: headers['content-type']?.split(';')[0], sha256: hash(body), bytes: body.length, owner: asset.owner, requested: true });
    })().catch(() => { failed = true; }));
  });
  let backend: Backend | undefined;
  try {
    backend = await Backend.login(input.backend_url, input);
    proof.last_completed_stage = 'backend_authenticated';
    if (input.request.host === 'custom-elements') throw new Error('native_embedding_journey_pending');
    await fullShell(page, input, backend, passed, proof, () => serverFrames);
    if (input.request.host === 'server') {
      if (!serverCircuit) throw new Error('server_circuit_missing');
      passed('server_circuit');
    }
  } catch {
    failed = true;
    proof.login_failure_visible = await page.getByText(/Invalid credentials[.] (Try again[.]|Please try again)/).isVisible().catch(() => false);
    proof.login_form_visible = await page.getByLabel(/^User ?name$/i).isVisible().catch(() => false);
    proof.server_circuit_observed = serverCircuit;
    proof.server_render_frames_observed = serverFrames >= 2;
    proof.elsa_identity_ui_visible = await page.getByText('Elsa account', { exact: true }).isVisible().catch(() => false);
  } finally {
    await Promise.all(pending);
    await backend?.dispose();
    await context.close(); await browser.close();
    passed('cleanup');
  }
  const requiredResources = input.resources.filter(asset => asset.owner === 'package');
  if (requiredResources.length > 0 && requiredResources.every(asset => resources.some(record => record.path === asset.path && record.sha256 === asset.sha256 && record.status === 200 && record.content_type === asset.content_type && record.bytes === asset.bytes))) passed('browser_resources');
  const complete = assertions.every(assertion => assertion.passed) && !failed;
  process.stdout.write(JSON.stringify({ host: input.request.host, framework: input.request.framework, version: input.request.version, result: failed ? 'failed' : complete ? 'passed' : 'incomplete', assertions, resources, proof, browser_version: browser.version(), failure_category: failed ? 'browser_execution_or_validation_failed' : null }));
  process.exitCode = failed ? 1 : 0;
}

main().catch(() => { process.stdout.write(JSON.stringify({ result: 'failed', failure_category: 'browser_setup_failed' })); process.exitCode = 1; });

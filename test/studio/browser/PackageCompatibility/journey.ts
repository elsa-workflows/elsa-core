import { chromium, expect, request, type Page, type APIRequestContext, type Locator } from '@playwright/test';
import { createHash } from 'node:crypto';
import { readFileSync } from 'node:fs';
const policy = JSON.parse(readFileSync(new URL('./coverage-policy.json', import.meta.url), 'utf8'));

type Cell = { host: 'server' | 'wasm' | 'hosted-wasm' | 'custom-elements'; framework: string; version: string; route_prefix?: string };
type Resource = { path: string; sha256: string; bytes: number; content_type: string; owner: 'package' | 'fixture' | 'platform'; required?: boolean };
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
  const controls = page.locator('.pane-left [role="toolbar"]');
  await expect(controls).toHaveCount(1);
  await expect(controls).toBeVisible();
  const button = controls.getByRole('button', { name: title, exact: true });
  if (await button.count() === 1) {
    await button.click();
    return;
  }
  const titles = controls.locator(`[title="${title}"], [aria-label="${title}"]`);
  if (await titles.count() === 1) {
    await titles.click();
    return;
  }
  // Package versions without accessible icon labels still expose an actual tooltip.
  for (const icon of await controls.locator('.mud-tooltip-root button').all()) {
    if (!await icon.isVisible() || !await icon.isEnabled()) continue;
    const label = await icon.getAttribute('aria-label');
    if (label && label !== title) continue;
    await icon.hover();
    const text = page.locator('.mud-tooltip').filter({ hasText: new RegExp('^' + title + '$') });
    try {
      // WorkflowEditor declares a 500 ms tooltip delay; wait for the actual rendered title.
      await expect(text).toBeVisible({ timeout: 1500 });
    } catch { continue; }
    await icon.click();
    return;
  }
  throw new Error('toolbar_control_unavailable');
}

function inputControl(page: Page, label: RegExp, scope: Page | Locator = page): Locator {
  // Extended inputs render visible labels whose for IDs differ from their actual inputs.
  return scope.locator('.mud-input-control').filter({ has: page.locator('label').filter({ hasText: label }) });
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

async function fullShell(page: Page, input: PrivateInput, backend: Backend, passed: (name: string) => void, proof: Record<string, unknown>): Promise<void> {
  await page.goto(input.studio_url + '/login');
  proof.last_completed_stage = 'login_navigation';
  // All three reviewed host compositions select ElsaIdentity, including released 3.8.4.
  await expect(page.getByText('Elsa account', { exact: true })).toBeVisible();
  proof.expected_auth_provider_observed = true;
  const username = page.getByLabel('User name', { exact: true });
  await expect(username).toBeVisible();
  const signIn = page.getByRole('button', { name: 'Sign in', exact: true });
  await expect(username).toBeEmpty();
  await expect(page.getByLabel('Password', { exact: true })).toBeEmpty();
  let interactive = false;
  for (let attempt = 0; attempt < 3 && !interactive; attempt++) {
    await signIn.click(); // Empty required fields cannot send a credentials request.
    try {
      await expect(username).toHaveAttribute('aria-invalid', 'true', { timeout: 1500 });
      await expect(page.getByLabel('Password', { exact: true })).toHaveAttribute('aria-invalid', 'true', { timeout: 1500 });
      interactive = true;
    } catch { /* A prerender click has no live form effect; retry the empty validation only. */ }
  }
  if (!interactive) throw new Error('interactive_form_validation_missing');
  proof.interactive_validation_observed = true;
  await username.fill(input.username);
  await page.getByLabel('Password', { exact: true }).fill(input.password);
  await page.getByLabel('Password', { exact: true }).blur();
  await expect(username).toHaveValue(input.username);
  await expect(page.getByLabel('Password', { exact: true })).toHaveValue(input.password);
  proof.private_input_values_retained = true;
  proof.last_completed_stage = 'login_form';
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  proof.last_completed_stage = 'login_submitted';
  await expect(page).not.toHaveURL(/\/login(?:$|[?#])/);
  passed('authentication');
  await page.goto(input.studio_url + '/workflows/definitions');
  await expect(page.getByRole('button', { name: 'Create workflow', exact: true })).toBeVisible();
  // ServerReload normalizes paging after both awaited definition-list reads.
  // Opening a dialog before this navigation completes can close it mid-initialization.
  await page.waitForURL(url => url.pathname.endsWith('/workflows/definitions') && url.searchParams.get('page') === '1' && url.searchParams.get('pageSize') === '10');
  proof.initial_list_navigation_completed = true;
  passed('shell_or_embedding');
  const name = input.safe_ids.definition_name ?? 'package-browser-workflow';
  const sentinel = input.safe_ids.activity_value ?? 'package-browser-output';
  proof.last_completed_stage = 'workflow_list';
  await page.getByRole('button', { name: 'Create workflow', exact: true }).click();
  let dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  proof.last_completed_stage = 'create_dialog_opened';
  await dialog.getByLabel('Name', { exact: true }).fill(name);
  await dialog.getByLabel('Name', { exact: true }).blur();
  proof.last_completed_stage = 'create_name_filled';
  await dialog.getByRole('button', { name: 'Ok', exact: true }).click();
  proof.last_completed_stage = 'create_submitted';
  await expect(page).toHaveURL(/\/workflows\/definitions\/[^/]+\/edit/);
  proof.last_completed_stage = 'workflow_created';
  const definitionId = new URL(page.url()).pathname.split('/').at(-2)!;
  const getDefinition = () => backend.get('/workflow-definitions/by-definition-id/' + encodeURIComponent(definitionId) + '?versionOptions=Latest');
  // A declared output is authored through the real package UI, not seeded via HTTP.
  await page.getByRole('tab', { name: /Input.*Output/i }).click();
  proof.last_completed_stage = 'output_tab_opened';
  await page.getByRole('button', { name: 'Add output', exact: true }).click();
  dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  proof.last_completed_stage = 'output_dialog_opened';
  // CodeBeam 9.1.0 renders a hidden input and toggles its visible MudInputControl.
  // Its Type label is not associated with the hidden input, so use the actual labeled component.
  const typeSelect = inputControl(page, /^Type$/, dialog);
  await expect(typeSelect).toHaveCount(1);
  // EditOutputDialog initializes Name and Type after its awaited descriptor reads.
  await expect.poll(async () => (await typeSelect.locator('[tabindex="0"]').first().innerText()).trim(), { timeout: 20_000 }).not.toBe('');
  await dialog.getByLabel('Name', { exact: true }).fill('sentinel');
  await dialog.getByLabel('Display name', { exact: true }).fill('sentinel');
  await typeSelect.click();
  const stringType = page.getByRole('option', { name: 'String', exact: true });
  await expect(stringType).toHaveCount(1);
  await stringType.click();
  proof.last_completed_stage = 'output_type_selected';
  await dialog.getByRole('button', { name: 'Ok', exact: true }).click();
  proof.last_completed_stage = 'output_declared';
  // The supported native pickers expose Search as a placeholder (accordion) or label (tree).
  const search = page.getByPlaceholder('Search', { exact: true }).or(page.getByLabel(/^Search(?:\.\.\.)?$/));
  await expect(search).toHaveCount(1);
  await search.fill('Set output');
  const category = page.locator('.mud-expand-panel-header').filter({ hasText: 'Composition' });
  if (await category.count()) await category.click();
  const activity = page.locator('[draggable="true"]').filter({ hasText: /^Set output$/i });
  await expect(activity).toHaveCount(1);
  await expect(activity).toBeVisible();
  passed('activity_registry');
  proof.last_completed_stage = 'activity_registry';
  const canvas = page.locator('.flowchart-diagram-designer-wrapper').first();
  await activity.dragTo(canvas, { targetPosition: { x: 260, y: 180 } });
  const node = page.locator('.x6-node').filter({ hasText: /Set output/i });
  await expect(node).toHaveCount(1);
  // Native AddNewActivityAsync selects the dragged activity and opens its property editor.
  await expect(node).toHaveClass(/x6-node-selected/);
  passed('editor_smoke');
  proof.last_completed_stage = 'activity_inserted';
  const output = inputControl(page, /^Output$/);
  await expect(output).toHaveCount(1);
  await output.click();
  const outputOption = page.getByRole('option', { name: 'sentinel', exact: true });
  await expect(outputOption).toHaveCount(1);
  await outputOption.click();
  const value = inputControl(page, /^Output Value$/i).locator('input[type="text"]');
  await expect(value).toHaveCount(1);
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
  proof.last_completed_stage = 'workflow_published';
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
    await fullShell(page, input, backend, passed, proof);
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
    proof.create_name_label_count = await page.getByRole('dialog').getByLabel('Name', { exact: true }).count();
    proof.create_name_textbox_count = await page.getByRole('dialog').getByRole('textbox', { name: /^Name(?:\s|$)/ }).count();
    proof.elsa_identity_ui_visible = await page.getByText('Elsa account', { exact: true }).isVisible().catch(() => false);
  } finally {
    await Promise.all(pending);
    await backend?.dispose();
    await context.close(); await browser.close();
    passed('cleanup');
  }
  const requiredResources = input.resources.filter(asset => asset.owner === 'package' && asset.required !== false);
  if (requiredResources.length > 0 && requiredResources.every(asset => resources.some(record => record.path === asset.path && record.sha256 === asset.sha256 && record.status === 200 && record.content_type === asset.content_type && record.bytes === asset.bytes))) passed('browser_resources');
  const complete = assertions.every(assertion => assertion.passed) && !failed;
  process.stdout.write(JSON.stringify({ host: input.request.host, framework: input.request.framework, version: input.request.version, result: failed ? 'failed' : complete ? 'passed' : 'incomplete', assertions, resources, proof, browser_version: browser.version(), failure_category: failed ? 'browser_execution_or_validation_failed' : null }));
  process.exitCode = failed ? 1 : 0;
}

main().catch(() => { process.stdout.write(JSON.stringify({ result: 'failed', failure_category: 'browser_setup_failed' })); process.exitCode = 1; });

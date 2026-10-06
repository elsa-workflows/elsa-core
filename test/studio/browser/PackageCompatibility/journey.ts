import { chromium, expect, request, type Page, type APIRequestContext, type Locator, type Download } from '@playwright/test';
import { createHash } from 'node:crypto';
import { closeSync, lstatSync, openSync, readFileSync, readSync, realpathSync, writeFileSync } from 'node:fs';
import { dirname, isAbsolute } from 'node:path';
import { pathToFileURL } from 'node:url';
import { isDeepStrictEqual } from 'node:util';
import { bpmnSemanticIdentity, checkedXmlText, type XmlElement } from './bpmn-roundtrip.js';
const policy = JSON.parse(readFileSync(new URL('./coverage-policy.json', import.meta.url), 'utf8'));

type Cell = { host: 'server' | 'wasm' | 'hosted-wasm' | 'custom-elements'; framework: string; version: string; route_prefix?: string };
type Resource = { path: string; sha256: string; bytes: number; content_type: string; owner: 'package' | 'fixture' | 'platform'; required?: boolean };
type ReleasedInput = { private_path: string; binding: { document: { source_cell: Cell; document_sha256: string; bytes: number; tool_version: string; definition_id_sha256: string; activity_id_sha256: string; value_sha256: string }; fixture_identity_sha256: string; source_evidence_sha256: string; browser_receipt_sha256: string }; source_binding_sha256: string; root_id_sha256: string };
type PrivateInput = { request: Cell; studio_url: string; backend_url: string; username: string; password: string; safe_ids: Record<string, string>; resources: Resource[]; released_document_output?: string; released_document_inputs?: ReleasedInput[] };
type Assertion = { name: string; passed: boolean; reason_category: string | null };
const baseline: string[] = policy.baseline;
const candidate: string[] = policy.candidate;
const hostAssertions: Record<Cell['host'], string[]> = policy.host_assertions;
const hash = (data: Buffer | string): string => createHash('sha256').update(data).digest('hex');

export async function resourceBody(response: { headers(): Record<string, string>; body(): Promise<Buffer> }, expectedBytes: number): Promise<Buffer> {
  // The WASM dev server can stream a response without Content-Length. Its
  // optional transport length is distinct from the decoded package byte count.
  const headers = response.headers();
  const declared = headers['content-length'];
  const encoded = headers['content-encoding'];
  // Gzip with no compression expands the payload by framing bytes. Bound wire
  // bytes separately; Playwright returns the decoded body checked below.
  const wireLimit = encoded && encoded !== 'identity' ? 32 * 1024 * 1024 : expectedBytes;
  if (!Number.isSafeInteger(expectedBytes) || expectedBytes < 0 || expectedBytes > 32 * 1024 * 1024 ||
      (encoded !== undefined && !['gzip', 'br', 'identity'].includes(encoded)) ||
      (declared !== undefined && (!/^[0-9]+$/.test(declared) || !Number.isSafeInteger(Number(declared)) || Number(declared) > wireLimit)))
    throw new Error('resource_body_limit');
  const body = await response.body();
  if (body.length !== expectedBytes) throw new Error('resource_body_size');
  return body;
}

export function readReleasedInput(input: ReleasedInput): { raw: Buffer; document: any } {
  const path = input.private_path;
  if (!isAbsolute(path) || realpathSync(path) !== path || lstatSync(path).isSymbolicLink() ||
      !lstatSync(path).isFile() || (lstatSync(path).mode & 0o777) !== 0o600) throw new Error('invalid_private_released_input');
  for (let parent = dirname(path); parent !== dirname(parent); parent = dirname(parent))
    if (lstatSync(parent).isSymbolicLink()) throw new Error('invalid_private_released_ancestor');
  const descriptor = openSync(path, 'r');
  const buffer = Buffer.alloc(1024 * 1024 + 1);
  let size = 0;
  try {
    while (size < buffer.length) {
      const read = readSync(descriptor, buffer, size, buffer.length - size, null);
      if (!read) break;
      size += read;
    }
  } finally { closeSync(descriptor); }
  const expected = input.binding.document;
  if (!size || size > 1024 * 1024 || size !== expected.bytes) throw new Error('released_input_limit');
  const raw = buffer.subarray(0, size);
  if (hash(raw) !== expected.document_sha256) throw new Error('released_input_hash_mismatch');
  const document = JSON.parse(new TextDecoder('utf-8', { fatal: true }).decode(raw));
  const activity = document.root?.activities?.[0];
  if (![document.definitionId, document.root?.id, activity?.id].every(id => typeof id === 'string' && /^[0-9a-f]{1,16}$/.test(id)) ||
      !/^paired-browser-[0-9a-f]{12}$/.test(document.name) ||
      document.$schema !== 'https://elsaworkflows.io/schemas/workflow-definition/v3.0.0/schema.json' ||
      document.root.nodeId !== 'Workflow1:' + document.root.id || activity.nodeId !== document.root.nodeId + ':' + activity.id ||
      document.root.name !== 'Flowchart1' || activity.name !== 'SetOutput1' ||
      typeof activity?.outputValue?.expression?.value !== 'string' || document.toolVersion !== expected.tool_version ||
      hash(document.root.id) !== input.root_id_sha256 ||
      hash(document.definitionId) !== expected.definition_id_sha256 || hash(activity.id) !== expected.activity_id_sha256 ||
      hash(activity.outputValue.expression.value) !== expected.value_sha256) throw new Error('released_input_identity_mismatch');
  checkReleasedDefinition(document, document);
  return { raw, document };
}

export function checkReleasedDefinition(actual: any, original: any): void {
  const same = isDeepStrictEqual;
  const root = actual.root, expectedRoot = original.root;
  if (actual.definitionId !== original.definitionId || actual.name !== original.name ||
      actual.toolVersion !== original.toolVersion || root?.type !== 'Elsa.Flowchart' ||
      root.id !== expectedRoot.id || root.nodeId !== expectedRoot.nodeId || root.name !== expectedRoot.name || root.version !== 1 ||
      !same(root.customProperties, expectedRoot.customProperties) ||
      !same(root.variables, []) || !same(root.connections, []) || !Array.isArray(root.activities) || root.activities.length !== 1 ||
      !same(actual.inputs, []) || !same(actual.variables, []) || !same(actual.outcomes, []) ||
      !same(actual.outputs, [{ type: 'String', name: 'sentinel', displayName: 'sentinel', description: '', category: 'Primitives' }]))
    throw new Error('released_graph_mismatch');
  const child = root.activities[0], expected = expectedRoot.activities[0];
  if (child.type !== 'Elsa.SetOutput' || child.version !== 1 || child.id !== expected.id ||
      child.nodeId !== expected.nodeId || child.name !== expected.name ||
      !same(child.customProperties, expected.customProperties) ||
      !same(child.outputName, { typeName: 'String', expression: { type: 'Literal', value: 'sentinel' } }) ||
      !same(child.outputValue, { typeName: 'Object', expression: { type: 'Literal', value: 'synthetic-browser-value' } }))
    throw new Error('released_activity_or_value_mismatch');
}

async function visibleReleasedActivity(page: Page, document: any): Promise<void> {
  const child = document.root.activities[0];
  await expect(page.locator('.flowchart-diagram-designer-wrapper .x6-graph-svg')).toBeVisible();
  await expect(page.getByLabel('Name', { exact: true })).toHaveValue(document.name);
  const activity = page.locator('elsa-activity-wrapper[activity-id="' + child.id + '"]');
  await expect(activity).toHaveCount(1); await activity.click();
  await expect(inputControl(page, /^Output$/).locator('[tabindex="0"]').first()).toContainText('sentinel');
  await expect(inputControl(page, /^Output Value$/i).locator('input[type="text"]')).toHaveValue(child.outputValue.expression.value);
}

async function definitionsList(page: Page, input: PrivateInput): Promise<void> {
  await page.goto(input.studio_url + '/workflows/definitions');
  await page.waitForURL(url => url.pathname.endsWith('/workflows/definitions') && url.searchParams.get('page') === '1' && url.searchParams.get('pageSize') === '10');
}

async function nativeImportChooser(page: Page, label: 'Import' | 'Import BPMN') {
  const group = page.locator('.definitions-table .mud-button-group-root').filter({ has: page.getByRole('button', { name: 'Create workflow', exact: true }) });
  await expect(group).toHaveCount(1);
  await group.locator('.mud-menu-icon-button-activator').click();
  const item = page.locator('.mud-menu-item:visible').filter({ hasText: new RegExp('^' + label + '$') });
  await expect(item).toHaveCount(1);
  const [chooser] = await Promise.all([page.waitForEvent('filechooser'), item.click()]);
  return chooser;
}

async function downloadedBytes(download: Download): Promise<Buffer> {
  const stream = await download.createReadStream();
  if (!stream) throw new Error('native_download_missing');
  const chunks: Buffer[] = [];
  let bytes = 0;
  for await (const chunk of stream) {
    bytes += chunk.length;
    if (bytes > 1024 * 1024) { stream.destroy(); throw new Error('native_download_limit'); }
    chunks.push(Buffer.from(chunk));
  }
  if (!bytes || await download.failure()) throw new Error('native_download_failed');
  return Buffer.concat(chunks);
}

async function bpmnTree(page: Page, raw: Buffer): Promise<XmlElement> {
  const text = checkedXmlText(raw);
  return page.evaluate(xml => {
    const document = new DOMParser().parseFromString(xml, 'application/xml');
    if (document.querySelector('parsererror') || !document.documentElement) throw new Error('bpmn_xml_parse');
    const read = (element: Element): XmlElement => ({
      namespace: element.namespaceURI, name: element.localName,
      attributes: Object.fromEntries([...element.attributes].filter(attribute => attribute.namespaceURI !== 'http://www.w3.org/2000/xmlns/').map(attribute => {
        if (attribute.namespaceURI) throw new Error('bpmn_foreign_attribute');
        return [attribute.localName, attribute.value];
      })),
      text: [...element.childNodes].filter(node => node.nodeType === Node.TEXT_NODE || node.nodeType === Node.CDATA_SECTION_NODE).map(node => node.textContent ?? '').join('').trim(),
      children: [...element.children].map(read)
    });
    return read(document.documentElement);
  }, text);
}

async function nativeBpmnRoundtrip(page: Page, input: PrivateInput, backend: Backend, proof: Record<string, unknown>): Promise<void> {
  const raw = readFileSync(new URL('./paired-browser.bpmn', import.meta.url));
  const semantic = bpmnSemanticIdentity(await bpmnTree(page, raw));
  const checks = { imported: false, rendered: false, selection_callback: false, exported: false, reimported: false, semantic_preserved: false };
  const record: Record<string, unknown> = { input_xml_sha256: hash(raw), input_xml_bytes: raw.length, semantic_sha256: hash(semantic),
    process_id_sha256: hash('paired-process'), start_id_sha256: hash('paired-start'), end_id_sha256: hash('paired-end'), flow_id_sha256: hash('paired-flow'), checks };
  proof.bpmn_roundtrip = record; proof.last_completed_stage = 'bpmn_input_validated';
  const importFile = async (buffer: Buffer): Promise<string> => {
    await definitionsList(page, input);
    const chooser = await nativeImportChooser(page, 'Import BPMN');
    await chooser.setFiles({ name: 'paired-browser.bpmn', mimeType: 'application/xml', buffer });
    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible();
    await expect(dialog.getByText('No findings. This document reads without loss.', { exact: true })).toBeVisible();
    await dialog.getByRole('button', { name: 'Import', exact: true }).click();
    await expect(page).toHaveURL(/\/workflows\/definitions\/[^/]+\/edit/);
    const id = new URL(page.url()).pathname.split('/').at(-2)!;
    const definition = await backend.get('/workflow-definitions/by-definition-id/' + encodeURIComponent(id) + '?versionOptions=Latest');
    if (definition.definitionId !== id || definition.root?.type !== 'Elsa.BpmnProcess' ||
        typeof definition.customProperties?.['Bpmn:SourceXml'] !== 'string' ||
        bpmnSemanticIdentity(await bpmnTree(page, Buffer.from(definition.customProperties['Bpmn:SourceXml']))) !== semantic)
      throw new Error('bpmn_backend_graph_mismatch');
    return id;
  };
  const renderAndSelect = async (): Promise<void> => {
    const start = page.locator('.x6-node[data-cell-id="paired-start"]');
    const end = page.locator('.x6-node[data-cell-id="paired-end"]');
    await expect(start).toBeVisible(); await expect(end).toBeVisible();
    await expect(page.locator('.x6-node')).toHaveCount(2);
    await expect(page.locator('.x6-edge[data-cell-id="paired-flow"]')).toHaveCount(1);
    checks.rendered = true; proof.last_completed_stage = 'bpmn_rendered';
    await start.click();
    // X6 node:selected -> .NET ElementSelected -> performed-by panel is a real native callback.
    await expect(page.getByTestId('bpmn-performed-by')).toBeVisible();
    await expect(page.getByTestId('bpmn-no-work')).toContainText("'Paired start' performs no work");
    checks.selection_callback = true; proof.last_completed_stage = 'bpmn_selected';
  };
  const first = await importFile(raw);
  record.first_definition_id_sha256 = hash(first); checks.imported = true; proof.last_completed_stage = 'bpmn_imported';
  await renderAndSelect();
  await toolbar(page, 'Export as BPMN 2.0 XML');
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  await expect(dialog.getByText("The exported file contains this workflow's binding configuration and expressions.", { exact: true })).toBeVisible();
  const [download] = await Promise.all([page.waitForEvent('download'), dialog.getByRole('button', { name: 'Export', exact: true }).click()]);
  const exported = await downloadedBytes(download);
  record.export_xml_sha256 = hash(exported); record.export_xml_bytes = exported.length;
  checks.exported = true; proof.last_completed_stage = 'bpmn_exported';
  if (bpmnSemanticIdentity(await bpmnTree(page, exported)) !== semantic) throw new Error('bpmn_export_semantic_mismatch');
  const second = await importFile(exported);
  if (second === first) throw new Error('bpmn_reimport_identity_reused');
  record.second_definition_id_sha256 = hash(second); checks.reimported = true; proof.last_completed_stage = 'bpmn_reimported';
  await renderAndSelect();
  checks.semantic_preserved = true;
}

async function nativeClipboard(page: Page, instanceId: string, value: string, proof: Record<string, unknown>): Promise<void> {
  const record: Record<string, unknown> = { instance_id_sha256: hash(instanceId), expected_value_sha256: hash(value), native_copy_observed: false };
  proof.clipboard = record;
  await page.getByRole('tab', { name: 'Input/output', exact: true }).click();
  const row = page.locator('tr.hover-row').filter({ has: page.locator('td').filter({ hasText: /^sentinel$/ }) });
  await expect(row).toHaveCount(1);
  await expect(row.locator('td').nth(1)).toHaveText(value);
  await row.hover();
  const copy = row.getByRole('button');
  await expect(copy).toHaveCount(1); await expect(copy).toBeEnabled();
  await copy.click();
  await expect(page.getByText('sentinel copied', { exact: true })).toBeVisible();
  const actual = await page.evaluate(() => navigator.clipboard.readText());
  if (actual.length > 1024 || actual !== value) throw new Error('native_clipboard_value_mismatch');
  record.actual_value_sha256 = hash(actual); record.native_copy_observed = true;
  proof.last_completed_stage = 'clipboard_copied';
}

async function reopenReleased(page: Page, input: PrivateInput, backend: Backend, proof: Record<string, unknown>): Promise<void> {
  const entries = input.released_document_inputs!;
  const reopens: Array<Record<string, any>> = [];
  for (const entry of entries) {
    const { raw, document } = readReleasedInput(entry);
    const child = document.root.activities[0];
    const checks = { imported: false, visible: false, saved: false, reloaded: false, published: false, studio_terminal: false, backend_output: false };
    const row: Record<string, any> = { source_cell: entry.binding.document.source_cell, source_binding_sha256: entry.source_binding_sha256,
      document_sha256: hash(raw), definition_id_sha256: hash(document.definitionId), root_id_sha256: hash(document.root.id),
      activity_id_sha256: hash(child.id), value_sha256: hash(child.outputValue.expression.value), tool_version: document.toolVersion, checks };
    reopens.push(row); // Preserve the actual partial journey if a later stage fails.
    proof.baseline_reopens = reopens;
    await definitionsList(page, input);
    const chooser = await nativeImportChooser(page, 'Import');
    // Upload the original buffer through the native picker; no workflow is authored via HTTP.
    await chooser.setFiles({ name: 'released-' + entry.binding.document.source_cell.version + '.json', mimeType: 'application/json', buffer: raw });
    const get = () => backend.get('/workflow-definitions/by-definition-id/' + encodeURIComponent(document.definitionId) + '?versionOptions=Latest');
    await expect(page.getByText('1 workflow imported successfully.', { exact: true })).toBeVisible();
    checkReleasedDefinition(await get(), document);
    checks.imported = true; proof.last_completed_stage = 'baseline_imported';
    const tableRow = page.locator('.definitions-table tbody tr').filter({ has: page.getByText(document.definitionId, { exact: true }) });
    await expect(tableRow).toHaveCount(1);
    await tableRow.getByText(document.name, { exact: true }).click();
    await expect(page).toHaveURL(new RegExp('/workflows/definitions/' + document.definitionId + '/edit'));
    await visibleReleasedActivity(page, document);
    checks.visible = true;
    await page.keyboard.press('ControlOrMeta+s');
    await expect(page.getByText('Workflow saved', { exact: true })).toBeVisible();
    checkReleasedDefinition(await get(), document); checks.saved = true;
    await page.reload();
    await visibleReleasedActivity(page, document);
    checkReleasedDefinition(await get(), document); checks.reloaded = true; proof.last_completed_stage = 'baseline_reloaded';
    await toolbar(page, 'Publish workflow');
    await expect.poll(async () => (await get()).isPublished).toBe(true); checks.published = true;
    await toolbar(page, 'Run Workflow');
    await expect(page).toHaveURL(/\/workflows\/instances\/[^/]+\/view/);
    const instanceId = new URL(page.url()).pathname.split('/').at(-2)!;
    row.instance_id_sha256 = hash(instanceId);
    await expect.poll(async () => (await backend.get('/workflow-instances/' + instanceId)).status).toBe('Finished');
    const instance = await backend.get('/workflow-instances/' + instanceId);
    if (instance.workflowState?.output?.sentinel !== child.outputValue.expression.value) throw new Error('released_backend_output_mismatch');
    checks.backend_output = true;
    await expect(page.getByText('Finished', { exact: true }).first()).toBeVisible(); checks.studio_terminal = true;
    proof.last_completed_stage = 'baseline_run';
  }
}

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
  // The route changes before the asynchronous editor/designer initialization completes.
  // Require the actual X6 graph and this workflow's populated metadata before changing tabs.
  await expect(page.locator('.flowchart-diagram-designer-wrapper .x6-graph-svg')).toBeVisible();
  await expect(page.getByLabel('Name', { exact: true })).toHaveValue(name);
  proof.editor_ready_observed = true;
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
  if (input.request.version !== '3.10.0') {
    if (input.released_document_output) {
      // Export the real saved workflow through the native menu and download interop.
      // WorkflowEditor places its JSON-file menu beside the hidden native upload wrapper;
      // the designer toolbar also has an unrelated canvas-export menu.
      const menu = page.locator('#workflow-file-upload-button-wrapper + .mud-button-group-root .mud-menu-icon-button-activator');
      await expect(menu).toHaveCount(1);
      await menu.click();
      proof.last_completed_stage = 'released_export_menu_opened';
      // MudBlazor9 renders a native div.mud-menu-item without a menuitem role.
      const exportItem = page.locator('.mud-menu-item:visible').filter({ hasText: /^Export$/ });
      await expect(exportItem).toHaveCount(1);
      await exportItem.click();
      const exportDialog = page.getByRole('dialog');
      await expect(exportDialog).toBeVisible();
      proof.last_completed_stage = 'released_export_dialog_opened';
      await expect(exportDialog.getByRole('checkbox', { name: 'Include referencing workflows', exact: true })).not.toBeChecked();
      const [download] = await Promise.all([
        page.waitForEvent('download'),
        exportDialog.getByRole('button', { name: 'Export', exact: true }).click()
      ]);
      const document = await downloadedBytes(download);
      const output = input.released_document_output;
      if (!isAbsolute(output) || realpathSync(dirname(output)) !== dirname(output)) throw new Error('invalid_private_document_output');
      writeFileSync(output, document, { flag: 'wx', mode: 0o600 });
      proof.released_document_sha256 = hash(document);
      proof.last_completed_stage = 'released_document_exported';
      // Shape, package provenance and retained-byte validation belong to the parent verifier.
    }
    return;
  }
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
  if (input.released_document_inputs) {
    await reopenReleased(page, input, backend, proof);
    passed('baseline_reopen');
  }
  if (input.request.host === 'server') {
    // Keep the first new slice scoped to the reviewed default X6 Server composition.
    await page.goto(input.studio_url + '/workflows/instances/' + encodeURIComponent(instanceId) + '/view');
    await expect(page.getByText('Finished', { exact: true }).first()).toBeVisible();
    await nativeClipboard(page, instanceId, sentinel, proof);
    passed('clipboard');
    await nativeBpmnRoundtrip(page, input, backend, proof);
    passed('bpmn_roundtrip');
  }
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
  if (input.released_document_inputs) {
    const entries = input.released_document_inputs;
    if (input.request.version !== '3.10.0' || !Array.isArray(entries) || entries.length !== 2 ||
        new Set(entries.map(entry => entry.binding.document.source_cell.version)).size !== 2 ||
        entries.some(entry => !['3.8.4', '3.9.0'].includes(entry.binding.document.source_cell.version) ||
          entry.binding.document.source_cell.framework !== input.request.framework || entry.binding.document.source_cell.host !== input.request.host))
      throw new Error('invalid_released_input_cells');
  }
  const required = [...baseline, ...(input.request.version === '3.10.0' ? candidate : []), ...hostAssertions[input.request.host]];
  const assertions: Assertion[] = required.map(name => ({ name, passed: false, reason_category: 'not_implemented' }));
  const passed = (name: string) => {
    const assertion = assertions.find(item => item.name === name);
    if (!assertion || assertion.passed) throw new Error('duplicate_or_unknown_assertion');
    assertion.passed = true; assertion.reason_category = null;
  };
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ serviceWorkers: 'block' });
  if (input.request.version === '3.10.0' && input.request.host === 'server')
    await context.grantPermissions(['clipboard-read', 'clipboard-write'], { origin: new URL(input.studio_url).origin });
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
      const body = await resourceBody(response, asset.bytes);
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

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href)
  main().catch(() => { process.stdout.write(JSON.stringify({ result: 'failed', failure_category: 'browser_setup_failed' })); process.exitCode = 1; });

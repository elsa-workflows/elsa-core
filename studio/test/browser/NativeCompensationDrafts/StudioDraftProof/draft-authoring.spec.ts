import { spawn } from 'node:child_process';
import { createHash, randomUUID } from 'node:crypto';
import { chmod, mkdir, readFile, rename, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { expect, test, type Browser, type Locator, type Page } from '@playwright/test';

type Status = 'NOT_RUN' | 'PASS' | 'FAIL';
type ResultRecord = { id: string; status: Status; code: string | null };
type ExpectedStep = { stepName: string; forwardLiteral: string; handlerLiteral: string };
type SqliteSnapshot = {
  schemaVersion: 2;
  status: 'OK';
  identitySha256: string;
  layoutSha256: string;
  rowSetSha256: string;
  diagnosticTargetsSha256: string;
  definitionRowCount: number;
  unpublished: true;
  literalValuesVerified: true;
};

const ids = [
  'NC-DRAFT-LOGIN',
  'NC-DRAFT-CREATE',
  'NC-DRAFT-SCOPE-OPENED-VIA-UI',
  'NC-DRAFT-TWO-STEP-KEYBOARD',
  'NC-DRAFT-PAIRED-CHILDREN',
  'NC-DRAFT-INPUT-LITERALS',
  'NC-DRAFT-SAVE-BASELINE',
  'NC-DRAFT-SQLITE-BASELINE',
  'NC-DRAFT-RENAME-AND-REORDER',
  'NC-DRAFT-SAVE-REORDERED',
  'NC-DRAFT-SQLITE-REORDERED',
  'NC-DRAFT-REOPEN',
  'NC-DRAFT-REOPEN-STABLE-IDENTITY',
  'NC-DRAFT-PRIVATE-CAPTURE',
  'NC-DRAFT-PUBLISH-UI-REFUSAL',
  'NC-DRAFT-ROWSET-UNCHANGED-ON-REFUSAL',
  'NC-DRAFT-CONTEXT-CLOSED',
  'NC-DRAFT-CHROMIUM-PROCESS-CAPTURE',
  'NC-DRAFT-CHROMIUM-PROCESS-EXIT'
] as const;

const draftOnlyHeading = 'Native compensation drafts cannot be published.';
const expectedChildVersion = 1;
const initialSteps: ExpectedStep[] = [
  {
    stepName: 'Reserve inventory',
    forwardLiteral: 'draft-proof-forward-one-literal',
    handlerLiteral: 'draft-proof-handler-one-literal'
  },
  {
    stepName: 'Release inventory',
    forwardLiteral: 'draft-proof-forward-two-literal',
    handlerLiteral: 'draft-proof-handler-two-literal'
  }
];
const reorderedSteps: ExpectedStep[] = [
  initialSteps[1],
  { ...initialSteps[0], stepName: 'Reserve stock' }
];
const resultPath = requiredAbsolutePath('ELSA_COMP_PROOF_ASSERTION_RESULTS');
const privateCapturePath = requiredAbsolutePath('ELSA_COMP_PROOF_PRIVATE_CAPTURE');
const browserProcessCapturePath = requiredAbsolutePath('ELSA_COMP_PROOF_BROWSER_PROCESS_CAPTURE');
const browserProcessEvidencePath = requiredAbsolutePath('ELSA_COMP_PROOF_BROWSER_PROCESS_EVIDENCE');
const sqlitePath = requiredAbsolutePath('ELSA_COMP_PROOF_SQLITE_PATH');
const verifierPath = requiredAbsolutePath('ELSA_COMP_PROOF_SQLITE_VERIFIER');
const pythonPath = requiredAbsolutePath('ELSA_COMP_PROOF_PYTHON');
const adminUsername = requiredSecret('ELSA_COMP_PROOF_ADMIN_USERNAME');
const adminPassword = requiredSecret('ELSA_COMP_PROOF_ADMIN_PASSWORD');

const records = new Map<string, ResultRecord>(
  ids.map(id => [id, { id, status: 'NOT_RUN', code: null }])
);

function requiredAbsolutePath(name: string): string {
  const value = process.env[name];
  if (!value || !path.isAbsolute(value))
    throw new Error('proof-harness-path-invalid');
  return value;
}

function requiredSecret(name: string): string {
  const value = process.env[name];
  if (!value)
    throw new Error('proof-harness-credential-missing');
  return value;
}

async function persistSafeResults(): Promise<void> {
  const parent = path.dirname(resultPath);
  await mkdir(parent, { recursive: true, mode: 0o700 });
  const temporary = resultPath + '.tmp';
  const payload = {
    schemaVersion: 1,
    interpretation: 'NOT_EVALUATED_BY_SUPERVISOR',
    assertions: ids.map(id => records.get(id)!)
  };
  await writeFile(temporary, JSON.stringify(payload, null, 2) + '\n', { mode: 0o600, flag: 'w' });
  await rename(temporary, resultPath);
  await chmod(resultPath, 0o600);
}

async function assertion(id: typeof ids[number], failureCode: string, action: () => Promise<void>): Promise<void> {
  try {
    await action();
    records.set(id, { id, status: 'PASS', code: 'checked' });
  } catch {
    records.set(id, { id, status: 'FAIL', code: failureCode });
    await persistSafeResults().catch(() => undefined);
    throw new Error(id + ':' + failureCode);
  }
  await persistSafeResults();
}

function requireCondition(condition: boolean, code: string): void {
  if (!condition)
    throw new Error(code);
}

async function requireVisible(locator: Locator): Promise<void> {
  try {
    await locator.waitFor({ state: 'visible', timeout: 8_000 });
  } catch {
    throw new Error('expected-control-not-visible');
  }
}

async function waitForDrawerState(drawer: Locator, expectedOpen: boolean): Promise<void> {
  await expect.poll(async () =>
    drawer.evaluate(element => element.classList.contains('mud-drawer--open')), {
      message: 'Wait for the managed navigation drawer state',
      timeout: 8_000,
      intervals: [50, 100, 250]
    }).toBe(expectedOpen);
}

async function verifyInteractiveStudioShell(page: Page): Promise<void> {
  const toggle = page.getByRole('button', { name: 'Toggle navigation', exact: true });
  const drawer = page.locator('.studio-drawer');
  await requireVisible(toggle);
  await drawer.waitFor({ state: 'attached', timeout: 8_000 });

  const initiallyOpen = await drawer.evaluate(element => element.classList.contains('mud-drawer--open'));
  try {
    await toggle.click();
    await waitForDrawerState(drawer, !initiallyOpen);
  } finally {
    const isOpen = await drawer.evaluate(element => element.classList.contains('mud-drawer--open')).catch(() => initiallyOpen);
    if (isOpen !== initiallyOpen) {
      await toggle.click();
      await waitForDrawerState(drawer, initiallyOpen);
    }
  }
}

async function openCompensationScope(page: Page): Promise<Locator> {
  const scopeType = page.locator('.flowchart-diagram-designer-wrapper .activity-type')
    .filter({ hasText: /^\s*Compensation\s+Scope\s*$/i })
    .first();
  const scopeActivity = scopeType.locator(
    'xpath=ancestor-or-self::*[contains(concat(" ", normalize-space(@class), " "), " elsa-activity ")][1]'
  );
  await scopeActivity.waitFor({ state: 'visible', timeout: 8_000 });
  await scopeActivity.dblclick();
  const editor = page.getByRole('region', { name: 'Compensation draft editor', exact: true });
  await editor.waitFor({ state: 'visible', timeout: 8_000 });
  return editor;
}

function getStepRow(editor: Locator, index: number): Locator {
  const stepName = editor.getByLabel('Step name', { exact: true }).nth(index);
  return stepName.locator('xpath=ancestor::*[contains(concat(" ", normalize-space(@class), " "), " mud-paper ")][1]');
}

async function chooseWriteLine(page: Page, row: Locator, fieldLabel: 'Forward activity' | 'Inline handler'): Promise<void> {
  await row.getByLabel(fieldLabel, { exact: true }).click();
  const option = page.getByRole('option').filter({ hasText: /write\s*line/i }).first();
  await option.waitFor({ state: 'visible', timeout: 8_000 });
  await option.click();
}

async function setSelectedChildLiteral(page: Page, row: Locator, childRole: 'forward' | 'handler', value: string): Promise<void> {
  await row.getByRole('button', { name: `Select ${childRole} activity properties`, exact: true }).click();
  const inputsTab = page.getByRole('tab', { name: 'Input', exact: true });
  await inputsTab.waitFor({ state: 'visible', timeout: 8_000 });
  await inputsTab.click();
  const textInput = page.getByLabel('Text', { exact: true });
  await textInput.waitFor({ state: 'visible', timeout: 8_000 });
  await textInput.fill(value);
  await textInput.press('Tab');
  await expect(textInput).toHaveValue(value);
}

function runSqliteSnapshot(workflowName: string, definitionId: string, expectedSteps: ExpectedStep[]): Promise<SqliteSnapshot> {
  return new Promise((resolve, reject) => {
    const safeEnvironment = {
      PATH: process.env.PATH ?? '',
      LANG: process.env.LANG ?? 'C',
      LC_ALL: process.env.LC_ALL ?? 'C'
    };
    const child = spawn(pythonPath, [verifierPath, '--database', sqlitePath, '--mode', 'snapshot'], {
      env: safeEnvironment,
      stdio: ['pipe', 'pipe', 'ignore'],
      shell: false
    });
    let output = '';
    let settled = false;
    const timeout = setTimeout(() => {
      if (!settled) {
        settled = true;
        child.kill('SIGTERM');
        setTimeout(() => child.kill('SIGKILL'), 250).unref();
        reject(new Error('sqlite-verifier-timeout'));
      }
    }, 8_000);

    child.stdout.on('data', (chunk: Buffer) => {
      output += chunk.toString('utf8');
      if (output.length > 2_048 && !settled) {
        settled = true;
        child.kill('SIGKILL');
        clearTimeout(timeout);
        reject(new Error('sqlite-verifier-output-invalid'));
      }
    });
    child.on('error', () => {
      if (settled)
        return;
      settled = true;
      clearTimeout(timeout);
      reject(new Error('sqlite-verifier-start-failed'));
    });
    child.on('close', code => {
      if (settled)
        return;
      settled = true;
      clearTimeout(timeout);
      if (code !== 0) {
        reject(new Error('sqlite-verifier-failed'));
        return;
      }

      try {
        const parsed = JSON.parse(output) as Partial<SqliteSnapshot>;
        if (parsed.schemaVersion !== 2 ||
            parsed.status !== 'OK' ||
            parsed.unpublished !== true ||
            parsed.literalValuesVerified !== true ||
            !Number.isInteger(parsed.definitionRowCount) ||
            typeof parsed.identitySha256 !== 'string' || !/^[a-f0-9]{64}$/.test(parsed.identitySha256) ||
            typeof parsed.layoutSha256 !== 'string' || !/^[a-f0-9]{64}$/.test(parsed.layoutSha256) ||
            typeof parsed.rowSetSha256 !== 'string' || !/^[a-f0-9]{64}$/.test(parsed.rowSetSha256))
          throw new Error('sqlite-verifier-result-invalid');
        if (typeof parsed.diagnosticTargetsSha256 !== 'string' || !/^[a-f0-9]{64}$/.test(parsed.diagnosticTargetsSha256))
          throw new Error('sqlite-verifier-result-invalid');
        resolve(parsed as SqliteSnapshot);
      } catch {
        reject(new Error('sqlite-verifier-result-invalid'));
      }
    });

    child.stdin.end(JSON.stringify({ workflowName, definitionId, expectedChildVersion, expectedSteps }));
  });
}

async function writePrivateCapture(
  workflowName: string,
  definitionId: string,
  identitySha256: string,
  expectedSteps: ExpectedStep[]
): Promise<void> {
  const temporary = privateCapturePath + '.tmp';
  const payload = {
    schemaVersion: 2,
    workflowName,
    definitionId,
    identitySha256,
    expectedChildVersion,
    expectedSteps
  };
  await writeFile(temporary, JSON.stringify(payload) + '\n', { mode: 0o600, flag: 'w' });
  await rename(temporary, privateCapturePath);
  await chmod(privateCapturePath, 0o600);
}

type ChromiumProcessInfo = { id: number; type: string };
type ChromiumProcessSnapshot = {
  capturedAtUnixMs: number;
  ownerPid: number;
  ownerParentPid: number;
  processes: ChromiumProcessInfo[];
};

async function capturePrivateBrowserProcessSnapshot(browser: Browser): Promise<ChromiumProcessInfo[]> {
  const processes = await getChromiumProcessInfo(browser);
  const snapshot: ChromiumProcessSnapshot = {
    capturedAtUnixMs: Date.now(),
    ownerPid: process.pid,
    ownerParentPid: process.ppid,
    processes
  };

  let snapshots: ChromiumProcessSnapshot[] = [];
  try {
    const previous = JSON.parse(await readFile(browserProcessCapturePath, 'utf8')) as {
      schemaVersion?: unknown;
      source?: unknown;
      snapshots?: unknown;
    };
    if (previous.schemaVersion !== 1 || previous.source !== 'Chromium-CDP-SystemInfo.getProcessInfo' || !Array.isArray(previous.snapshots))
      throw new Error('chromium-process-capture-invalid');
    snapshots = previous.snapshots as ChromiumProcessSnapshot[];
  } catch (error) {
    if ((error as NodeJS.ErrnoException).code !== 'ENOENT')
      throw new Error('chromium-process-capture-invalid');
  }
  snapshots.push(snapshot);
  const temporary = browserProcessCapturePath + '.tmp';
  const payload = { schemaVersion: 1, source: 'Chromium-CDP-SystemInfo.getProcessInfo', snapshots };
  await writeFile(temporary, JSON.stringify(payload) + '\n', { mode: 0o600, flag: 'w' });
  await rename(temporary, browserProcessCapturePath);
  await chmod(browserProcessCapturePath, 0o600);
  return [...new Map(snapshots.flatMap(item => item.processes).map(item => [`${item.id}:${item.type}`, item])).values()];
}

async function writeSafeBrowserProcessEvidence(
  processes: ChromiumProcessInfo[],
  processCaptureSucceeded: boolean,
  contextCloseCompleted: boolean,
  browserCloseCompleted: boolean,
  allReportedProcessesExited: boolean
): Promise<void> {
  const processProjection: Array<[number, string]> = processes
    .map(({ id, type }): [number, string] => [id, type])
    .sort(([leftId, leftType], [rightId, rightType]) => leftId - rightId || leftType.localeCompare(rightType));
  const payload = {
    schemaVersion: 1,
    source: 'Chromium-CDP-SystemInfo.getProcessInfo',
    browserProcessSeen: processCaptureSucceeded && processes.some(process => process.type === 'browser'),
    processCount: processes.length,
    processProjectionSha256: createHash('sha256').update(JSON.stringify(processProjection)).digest('hex'),
    contextCloseCompleted,
    browserCloseCompleted,
    allReportedProcessesExited
  };
  const temporary = browserProcessEvidencePath + '.tmp';
  await writeFile(temporary, JSON.stringify(payload, null, 2) + '\n', { mode: 0o600, flag: 'w' });
  await rename(temporary, browserProcessEvidencePath);
  await chmod(browserProcessEvidencePath, 0o600);
}

async function getChromiumProcessInfo(browser: Browser): Promise<ChromiumProcessInfo[]> {
  const session = await browser.newBrowserCDPSession();
  try {
    const result = await session.send('SystemInfo.getProcessInfo') as { processInfo?: unknown };
    if (!Array.isArray(result.processInfo))
      throw new Error('chromium-process-info-missing');
    const processes = result.processInfo.map((entry: unknown) => {
      if (!entry || typeof entry !== 'object')
        throw new Error('chromium-process-info-invalid');
      const item = entry as { id?: unknown; type?: unknown };
      if (!Number.isInteger(item.id) || (item.id as number) <= 0 || typeof item.type !== 'string')
        throw new Error('chromium-process-info-invalid');
      return { id: item.id as number, type: item.type };
    });
    if (!processes.some(process => process.type === 'browser') || new Set(processes.map(process => process.id)).size !== processes.length)
      throw new Error('chromium-process-ownership-unverified');
    return processes;
  } finally {
    await session.detach();
  }
}

function processExists(pid: number): boolean {
  try {
    process.kill(pid, 0);
    return true;
  } catch (error) {
    if ((error as NodeJS.ErrnoException).code === 'ESRCH')
      return false;
    return true;
  }
}

async function waitForChromiumExit(processes: ChromiumProcessInfo[]): Promise<boolean> {
  const deadline = Date.now() + 10_000;
  while (Date.now() < deadline) {
    if (processes.every(({ id }) => !processExists(id)))
      return true;
    await new Promise(resolve => setTimeout(resolve, 100));
  }
  return processes.every(({ id }) => !processExists(id));
}

/*
 * The supervisor polls the private snapshot file while this test is running,
 * attaching exact OS birth/parent/process-group identities before a timeout
 * can interrupt this worker. The file contains no command lines or profiles.
 */
test.beforeEach(async ({ browser }) => {
  await capturePrivateBrowserProcessSnapshot(browser);
});

test('native compensation draft persists two paired rows and refuses UI publish', async ({ page }) => {
  test.setTimeout(150_000);
  page.setDefaultTimeout(8_000);
  page.setDefaultNavigationTimeout(15_000);
  await persistSafeResults();

  const workflowName = 'native-compensation-draft-' + randomUUID();
  let definitionId = '';
  let editor!: Locator;
  let baselineSnapshot!: SqliteSnapshot;
  let reorderedSnapshot!: SqliteSnapshot;
  let reopenedSnapshot!: SqliteSnapshot;

  await assertion('NC-DRAFT-LOGIN', 'normal-login-failed', async () => {
    await page.goto('/login');
    const username = page.getByLabel('User name', { exact: true });
    const password = page.getByLabel('Password', { exact: true });
    const signIn = page.getByRole('button', { name: 'Sign in', exact: true });

    // Prerendered fields are visible before the Server circuit can handle events. Empty-submit validation
    // exercises the managed click path without reaching credential validation; wait for its DOM state.
    await expect.poll(async () => {
      const valuesBefore = [await username.inputValue(), await password.inputValue()];
      if (valuesBefore.some(value => value !== ''))
        return { emptyBefore: false, emptyAfter: false, usernameInvalid: false, passwordInvalid: false };

      await signIn.click();
      const valuesAfter = [await username.inputValue(), await password.inputValue()];
      return {
        emptyBefore: true,
        emptyAfter: valuesAfter.every(value => value === ''),
        usernameInvalid: (await username.getAttribute('aria-invalid')) === 'true',
        passwordInvalid: (await password.getAttribute('aria-invalid')) === 'true'
      };
    }, {
      message: 'Wait for interactive login validation before entering credentials',
      timeout: 15_000,
      intervals: [100, 250, 500]
    }).toEqual({ emptyBefore: true, emptyAfter: true, usernameInvalid: true, passwordInvalid: true });

    await username.fill(adminUsername);
    await password.fill(adminPassword);
    await signIn.click();
    await page.waitForURL(url => !url.pathname.endsWith('/login'), { timeout: 15_000 });
  });

  await assertion('NC-DRAFT-CREATE', 'workflow-create-or-navigation-failed', async () => {
    await page.goto('/workflows/definitions');
    await requireVisible(page.getByRole('heading', { name: 'Workflow definitions Editor' }));
    await verifyInteractiveStudioShell(page);
    await page.getByRole('button', { name: 'Create workflow', exact: true }).click();
    const dialog = page.getByRole('dialog');
    await dialog.getByLabel('Name', { exact: true }).fill(workflowName);
    await dialog.getByRole('button', { name: 'Ok', exact: true }).click();
    await page.waitForURL(/\/workflows\/definitions\/[^/]+\/edit(?:$|[?#])/, { timeout: 15_000 });
    definitionId = decodeURIComponent(new URL(page.url()).pathname.split('/').filter(Boolean).at(-2) ?? '');
    requireCondition(definitionId.length > 0, 'definition-id-not-created');
  });

  await assertion('NC-DRAFT-SCOPE-OPENED-VIA-UI', 'scope-picker-or-double-click-unavailable', async () => {
    const search = page.getByLabel('Search...', { exact: true });
    await search.fill('Compensation');
    const tree = page.locator('.mud-treeview');
    const flowCategoryLabel = tree.locator('.mud-treeview-item-label')
      .filter({ hasText: /^\s*Flow\s*$/ });
    const scopeCard = tree.locator('.mud-treeview-item[draggable="true"]')
      .filter({ hasText: /^\s*Compensation Scope\s*$/i });
    await requireVisible(flowCategoryLabel);
    if (!(await scopeCard.isVisible())) {
      await flowCategoryLabel.click();
    }
    await requireVisible(scopeCard);
    await scopeCard.dragTo(page.locator('.flowchart-diagram-designer-wrapper'));
    editor = await openCompensationScope(page);
    await requireVisible(editor.getByRole('button', { name: 'Add compensation step', exact: true }));
  });

  await assertion('NC-DRAFT-TWO-STEP-KEYBOARD', 'two-step-keyboard-creation-unavailable', async () => {
    const addStep = editor.getByRole('button', { name: 'Add compensation step', exact: true });
    const stepNames = editor.getByLabel('Step name', { exact: true });
    for (let index = 0; index < initialSteps.length; index++) {
      await expect(addStep).toBeEnabled();
      await addStep.focus();
      await addStep.press('Enter');
      await expect(stepNames).toHaveCount(index + 1);
      await stepNames.nth(index).fill(initialSteps[index].stepName);
    }
  });

  await assertion('NC-DRAFT-PAIRED-CHILDREN', 'paired-child-controls-unavailable', async () => {
    for (let index = 0; index < initialSteps.length; index++) {
      const row = getStepRow(editor, index);
      await chooseWriteLine(page, row, 'Forward activity');
      await chooseWriteLine(page, row, 'Inline handler');
      await requireVisible(row.getByLabel('Forward activity', { exact: true }));
      await requireVisible(row.getByLabel('Inline handler', { exact: true }));
    }
  });

  await assertion('NC-DRAFT-INPUT-LITERALS', 'child-input-authoring-unavailable', async () => {
    for (let index = 0; index < initialSteps.length; index++) {
      const row = getStepRow(editor, index);
      await setSelectedChildLiteral(page, row, 'forward', initialSteps[index].forwardLiteral);
      await setSelectedChildLiteral(page, row, 'handler', initialSteps[index].handlerLiteral);
    }
  });

  async function saveDraft(): Promise<void> {
    const savedNotice = page.getByText('Workflow saved', { exact: true });
    await savedNotice.waitFor({ state: 'hidden', timeout: 10_000 }).catch(() => undefined);
    await page.keyboard.press(process.platform === 'darwin' ? 'Meta+s' : 'Control+s');
    await savedNotice.waitFor({ state: 'visible', timeout: 15_000 });
  }

  await assertion('NC-DRAFT-SAVE-BASELINE', 'baseline-draft-save-not-confirmed', saveDraft);

  await assertion('NC-DRAFT-SQLITE-BASELINE', 'baseline-draft-sqlite-check-failed', async () => {
    baselineSnapshot = await runSqliteSnapshot(workflowName, definitionId, initialSteps);
    requireCondition(baselineSnapshot.definitionRowCount >= 1, 'baseline-definition-rows-missing');
  });

  await assertion('NC-DRAFT-RENAME-AND-REORDER', 'step-rename-or-reorder-unavailable', async () => {
    const firstRow = getStepRow(editor, 0);
    await firstRow.getByLabel('Step name', { exact: true }).fill('Reserve stock');
    await firstRow.getByRole('button', { name: 'Move step down', exact: true }).click();
    const stepNames = editor.getByLabel('Step name', { exact: true });
    await expect(stepNames.nth(0)).toHaveValue('Release inventory');
    await expect(stepNames.nth(1)).toHaveValue('Reserve stock');
  });

  await assertion('NC-DRAFT-SAVE-REORDERED', 'reordered-draft-save-not-confirmed', saveDraft);

  await assertion('NC-DRAFT-SQLITE-REORDERED', 'reordered-draft-sqlite-check-failed', async () => {
    reorderedSnapshot = await runSqliteSnapshot(workflowName, definitionId, reorderedSteps);
    requireCondition(reorderedSnapshot.identitySha256 === baselineSnapshot.identitySha256,
      'identity-fingerprint-changed-during-rename-or-reorder');
    requireCondition(reorderedSnapshot.layoutSha256 !== baselineSnapshot.layoutSha256,
      'layout-fingerprint-did-not-change-after-rename-or-reorder');
  });

  await assertion('NC-DRAFT-REOPEN', 'saved-draft-reopen-failed', async () => {
    await page.goto('/workflows/definitions');
    const row = page.getByRole('row').filter({ hasText: workflowName }).first();
    await row.getByText(workflowName, { exact: true }).click();
    await page.waitForURL(/\/workflows\/definitions\/[^/]+\/edit(?:$|[?#])/, { timeout: 15_000 });
    editor = await openCompensationScope(page);
    const stepNames = editor.getByLabel('Step name', { exact: true });
    await expect(stepNames).toHaveCount(2);
    await expect(stepNames.nth(0)).toHaveValue('Release inventory');
    await expect(stepNames.nth(1)).toHaveValue('Reserve stock');
  });

  await assertion('NC-DRAFT-REOPEN-STABLE-IDENTITY', 'reopen-roundtrip-check-failed', async () => {
    reopenedSnapshot = await runSqliteSnapshot(workflowName, definitionId, reorderedSteps);
    requireCondition(reopenedSnapshot.identitySha256 === reorderedSnapshot.identitySha256,
      'identity-fingerprint-changed-after-reopen');
    requireCondition(reopenedSnapshot.layoutSha256 === reorderedSnapshot.layoutSha256,
      'layout-fingerprint-changed-after-reopen');
    requireCondition(reopenedSnapshot.rowSetSha256 === reorderedSnapshot.rowSetSha256,
      'stored-definition-rows-changed-after-reopen');
  });

  await assertion('NC-DRAFT-PRIVATE-CAPTURE', 'private-capture-write-failed', async () => {
    await writePrivateCapture(workflowName, definitionId, reopenedSnapshot.identitySha256, reorderedSteps);
  });

  let beforeRefusalSnapshot!: SqliteSnapshot;
  await assertion('NC-DRAFT-PUBLISH-UI-REFUSAL', 'fixed-publish-refusal-not-rendered', async () => {
    beforeRefusalSnapshot = await runSqliteSnapshot(workflowName, definitionId, reorderedSteps);
    requireCondition(beforeRefusalSnapshot.rowSetSha256 === reopenedSnapshot.rowSetSha256,
      'stored-definition-rows-changed-before-publish-click');
    const publishButton = page.getByRole('button', { name: 'Publish workflow', exact: true });
    await publishButton.click();
    const summary = page.getByRole('alert', { name: 'Native compensation publication refusal', exact: true });
    await expect(summary).toBeVisible({ timeout: 15_000 });
    await expect(summary.getByText(draftOnlyHeading, { exact: true })).toBeVisible();
    await expect(summary).toBeFocused();
    await expect(summary.locator('li')).toHaveCount(6);
    const diagnosticTargets = await summary.locator('li').evaluateAll(items =>
      items.map(item => Array.from(item.querySelectorAll('code'), code => code.textContent ?? '')));
    diagnosticTargets.sort((left, right) => {
      const leftJson = JSON.stringify(left), rightJson = JSON.stringify(right);
      return leftJson < rightJson ? -1 : leftJson > rightJson ? 1 : 0;
    });
    requireCondition(createHash('sha256').update(JSON.stringify(diagnosticTargets)).digest('hex') ===
      beforeRefusalSnapshot.diagnosticTargetsSha256, 'publish-summary-targets-mismatch');
  });

  await assertion('NC-DRAFT-ROWSET-UNCHANGED-ON-REFUSAL', 'publish-refusal-mutated-stored-definition-rows', async () => {
    const afterRefusalSnapshot = await runSqliteSnapshot(workflowName, definitionId, reorderedSteps);
    requireCondition(afterRefusalSnapshot.unpublished, 'post-refusal-latest-row-is-published');
    requireCondition(afterRefusalSnapshot.identitySha256 === beforeRefusalSnapshot.identitySha256,
      'post-refusal-identity-changed');
    requireCondition(afterRefusalSnapshot.layoutSha256 === beforeRefusalSnapshot.layoutSha256,
      'post-refusal-layout-changed');
    requireCondition(afterRefusalSnapshot.rowSetSha256 === beforeRefusalSnapshot.rowSetSha256,
      'post-refusal-definition-row-set-changed');
  });
});

test.afterEach(async ({ browser, context }) => {
  let processes: ChromiumProcessInfo[] = [];
  let processCaptureSucceeded = false;
  let contextCloseCompleted = false;
  let browserCloseCompleted = false;
  let allReportedProcessesExited = false;

  try {
    processes = await capturePrivateBrowserProcessSnapshot(browser);
    processCaptureSucceeded = true;
    records.set('NC-DRAFT-CHROMIUM-PROCESS-CAPTURE', {
      id: 'NC-DRAFT-CHROMIUM-PROCESS-CAPTURE', status: 'PASS', code: 'checked'
    });
  } catch {
    records.set('NC-DRAFT-CHROMIUM-PROCESS-CAPTURE', {
      id: 'NC-DRAFT-CHROMIUM-PROCESS-CAPTURE', status: 'FAIL', code: 'process-capture-failed'
    });
  }

  try {
    await context.close();
    contextCloseCompleted = true;
    records.set('NC-DRAFT-CONTEXT-CLOSED', {
      id: 'NC-DRAFT-CONTEXT-CLOSED', status: 'PASS', code: 'checked'
    });
  } catch {
    records.set('NC-DRAFT-CONTEXT-CLOSED', {
      id: 'NC-DRAFT-CONTEXT-CLOSED', status: 'FAIL', code: 'context-close-failed'
    });
  }

  try {
    await browser.close();
    browserCloseCompleted = true;
  } catch {
    browserCloseCompleted = false;
  }

  if (processCaptureSucceeded && processes.length > 0)
    allReportedProcessesExited = await waitForChromiumExit(processes);

  try {
    await writeSafeBrowserProcessEvidence(
      processes,
      processCaptureSucceeded,
      contextCloseCompleted,
      browserCloseCompleted,
      processCaptureSucceeded && processes.length > 0 && allReportedProcessesExited
    );
  } catch {
    allReportedProcessesExited = false;
  }

  records.set('NC-DRAFT-CHROMIUM-PROCESS-EXIT', {
    id: 'NC-DRAFT-CHROMIUM-PROCESS-EXIT',
    status: processCaptureSucceeded && processes.length > 0 && contextCloseCompleted && browserCloseCompleted && allReportedProcessesExited
      ? 'PASS'
      : 'FAIL',
    code: processCaptureSucceeded && processes.length > 0 && contextCloseCompleted && browserCloseCompleted && allReportedProcessesExited
      ? 'checked'
      : 'chromium-process-exit-unconfirmed'
  });

  await persistSafeResults();
  if (!processCaptureSucceeded || !contextCloseCompleted || !browserCloseCompleted || !allReportedProcessesExited)
    throw new Error('browser-cleanup-incomplete');
});

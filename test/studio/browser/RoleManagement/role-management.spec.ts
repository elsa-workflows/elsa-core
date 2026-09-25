import AxeBuilder from '@axe-core/playwright';
import { randomUUID } from 'node:crypto';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';
import type { Page, TestInfo } from '@playwright/test';
import { CoreApiSession, expect, openRoles, signIn, test, assertCleanRuntime } from './fixtures';

const roleHostConfigured = ['SERVER', 'WASM'].some(prefix =>
  Boolean(process.env[`ROLE_E2E_${prefix}_STUDIO_URL`] && process.env[`ROLE_E2E_${prefix}_BACKEND_URL`]));
const actorsConfigured = Boolean(process.env.ROLE_E2E_ADMIN_USERNAME && process.env.ROLE_E2E_ADMIN_PASSWORD);

test.skip(!roleHostConfigured || !actorsConfigured,
  'Set local Studio/Core URLs and ROLE_E2E_ADMIN_USERNAME/ROLE_E2E_ADMIN_PASSWORD to run real-host role tests.');

function roleName(prefix: string): string {
  return `${prefix}-${randomUUID()}`;
}

function roleIdFromUrl(url: string): string {
  const segment = new URL(url).pathname.split('/').filter(Boolean).pop();
  if (!segment)
    throw new Error('The role editor did not navigate to a role ID.');
  return decodeURIComponent(segment);
}

async function captureEvidence(page: Page, testInfo: TestInfo, label: string): Promise<void> {
  const configuredDirectory = process.env.ROLE_E2E_EVIDENCE_DIR?.trim();
  if (!configuredDirectory)
    return;

  const repositoryRoot = path.resolve(process.cwd(), '../../..');
  const evidenceDirectory = path.resolve(process.cwd(), configuredDirectory);
  if (evidenceDirectory !== repositoryRoot && !evidenceDirectory.startsWith(`${repositoryRoot}${path.sep}`))
    throw new Error('ROLE_E2E_EVIDENCE_DIR must resolve inside the Studio repository.');

  await mkdir(evidenceDirectory, { recursive: true });
  await page.screenshot({
    path: path.join(evidenceDirectory, `${testInfo.project.name}-${label}.png`),
    fullPage: true
  });
}

async function createRoleInEditor(
  page: Page,
  adminApi: CoreApiSession,
  registerRole: (id: string) => void,
  name: string
): Promise<string> {
  await page.goto('/security/roles/new');
  await expect(page.getByRole('heading', { name: 'New role' })).toBeVisible();
  await page.getByLabel('Role name').fill(name);

  await page.getByLabel('Filter permissions').fill('identity/roles');
  const viewGrant = page.getByLabel('identity/roles:view');
  await expect(viewGrant).toHaveCount(1);
  const categoryBulk = page.getByRole('button', { name: 'Select all exact', exact: true }).first();
  await expect(categoryBulk).toBeVisible();
  await categoryBulk.click();
  await expect(viewGrant).toBeChecked();

  const advancedTab = page.locator('[role="tab"]').filter({ hasText: 'Advanced grants' }).first();
  await advancedTab.click();
  await expect(page.getByText('No advanced grants.', { exact: false })).toBeVisible();
  await page.getByRole('textbox', { name: 'Advanced grant' }).fill('identity/roles:*');
  await page.getByRole('button', { name: 'Add advanced grant' }).click();
  await expect(page.getByText('identity/roles:*', { exact: true })).toBeVisible();
  await expect(page.getByText(/\d+ resources today/)).toBeVisible();
  await expect(page.getByText('Future reach:', { exact: false })).toBeVisible();

  const createButton = page.getByRole('button', { name: 'Create role', exact: true });
  try {
    await expect(createButton).toBeEnabled();
  } catch {
    const pageText = (await page.locator('body').innerText()).replaceAll(name, '[role-name]');
    throw new Error(`Create role remained disabled. ${pageText.slice(0, 1200)}`);
  }
  await createButton.click();
  await expect(page).not.toHaveURL(/\/security\/roles\/new(?:$|[?#])/);
  await expect(page).toHaveURL(/\/security\/roles\/[^/]+$/);
  const id = roleIdFromUrl(page.url());
  registerRole(id);
  await expect.poll(async () => (await adminApi.findRole(id))?.name).toBe(name);
  return id;
}

async function openDeletionDialog(page: Page, actor: Parameters<typeof openRoles>[1], roleId: string): Promise<void> {
  await signIn(page, actor);
  await page.goto(`/security/roles/${encodeURIComponent(roleId)}`);
  await expect(page.getByRole('button', { name: 'Delete role' })).toBeVisible();
  await page.getByRole('button', { name: 'Delete role' }).click();
}

async function confirmRemediation(page: Page): Promise<void> {
  const dialog = page.locator('.delete-role-dialog').last();
  await expect(dialog).toBeVisible();
  const confirmations = dialog.getByRole('checkbox');
  for (let index = 0; index < await confirmations.count(); index++) {
    const checkbox = confirmations.nth(index);
    if (await checkbox.isVisible() && !(await checkbox.isChecked()))
      await checkbox.check();
  }
}

test.describe('role management against a real Core host', () => {
  test('administrator completes create, save, reload, update, reload, and safe delete', async ({ page, config, adminApi, registerRole, diagnostics }) => {
    await openRoles(page, config.admin);
    const name = roleName('browser-role');
    const id = await createRoleInEditor(page, adminApi, registerRole, name);

    await page.reload();
    await expect(page.getByLabel('Role name')).toHaveValue(name);
    await expect(page.getByLabel('identity/roles:view')).toBeChecked();
    const advancedTab = page.locator('[role="tab"]').filter({ hasText: /Advanced grants/ }).first();
    await advancedTab.click();
    await expect(page.getByText('identity/roles:*', { exact: true })).toBeVisible();
    await expect(page.getByText(/Future reach:/)).toBeVisible();

    const updatedName = `${name}-updated`;
    await page.getByLabel('Role name').fill(updatedName);
    await page.getByRole('button', { name: 'Save changes', exact: true }).click();
    await expect.poll(async () => (await adminApi.findRole(id))?.name).toBe(updatedName);

    await page.reload();
    await expect(page.getByLabel('Role name')).toHaveValue(updatedName);
    await expect(page.getByText(id, { exact: true })).toBeVisible();

    await page.getByRole('button', { name: 'Delete role', exact: true }).click();
    await expect(page.getByTestId('role-deletion-safe')).toBeVisible();
    await expect(page.getByText(/Existing access tokens are not changed/)).toBeVisible();
    await page.getByRole('button', { name: `Delete ${updatedName}`, exact: true }).click();
    await expect(page).toHaveURL(/\/security\/roles(?:$|[?#])/);
    await expect.poll(async () => await adminApi.findRole(id)).toBeUndefined();

    // The role was deleted by the UI; cleanup accepts the expected 404.
    await assertCleanRuntime(diagnostics);
  });

  test('role list is keyboard accessible and switches layout at the responsive breakpoint', async ({ page, config, adminApi, registerRole, diagnostics }, testInfo) => {
    const fixture = await adminApi.createRole(roleName('responsive-role'), ['identity/roles:view']);
    registerRole(fixture.id);
    await openRoles(page, config.admin);
    const width = page.viewportSize()?.width ?? 0;

    const search = page.getByLabel('Search roles by name, ID, or permission');
    await expect(search).toBeVisible();
    await expect(page.getByRole('status')).toContainText(/all loaded|matching search/);

    if (width < 960) {
      await expect(page.locator('.roles-mobile-list')).toBeVisible();
      await expect(page.locator('.roles-desktop-list')).toBeHidden();
    } else {
      await expect(page.locator('.roles-desktop-list')).toBeVisible();
      await expect(page.locator('.roles-mobile-list')).toBeHidden();
    }

    await search.focus();
    await expect(search).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.locator(':focus')).toBeVisible();

    const results = await new AxeBuilder({ page }).analyze();
    const blockingViolations = results.violations.filter(violation =>
      violation.impact === 'serious' || violation.impact === 'critical');
    await captureEvidence(page, testInfo, 'roles-list');
    const violationSummary = blockingViolations
      .map(violation => `${violation.id}: ${violation.nodes.map(node => node.target.join(' ')).join(', ')}`)
      .join('\n');
    expect(blockingViolations, violationSummary).toHaveLength(0);
    await assertCleanRuntime(diagnostics);
  });

  test('role search reports an explicit filtered-empty state', async ({ page, config, adminApi, registerRole, diagnostics }) => {
    const fixture = await adminApi.createRole(roleName('filtered-role'), ['identity/roles:view']);
    registerRole(fixture.id);
    await openRoles(page, config.admin);

    const search = page.getByLabel('Search roles by name, ID, or permission');
    await search.fill(`no-match-${randomUUID()}`);
    await expect(page.getByRole('status')).toContainText('0 roles · matching search');
    await expect(page.getByRole('heading', { name: 'No matching roles' })).toBeVisible();
    await expect(page.getByText('Try a different name, ID, or permission.')).toBeVisible();
    await assertCleanRuntime(diagnostics);
  });

  test('restricted actor sees read-only roles and Core rejects mutation', async ({ page, request, config, adminApi, diagnostics }) => {
    const fixture = config.restricted
      ? undefined
      : await adminApi.createRole(roleName('restricted-role'), [
          'identity/roles:view',
          'system/features:view',
          'user-tasks:view'
        ]);
    const generatedUser = fixture ? await adminApi.createUser(roleName('restricted-user'), [fixture.id]) : undefined;
    const restrictedActor = config.restricted ?? { username: generatedUser!.name, password: generatedUser!.password };
    const restrictedSession = await CoreApiSession.signIn(request, config.backendUrl, restrictedActor);
    try {
      await openRoles(page, restrictedActor);
      await expect(page.getByText('You can view roles, but you cannot create, edit, or delete them.')).toBeVisible();
      await expect(page.getByRole('button', { name: 'New role', exact: true })).toHaveCount(0);
      await page.goto('/security/roles/new');
      await expect(page.getByText('You can view roles, but your current sign-in cannot create a role.')).toBeVisible();
      await restrictedSession.expectForbiddenRoleCreation();
      await assertCleanRuntime(diagnostics);
    } finally {
      if (generatedUser)
        await adminApi.deleteUser(generatedUser.id);
      if (fixture)
        await adminApi.deleteRole(fixture.id);
    }
  });

  test('role changes reach an existing user only after token refresh', async ({ request, config, adminApi }) => {
    const assignedRole = await adminApi.createRole(roleName('refresh-role'), ['identity/roles:view']);
    const generatedUser = await adminApi.createUser(roleName('refresh-user'), [assignedRole.id]);
    let createdAfterRefresh: string | undefined;
    try {
      const originalSession = await CoreApiSession.signIn(request, config.backendUrl, {
        username: generatedUser.name,
        password: generatedUser.password
      });
      await adminApi.updateRole(assignedRole.id, assignedRole.name, ['identity/roles:view', 'identity/roles:create']);

      await originalSession.expectForbiddenRoleCreation();
      const refreshedSession = await originalSession.refresh();
      const created = await refreshedSession.createRole(roleName('refresh-proof'));
      createdAfterRefresh = created.id;
    } finally {
      if (createdAfterRefresh)
        await adminApi.deleteRole(createdAfterRefresh);
      await adminApi.deleteUser(generatedUser.id);
      await adminApi.deleteRole(assignedRole.id);
    }
  });

  test('recognized unverified catalog entries remain selectable', async ({ page, config, diagnostics }) => {
    test.skip(!config.requireUnverifiedCatalog,
      'Set ROLE_E2E_REQUIRE_UNVERIFIED_CATALOG=true only when the isolated Core catalog includes a verified:false descriptor.');
    await signIn(page, config.admin);
    await page.goto('/security/roles/new');
    await page.getByLabel('Filter permissions').fill('e2e/role-management/unverified');

    const marker = page.getByText('Unverified · verified:false', { exact: true }).first();
    await expect(marker).toBeVisible();
    const row = marker.locator('xpath=ancestor::div[contains(@class,"role-resource-row")]');
    const grant = row.getByRole('checkbox').first();
    await expect(grant).toBeEnabled();
    await grant.check();
    await expect(grant).toBeChecked();
    await assertCleanRuntime(diagnostics);
  });
});

test.describe('optional real-host deletion dependency outcomes', () => {
  test('configuration-owned dependency is visibly blocked without a mutation action', async ({ page, config, diagnostics }) => {
    test.skip(!config.blockedRoleId, 'Set ROLE_E2E_BLOCKED_ROLE_ID to an isolated role referenced by configuration.');
    await openDeletionDialog(page, config.admin, config.blockedRoleId!);
    await expect(page.getByTestId('role-deletion-blocked')).toBeVisible();
    await expect(page.getByText('cannot be changed from Studio')).toBeVisible();
    await expect(page.getByRole('button', { name: /Apply remediation & delete/ })).toHaveCount(0);
    await assertCleanRuntime(diagnostics);
  });

  test('editable dependency remediation submits the inspected version and deletes after confirmation', async ({ page, config, adminApi, diagnostics }) => {
    test.skip(!config.remediableRoleId, 'Set ROLE_E2E_REMEDIABLE_ROLE_ID to an isolated role with database-owned editable references.');
    await openDeletionDialog(page, config.admin, config.remediableRoleId!);
    await expect(page.getByTestId('role-deletion-remediation')).toBeVisible();
    await confirmRemediation(page);
    await page.getByRole('button', { name: 'Apply remediation & delete', exact: true }).click();
    await expect(page).toHaveURL(/\/security\/roles(?:$|[?#])/);
    await expect.poll(async () => await adminApi.findRole(config.remediableRoleId!)).toBeUndefined();
    await assertCleanRuntime(diagnostics);
  });

  test('dependency conflict clears confirmation and presents refreshed impact', async ({ page, config, adminApi, diagnostics }) => {
    test.skip(!config.conflictRoleId || !config.conflictTriggerUrl,
      'Set ROLE_E2E_CONFLICT_ROLE_ID and a local ROLE_E2E_CONFLICT_TRIGGER_URL fixture hook.');
    await openDeletionDialog(page, config.admin, config.conflictRoleId!);
    await expect(page.getByTestId('role-deletion-remediation')).toBeVisible();
    await confirmRemediation(page);
    await adminApi.triggerLocalFixture(config.conflictTriggerUrl!, config.conflictRoleId!);
    await page.getByRole('button', { name: 'Apply remediation & delete', exact: true }).click();
    await expect(page.getByTestId('role-deletion-conflict')).toBeVisible();
    await expect(page.getByText(/previous confirmation was cleared/)).toBeVisible();
    await assertCleanRuntime(diagnostics);
  });

  test('incomplete remediation retains the role and reports changed and remaining owners', async ({ page, config, adminApi, diagnostics }) => {
    test.skip(!config.incompleteRoleId || !config.incompleteTriggerUrl,
      'Set ROLE_E2E_INCOMPLETE_ROLE_ID and a local ROLE_E2E_INCOMPLETE_TRIGGER_URL fixture hook.');
    await openDeletionDialog(page, config.admin, config.incompleteRoleId!);
    await expect(page.getByTestId('role-deletion-remediation')).toBeVisible();
    await confirmRemediation(page);
    await adminApi.triggerLocalFixture(config.incompleteTriggerUrl!, config.incompleteRoleId!);
    await page.getByRole('button', { name: 'Apply remediation & delete', exact: true }).click();
    await expect(page.getByTestId('role-deletion-incomplete')).toBeVisible();
    await expect(page.getByText('Role retained')).toBeVisible();
    await expect(page.getByText(/Changed:/)).toBeVisible();
    await expect(page.getByText(/Remaining:/)).toBeVisible();
    await assertCleanRuntime(diagnostics);
  });

  test('unresolved legacy grants stay verbatim and block save until repaired', async ({ page, config, adminApi, diagnostics }) => {
    test.skip(!config.unresolvedRoleId,
      'Set ROLE_E2E_UNRESOLVED_ROLE_ID to an isolated database-seeded role containing a legacy grant. Core rejects unknown grants on create/update.');
    await openRoles(page, config.admin);
    await page.goto(`/security/roles/${encodeURIComponent(config.unresolvedRoleId!)}`);

    const repair = page.getByText(/Review and repair ·/).locator('..');
    await expect(repair).toBeVisible();
    await expect(page.getByText('Unresolved legacy grant · verified:false')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Save changes', exact: true })).toBeDisabled();

    const replacement = page.getByLabel('Replacement grant').first();
    await replacement.fill('identity/roles:view');
    await page.getByRole('button', { name: 'Replace', exact: true }).first().click();
    await expect(page.getByText(/Review and repair ·/)).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Save changes', exact: true })).toBeEnabled();

    await page.getByRole('button', { name: 'Save changes', exact: true }).click();
    await expect.poll(async () => (await adminApi.findRole(config.unresolvedRoleId!))?.permissions)
      .toContain('identity/roles:view');
    await page.reload();
    await expect(page.getByText(/Review and repair ·/)).toHaveCount(0);
    await expect(page.getByLabel('identity/roles:view')).toBeChecked();
    await assertCleanRuntime(diagnostics);
  });
});

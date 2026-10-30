import AxeBuilder from '@axe-core/playwright';
import { expect, test } from '@playwright/test';

const surfaces = [
  { path: '/login?choose=true', name: 'login chooser' },
  { path: '/security/external-authentication/connections', name: 'connection management' },
  { path: '/security/external-authentication/identity-links', name: 'identity-link management' }
];

for (const surface of surfaces) {
  test(`${surface.name} has no serious or critical axe violations`, async ({ page }) => {
    await page.goto(surface.path);

    const results = await new AxeBuilder({ page }).analyze();
    const blockingViolations = results.violations.filter(violation =>
      violation.impact === 'serious' || violation.impact === 'critical');

    expect(blockingViolations, JSON.stringify(blockingViolations, null, 2)).toEqual([]);
  });
}

test('login chooser is operable with a keyboard and exposes a visible focus indicator', async ({ page }) => {
  await page.goto('/login?choose=true');

  await page.keyboard.press('Tab');
  await expect(page.getByLabel('User name')).toBeFocused();
  await page.keyboard.press('Tab');
  await expect(page.getByLabel('Password')).toBeFocused();
  await page.keyboard.press('Tab');
  await expect(page.getByRole('button', { name: 'Sign in', exact: true })).toBeFocused();
  await page.keyboard.press('Tab');

  const github = page.getByRole('button', { name: 'Sign in with GitHub' });
  await expect(github).toBeFocused();
  expect(await github.evaluate(element => getComputedStyle(element).outlineStyle)).not.toBe('none');

  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(/\/authorize\?.*code_challenge_method=S256/);
});

test('login chooser exposes deterministic text-first screen-reader semantics', async ({ page }) => {
  await page.goto('/login?choose=true');

  const main = page.getByRole('main');
  await expect(main).toHaveAttribute('aria-labelledby', 'external-login-heading');
  await expect(page.getByRole('heading', { level: 1, name: 'Sign in' })).toHaveId('external-login-heading');
  await expect(page.getByRole('status').filter({ hasText: 'enabled identity provider' })).toBeVisible();
  await expect(page.getByLabel('User name')).toHaveAttribute('autocomplete', 'username');
  await expect(page.getByLabel('Password')).toHaveAttribute('autocomplete', 'current-password');

  const externalMethods = page.locator('button[data-external]');
  await expect(externalMethods).toHaveCount(3);
  expect(await externalMethods.evaluateAll(buttons => buttons.map(button => button.getAttribute('aria-label'))))
    .toEqual(['Sign in with GitHub', 'Sign in with Microsoft', 'Sign in with Contoso']);
  await expect(externalMethods.nth(0)).toContainText('Sign in with GitHub');
  await expect(externalMethods.nth(1)).toContainText('Sign in with Microsoft');
  await expect(externalMethods.nth(2)).toContainText('Sign in with Contoso');
});

test('preferred login method is visual guidance and never starts without a user action', async ({ page }) => {
  await page.goto('/login');

  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole('status').filter({ hasText: 'Preferred' })).toBeVisible();
  await expect(page).not.toHaveURL(/\/authorize/);
});

test('login chooser accepts only same-origin assets and preserves a text fallback', async ({ page }) => {
  await page.goto('/login?choose=true');

  const assetUrls = await page.locator('[src]').evaluateAll((elements, origin) =>
    elements.map(element => new URL(element.getAttribute('src')!, document.baseURI))
      .filter(url => url.origin !== origin)
      .map(url => url.href), new URL(page.url()).origin);

  expect(assetUrls).toEqual([]);
  await expect(page.locator('img[src^="http://"], img[src^="https://"]')).toHaveCount(0);

  const fallbackMethod = page.getByRole('button', { name: 'Sign in with Contoso' });
  await expect(fallbackMethod).toContainText('Sign in with Contoso');
  await expect(fallbackMethod.locator('[aria-hidden="true"]')).toHaveText('identity provider');
});

test('management surfaces expose named landmarks, filters, tables, pagination, and row actions', async ({ page }) => {
  await page.goto('/security/external-authentication/connections');
  await expect(page.getByRole('main')).toHaveAttribute('aria-labelledby', 'connections-heading');
  await expect(page.getByRole('heading', { level: 1, name: 'Identity provider connections' })).toBeVisible();
  await expect(page.getByLabel('Search connections')).toBeVisible();
  await expect(page.getByLabel('Source')).toBeVisible();
  await expect(page.getByRole('table', { name: 'Identity provider connections' })).toBeVisible();
  await expect(page.getByRole('columnheader', { name: 'Actions' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Manage Contoso' })).toBeVisible();

  await page.goto('/security/external-authentication/identity-links');
  await expect(page.getByRole('main')).toHaveAttribute('aria-labelledby', 'identity-links-heading');
  await expect(page.getByRole('heading', { level: 1, name: 'External identity links' })).toBeVisible();
  await expect(page.getByRole('alert')).toContainText('tenant-scoped');
  await expect(page.getByLabel('Filter by user ID')).toBeVisible();
  await expect(page.getByLabel('Filter by connection key')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Create link' })).toBeVisible();
  await expect(page.getByRole('table', { name: 'External identity links' })).toBeVisible();
  await expect(page.getByRole('columnheader', { name: 'Actions' })).toBeVisible();
  const linkActions = page.getByRole('button', { name: 'Actions for Ada Lovelace via Contoso' });
  await expect(linkActions).toBeVisible();
  await linkActions.click();
  await expect(page.getByRole('menuitem', { name: 'Edit' })).toBeVisible();
  await expect(page.getByRole('menuitem', { name: 'Unlink' })).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(page.getByRole('navigation', { name: 'External identity links pagination' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Next page' })).toBeVisible();
  await expect(page.locator('main form')).toHaveCount(0);
  await expect(page.locator('main')).not.toContainText(/prelink/i);

});

test('creating an external identity link uses a responsive, keyboard-operable dialog', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/security/external-authentication/identity-links');

  const create = page.getByRole('button', { name: 'Create link' });
  await create.click();

  const dialog = page.getByRole('dialog', { name: 'Create external identity link' });
  await expect(dialog).toBeVisible();
  await expect(page.getByLabel('Find Elsa user')).toBeFocused();
  await expect(dialog.getByLabel('Elsa user', { exact: true })).toBeVisible();
  await expect(dialog.getByLabel('Identity provider connection')).toBeVisible();
  await expect(dialog.getByLabel('Issuer namespace')).toBeVisible();
  await expect(dialog.getByRole('button', { name: 'Create link' })).toBeVisible();

  const dialogBox = await dialog.boundingBox();
  expect(dialogBox).not.toBeNull();
  expect(dialogBox!.width).toBeLessThanOrEqual(390);
  expect(dialogBox!.x).toBeGreaterThanOrEqual(0);
  expect(Math.abs(dialogBox!.x - ((390 - dialogBox!.width) / 2))).toBeLessThanOrEqual(2);

  await dialog.getByLabel('External subject', { exact: true }).fill('discard-on-escape');
  await page.keyboard.press('Escape');
  await expect(dialog).not.toBeVisible();
  await expect(create).toBeFocused();
  await create.click();
  await expect(dialog.getByLabel('External subject', { exact: true })).toHaveValue('');
  await page.keyboard.press('Escape');
});

test('editing a link replaces it through the shared dialog without retaining the raw subject', async ({ page }) => {
  await page.goto('/security/external-authentication/identity-links');

  const actions = page.getByRole('button', { name: 'Actions for Ada Lovelace via Contoso' });
  await actions.click();
  await page.getByRole('menuitem', { name: 'Edit' }).click();

  const dialog = page.getByRole('dialog', { name: 'Edit external identity link' });
  await expect(dialog).toBeVisible();
  await expect(dialog.getByLabel('Elsa user', { exact: true })).toHaveValue('ada');
  await expect(dialog.getByLabel('Identity provider connection')).toHaveValue('contoso');
  await expect(dialog.getByLabel('Issuer namespace')).toHaveValue('https://login.contoso.example');

  const subject = dialog.getByLabel('External subject', { exact: true });
  await expect(subject).toHaveValue('');
  await expect(subject).toHaveAttribute('type', 'password');
  await expect(subject).toHaveAttribute('required', '');
  await expect(subject).toHaveAttribute('autocomplete', 'off');
  await expect(dialog.getByRole('button', { name: 'Show external subject' })).toBeVisible();
  await dialog.getByRole('button', { name: 'Show external subject' }).click();
  await expect(subject).toHaveAttribute('type', 'text');
  await expect(dialog.getByRole('button', { name: 'Hide external subject' })).toBeVisible();

  const warning = dialog.getByRole('alert');
  await expect(warning).toContainText('creates a new external identity link');
  await expect(warning).toContainText('resets its sign-in history');
  await expect(warning).toContainText('cannot be undone');
  await expect(dialog.getByRole('button', { name: 'Replace link' })).toBeVisible();

  await subject.fill('subject-that-must-not-persist');
  await dialog.getByRole('button', { name: 'Cancel' }).click();
  await expect(dialog).not.toBeVisible();
  await expect(actions).toBeFocused();

  await actions.click();
  await page.getByRole('menuitem', { name: 'Edit' }).click();
  await expect(subject).toHaveValue('');
  await expect(subject).toHaveAttribute('type', 'password');
  await dialog.getByRole('button', { name: 'Close link dialog' }).click();
  await expect(dialog).not.toBeVisible();
  await expect(actions).toBeFocused();
});

test('unlink confirmation describes the sign-in consequence without an archival-retention claim', async ({ page }) => {
  await page.goto('/security/external-authentication/identity-links');
  await page.getByRole('button', { name: 'Actions for Ada Lovelace via Contoso' }).click();
  await page.getByRole('menuitem', { name: 'Unlink' }).click();

  const dialog = page.getByRole('dialog', { name: 'Unlink external identity?' });
  await expect(dialog).toContainText('will no longer sign in through this external identity');
  await expect(dialog).not.toContainText(/archiv|retention/i);
  await expect(dialog.getByRole('button', { name: 'Unlink' })).toBeVisible();
  await expect(dialog.getByRole('button', { name: 'Cancel' })).toBeVisible();
});

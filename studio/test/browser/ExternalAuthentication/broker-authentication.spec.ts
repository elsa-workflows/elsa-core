import { expect, test } from '@playwright/test';

test.describe('brokered WebAssembly authentication', () => {
  test('uses S256 PKCE and only the exact registered callback origin', async ({ page }) => {
    await page.goto('/login');
    await page.getByRole('button', { name: /sign in with/i }).first().click();

    const authorize = new URL(page.url());
    expect(authorize.searchParams.get('code_challenge_method')).toBe('S256');
    expect(authorize.searchParams.get('code_challenge')).toBeTruthy();
    expect(authorize.searchParams.get('redirect_uri')).toBe(`${new URL(test.info().project.use.baseURL as string).origin}/authentication/external/callback`);
    await expect(page).not.toHaveURL(/access_token|refresh_token|client_secret/);
  });

  test('memory storage requires sign-in after reload and in a new tab', async ({ page, context }) => {
    await page.goto('/__external-authentication-fixture/sign-in?storage=Memory');
    await expect(page).toHaveURL(/workflows/);
    await page.reload();
    await expect(page).toHaveURL(/login/);

    const secondTab = await context.newPage();
    await secondTab.goto('/');
    await expect(secondTab).toHaveURL(/login/);
  });

  test('explicit session and durable storage display a warning and retain only their configured scope', async ({ page }) => {
    for (const storage of ['Session', 'Durable']) {
      await page.goto(`/__external-authentication-fixture/sign-in?storage=${storage}`);
      await expect(page.getByRole('status')).toContainText(/browser storage|security warning/i);
      await page.reload();
      await expect(page).toHaveURL(/workflows/);
    }
  });

  test('refresh token rotation succeeds once and refresh-token reuse forces reauthentication', async ({ page }) => {
    await page.goto('/__external-authentication-fixture/sign-in?expiredAccessToken=true');
    await expect(page).toHaveURL(/workflows/);
    await page.goto('/__external-authentication-fixture/reuse-rotated-refresh-token');
    await expect(page).toHaveURL(/login/);
  });

  test('callback state is single-use and rejects replay', async ({ page }) => {
    await page.goto('/__external-authentication-fixture/callback-replay');
    await expect(page).toHaveURL(/login\?choose=true/);
  });

  test('local and upstream logout clear the Studio session and use only the authorized continuation', async ({ page }) => {
    for (const mode of ['local', 'upstream']) {
      await page.goto(`/__external-authentication-fixture/sign-in?logout=${mode}`);
      await page.getByRole('button', { name: new RegExp(`sign out.*${mode}`, 'i') }).click();
      await expect(page).toHaveURL(/login/);
      await expect(page).not.toHaveURL(/access_token|refresh_token/);
    }
  });
});

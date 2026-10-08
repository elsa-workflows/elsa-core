import { APIRequestContext, Page, test as base, expect, request as playwrightRequest } from '@playwright/test';
import { randomUUID } from 'node:crypto';

export type HostKind = 'server' | 'wasm';

export type RoleManagementConfig = {
  host: HostKind;
  studioUrl: string;
  backendUrl: string;
  admin: ActorCredentials;
  restricted?: ActorCredentials;
  blockedRoleId?: string;
  remediableRoleId?: string;
  conflictRoleId?: string;
  conflictTriggerUrl?: string;
  incompleteRoleId?: string;
  incompleteTriggerUrl?: string;
  unresolvedRoleId?: string;
  requireUnverifiedCatalog: boolean;
};

export type ActorCredentials = {
  username: string;
  password: string;
};

type LoginResponse = {
  isAuthenticated?: boolean;
  accessToken?: string | null;
  refreshToken?: string | null;
};

export type RoleRecord = {
  id: string;
  name: string;
  permissions?: string[];
};

export type UserRecord = {
  id: string;
  name: string;
  password: string;
};

const secretUrlPattern = /(access[_-]?token|refresh[_-]?token|client[_-]?secret|password|cookie|authorization)/i;
const localHosts = new Set(['localhost', '127.0.0.1', '[::1]', '::1']);

function environmentValue(name: string): string | undefined {
  const value = process.env[name]?.trim();
  return value || undefined;
}

function secretEnvironmentValue(name: string): string | undefined {
  const value = process.env[name];
  return value && value.length > 0 ? value : undefined;
}

function hostFromProject(projectName: string): HostKind {
  if (projectName.startsWith('server-'))
    return 'server';
  if (projectName.startsWith('wasm-'))
    return 'wasm';
  throw new Error('Role browser tests require a server or wasm project.');
}

function required(name: string): string {
  const value = environmentValue(name);
  if (!value)
    throw new Error(`Missing required role-browser setting: ${name}`);
  return value;
}

function requiredSecret(name: string): string {
  const value = secretEnvironmentValue(name);
  if (!value)
    throw new Error(`Missing required role-browser setting: ${name}`);
  return value;
}

function validateLocalUrl(value: string, name: string): string {
  let url: URL;
  try {
    url = new URL(value);
  } catch {
    throw new Error(`${name} must be an absolute URL.`);
  }

  if (!['http:', 'https:'].includes(url.protocol) || !localHosts.has(url.hostname))
    throw new Error(`${name} must point to a local development host.`);

  return value.replace(/\/+$/, '');
}

export function loadConfig(projectName: string): RoleManagementConfig {
  const host = hostFromProject(projectName);
  const prefix = host.toUpperCase();
  const studioUrl = validateLocalUrl(required(`ROLE_E2E_${prefix}_STUDIO_URL`), `ROLE_E2E_${prefix}_STUDIO_URL`);
  const backendUrl = validateLocalUrl(required(`ROLE_E2E_${prefix}_BACKEND_URL`), `ROLE_E2E_${prefix}_BACKEND_URL`);

  return {
    host,
    studioUrl,
    backendUrl,
    admin: {
      username: requiredSecret('ROLE_E2E_ADMIN_USERNAME'),
      password: requiredSecret('ROLE_E2E_ADMIN_PASSWORD')
    },
    restricted: readActor('ROLE_E2E_RESTRICTED_USERNAME', 'ROLE_E2E_RESTRICTED_PASSWORD'),
    blockedRoleId: environmentValue('ROLE_E2E_BLOCKED_ROLE_ID'),
    remediableRoleId: environmentValue('ROLE_E2E_REMEDIABLE_ROLE_ID'),
    conflictRoleId: environmentValue('ROLE_E2E_CONFLICT_ROLE_ID'),
    conflictTriggerUrl: readLocalTrigger('ROLE_E2E_CONFLICT_TRIGGER_URL'),
    incompleteRoleId: environmentValue('ROLE_E2E_INCOMPLETE_ROLE_ID'),
    incompleteTriggerUrl: readLocalTrigger('ROLE_E2E_INCOMPLETE_TRIGGER_URL'),
    unresolvedRoleId: environmentValue('ROLE_E2E_UNRESOLVED_ROLE_ID'),
    requireUnverifiedCatalog: environmentValue('ROLE_E2E_REQUIRE_UNVERIFIED_CATALOG') === 'true'
  };
}

function readActor(usernameName: string, passwordName: string): ActorCredentials | undefined {
  const username = secretEnvironmentValue(usernameName);
  const password = secretEnvironmentValue(passwordName);
  if (!username && !password)
    return undefined;
  if (!username || !password)
    throw new Error(`${usernameName} and ${passwordName} must be supplied together.`);
  return { username, password };
}

function readLocalTrigger(name: string): string | undefined {
  const value = environmentValue(name);
  if (!value)
    return undefined;
  return validateLocalUrl(value, name);
}

export async function signIn(page: Page, actor: ActorCredentials): Promise<void> {
  await page.goto('/login');
  if (!page.url().includes('/login'))
    return;

  await page.getByLabel('User name').fill(actor.username);
  await page.getByLabel('Password').fill(actor.password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  try {
    await expect(page).not.toHaveURL(/\/login(?:$|[?#])/);
  } catch {
    const visibleAlerts = await page.getByRole('alert').allTextContents();
    const detail = visibleAlerts.length === 0 ? 'No actionable error was rendered.' : visibleAlerts.join(' ');
    throw new Error(`Studio sign-in did not complete. ${detail}`);
  }
}

export async function openRoles(page: Page, actor: ActorCredentials): Promise<void> {
  await signIn(page, actor);
  await page.goto('/security/roles');
  await expect(page).toHaveURL(/\/security\/roles(?:$|[?#])/);
  try {
    await expect(page.getByRole('heading', { level: 1, name: 'Roles' })).toBeVisible();
  } catch {
    const pageText = (await page.locator('body').innerText()).replaceAll(actor.username, '[actor]');
    throw new Error(`Roles route did not render its heading. ${pageText.slice(0, 800)}`);
  }
}

export class CoreApiSession {
  private constructor(
    private readonly request: APIRequestContext,
    private readonly backendUrl: string,
    private readonly accessToken: string,
    private readonly refreshToken?: string
  ) {
  }

  static async signIn(request: APIRequestContext, backendUrl: string, actor: ActorCredentials): Promise<CoreApiSession> {
    const response = await request.post(`${backendUrl}/identity/login`, {
      data: { username: actor.username, password: actor.password }
    });
    const body = await response.json().catch(() => ({})) as LoginResponse;
    if (response.status() !== 200 || body.isAuthenticated !== true || !body.accessToken)
      throw new Error(`Core login failed with status ${response.status()}.`);

    // The token is intentionally kept only in this object for the duration of the test.
    return new CoreApiSession(request, backendUrl, body.accessToken, body.refreshToken ?? undefined);
  }

  async createRole(name: string, permissions: string[] = []): Promise<RoleRecord> {
    const response = await this.send('POST', '/identity/roles', { name, permissions });
    const body = await response.json().catch(() => ({})) as Partial<RoleRecord>;
    if (typeof body.id !== 'string' || typeof body.name !== 'string')
      throw new Error(`Core role creation returned status ${response.status()}.`);
    return { id: body.id, name: body.name };
  }

  async updateRole(id: string, name: string, permissions: string[]): Promise<void> {
    const response = await this.send('PUT', `/identity/roles/${encodeURIComponent(id)}`, { name, permissions });
    if (response.status() !== 200)
      throw new Error(`Core role update returned status ${response.status()}.`);
  }

  async findRole(id: string): Promise<RoleRecord | undefined> {
    const response = await this.send('GET', '/identity/roles');
    if (response.status() !== 200)
      throw new Error(`Core role list returned status ${response.status()}.`);
    const body = await response.json().catch(() => ({})) as { roles?: RoleRecord[] };
    const normalizedId = id.trim().toLocaleLowerCase();
    return body.roles?.find(role =>
      role.id?.trim().toLocaleLowerCase() === normalizedId || role.name?.trim().toLocaleLowerCase() === normalizedId);
  }

  async deleteRole(id: string): Promise<void> {
    const response = await this.send('DELETE', `/identity/roles/${encodeURIComponent(id)}`);
    if (![204, 404].includes(response.status()))
      throw new Error(`Core role cleanup returned status ${response.status()}.`);
  }

  async createUser(name: string, roles: string[]): Promise<UserRecord> {
    const response = await this.send('POST', '/identity/users', { name, password: null, roles });
    const body = await response.json().catch(() => ({})) as Partial<UserRecord>;
    if (response.status() !== 200 || typeof body.id !== 'string' || typeof body.name !== 'string' || typeof body.password !== 'string')
      throw new Error(`Core user creation returned status ${response.status()}.`);
    return { id: body.id, name: body.name, password: body.password };
  }

  async deleteUser(id: string): Promise<void> {
    const response = await this.send('DELETE', `/identity/users/${encodeURIComponent(id)}`);
    if (![204, 404].includes(response.status()))
      throw new Error(`Core user cleanup returned status ${response.status()}.`);
  }

  async expectForbiddenRoleCreation(name = `unauthorized-${randomUUID()}`): Promise<void> {
    const response = await this.send('POST', '/identity/roles', { name, permissions: [] });
    expect(response.status()).toBe(403);
  }

  async refresh(): Promise<CoreApiSession> {
    if (!this.refreshToken)
      throw new Error('Core login did not return a refresh token.');
    const response = await this.request.post(`${this.backendUrl}/identity/refresh-token`, {
      headers: { Authorization: `Bearer ${this.refreshToken}` }
    });
    const body = await response.json().catch(() => ({})) as LoginResponse;
    if (response.status() !== 200 || body.isAuthenticated !== true || !body.accessToken)
      throw new Error(`Core token refresh failed with status ${response.status()}.`);
    return new CoreApiSession(this.request, this.backendUrl, body.accessToken, body.refreshToken ?? undefined);
  }

  async triggerLocalFixture(url: string, roleId: string): Promise<void> {
    const parsed = new URL(url);
    if (!localHosts.has(parsed.hostname))
      throw new Error('Role mutation triggers must remain local.');
    const response = await this.request.post(url, { data: { roleId } });
    if (response.status() < 200 || response.status() >= 300)
      throw new Error(`Local role fixture trigger returned status ${response.status()}.`);
  }

  private async send(method: string, path: string, data?: unknown): Promise<Awaited<ReturnType<APIRequestContext['fetch']>>> {
    return this.request.fetch(`${this.backendUrl}${path}`, {
      method,
      data,
      headers: { Authorization: `Bearer ${this.accessToken}` }
    });
  }
}

export type RuntimeDiagnostics = {
  consoleIssues: number;
  consoleIssueSummaries: string[];
  clientIssueSummaries: string[];
  serverErrors: number;
  secretUrls: number;
};

function sanitizeDiagnostic(value: string, secrets: string[]): string {
  let sanitized = value
    .replace(/https?:\/\/\S+/gi, '[url]')
    .replace(/Bearer\s+\S+/gi, 'Bearer [redacted]')
    .replace(/[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}/g, '[token]')
    .replace(/((?:(?:access|refresh)[_-]?token|client[_-]?secret|password|cookie|authorization)\s*[:=]\s*)\S+/gi, '$1[redacted]');
  for (const secret of secrets.filter(Boolean))
    sanitized = sanitized.replaceAll(secret, '[redacted]');
  return sanitized.slice(0, 300);
}

function containsSecretUrlData(value: string, secrets: string[]): boolean {
  const url = new URL(value);
  if (url.username || url.password)
    return true;

  const parameters = [url.searchParams];
  if (url.hash.includes('='))
    parameters.push(new URLSearchParams(url.hash.replace(/^#\??/, '')));

  return parameters.some(items => [...items].some(([key, parameterValue]) =>
    secretUrlPattern.test(key) || secrets.filter(Boolean).some(secret => parameterValue.includes(secret))));
}

export function captureRuntimeDiagnostics(page: Page, secrets: string[]): RuntimeDiagnostics {
  const diagnostics: RuntimeDiagnostics = { consoleIssues: 0, consoleIssueSummaries: [], clientIssueSummaries: [], serverErrors: 0, secretUrls: 0 };

  page.on('console', message => {
    if (message.type() === 'error' || message.type() === 'warning') {
      diagnostics.consoleIssues++;
      diagnostics.consoleIssueSummaries.push(`${message.type()}: ${sanitizeDiagnostic(message.text(), secrets)}`);
    }
  });
  page.on('request', request => {
    if (containsSecretUrlData(request.url(), secrets))
      diagnostics.secretUrls++;
  });
  page.on('response', response => {
    if (response.status() >= 400 && response.status() < 500) {
      const url = new URL(response.url());
      diagnostics.clientIssueSummaries.push(`${response.request().method()} ${url.pathname} -> ${response.status()}`);
    }
    if (response.status() >= 500)
      diagnostics.serverErrors++;
  });

  return diagnostics;
}

export async function assertCleanRuntime(diagnostics: RuntimeDiagnostics): Promise<void> {
  // Diagnostics are sanitized before they are included in assertion failures.
  expect(diagnostics.secretUrls).toBe(0);
  expect(diagnostics.consoleIssues,
    [...diagnostics.consoleIssueSummaries, ...diagnostics.clientIssueSummaries].join('\n')).toBe(0);
  expect(diagnostics.serverErrors).toBe(0);
}

type Fixtures = {
  config: RoleManagementConfig;
  adminApi: CoreApiSession;
  restrictedApi?: CoreApiSession;
  registerRole: (id: string) => void;
  diagnostics: RuntimeDiagnostics;
};

export const test = base.extend<Fixtures>({
  config: async ({}, use, testInfo) => {
    await use(loadConfig(testInfo.project.name));
  },
  adminApi: async ({ config }, use) => {
    const apiRequest = await playwrightRequest.newContext({
      ignoreHTTPSErrors: true,
      extraHTTPHeaders: { Accept: 'application/json' }
    });
    const session = await CoreApiSession.signIn(apiRequest, config.backendUrl, config.admin);
    try {
      await use(session);
    } finally {
      await apiRequest.dispose();
    }
  },
  restrictedApi: async ({ config }, use) => {
    if (!config.restricted) {
      await use(undefined);
      return;
    }

    const apiRequest = await playwrightRequest.newContext({
      ignoreHTTPSErrors: true,
      extraHTTPHeaders: { Accept: 'application/json' }
    });
    const session = await CoreApiSession.signIn(apiRequest, config.backendUrl, config.restricted);
    try {
      await use(session);
    } finally {
      await apiRequest.dispose();
    }
  },
  registerRole: async ({ adminApi }, use) => {
    const roleIds = new Set<string>();
    await use(id => roleIds.add(id));
    for (const id of roleIds)
      await adminApi.deleteRole(id);
  },
  diagnostics: async ({ page, config }, use) => {
    const diagnostics = captureRuntimeDiagnostics(page, [config.admin.username, config.admin.password]);
    await use(diagnostics);
    await assertCleanRuntime(diagnostics);
  }
});

export { expect };

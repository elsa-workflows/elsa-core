import { spawnSync } from 'node:child_process';
import { createServer, type IncomingMessage, type ServerResponse } from 'node:http';
import { existsSync, lstatSync, readFileSync, unlinkSync, writeFileSync } from 'node:fs';
import path from 'node:path';

const FIXTURE_MARKER = 'elsa-studio-role-management-fixture-v1';
const CONFIRMATION = 'isolated';
const DEFAULT_PORT = 42817;
const LOCAL_TRIGGER_PREFIX = '/role-management';

const fixture = {
  roles: {
    blocked: 'role-e2e-blocked',
    remediable: 'role-e2e-remediable',
    conflict: 'role-e2e-conflict',
    incomplete: 'role-e2e-incomplete',
    unresolved: 'role-e2e-unresolved',
    replacement: 'role-e2e-replacement'
  },
  connections: {
    remediable: 'role-e2e-connection-remediable',
    conflict: 'role-e2e-connection-conflict',
    incompleteA: 'role-e2e-connection-incomplete-a',
    incompleteB: 'role-e2e-connection-incomplete-b'
  },
  triggers: {
    incomplete: 'role_e2e_incomplete_abort'
  }
} as const;

type FixtureManifest = {
  version: 1;
  marker: typeof FIXTURE_MARKER;
  dbPath: string;
  overlayPath: string;
  roles: typeof fixture.roles;
  connections: typeof fixture.connections;
  triggers: typeof fixture.triggers;
};

type SqliteRow = Record<string, string | number | null>;

function fail(message: string): never {
  throw new Error(message);
}

function env(name: string): string | undefined {
  const value = process.env[name]?.trim();
  return value || undefined;
}

function requiredEnv(name: string): string {
  return env(name) ?? fail(`Missing required fixture setting: ${name}`);
}

function assertAbsolutePath(value: string, name: string): string {
  if (!path.isAbsolute(value))
    fail(`${name} must be an absolute path.`);
  return value;
}

function assertNotSymlink(value: string, name: string): void {
  if (existsSync(value) && lstatSync(value).isSymbolicLink())
    fail(`${name} must not be a symbolic link.`);
}

function databasePath(): string {
  const value = assertAbsolutePath(requiredEnv('ROLE_E2E_SQLITE_PATH'), 'ROLE_E2E_SQLITE_PATH');
  assertNotSymlink(value, 'ROLE_E2E_SQLITE_PATH');
  if (!existsSync(value) || !lstatSync(value).isFile())
    fail(`ROLE_E2E_SQLITE_PATH must point to an existing SQLite database file: ${value}`);
  if (!value.endsWith('.db'))
    fail('ROLE_E2E_SQLITE_PATH must point to a .db file from an isolated Core host.');
  if (env('ROLE_E2E_SQLITE_CONFIRM') !== CONFIRMATION)
    fail(`Set ROLE_E2E_SQLITE_CONFIRM=${CONFIRMATION} to confirm that the database is isolated.`);
  return value;
}

function overlayPath(dbPath: string): string {
  const value = env('ROLE_E2E_CORE_OVERLAY_PATH') ?? `${dbPath}.role-e2e-overrides.json`;
  const absolute = assertAbsolutePath(value, 'ROLE_E2E_CORE_OVERLAY_PATH');
  assertNotSymlink(absolute, 'ROLE_E2E_CORE_OVERLAY_PATH');
  if (!path.basename(absolute).includes('role-e2e'))
    fail('ROLE_E2E_CORE_OVERLAY_PATH must name a role-e2e fixture overlay file.');
  return absolute;
}

function manifestPath(dbPath: string): string {
  return `${dbPath}.role-e2e-manifest.json`;
}

function sqlString(value: string): string {
  return `'${value.replaceAll("'", "''")}'`;
}

function runSql(dbPath: string, sql: string, json = false): string {
  const result = spawnSync(
    'sqlite3',
    ['-batch', '-bail', '-cmd', '.timeout 5000', ...(json ? ['-json'] : []), dbPath],
    { input: sql, encoding: 'utf8' }
  );
  if (result.error)
    fail('The local sqlite3 command is required to prepare RoleManagement fixtures.');
  if (result.status !== 0)
    fail(`sqlite3 fixture operation failed with exit code ${result.status ?? 'unknown'}.`);
  return result.stdout.trim();
}

function query(dbPath: string, sql: string): SqliteRow[] {
  const output = runSql(dbPath, sql, true);
  if (!output)
    return [];
  try {
    return JSON.parse(output) as SqliteRow[];
  } catch {
    fail('The local SQLite query did not return JSON.');
  }
}

function verifySchema(dbPath: string): void {
  const names = new Set(query(dbPath, "SELECT name FROM sqlite_master WHERE type = 'table'").map(row => String(row.name)));
  for (const required of ['Roles', 'IdentityProviderConnections']) {
    if (!names.has(required))
      fail(`The supplied SQLite database is missing the Core table ${required}. Use the current Modular Core database.`);
  }
}

function expectedManifest(dbPath: string, overridePath = overlayPath(dbPath)): FixtureManifest {
  return {
    version: 1,
    marker: FIXTURE_MARKER,
    dbPath,
    overlayPath: overridePath,
    roles: { ...fixture.roles },
    connections: { ...fixture.connections },
    triggers: { ...fixture.triggers }
  };
}

function readManifest(dbPath: string, overridePath = overlayPath(dbPath)): FixtureManifest | undefined {
  const file = manifestPath(dbPath);
  if (!existsSync(file))
    return undefined;
  assertNotSymlink(file, 'fixture manifest');
  let parsed: unknown;
  try {
    parsed = JSON.parse(readFileSync(file, 'utf8'));
  } catch {
    fail('The existing RoleManagement fixture manifest is not valid JSON.');
  }
  const expected = expectedManifest(dbPath, overridePath);
  if (JSON.stringify(parsed) !== JSON.stringify(expected))
    fail('The existing RoleManagement fixture manifest does not match this database or overlay path. Clean it up first.');
  return expected;
}

function roleRows(dbPath: string): SqliteRow[] {
  const ids = Object.values(fixture.roles).map(sqlString).join(', ');
  return query(dbPath, `SELECT Id, Name FROM Roles WHERE Id IN (${ids}) ORDER BY Id`);
}

function connectionRows(dbPath: string): SqliteRow[] {
  const ids = Object.values(fixture.connections).map(sqlString).join(', ');
  return query(dbPath, `SELECT Id, Key, DisplayName FROM IdentityProviderConnections WHERE Id IN (${ids}) ORDER BY Id`);
}

function assertOwnedRows(dbPath: string): void {
  const expectedRoles = new Map<string, string>(Object.entries(fixture.roles).map(([kind, id]) => [id, `role-e2e-${kind}`]));
  for (const row of roleRows(dbPath)) {
    const id = String(row.Id);
    if (String(row.Name) !== expectedRoles.get(id))
      fail(`Refusing to modify role ${id}: it is not an exact RoleManagement fixture row.`);
  }

  for (const row of connectionRows(dbPath)) {
    const id = String(row.Id);
    if (String(row.Key) !== id || String(row.DisplayName) !== `Role E2E ${id}`)
      fail(`Refusing to modify connection ${id}: it is not an exact RoleManagement fixture row.`);
  }
}

function policy(roleIds: string[]): string {
  return JSON.stringify({ type: 'create-user', settingsVersion: 1, settings: { defaultRoleIds: roleIds } });
}

function rolePermissionValue(permissions: string[]): string {
  return permissions.join(',');
}

function connectionInsert(id: string, roleIds: string[], now: string): string {
  const materialRevision = `role-e2e-${id}-revision-1`;
  return `INSERT INTO IdentityProviderConnections (
    Id, TenantId, Key, AdapterType, AdapterSettingsVersion, AdapterSettingsJson,
    SecretBindingsJson, DisplayName, IconId, DisplayOrder, IsPreferred, IsEnabled,
    OverridesConfigurationConnection, ArchivedAt, UnlinkedPolicyJson,
    PermissionGrantSourcesJson, ClaimProjectionJson, UpstreamLogoutMode, Revision,
    MaterialRevision, CreatedAt, UpdatedAt
  ) VALUES (
    ${sqlString(id)}, ${sqlString('')}, ${sqlString(id)}, ${sqlString('openid-connect')}, 2,
    ${sqlString('{}')}, ${sqlString('{}')}, ${sqlString(`Role E2E ${id}`)}, NULL, 0, 0, 0,
    0, NULL, ${sqlString(policy(roleIds))}, ${sqlString('[]')},
    ${sqlString('{"allowedClaimTypes":[],"redactedClaimTypes":[],"maximumClaimCount":0,"maximumValueLength":0,"maximumTotalBytes":0}')},
    0, 1, ${sqlString(materialRevision)}, ${sqlString(now)}, ${sqlString(now)}
  );`;
}

function overlayDocument(): object {
  const roleId = fixture.roles.blocked;
  return {
    RoleE2EFixture: { Marker: FIXTURE_MARKER },
    CShells: {
      Shells: {
        Default: {
          Features: {
            ExternalAuthentication: {
              Connections: [{
                Id: 'role-e2e-configuration-connection',
                TenantId: '*',
                Key: 'role-e2e-configuration',
                AdapterType: 'openid-connect',
                AdapterSettingsVersion: 2,
                AdapterSettings: {},
                SecretBindings: {},
                DisplayName: 'Role E2E configuration',
                DisplayOrder: 0,
                IsPreferred: false,
                IsEnabled: false,
                UnlinkedPolicy: {
                  Type: 'create-user',
                  SettingsVersion: 1,
                  Settings: { defaultRoleIds: [roleId] }
                },
                PermissionGrantSources: []
              }]
            }
          }
        }
      }
    }
  };
}

function writeOverlay(overridePath: string): void {
  if (existsSync(overridePath)) {
    let existing: unknown;
    try {
      existing = JSON.parse(readFileSync(overridePath, 'utf8'));
    } catch {
      fail('The existing role-e2e overlay is not valid JSON; it was left untouched.');
    }
    if ((existing as { RoleE2EFixture?: { Marker?: string } }).RoleE2EFixture?.Marker !== FIXTURE_MARKER)
      fail('The requested role-e2e overlay already exists without the fixture marker; it was left untouched.');
  }
  const parent = path.dirname(overridePath);
  if (!existsSync(parent))
    fail(`The overlay directory does not exist: ${parent}`);
  writeFileSync(overridePath, `${JSON.stringify(overlayDocument(), null, 2)}\n`, { encoding: 'utf8', flag: 'w' });
}

function removeOverlay(overridePath: string): void {
  if (!existsSync(overridePath))
    return;
  assertNotSymlink(overridePath, 'ROLE_E2E_CORE_OVERLAY_PATH');
  let existing: unknown;
  try {
    existing = JSON.parse(readFileSync(overridePath, 'utf8'));
  } catch {
    fail('The role-e2e overlay is not valid JSON; it was left untouched.');
  }
  if ((existing as { RoleE2EFixture?: { Marker?: string } }).RoleE2EFixture?.Marker !== FIXTURE_MARKER)
    fail('The role-e2e overlay marker is missing; it was left untouched.');
  unlinkSync(overridePath);
}

function cleanupDatabase(dbPath: string): void {
  verifySchema(dbPath);
  assertOwnedRows(dbPath);
  const roleIds = Object.values(fixture.roles).map(sqlString).join(', ');
  const connectionIds = Object.values(fixture.connections).map(sqlString).join(', ');
  runSql(dbPath, `BEGIN IMMEDIATE;
DROP TRIGGER IF EXISTS "${fixture.triggers.incomplete}";
DELETE FROM IdentityProviderConnections WHERE Id IN (${connectionIds});
DELETE FROM Roles WHERE Id IN (${roleIds});
COMMIT;`);
}

function prepareDatabase(dbPath: string): void {
  verifySchema(dbPath);
  assertOwnedRows(dbPath);
  const roleIds = Object.values(fixture.roles).map(sqlString).join(', ');
  const connectionIds = Object.values(fixture.connections).map(sqlString).join(', ');
  const now = new Date().toISOString();
  const replacement = fixture.roles.replacement;
  const incompleteRoles = [fixture.roles.incomplete, replacement];
  runSql(dbPath, `BEGIN IMMEDIATE;
DROP TRIGGER IF EXISTS "${fixture.triggers.incomplete}";
DELETE FROM IdentityProviderConnections WHERE Id IN (${connectionIds});
DELETE FROM Roles WHERE Id IN (${roleIds});
INSERT INTO Roles (Id, Name, Permissions) VALUES
  (${sqlString(fixture.roles.blocked)}, ${sqlString('role-e2e-blocked')}, ${sqlString(rolePermissionValue(['identity/roles:view']))}),
  (${sqlString(fixture.roles.remediable)}, ${sqlString('role-e2e-remediable')}, ${sqlString(rolePermissionValue(['identity/roles:view']))}),
  (${sqlString(fixture.roles.conflict)}, ${sqlString('role-e2e-conflict')}, ${sqlString(rolePermissionValue(['identity/roles:view']))}),
  (${sqlString(fixture.roles.incomplete)}, ${sqlString('role-e2e-incomplete')}, ${sqlString(rolePermissionValue(['identity/roles:view']))}),
  (${sqlString(fixture.roles.unresolved)}, ${sqlString('role-e2e-unresolved')}, ${sqlString(rolePermissionValue(['identity/roles:view', 'legacy/removed:grant']))}),
  (${sqlString(replacement)}, ${sqlString('role-e2e-replacement')}, ${sqlString(rolePermissionValue(['identity/roles:view']))});
${connectionInsert(fixture.connections.remediable, [fixture.roles.remediable, replacement], now)}
${connectionInsert(fixture.connections.conflict, [fixture.roles.conflict, replacement], now)}
${connectionInsert(fixture.connections.incompleteA, incompleteRoles, now)}
${connectionInsert(fixture.connections.incompleteB, incompleteRoles, now)}
COMMIT;`);
}

function shellQuote(value: string): string {
  return `'${value.replaceAll("'", "'\\''")}'`;
}

function printEnvironment(dbPath: string, overridePath: string, port?: number): void {
  const lines = [
    `export ROLE_E2E_SQLITE_PATH=${shellQuote(dbPath)}`,
    `export ROLE_E2E_SQLITE_CONFIRM=${CONFIRMATION}`,
    `export ROLE_E2E_CORE_OVERLAY_PATH=${shellQuote(overridePath)}`,
    'export RoleManagementE2EFixtures__IncludeUnverifiedPermissionDescriptor=true',
    'export ROLE_E2E_REQUIRE_UNVERIFIED_CATALOG=true',
    `export ROLE_E2E_BLOCKED_ROLE_ID=${fixture.roles.blocked}`,
    `export ROLE_E2E_REMEDIABLE_ROLE_ID=${fixture.roles.remediable}`,
    `export ROLE_E2E_CONFLICT_ROLE_ID=${fixture.roles.conflict}`,
    `export ROLE_E2E_INCOMPLETE_ROLE_ID=${fixture.roles.incomplete}`,
    `export ROLE_E2E_UNRESOLVED_ROLE_ID=${fixture.roles.unresolved}`
  ];
  if (port) {
    lines.push(`export ROLE_E2E_CONFLICT_TRIGGER_URL=http://127.0.0.1:${port}${LOCAL_TRIGGER_PREFIX}/conflict`);
    lines.push(`export ROLE_E2E_INCOMPLETE_TRIGGER_URL=http://127.0.0.1:${port}${LOCAL_TRIGGER_PREFIX}/incomplete`);
  }
  console.log(lines.join('\n'));
}

async function readBody(request: IncomingMessage): Promise<string> {
  let body = '';
  for await (const chunk of request) {
    body += String(chunk);
    if (body.length > 4096)
      fail('Fixture trigger request body is too large.');
  }
  return body;
}

function respond(response: ServerResponse, status: number, body?: string): void {
  response.statusCode = status;
  response.setHeader('Cache-Control', 'no-store');
  if (body) {
    response.setHeader('Content-Type', 'text/plain; charset=utf-8');
    response.end(body);
  } else {
    response.end();
  }
}

async function handleTrigger(request: IncomingMessage, response: ServerResponse, manifest: FixtureManifest, dbPath: string): Promise<void> {
  if (request.method !== 'POST') {
    respond(response, 405);
    return;
  }
  let body: { roleId?: unknown };
  try {
    body = JSON.parse(await readBody(request)) as { roleId?: unknown };
  } catch {
    respond(response, 400);
    return;
  }
  if (typeof body.roleId !== 'string') {
    respond(response, 400);
    return;
  }

  const requestPath = request.url?.split('?')[0];
  if (requestPath === `${LOCAL_TRIGGER_PREFIX}/conflict`) {
    if (body.roleId !== manifest.roles.conflict) {
      respond(response, 404);
      return;
    }
    const owner = manifest.connections.conflict;
    if (query(dbPath, `SELECT Id FROM IdentityProviderConnections WHERE Id = ${sqlString(owner)}`).length === 0) {
      respond(response, 404);
      return;
    }
    runSql(dbPath, `UPDATE IdentityProviderConnections
      SET Revision = Revision + 1,
          MaterialRevision = ${sqlString(`role-e2e-${owner}-conflict`)},
          UpdatedAt = ${sqlString(new Date().toISOString())}
      WHERE Id = ${sqlString(owner)};`);
    respond(response, 204);
    return;
  }

  if (requestPath === `${LOCAL_TRIGGER_PREFIX}/incomplete`) {
    if (body.roleId !== manifest.roles.incomplete) {
      respond(response, 404);
      return;
    }
    const owner = manifest.connections.incompleteB;
    if (query(dbPath, `SELECT Id FROM IdentityProviderConnections WHERE Id = ${sqlString(owner)}`).length === 0) {
      respond(response, 404);
      return;
    }
    runSql(dbPath, `DROP TRIGGER IF EXISTS "${manifest.triggers.incomplete}";
CREATE TRIGGER "${manifest.triggers.incomplete}"
BEFORE UPDATE ON IdentityProviderConnections
WHEN NEW.Id = ${sqlString(owner)}
BEGIN
  SELECT RAISE(ABORT, 'role-e2e-incomplete');
END;`);
    respond(response, 204);
    return;
  }

  respond(response, 404);
}

async function serve(dbPath: string, overridePath: string): Promise<void> {
  const manifest = readManifest(dbPath, overridePath) ?? fail('Run fixtures:prepare before fixtures:serve.');
  verifySchema(dbPath);
  const portValue = env('ROLE_E2E_FIXTURE_PORT');
  const port = portValue ? Number(portValue) : DEFAULT_PORT;
  if (!Number.isInteger(port) || port < 1024 || port > 65535)
    fail('ROLE_E2E_FIXTURE_PORT must be an integer between 1024 and 65535.');

  const server = createServer((request, response) => {
    void handleTrigger(request, response, manifest, dbPath).catch(() => respond(response, 500));
  });
  const close = () => {
    try {
      runSql(dbPath, `DROP TRIGGER IF EXISTS "${manifest.triggers.incomplete}";`);
    } finally {
      server.close(() => process.exit(0));
    }
  };
  process.once('SIGINT', close);
  process.once('SIGTERM', close);
  server.listen(port, '127.0.0.1', () => {
    console.log(`RoleManagement fixture trigger server listening on http://127.0.0.1:${port}`);
    printEnvironment(dbPath, overridePath, port);
  });
}

function cleanup(dbPath: string, overridePath: string): void {
  const manifest = readManifest(dbPath, overridePath);
  cleanupDatabase(dbPath);
  removeOverlay(manifest?.overlayPath ?? overridePath);
  const file = manifestPath(dbPath);
  if (existsSync(file))
    unlinkSync(file);
  console.log('RoleManagement fixture rows, exact triggers, marked overlay, and manifest cleaned up.');
}

function prepare(dbPath: string, overridePath: string): void {
  readManifest(dbPath, overridePath);
  prepareDatabase(dbPath);
  try {
    writeOverlay(overridePath);
    writeFileSync(manifestPath(dbPath), `${JSON.stringify(expectedManifest(dbPath, overridePath), null, 2)}\n`, { encoding: 'utf8', flag: 'w' });
  } catch (error) {
    cleanupDatabase(dbPath);
    throw error;
  }
  console.log('RoleManagement fixture database prepared. Source these values into the test shell:');
  printEnvironment(dbPath, overridePath);
}

async function main(): Promise<void> {
  const command = process.argv[2];
  if (!['prepare', 'serve', 'cleanup'].includes(command))
    fail('Usage: node fixture-tool.ts <prepare|serve|cleanup>');

  const dbPath = databasePath();
  const overridePath = overlayPath(dbPath);
  if (command === 'prepare')
    prepare(dbPath, overridePath);
  else if (command === 'serve')
    await serve(dbPath, overridePath);
  else
    cleanup(dbPath, overridePath);
}

main().catch(error => {
  console.error(error instanceof Error ? error.message : 'RoleManagement fixture tool failed.');
  process.exitCode = 1;
});

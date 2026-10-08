# Role-management browser tests

This suite is a black-box Playwright suite for the role-management journey. It
does not start Studio or Core and it does not use a fake backend. Start an
isolated current-main Core host and the matching Studio host first:

- Server Studio (`https://localhost:7113`) against Modular Core
  (`https://localhost:7294/elsa/api`).
- WebAssembly Studio (`https://localhost:7052`) against Classic Core
  (`https://localhost:5001/elsa/api`).

The four projects per host cover 320, 768, 1024, and 1440 pixel viewports.
Only local development URLs are accepted. The browser context and API context
are ephemeral; no Playwright `storageState`, trace, video, screenshot, cookie,
token, or credential artifact is written. Console/network diagnostics retain
counts only, never message text, headers, bodies, or query strings.

## Host topology

Run each host in its own terminal from a clean, isolated development database.
The launch profiles provide the ports below; the Studio backend settings use
the `/elsa/api` prefix shown here.

```bash
# Current-main Core + Server Studio
dotnet run --project /path/to/elsa-core/src/apps/Elsa.ModularServer.Web/Elsa.ModularServer.Web.csproj --framework net8.0 --launch-profile https
dotnet run --project src/hosts/Elsa.Studio.Host.Server/Elsa.Studio.Host.Server.csproj --framework net10.0 --launch-profile https

# Current-main Core + WebAssembly Studio (use a separate Core database)
dotnet run --project /path/to/elsa-core/src/apps/Elsa.Server.Web/Elsa.Server.Web.csproj --framework net8.0 --launch-profile Elsa.WorkflowServer.Web
dotnet run --project src/hosts/Elsa.Studio.Host.Wasm/Elsa.Studio.Host.Wasm.csproj --framework net10.0 --launch-profile https
```

The resulting default URLs are Server Studio `https://localhost:7113` →
Modular Core `https://localhost:7294/elsa/api`, and WebAssembly Studio
`https://localhost:7052` → Classic Core `https://localhost:5001/elsa/api`.
Do not copy development bootstrap credentials from Core configuration into the
test environment; inject a dedicated actor through the operator's secret
channel instead. If either Studio host has a local appsettings override, set
its backend URL to the matching current-main Core URL before starting it.

## Run

Provide actor credentials through an operator-controlled secret channel. Do not
put them in shell history, committed files, test titles, or CI logs. The suite
requires an administrator with role view/create/update/delete and permission
catalog access:

```bash
cd tests/browser/RoleManagement
npm install
npx playwright install chromium

# Inject these values from the local secret manager or an interactive shell:
# ROLE_E2E_ADMIN_USERNAME, ROLE_E2E_ADMIN_PASSWORD
# ROLE_E2E_SERVER_STUDIO_URL, ROLE_E2E_SERVER_BACKEND_URL
# ROLE_E2E_WASM_STUDIO_URL, ROLE_E2E_WASM_BACKEND_URL
npm test
```

For the fail-closed Identity-absent composition, disable the Core `Identity`
and `DefaultAuthentication` shell features, start the same Studio hosts without
signing in, and run `npm run test:identity-absent`. The separate matrix verifies
at all four widths that the Security navigation entry is absent and a direct
`/security/roles` visit renders only the unavailable state with no mutation
action or role table.

## Deterministic deletion fixtures

The optional deletion states can be prepared against an isolated current
Modular Core SQLite database. The fixture tool requires Node 20+ and the local
`sqlite3` command; it never reads Core or Studio credentials and it never calls
a production endpoint. Use a copy of the database or a dedicated development
database, and stop the matching Core host while replacing its database rows:

```bash
cd tests/browser/RoleManagement

# The confirmation is intentionally required to prevent a shared database
# from being modified by accident.
export ROLE_E2E_SQLITE_PATH=/absolute/path/to/elsa-core/src/apps/Elsa.ModularServer.Web/elsa_workflows.db
export ROLE_E2E_SQLITE_CONFIRM=isolated

# This creates exact role-e2e-* rows, four database-owned connections, the
# marked configuration overlay, and a manifest beside the database. Source
# only the generated export lines into the current shell.
fixture_output="$(npm run fixtures:prepare)"
printf '%s\n' "$fixture_output"
eval "$(printf '%s\n' "$fixture_output" | sed -n '/^export /p')"
```

The generated `ROLE_E2E_CORE_OVERLAY_PATH` is a marked, secret-free JSON file.
The exported `RoleManagementE2EFixtures__IncludeUnverifiedPermissionDescriptor`
switch opts the Modular sample host into the single `verified:false` catalog
descriptor supplied for this acceptance harness; it is disabled by default.
Start Modular Core from its app directory with the overlay path supplied before
starting Studio (the overlay is loaded at startup):

```bash
cd /absolute/path/to/elsa-core/src/apps/Elsa.ModularServer.Web
dotnet run --project Elsa.ModularServer.Web.csproj --framework net8.0 \
  --launch-profile https -- \
  --Elsa:PlatformIntegration:ShellOverlayPath="$ROLE_E2E_CORE_OVERLAY_PATH"
```

In another terminal, source the generated exports and run the localhost-only
trigger server. It binds to `127.0.0.1`; its two POST routes accept only the
exact generated role ID and perform only the conflict revision bump or the
incomplete SQLite trigger installation:

```bash
cd tests/browser/RoleManagement
export ROLE_E2E_SQLITE_PATH=/absolute/path/to/elsa-core/src/apps/Elsa.ModularServer.Web/elsa_workflows.db
export ROLE_E2E_SQLITE_CONFIRM=isolated
npm run fixtures:serve
```

Keep that process running while the browser tests execute. The emitted URLs
are the values for `ROLE_E2E_CONFLICT_TRIGGER_URL` and
`ROLE_E2E_INCOMPLETE_TRIGGER_URL`; the browser suite posts only the fixture role
ID. The incomplete route creates a `BEFORE UPDATE` trigger for the second
fixture connection, so the first owner can be changed and the second owner
fails during the real Core best-effort operation. This is an SQLite race seam,
not a Core endpoint or a response interception.

Run the optional states by exporting the generated role IDs alongside the
matching Studio/Core URL pair. The trigger server prints the two URL exports
when it starts; copy those lines into the shell running `npm test`. Then clean
up in a separate terminal after stopping Studio and Core:

```bash
cd tests/browser/RoleManagement
ROLE_E2E_SQLITE_PATH=/absolute/path/to/elsa-core/src/apps/Elsa.ModularServer.Web/elsa_workflows.db \
ROLE_E2E_SQLITE_CONFIRM=isolated \
npm run fixtures:cleanup
```

Cleanup drops only the manifest's exact trigger name, deletes only the six
`role-e2e-*` role IDs and four exact connection IDs, removes the marked overlay,
and removes the manifest. It refuses to modify a row whose ID has been reused
with different fixture content. It is safe to run again after successful
cleanup. If the trigger server is interrupted, it drops its exact incomplete
trigger but leaves fixture rows for the explicit cleanup command.

For a single host, set only its matching Studio/Core URL pair. Supplying both
pairs runs both topologies and all four viewport projects. To exercise the
real load-error recovery path, open Roles, stop only the matching Core process
with Ctrl-C in its own terminal, reload and verify the `Roles could not be
loaded` card, restart that same command, then activate `Try again`. This is a
manual gate because intercepting a successful browser response would no longer
be a real-host proof.

For restricted-user proof, the suite provisions a generated view-only Role and
User through the authenticated Core APIs, checks read-only rendering and a
direct create rejection, then deletes both fixtures. Operators may instead
inject `ROLE_E2E_RESTRICTED_USERNAME` and `ROLE_E2E_RESTRICTED_PASSWORD`; the
response body from the rejected mutation is never read or logged.

## Catalog fixture

The prepared environment sets `ROLE_E2E_REQUIRE_UNVERIFIED_CATALOG=true` and
enables Core's dormant sample-host descriptor switch. The fixture tool prepares
the unresolved legacy grant directly in SQLite because Core correctly rejects
unknown grants through its public API; the test preserves the original text and
explicitly replaces it.

Ordinary test roles are created with a short-lived in-memory Core API session
and deleted in fixture teardown. If a host is interrupted, remove only the
explicitly named isolated fixture role/database; do not clean broad paths or a
shared development database.

The Modular sample host intentionally has no browser CORS policy. For the WASM
matrix, `npm run proxy:serve` supplies a localhost-only HTTPS forwarding seam
that adds CORS headers while leaving every request, response, authorization
decision, and mutation on the real Core API. Export an exact loopback origin,
upstream, listener URL, and a temporary PFX produced by `dotnet dev-certs https`:

```bash
export ROLE_E2E_PROXY_ALLOWED_ORIGIN=https://localhost:7052
export ROLE_E2E_PROXY_UPSTREAM=https://localhost:7294
export ROLE_E2E_PROXY_URL=https://localhost:5001
export ROLE_E2E_PROXY_PFX_PATH=/absolute/path/to/temporary-dev-cert.pfx
export ROLE_E2E_PROXY_PFX_PASSWORD='<runtime-only password>'
export ROLE_E2E_PROXY_UPSTREAM_CERT_PATH=/absolute/path/to/core-public-cert.pem
npm run proxy:serve
```

The proxy refuses non-loopback endpoints, non-matching origins, and paths
outside `/elsa/api/`. It validates the upstream TLS certificate against the
explicit public certificate instead of disabling TLS verification. A local
Core certificate can be captured without its private key by piping
`openssl s_client -connect localhost:7294 -servername localhost -showcerts`
into `openssl x509 -out core-public-cert.pem`. Remove both temporary
certificates after the run.

## Scope of evidence

The suite exercises the real Studio DOM and observes the role API methods and
status codes without reading response bodies containing authentication data.
It covers:

- create, save, reload, update, reload, safe impact, and 204 delete;
- exact and wildcard grants, visible token-refresh messaging;
- restricted rendering and server-side create rejection;
- keyboard focus, axe serious/critical checks, and responsive list layout;
- opt-in configuration blocking, editable remediation, dependency conflict,
  incomplete-retention outcomes, and unresolved-grant repair.

The existing External Authentication browser suite remains separate:
`tests/browser/ExternalAuthentication`.

Set `ROLE_E2E_EVIDENCE_DIR` to a repository-local directory to save role-list
screenshots made only from generated fixture names. The suite rejects paths
outside the Studio checkout and still leaves traces, videos, credentials,
cookies, and tokens off disk.

## Coverage and known gates

| Required behavior | Harness proof | Default state |
| --- | --- | --- |
| Server and WASM against current Core | host-specific base URL and API URL projects | Runnable when local hosts and admin actor are injected |
| Authorized lifecycle | real UI create/update/delete; observed Core `200`/`204` responses | Covered |
| Exact, category bulk, wildcard, reach | category action, sibling advanced tab, wildcard reach card | Covered |
| Search, empty, responsive, keyboard, accessibility | real list search, filtered-empty card, four widths, focus and axe | Covered |
| Restricted rendering and mutation authorization | restricted browser session plus separate restricted Core API request expecting `403` | Requires restricted actor |
| Safe deletion and token-refresh note | impact modal, confirmation, `204`, refresh/reissuance wording | Covered |
| Configuration blocker | isolated role ID referenced by startup configuration | Opt-in; skipped until seeded |
| Editable remediation | isolated database policy references, explicit checkboxes, `204` remediation | Opt-in; skipped until seeded |
| Conflict / incomplete retention | local-only post-inspection fixture hook | Opt-in; skipped until a repeatable hook exists |
| Unresolved legacy repair | isolated DB-seeded role; original text, disabled save, explicit replacement, persisted reload | Opt-in; Core public API cannot seed it |
| `verified:false` catalog entry | isolated Core catalog descriptor; marker and selectable checkbox | Opt-in; sample hosts have none |
| Core outage/error and retry | operator stop Core after navigation, observe error card, restart Core, click Try again | Manual runbook gate; no network interception is accepted |
| Identity feature absent | no supported toggle in shipped Server/Modular or WASM/Classic hosts | Blocked until a separate host composition omits Identity; restricted direct-route proof remains covered |

Skipped optional tests are intentional failed gates for release review, not
passes. A complete M3 run requires recording an explicit fixture ID/hook and
the resulting observed status for each optional row. Actual permission claim
change for an already issued token is an operator-controlled follow-up: use a
second ephemeral browser context, reissue/refresh through the host, and verify
the effective permission without recording a token, cookie, URL query, or
response body.

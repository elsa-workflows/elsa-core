# Migrate to the Structured Authorization Model

Elsa's permission vocabulary changes shape. A permission is now `{resource}:{verb}` — a hierarchical resource path paired with a verb — replacing the flat `verb:resource` strings.

**This is a breaking change for any deployment with hand-authored roles.** Legacy permission strings stop authorizing. Nothing silently degrades: a startup validator reports every stored permission that no longer resolves, identified by the role that holds it.

## What you have to do

Re-author each role's permissions using the table below, or through the catalog at `GET /identity/permissions`, which lists every registered resource and the verbs it accepts.

**`*` keeps working.** It parses to `*:*`, so the seeded administrator role continues to authorize everything and an instance cannot lock itself out while the rest is re-authored. Do this first, before touching anything else.

## Three things that are not a simple rename

### The migration expands

Some new resources are finer-grained than the permissions they replace, so one legacy string becomes several. **A one-for-one substitution silently narrows the role.**

| Legacy | Expands to |
| --- | --- |
| `read:workflow-definitions` | `workflows/definitions:view` **and** `workflows/definitions/versions:view` |
| `delete:workflow-definitions` | `workflows/definitions:delete` **and** `workflows/definitions/versions:delete` |
| `publish:workflow-definitions` | `workflows/definitions:publish` **and** `workflows/definitions/versions:revert` |
| `external-authentication:links:manage` | `identity-links:view`, `:write` **and** `:delete` |
| `external-authentication:policies:manage` | `policies:view` **and** `:update` |

### `read:*` and `exec:*` become more powerful

Today they are literal claim values, not patterns: `read:*` authorizes only the twelve endpoints that happen to list it, out of roughly forty read endpoints. Their replacements, `*:view` and `*:execute`, work as the names always implied — across every resource, including ones added later.

**Review any role holding them by hand.** Do not rewrite them automatically.

### The C#/Python expression permissions are removed

`exec:csharp-expressions` and `exec:python-expressions` are dropped rather than translated. They conflated an incoherent execution-side gate — a workflow runs under the server's authority, not the caller's, so the check never constrained what a script could do — with a meaningful authoring-side one.

**This is a deliberate reduction in control.** The host switch (`CSharpOptions.AllowHostCodeExecution`, `PythonOptions.AllowHostCodeExecution`) becomes the single control:

- Where host code is **disabled**, nothing changes.
- Where host code is **enabled**, any author who may write workflow definitions may use C# and Python, and the editor offers those expression types to every such author.

Deployments that enabled host code while trusting only *some* authors lose that granularity, and this is now the
intended posture rather than a gap awaiting a fix: [#7975](https://github.com/elsa-workflows/elsa-core/issues/7975)
was closed as won't-do. Authoring a workflow is a trusted act, and a per-author gate would not have changed what a
script can do once it runs. If some of your authors are not trusted with host code, give them a host with the
switch off; the switch is per language, so C# and Python can be decided separately.

## Revocation

The default access-token lifetime drops from 1 hour to **15 minutes**. This is the revocation bound: permission claims are issued at sign-in, and refreshing re-reads the user's roles, so removing a role takes effect at most one access-token lifetime later. Refresh already rotates both tokens, so no client change is required. User resolution on refresh is a separate break: see [refresh-token user resolution](refresh-token-user-resolution.md).

Elsa role and permission changes take effect on the next token refresh or expiry. Changes to grants from an external identity provider take effect only after a fresh sign-in. Stamp-based revalidation (`Identity:PermissionStamp:IsEnabled` and a ~30 second `CacheLifetime`) is planned for 3.10; it is not available in 3.9.

## Signing out revokes the session

`POST /identity/logout` revokes the caller's sign-in session: the refresh token it is given, and every refresh token issued in the same session, are refused by `/identity/refresh-token` with the same `401` as an invalid token. Access tokens are not revoked and stay valid until they expire. See [Signing Out](../wiki/identity-tenancy-security.md#signing-out) for the contract.

Revocations need storage. With EF Core persistence, apply the `RevokedSessions` migration for your provider; it adds the `RevokedSessions` table (indexed on `ExpiresAt`) and changes nothing else. Without persistence, revocations are held in memory, which is only sound for a single node.

**Apply the migration before upgraded hosts refresh tokens.** Every refresh consults the `RevokedSessions` table, so an upgraded host running against a database without it fails to refresh tokens. Elsa's automatic migration covers it: the Identity persistence features run their migrations at startup unless you turned that off (`RunMigrations = false`), in which case apply the `RevokedSessions` migration yourself before rolling out the new version.

**Changing `RefreshTokenLifetime`.** Raising it is safe. A revocation outlives every refresh token issued with a lifetime no longer than the current one, and every refresh token that the one presented at sign-out was refreshed from, whatever lifetime they were issued with. Lowering it leaves one gap, for at most the old lifetime: a refresh token issued with the old, longer lifetime that the presented one was not refreshed from, such as one obtained by refreshing a copy of an earlier token of the session, can outlive the revocation and work again once it is pruned. See [Signing Out](../wiki/identity-tenancy-security.md#signing-out).

### Breaking change: custom `IAccessTokenIssuer` implementations

`IAccessTokenIssuer` has a new member, `IssueTokensAsync(User user, SignInSession? session, CancellationToken cancellationToken = default)`, without a default implementation, so a custom issuer no longer compiles until it implements it. `/identity/refresh-token` and `IIdentityRefreshTokenService` call it with the session of the refresh token they exchange; `null` starts a new session, as signing in does.

The refresh token it issues must continue that session. Pass it to `IElsaTokenService.IssueRefreshTokenAsync` as `TokenIssuanceContext.Session`, which is what `DefaultAccessTokenIssuer` does. An issuer that creates refresh tokens itself puts `session.Id` in the `elsa:session_id` claim, and the later of `session.ExpiresAt` and the token's own expiry in the `elsa:session_exp` claim, in seconds since the Unix epoch like `exp`. An issuer that drops the session starts a new one on every refresh, and signing out then leaves valid, until they expire, the refresh tokens held before the latest refresh; a default implementation would have done exactly that without warning, which is why there is none.

### Breaking change: `DefaultIdentityRefreshTokenService` constructor

The public constructor of `DefaultIdentityRefreshTokenService` now requires a `SessionRevoker`. This affects only code that constructs the service directly (for example, in tests or a hand-built container); hosts that use the registered `IIdentityRefreshTokenService` are unaffected. Resolve `SessionRevoker` from DI and pass it to the constructor.

Nothing else changes for clients. Refresh tokens now carry `elsa:session_id` and `elsa:session_exp` claims, and ones issued before the upgrade keep working until they expire. Signing out with a pre-upgrade token revokes only the session derived from that token; older pre-upgrade tokens of the same refresh chain stay valid for the remaining time under their issued expiry, which can outlast the current `RefreshTokenLifetime` if that setting was lowered during the upgrade.

## External authentication grant boundaries

`ExternalAuthentication:PermissionGrants:AllowedPermissions` and `DeniedPermissions` bound which permissions an
external identity provider connection may confer. Both lists are now matched as **permission patterns** rather than
by exact string, so they read the way a role does.

- **Denied** is matched in both directions. `workflows/*:delete` denies `workflows/definitions:delete`, and a
  connection granting `workflows/*:delete` is denied by a deny list naming only `workflows/definitions:delete`.
  Before this release both comparisons were exact, so either spelling slipped past the other and a deployment's
  deny list did not hold. If you carried a deny list across the upgrade, re-read it: it may now deny more than it
  used to, which is the intent. A consequence to plan for: any non-empty deny list refuses every wildcard grant
  that could reach a denied permission, and `*` (which parses to `*:*`) reaches all of them — so a role holding
  `*`, including the seeded administrator role, will not survive external issuance. Operators using
  `DeniedPermissions` must give externally-authenticating administrators enumerated grants instead of `*`.
- **Allowed** is matched one way: an allow entry must cover the whole grant. `workflows/*:delete` admits
  `workflows/definitions:delete`, but an allow list naming only `workflows/definitions:delete` refuses a
  `workflows/*:delete` grant rather than admitting the part that overlaps.

The boundary now also applies to permissions the user's **own Elsa roles** carry, not only to those an external
claim mapping confers. Previously token issuance concatenated role permissions raw, so a permission the boundary
excluded during sign-in reappeared in the issued token from the same roles — which made the deny list
unenforceable for anything a role happened to carry. If you configured a boundary expecting it to bound the whole
token, it now does. If you configured one expecting it to bound only claim-mapped permissions, an external login
may now carry fewer permissions than before; widen the list, or move the restriction into the roles themselves.
Deployments with no boundary configured, which is the default, are unaffected.

A boundary that does not parse is now a **startup failure** rather than a silently ignored setting. An allow list
whose entries are all malformed used to reduce to an empty list, which means unrestricted, so a typo turned the
boundary off. Fix the entries the startup error names; the mapping table below gives the new spelling.

Rewrite both lists into the new `{resource}:{verb}` vocabulary using the [full mapping](#full-mapping). A value that
is not a well-formed permission matches nothing, and a grant that is not well-formed is dropped at sign-in with a
`malformed_permission` warning instead of being carried into a token.

The same matching now governs the delegation check: an actor may configure a mapping only for permissions their own
grants cover, so holding `workflows/*:delete` lets them delegate `workflows/definitions:delete`, while holding just
that leaf does not let them delegate the subtree.

## The legacy permission constant classes are gone

The `<Module>Permissions` classes holding `verb:resource` strings — `AIPermissions`, `ConsoleLogsPermissions`,
`DashboardPermissions`, `ExternalAuthenticationPermissions`, `OpenTelemetryPermissions`, `SecretsPermissions`,
`StructuredLogsPermissions` and `UserTasksPermissions` — are removed rather than marked obsolete. Referencing one is now a compile
error, which is deliberate: every string they held is unparseable under the `{resource}:{verb}` grammar, so
keeping them would leave code that still compiles, still reads as a permission check, and silently authorizes
nothing. A compile error names the site and can be fixed against the mapping table below; an obsolete constant
gives a warning that is easy to suppress and a runtime failure that is not visible at all.

Replace each with the module's `<Module>ResourcePermissions` constant and a verb. Classes still referenced by
their own modules — `WorkflowPermissions`, `IdentityPermissions` and the rest — are untouched.

## Setting default roles now requires its own permission

Authoring the `defaultRoleIds` of an unlinked-identity policy now requires
`external-authentication/policies/default-roles:update`. Previously only the subset rule applied — you could
not grant roles carrying permissions you did not hold, but any actor who could edit a connection could decide
what auto-created users receive.

The permission was enforced only when removing policy references during role deletion, while its sibling
`external-authentication/policies:update` was already enforced on the write path. That asymmetry is what this
closes, and it makes "may configure connections, may not decide what auto-created users receive" expressible.

**Who this affects.** Anyone who held legacy `external-authentication:roles:assign` already maps to the new
permission and is unaffected. The break is for roles holding `external-authentication:policies:manage`
(→ `policies:view` + `policies:update`) but *not* `roles:assign`, which set default roles today. Grant them
`external-authentication/policies/default-roles:update`, or move that responsibility to a role that has it.

The permission is required when the default-role set **changes** — adding, removing, clearing, or switching
the policy away from one that creates users, which drops its roles just as surely. Leaving a stored set alone
needs nothing extra, so an administrator without the permission can still edit other fields on a connection
whose default roles someone else configured, enable it, or validate it.

## User tasks join the structured vocabulary

User Tasks was still declaring access through the legacy channel after the rest of the codebase had moved, so its
nine permissions are re-authored in this release: `read:user-tasks` and its siblings become verbs on a `user-tasks`
resource, and participant lookup becomes the sub-resource `user-tasks/participants`.

**This is a breaking change for anyone granting the legacy strings**, which is everyone who granted User Tasks
anything — they were the only spelling that ever worked. Rewrite them from the table below. The strings do not
merely stop matching new-style grants; they were being compared as exact claim values, so nothing else had ever
matched them either.

**Pattern grants now reach these endpoints for the first time.** Under the legacy declaration a claim had to equal
the required string character for character, so `*:view`, `user-tasks:*` and `user-tasks/*:view` all failed against
every User Tasks endpoint even though they read as though they covered it. A bare `*` worked, because it was
special-cased. If you worked around this by granting the exact legacy strings alongside a pattern, the pattern is
now doing the work and the legacy strings can go.

**`manage:user-tasks` becomes `user-tasks:supervise`, not `user-tasks:manage`.** The permission never was an
aggregate — it grants tenant-wide oversight (read every task, assign, reschedule, cancel, see blocked tasks, retry
a failed resolution) and confers none of `claim`, `complete`, `assign`, `cancel` or `invite`. It is renamed for the
same reason `workflows/runtime:control` is not called `manage`: a name that reads like an aggregate invites being
granted as one.

**Recorded consequence:** because participant lookup is a sub-resource, `user-tasks/*:view` now grants it along
with task read, where legacy `read:user-tasks` did not. Participant lookup returns a tenant-scoped directory of
users and groups, so a role that should read tasks without enumerating the directory must name `user-tasks:view`
rather than the subtree.

If you implement `IUserTaskAccessPolicy` or construct `UserTaskActor` yourself: `UserTaskActor.HasPermission` now
takes a `Permission` (or a resource and verb) instead of a single string, and matches through `PermissionMatcher`
rather than by equality, so pattern grants reach your policy too. `UserTaskActor.Permissions` is compared ordinally
rather than case-insensitively, matching the rest of the model.

## The SecurityRoot policy and the localhost grant are gone

`SecurityRoot` is removed, and with it every type and switch that existed only to serve it:
`IdentityPolicyNames`, `LocalHostRequirement` and `LocalHostRequirementHandler`,
`LocalHostPermissionRequirement` and `LocalHostPermissionRequirementHandler`,
`LocalHostPermissionRequirementOptions`, the `EnableLocalHostPermissionGrant` property on both
`DefaultAuthenticationFeature` types, and the `EnableLocalHostPermissionGrantForSecurityRoot` /
`DisableLocalHostPermissionGrantForSecurityRoot` toggles along with the already-obsolete
`DisableLocalHostRequirement()` alias. Calls to any of them are now compile errors; delete them, since with no
policy left to grant into there is nothing for them to configure. The
`ConfigureAuthorizationOptions` hook on `DefaultAuthenticationFeature` stays, now defaulting to a no-op rather
than to registering the policy, so a host that adds policies of its own keeps working unchanged. ADR 0010 had already decided endpoints should be
authorized by their own permissions; this finishes it.

Two of the three endpoints that used the policy (`Roles/Create`, `Applications/Create`) already declared a
permission, so nothing changes for them. **`POST /identity/secrets/hash` is a tightening**: `SecurityRoot`
resolved by default to `RequireAuthenticatedUser()`, so any signed-in caller could exercise the password
hasher. It now requires `identity/users:create`. The scope is user-only on purpose: application provisioning
does not go through it, because `POST /identity/applications` generates and hashes the client secret and API
key itself and returns both, so `identity/applications:create` alone remains sufficient to create an
application.

**If you relied on the localhost permission grant to bootstrap an instance**, configure one of these instead —
both work in a deployed environment, not just on localhost, and both attach an identity to whatever the
caller then does:

- **A seeded administrator.** `UseDefaultAdmin(username, password, roleName, permissions)` in code-first
  hosts, or the `DefaultAdminUser` shell feature in shell-based hosts. It runs when each tenant is activated,
  gives each tenant its own admin role and user (also when tenants share one store), and adds any configured
  permissions an existing admin role lacks. See
  [Default Admin User Bootstrap](../../src/modules/Elsa.Identity/README.md#default-admin-user-bootstrap) in the
  Identity README for configuration and operational notes, including what to do with the bootstrap password
  when you rotate the admin's password.
- **An admin API key.** In code-first hosts, `UseAdminApiKey(key)` on `DefaultAuthenticationFeature` (or
  `UseAdminApiKey(options => ...)` to also set the owner name and permissions). In shell-based hosts, the
  `AdminApiKey` setting on the `DefaultAuthentication` shell feature. Disabled unless configured. While it is
  enabled it replaces the application-based API key provider; see the
  [Identity README](../../src/modules/Elsa.Identity/README.md#admin-api-key-bootstrap) for turning it off.

The localhost grant trusted network position, which stops meaning anything behind a reverse proxy, inside a
container, or across a port-forward — and it granted *unauthenticated* access, so the bootstrap action had no
identity to audit. It was also already unable to do the thing it existed for: it granted
`identity/users:create`, but `POST /identity/users` did not carry the policy that injected it.

If neither is configured and no users exist, startup now logs an error naming both options, rather than
leaving permission-protected management endpoints to refuse every caller without explanation: anonymous
requests get 401, and authenticated callers without the required permission get 403.

## Installed features are readable by any signed-in user

`GET /features/installed` and `GET /features/installed/{fullName}` require an authenticated caller and no
permission. Clients such as Elsa Studio decide which modules to render from this list, so gating it hid every
module, including ones the caller holds permissions for, from anyone without the grant. Anonymous callers are
still rejected.

`system/features:view` therefore no longer gates anything. It stays in the catalog, and in the mapping below,
so roles that already hold it keep resolving instead of being reported by the startup validator; you can drop
it from your roles at your convenience.

## Descriptor catalogs are readable by any signed-in user, and a definition viewer can list versions

Elsa Studio loads the descriptor catalogs, and a definition's versions, to open the designer. A role holding only
`workflows/definitions:view` could list definitions but got a 403 on each of those calls when it opened one.

These read-only catalogs now require an authenticated caller and no permission. Anonymous callers are still rejected.
Most describe what is installed, not anything stored, so gating them added no protection. The exception is the
activity catalog: `GET /descriptors/activities` and `GET /descriptors/activities/{typeName}` also list an activity for
each stored workflow definition that is marked as usable as an activity, so any authenticated user in the tenant can
now see those workflows' names, descriptions and inputs. This is deliberate, because instance viewers need those
descriptors to render workflows that use them.

- `GET /descriptors/activities` and `GET /descriptors/activities/{typeName}`
- `GET /descriptors/variables`
- `GET /descriptors/storage-drivers`
- `GET /descriptors/output-converters`
- `GET /descriptors/expression-descriptors`
- `GET /descriptors/workflow-activation-strategies`
- `GET /descriptors/incident-strategies`
- `GET /descriptors/log-persistence-strategies`
- `GET /descriptors/commit-strategies/activities` and `GET /descriptors/commit-strategies/workflows`
- `GET /resilience/strategies`

`GET /workflow-definitions/{definitionId}/versions` now requires `workflows/definitions:view` instead of
`workflows/definitions/versions:view`. It is not a static catalog, but a caller who can read a definition could already
read every one of its versions, so the version list disclosed nothing further. A role that held
`workflows/definitions/versions:view` without `workflows/definitions:view` can no longer list versions; grant
`workflows/definitions:view`, which the `read:workflow-definitions` mapping below already includes. Deleting and
reverting versions still require `workflows/definitions/versions:delete` and `:revert`.

The permissions these endpoints used to require (`workflows/descriptors/<kind>:view`, `resilience/strategies:view` and
`workflows/definitions/versions:view`) no longer gate reading them, with one exception described below. They stay in the
catalog, and in the mapping below, so roles that already hold them keep resolving instead of being reported by the
startup validator; you can drop them from your roles at your convenience.

`workflows/descriptors/activities:view` is not one of the permissions you can drop. It still guards two things:

- `GET /descriptors/activities?refresh=true`, which rebuilds the activity registry from the stored definitions. The flag
  takes effect for a caller holding this permission or `workflows/definitions:view`; any other caller is not rejected,
  the flag is ignored and the current registry is returned. Elsa Studio sends the flag on every load, which in a cluster
  is how a node picks up a workflow-as-activity published through another node, so a role holding only
  `workflows/definitions:view` opens the designer with a current catalog.
- `POST /descriptors/activities/{activityTypeName}/options/{propertyName}`, which runs the property's option provider
  with caller-supplied context.

Some other calls the designer makes are also unchanged, so a role holding only `workflows/definitions:view` still
needs their own permissions for them:

- `GET /workflow-definitions/{definitionId}/labels` and `GET /labels` (the label permissions)
- `POST /scripting/javascript/type-definitions/{definitionId}` (the JavaScript scripting permission)
- `GET /secrets/descriptors` (the secrets permission)

## The dashboard is readable by any signed-in user, one section at a time

Elsa Studio shows the Dashboard to every signed-in user and gates each widget by the permission of the data it shows. The
API applies the same rule. `dashboard:view` still reads the whole operational overview, so roles that hold it keep working
unchanged; a narrower permission now reads just the data it guards.

`GET /dashboard/overview` requires an authenticated caller and no permission; anonymous callers still get 401. It never
refuses a caller who can read part of it. A section the caller may not read comes back with `Capability` set to
`Unauthorized` and no data:

| Overview section | Readable with `dashboard:view` or |
| --- | --- |
| `workflowInstances` | `workflows/instances:view` |
| `runtime` | `workflows/runtime:view` |
| `diagnostics.structuredLogs` | `diagnostics/structured-logs:view` |
| `diagnostics.consoleLogs` | `diagnostics/console-logs:view` |

`runtime` and `workflowInstances` carry a new `capability` field for this, which reads `Available` for a section the
caller may read. Metric cards and panels follow the permission of the data they summarise, and a caller who may not read
them does not receive them. A caller holding no dashboard-related permission gets an overview with every section
`Unauthorized`.

`POST /dashboard/workflow-trends`, `GET /dashboard/recent-activity`, `GET /dashboard/needs-attention` and
`POST /dashboard/workflow-hotspots` answer 403 unless the caller holds `dashboard:view` or `workflows/instances:view`.
They declare this with `RequireAnyPermission`, so `EndpointPermissionRegistry.FindRequirement` reports both permissions
for them.
Findings on `needs-attention` follow the same rule per finding, so a caller holding only `workflows/instances:view` sees the
workflow findings but not the runtime or diagnostics ones.

Modules that add to the dashboard declare the permission of their data. A contributor can check
`DashboardContext.CanRead` to skip queries for data the caller cannot read; `null` means unrestricted. A contributor declares
`IDashboardContributor.OverviewPermissions` up front for the runtime, instance and diagnostics sections it supplies, and
sets `Permission` on each metric card, panel, finding, and trend, recent-activity or hotspot response it returns. Anything
a contributor supplies without a declaration needs `dashboard:view`, so an existing third-party contributor keeps working
and stays hidden from callers holding only a narrower permission until it declares one. That includes trend, recent
activity and hotspot rows: a caller holding only `workflows/instances:view` receives only the rows of contributions
that declare `workflows/instances:view`, which the built-in workflow contributor does.

`OverviewPermissions` lists the permissions of everything a contributor adds to the overview, that is its sections, metric
cards and panels, and is used only to skip the overview call for a caller who holds none of them; a contributor must
therefore declare every permission its cards and panels carry, or a caller holding only that permission never receives
them. This way a signed-in account with no dashboard permissions costs no database counts, runtime queries or log
queries. An undeclared section still needs `dashboard:view`. Needs-attention, trends,
recent activity and hotspots always invoke every contributor and filter afterwards by the `Permission` on what it returns.
When several contributors add to one section, contributions the caller may not read are ignored. If any contribution the caller may read failed, the section is `Unavailable` with no figures, never partial totals and never `Unauthorized`; it is `Unauthorized` only when the caller may read none of them. A caller whose
permissions read nothing the contributors declared or supplied gets every section as `Unauthorized`, including the ones no module supplies,
so the response does not reveal which modules are installed, and `backendName` and `environmentName` are left empty.

## Third-party modules

Modules outside this repository keep compiling. `ConfigurePermissions(params string[])` remains available but obsolete, and a permission that resolves to no registered descriptor registers an implicit one marked unverified, logs a warning, and appears as such in the catalog. The module keeps working and the gap stays visible.

An endpoint that accepts any one of several permissions declares `RequireAnyPermission((resource, verb), ...)`. It is
evaluated, recorded and governed by `EndpointSecurityOptions.SecurityIsEnabled` exactly as `RequirePermission` is, and
the endpoint coverage gate accepts it. `EndpointPermissionRegistry` records each declaration as an
`EndpointPermissionRequirement` whose `AnyOf` lists the permissions that satisfy it; read it with `FindRequirement`, or
enumerate every declaration with `AllRequirements`. `Find` and `All` are unchanged for an endpoint that requires exactly
one permission, and report nothing for one that accepts any of several, so tooling that should see those endpoints
moves to the new accessors.

## Per-tenant identity uniqueness

Included in the same release: `User.Name`, `Role.Name`, `Application.Name` and `Application.ClientId` move from globally unique indexes to composite indexes on `(TenantId, Name)`. Two tenants could not previously hold a role of the same name. Apply the `PerTenantIdentityUniqueness` migration for your provider.

If you have duplicate names across tenants today, they were impossible to create, so no data conflict can arise. Going the other way — downgrading — will fail if duplicates exist by then.

One caveat: the composite indexes only cover rows whose `TenantId` is non-null (SQL Server filters null rows out of the index; SQLite, PostgreSQL and MySQL treat nulls as distinct — Oracle alone still rejects null-tenant duplicates). `TenantId` is only assigned when multitenancy is enabled, so in a single-tenant deployment every row keeps null and user, role and application name uniqueness becomes application-enforced rather than schema-enforced: the pre-save existence checks block sequential duplicates, but the database no longer backstops concurrent ones. Likewise, rows written before the upgrade keep a null `TenantId`, and in a multi-tenant deployment's default tenant those legacy rows and new `""`-tenant rows are distinct index keys, so the index cannot catch a name collision between them. See the same caveat, with the reasoning, in [secrets-tenancy.md](secrets-tenancy.md).

## 3.10: Generated role IDs and tenant-scoped role names

Role IDs are unique across the whole identity store, but until 3.10 they were derived from the role name, so tenants
sharing one store could not each hold a same-named role, and only the first tenant activated got a seeded admin
([#8615](https://github.com/elsa-workflows/elsa-core/issues/8615)). From 3.10:

- **New roles get generated IDs.** `POST /identity/roles` without an `id`, and the `DefaultAdminUser` seeder, create
  roles with an opaque generated ID instead of the kebab-cased name (`admin`, `power-user`). A role name only has to
  be unique within its tenant. Each tenant's seeded admin user references its own tenant's admin role by that ID.
- **Existing data is unchanged.** No data migration is needed: Dapper adds an index, and MongoDB replaces its role
  name index (`Name_1` becomes `TenantId_1_Name_1`) at startup; see below. Roles keep their name-derived IDs, and
  users and applications that reference them keep resolving. The seeder still reuses an existing role whose ID is
  `AdminRoleName`.
- **Look IDs up instead of assuming them.** `GET /identity/roles` returns each role's `id` and `name` for the current
  tenant, and `POST /identity/roles` returns the new role's `id`.
- **Review hard-coded role IDs.** On a **new** store, the seeded admin role's ID is no longer `admin`, and a role
  created through `POST /identity/roles` without an `id` no longer gets one derived from its name. Users and
  applications that reference such an ID, including ones defined in configuration, **silently get no permissions** from
  it. Requests that assign it are rejected instead: `POST /identity/users` and `PUT /identity/users/{id}` return 403,
  saving an External Authentication connection whose unlinked-identity policy lists it in `defaultRoleIds` (for
  example `defaultRoleIds: ["admin"]`) fails validation, and a policy that already stores it fails every external
  sign-in that would create a new user with "A configured default role no longer exists." (users who are already
  linked sign in normally). Replace those values with the IDs from `GET /identity/roles`. On an existing store whose `admin` role was created by an earlier version, those references
  keep working.
- **MongoDB and Dapper stores.** The 3.10 MongoDB and Dapper packages, published from this repository's
  `src/extensions`, carry the matching store changes ([#8615](https://github.com/elsa-workflows/elsa-core/issues/8615),
  ported from elsa-extensions#281):
  - Dapper resolves role references by ID (earlier versions matched them against the name column, so a role whose
    ID differs from its name granted nothing). A side effect on upgrade: a user or application that references a
    legacy role whose ID differs from its name, for example `power-user`, now gets that role's permissions.
  - Saving a Dapper role never moves another tenant's row or a tenant-agnostic (`*`) row into the current tenant;
    it fails with an error instead.
  - A new Dapper migration (`Elsa:Identity:V3.10`, version 30005) adds a unique index on `Roles (TenantId, Name)`.
    It never changes or deletes rows: if one tenant already has two roles whose names match ignoring case (with
    `NULL` and `''` both counted as the default tenant), the migration fails, lists the role IDs, and changes
    nothing. Rename or remove the extra roles, update what references them, and run it again. SQL Server treats
    `NULL`s as equal in a unique index; SQLite, PostgreSQL, MySQL and Oracle do not, so on those databases the
    index does not stop a duplicate name among rows without a tenant. On case-sensitive collations (SQLite,
    PostgreSQL, Oracle) the index only rejects exact duplicates, and names that differ only in case are rejected by
    `RoleManager` before saving.
  - MongoDB replaces the store-wide unique role name index `Name_1` with a per-tenant `TenantId_1_Name_1` when the
    host starts. It creates the new index first and then drops the old one, so the collection is never without name
    uniqueness and several nodes can start at once.
  - **Rolling back on MongoDB:** once a second tenant has created a role with a name another tenant already uses, a
    3.9 node can no longer start, because it cannot recreate its store-wide `Name_1` index over those duplicates.
  - MongoDB still keeps user names, and application names and client IDs, unique across the whole store. A second
    tenant sharing a MongoDB store therefore still cannot get a seeded admin user with the same user name. That is
    tracked separately ([#8617](https://github.com/elsa-workflows/elsa-core/issues/8617)).

## Full mapping

| Legacy permission | Replacement |
| --- | --- |
| `*` | `*:*` |
| `read:*` | `*:view` |
| `exec:*` | `*:execute` |
| `read:workflow-definitions` | `workflows/definitions:view` **+** `workflows/definitions/versions:view` |
| `write:workflow-definitions` | `workflows/definitions:write` |
| `delete:workflow-definitions` | `workflows/definitions:delete` **+** `workflows/definitions/versions:delete` |
| `exec:workflow-definitions` | `workflows/definitions:execute` |
| `publish:workflow-definitions` | `workflows/definitions:publish` **+** `workflows/definitions/versions:revert` |
| `retract:workflow-definitions` | `workflows/definitions:retract` |
| `actions:workflow-definitions:refresh` | `workflows/definitions:refresh` |
| `actions:workflow-definitions:reload` | `workflows/definitions:reload` |
| `read:workflow-definition-labels` | `workflows/definitions/labels:view` |
| `update:workflow-definition-labels` | `workflows/definitions/labels:update` |
| `read:workflow-instances` | `workflows/instances:view` |
| `write:workflow-instances` | `workflows/instances:write` |
| `delete:workflow-instances` | `workflows/instances:delete` |
| `cancel:workflow-instances` | `workflows/instances:cancel` |
| `read:activity-execution` | `workflows/activity-executions:view` |
| `read:workflow-runtime` | `workflows/runtime:view` |
| `ManageWorkflowRuntime` | `workflows/runtime:control` |
| `read:bookmark-queue:dead-letters` | `workflows/bookmark-queue/dead-letters:view` |
| `replay:bookmark-queue:dead-letters` | `workflows/bookmark-queue/dead-letters:replay` |
| `delete:bookmark-queue:dead-letters` | `workflows/bookmark-queue/dead-letters:delete` |
| `trigger:event` | `workflows/events:trigger` |
| `tasks:complete` | `workflows/tasks:complete` |
| `exec:tests` | `workflows/tests:execute` |
| `read:activity-descriptors` | `workflows/descriptors/activities:view` |
| `read:activity-descriptors-options` | `workflows/descriptors/activities:view` |
| `read:expression-descriptors` | `workflows/descriptors/expressions:view` |
| `read:storage-drivers` | `workflows/descriptors/storage-drivers:view` |
| `read:variable-descriptors` | `workflows/descriptors/variables:view` |
| `read:commit-strategies` | `workflows/descriptors/commit-strategies:view` |
| `read:incident-strategies` | `workflows/descriptors/incident-strategies:view` |
| `read:log-persistence-strategies` | `workflows/descriptors/log-persistence-strategies:view` |
| `read:output-converters` | `workflows/descriptors/output-converters:view` |
| `read:workflow-activation-strategies` | `workflows/descriptors/activation-strategies:view` |
| `read:javascript-type-definitions` | `workflows/scripting/javascript:view` |
| `exec:csharp-expressions` | *removed* — the host switch is the control; see #7975 |
| `exec:python-expressions` | *removed* — the host switch is the control; see #7975 |
| `read:user` | `identity/users:view` |
| `create:user` | `identity/users:create` |
| `update:user` | `identity/users:update` |
| `delete:user` | `identity/users:delete` |
| `read:role` | `identity/roles:view` |
| `create:role` | `identity/roles:create` |
| `update:role` | `identity/roles:update` |
| `delete:role` | `identity/roles:delete` |
| `create:application` | `identity/applications:create` |
| `read:secrets` | `secrets:view` |
| `write:secrets` | `secrets:write` |
| `delete:secrets` | `secrets:delete` |
| `test:secrets` | `secrets:test` |
| `use:secrets` | *removed* — unused |
| `import:secrets` | *removed* — unused |
| `export:secrets` | *removed* — unused |
| `external-authentication:connections:read` | `external-authentication/connections:view` **+** `external-authentication/descriptors:view` |
| `external-authentication:connections:create` | `external-authentication/connections:create` |
| `external-authentication:connections:update` | `external-authentication/connections:update` |
| `external-authentication:connections:archive` | `external-authentication/connections:archive` |
| `external-authentication:connections:test` | `external-authentication/connections:test` |
| `external-authentication:connections:preview` | `external-authentication/connections:preview` |
| `external-authentication:links:manage` | `external-authentication/identity-links:view` **+** `external-authentication/identity-links:write` **+** `external-authentication/identity-links:delete` |
| `external-authentication:sessions:read` | `external-authentication/sessions:view` |
| `external-authentication:sessions:revoke` | `external-authentication/sessions:revoke` |
| `external-authentication:policies:manage` | `external-authentication/policies:view` **+** `external-authentication/policies:update` |
| `external-authentication:roles:assign` | `external-authentication/policies/default-roles:update` |
| `external-authentication:provider-trust:unsafe` | `external-authentication/provider-trust:override` |
| `external-authentication:permissions:delegate` | `external-authentication/permission-grants:delegate` |
| `external-authentication:permissions:delegate-unrestricted` | `external-authentication/permission-grants:delegate-unrestricted` |
| `read:user-tasks` | `user-tasks:view` |
| `claim:user-tasks` | `user-tasks:claim` |
| `complete:user-tasks` | `user-tasks:complete` |
| `assign:user-tasks` | `user-tasks:assign` |
| `update:user-tasks` | `user-tasks:update` |
| `cancel:user-tasks` | `user-tasks:cancel` |
| `invite:user-tasks` | `user-tasks:invite` |
| `manage:user-tasks` | `user-tasks:supervise` |
| `lookup:user-task-participants` | `user-tasks/participants:view` |
| `read:dashboard` | `dashboard:view` |
| `read:diagnostics:console-logs` | `diagnostics/console-logs:view` |
| `read:diagnostics:structured-logs` | `diagnostics/structured-logs:view` |
| `read:diagnostics:opentelemetry` | `diagnostics/opentelemetry:view` |
| `ingest:diagnostics:opentelemetry` | *removed* — unused |
| `read:resilience` | `resilience/*:view` |
| `read:resilience:retries` | `resilience/retries:view` |
| `read:resilience:strategies` | `resilience/strategies:view` |
| `exec:resilience` | `resilience/*:execute` |
| `exec:resilience:simulate-response` | `resilience/simulation:execute` |
| `read:alterations` | `alterations:view` |
| `run:alterations` | `alterations:execute` |
| `read:labels` | `labels:view` |
| `create:labels` | `labels:create` |
| `update:labels` | `labels:update` |
| `delete:labels` | `labels:delete` |
| `read:tenants` | `tenants:view` |
| `write:tenants` | `tenants:write` |
| `delete:tenants` | `tenants:delete` |
| `execute:tenants:refresh` | `tenants:refresh` |
| `read:installed-features` | `system/features:view` |
| `actions:shells:reload` | `system/shells:reload` |
| `ai:chat` | `ai/chat:execute` |
| `ai:tools:view` | `ai/tools:view` |
| `ai:capabilities:view` | `ai/capabilities:view` |
| `ai:tools:manage` | *removed* — unused |
| `ai:proposals:view` | *removed* — unused |
| `ai:proposals:approve` | *removed* — unused |
| `ai:proposals:apply` | *removed* — unused |

## Studio hosts without the Security module

Studio matches 3.9: a host that does not register `IPermissionService` (no Security module) uses `UserPermissions.Unknown`, so every permission check passes. That is an explicit choice for hosts that do not enforce authorization in the shell. The CustomElements host (`Elsa.Studio.Host.CustomElements`) runs in this Unknown mode: it registers Core, Shell, Workflows, Secrets and User Tasks, but not Security. Install the Security module to fail closed on `GET /identity/me/permissions`.

The Webhooks page is gated on `http/webhooks:view`. That resource is in the backend catalog so a `*` administrator receives it. Dashboard per-widget gating depends on the dashboard section permissions from #8572.

## Studio public API notes

These Studio-side contract changes ship with this release and need a rebuild of any extension that referenced the old names:

- Studio-local Refit interfaces were renamed so they do not collide with `Elsa.Api.Client`: `IExternalAuthenticationConnectionsApi` → `IExternalAuthenticationConnectionManagementApi`, and `IExternalIdentityLinksApi` → `IExternalIdentityLinkManagementApi`.
- `ExternalAuthenticationPermissions` values now use the `{resource}:{verb}` grammar (`external-authentication/connections:view`, and so on). Old Studio-only names such as `external-authentication:connections:read` are not granted by the catalog.
- `IFeatureService.IsInitialized` stays a default interface member (`=> false`). Third-party implementations and decorators do not have to add the member.
- Direct OIDC logout is POST-only: `GET /authentication/logout` does not sign the user out. The hosted form posts to `HostedAuthenticationPaths.LogoutFormAction(baseUri)`, which is `NavigationManager.ToAbsoluteUri("authentication/logout").AbsolutePath`, so a PathBase such as `/studio/` is preserved.
- `AddOpenIdConnectAuth` calls `AddControllersWithViews()`, not `AddControllers()`. The logout action uses `[ValidateAntiForgeryToken]`, and that filter is registered only with the MVC view features.
- `IPermissionSnapshotCache` is owned by `IdentityPermissionContext`. `Invalidate` increments a generation token, drops the snapshot, then raises `Changed` so permission-dependent UI re-fetches. A snapshot is stored only for the generation it was loaded under, and `GetAsync` returns only current-generation grants. An overtaken load is retried at most three times, then `Unavailable` is returned without being cached (fail closed). Environment switches raise `IPermissionRefreshSignal`. A silent JWT refresh raises it only when the refresh failed, or when the `sub` or `permissions` claim changed between the old and new tokens. The signal is a no-dependency scoped service, one per Blazor Server circuit, instead of taking the cache, which would cycle through the backend client.
- `DefaultEnvironmentService` now takes an optional `IEnumerable<IPermissionRefreshSignal>` instead of `IEnumerable<IPermissionSnapshotCache>`. The previous two-argument constructor that accepted the cache enumerable is gone.
- `JwtTokenProvider` gained an `IEnumerable<IPermissionRefreshSignal>` constructor parameter; the original four-argument constructor remains and passes an empty set.
- `ElsaIdentitySignOutService.LoginPath` is obsolete. Sign-out navigates with `NavigationManager.ToAbsoluteUri("login").PathAndQuery` so a PathBase is kept. If clearing the stored tokens fails, the service stays on the page rather than opening login with the token still present.

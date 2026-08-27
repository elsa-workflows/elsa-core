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

The default access-token lifetime drops from 1 hour to **15 minutes**. This is the revocation bound: permission claims are issued at sign-in, and refreshing re-reads the user's roles, so removing a role takes effect at most one access-token lifetime later. Refresh already rotates both tokens, so no client change is required.

For a tighter bound, enable the optional permission stamp (`Identity:PermissionStamp:IsEnabled`). It is derived from the user's roles rather than stored, so it needs no schema change and no cross-node cache invalidation. `CacheLifetime`, default 30 seconds, is the effective bound when enabled.

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

`SecurityRoot` is removed, along with `IdentityPolicyNames`, `LocalHostRequirement`,
`LocalHostPermissionRequirement` and the `EnableLocalHostPermissionGrantForSecurityRoot` /
`DisableLocalHostPermissionGrantForSecurityRoot` toggles. ADR 0010 had already decided endpoints should be
authorized by their own permissions; this finishes it.

Two of the three endpoints that used the policy (`Roles/Create`, `Applications/Create`) already declared a
permission, so nothing changes for them. **`POST /identity/secrets/hash` is a tightening**: `SecurityRoot`
resolved by default to `RequireAuthenticatedUser()`, so any signed-in caller could exercise the password
hasher. It now requires `identity/users:create`.

**If you relied on the localhost permission grant to bootstrap an instance**, configure one of these instead —
both work in a deployed environment, not just on localhost, and both attach an identity to whatever the
caller then does:

- **A seeded administrator.** `UseDefaultAdmin(username, password, roleName, permissions)`, or the
  `DefaultAdminUser` configuration section. It creates the admin role and user at startup and is idempotent,
  so it is safe to leave configured.
- **An admin API key.** `UseAdminApiKey(key)` or the `AdminApiKey` setting. Disabled unless configured.

The localhost grant trusted network position, which stops meaning anything behind a reverse proxy, inside a
container, or across a port-forward — and it granted *unauthenticated* access, so the bootstrap action had no
identity to audit. It was also already unable to do the thing it existed for: it granted
`identity/users:create`, but `POST /identity/users` did not carry the policy that injected it.

If neither is configured and no users exist, startup now logs an error naming both options, rather than
leaving every endpoint to answer 403 without explanation.

## Third-party modules

Modules outside this repository keep compiling. `ConfigurePermissions(params string[])` remains available but obsolete, and a permission that resolves to no registered descriptor registers an implicit one marked unverified, logs a warning, and appears as such in the catalog. The module keeps working and the gap stays visible.

## Per-tenant identity uniqueness

Included in the same release: `User.Name`, `Role.Name`, `Application.Name` and `Application.ClientId` move from globally unique indexes to composite indexes on `(TenantId, Name)`. Two tenants could not previously hold a role of the same name. Apply the `PerTenantIdentityUniqueness` migration for your provider.

If you have duplicate names across tenants today, they were impossible to create, so no data conflict can arise. Going the other way — downgrading — will fail if duplicates exist by then.

One caveat: the composite indexes only cover rows whose `TenantId` is non-null (SQL Server filters null rows out of the index; SQLite, PostgreSQL and MySQL treat nulls as distinct — Oracle alone still rejects null-tenant duplicates). `TenantId` is only assigned when multitenancy is enabled, so in a single-tenant deployment every row keeps null and user, role and application name uniqueness becomes application-enforced rather than schema-enforced: the pre-save existence checks block sequential duplicates, but the database no longer backstops concurrent ones. Likewise, rows written before the upgrade keep a null `TenantId`, and in a multi-tenant deployment's default tenant those legacy rows and new `""`-tenant rows are distinct index keys, so the index cannot catch a name collision between them. See the same caveat, with the reasoning, in [secrets-tenancy.md](secrets-tenancy.md).

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

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

Deployments that enabled host code while trusting only *some* authors lose that granularity until [#7975](https://github.com/elsa-workflows/elsa-core/issues/7975) lands. If that matters to you, disable host code until then.

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
  used to, which is the intent.
- **Allowed** is matched one way: an allow entry must cover the whole grant. `workflows/*:delete` admits
  `workflows/definitions:delete`, but an allow list naming only `workflows/definitions:delete` refuses a
  `workflows/*:delete` grant rather than admitting the part that overlaps.

Rewrite both lists into the new `{resource}:{verb}` vocabulary using the [full mapping](#full-mapping). A value that
is not a well-formed permission matches nothing, and a grant that is not well-formed is dropped at sign-in with a
`malformed_permission` warning instead of being carried into a token.

The same matching now governs the delegation check: an actor may configure a mapping only for permissions their own
grants cover, so holding `workflows/*:delete` lets them delegate `workflows/definitions:delete`, while holding just
that leaf does not let them delegate the subtree.

## Third-party modules

Modules outside this repository keep compiling. `ConfigurePermissions(params string[])` remains available but obsolete, and a permission that resolves to no registered descriptor registers an implicit one marked unverified, logs a warning, and appears as such in the catalog. The module keeps working and the gap stays visible.

## Per-tenant identity uniqueness

Included in the same release: `User.Name`, `Role.Name`, `Application.Name` and `Application.ClientId` move from globally unique indexes to composite indexes on `(TenantId, Name)`. Two tenants could not previously hold a role of the same name. Apply the `PerTenantIdentityUniqueness` migration for your provider.

If you have duplicate names across tenants today, they were impossible to create, so no data conflict can arise. Going the other way — downgrading — will fail if duplicates exist by then.

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
| `exec:csharp-expressions` | *removed* — see #7975 |
| `exec:python-expressions` | *removed* — see #7975 |
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

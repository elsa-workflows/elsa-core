# Contract: Permission Vocabulary

**Status**: Draft — module-owner review pass completed 2026-08-23

**Derived from**: a full census of every permission-declaring endpoint across 16 modules

A permission is `{resource}:{verb}`. Both axes are open, string-keyed, and contributed by modules through descriptors.

## Structure

```
permission := {resource}:{verb}
resource   := hierarchical, '/'-separated
verb       := flat string
satisfies  := resourceMatches(granted, required) && verbMatches(granted, required)
```

**Wildcards.** A trailing `*` on the resource axis matches the named node **and every descendant at any depth**: `workflows/definitions/*` covers `workflows/definitions`, `workflows/definitions/versions` and `workflows/definitions/labels`. A bare `*` matches every resource. On the verb axis, `*` matches any verb. `*:*` is the whole vocabulary.

Including the node itself is deliberate — it is how an administrator reads "grant this subtree", and it behaves consistently whether or not the parent is itself a resource. Withholding a parent while granting children remains possible by naming the children.

Wildcards are the only construct with forward reach: they cover resources and verbs registered in later releases. There are no aggregates, and no verb implies another.

**A bare `*` parses as `*:*`.** A permission string containing no `:` and consisting solely of `*` is normalized at parse time to resource `*`, verb `*`. This is a parsing rule, not an evaluation special case, so FR-021 holds: the evaluator never sees a sentinel and superuser is an ordinary grant. It is what allows the seeded admin role (`DefaultAdminUserOptions.AdminRolePermissions`, defaulting to `["*"]`) and any stored `*` to keep authorizing across the migration without a lock-out window, and it is why the migration table maps `*` to `*:*` as a normalization rather than a behavioral change.

**Wildcards are valid in grants and are validated structurally, not against the catalog.** A grant may name a wildcard resource, a wildcard verb, or both. Because `workflows/*` matches no single descriptor and `*` is deliberately absent from any resource's supported verbs, descriptor validation applies only to *concrete* grants: a concrete resource must be registered, and a concrete verb must appear in that resource's supported verbs. A wildcard segment is accepted whenever it is syntactically well formed, including when it currently matches nothing — a grant written against a module that is not yet installed must survive, since installing the module later is what gives it meaning. The reach report is how an author sees what a wildcard covers today.

## Core verbs (convention, not enforcement)

| Verb | Meaning |
| --- | --- |
| `view` | read, list, query, inspect, export |
| `create` | bring a new record into existence |
| `update` | modify an existing record |
| `write` | create or modify, where the API does not separate the two |
| `delete` | remove a record |
| `execute` | run, dispatch, or invoke against a live system |

**A resource declares either `create` + `update`, or `write` — never both.** Which one depends on whether the module's API separates the operations. Never-both is what stops `write` becoming an aggregate: within any one resource there is no ambiguity about which verb an endpoint requires, so no implication is needed and FR-009 holds. `update` is rejected for upsert endpoints because it misdescribes the grant — `POST /workflow-definitions` creates when no definition ID is supplied. A resource may declare just one where only one operation exists.

Any other verb is module-specific and legitimate; the catalog marks non-core verbs so a reviewer can spot needless synonyms. The check is "is this a redundant synonym", not "is this on an approved list".

## Resource tree

Verbs marked ★ are module-specific.

### Workflows

| Resource | Verbs |
| --- | --- |
| `workflows/definitions` | view, write, delete, execute, publish★, retract★, refresh★, reload★ |
| `workflows/definitions/versions` | view, delete, revert★ |
| `workflows/definitions/labels` | view, update |
| `workflows/instances` | view, write, delete, cancel★ |
| `workflows/activity-executions` | view |
| `workflows/runtime` | view, control★ |
| `workflows/bookmark-queue/dead-letters` | view, delete, replay★ |
| `workflows/events` | trigger★ |
| `workflows/tasks` | complete★ |
| `workflows/tests` | execute |
| `workflows/descriptors/activities` | view |
| `workflows/descriptors/expressions` | view |
| `workflows/descriptors/storage-drivers` | view |
| `workflows/descriptors/variables` | view |
| `workflows/descriptors/commit-strategies` | view |
| `workflows/descriptors/incident-strategies` | view |
| `workflows/descriptors/log-persistence-strategies` | view |
| `workflows/descriptors/output-converters` | view |
| `workflows/descriptors/activation-strategies` | view |
| `workflows/scripting/javascript` | view |

`refresh` and `reload` are both retained and are genuinely distinct: `refresh` is targeted (takes definition IDs, `IWorkflowDefinitionsRefresher`), `reload` is wholesale (`IWorkflowDefinitionsReloader`).

`workflows/runtime:control` is one verb because a single permission governs pause, resume and force-drain today. Named `control` rather than `manage` so it is not mistaken for an aggregate; splitting into `pause`/`resume`/`drain` later is additive.

The nine `workflows/descriptors/*` resources stay distinct because the hierarchy already collapses them — `workflows/descriptors/*:view` grants all nine — so distinctness costs nothing and preserves the option to withhold one.

BPMN interchange reuses `workflows/definitions`: analyze and export require `view`, import requires `write`.

**`exec:csharp-expressions` and `exec:python-expressions` are not carried forward.** See [research.md](../research.md) D21 and [#7975](https://github.com/elsa-workflows/elsa-core/issues/7975). They conflated an incoherent execution-side gate with a meaningful authoring-side one; the host switch (`AllowHostCodeExecution`) becomes the single control. This is a deliberate reduction in control and must be prominent in the migration document.

### Identity

| Resource | Verbs |
| --- | --- |
| `identity/users` | view, create, update, delete |
| `identity/roles` | view, create, update, delete |
| `identity/applications` | create |

`identity/applications` declares `create` alone because a create endpoint is all that exists.

### Secrets

| Resource | Verbs |
| --- | --- |
| `secrets` | view, write, delete, test★ |

`write` covers rotate and revoke, as `write:secrets` does today. `use:secrets`, `import:secrets` and `export:secrets` are dropped as unused ([research.md](../research.md) D14).

### External Authentication

| Resource | Verbs |
| --- | --- |
| `external-authentication/connections` | view, create, update, archive★, test★, preview★ |
| `external-authentication/descriptors` | view |
| `external-authentication/identity-links` | view, write, delete |
| `external-authentication/sessions` | view, revoke★ |
| `external-authentication/policies` | view, update |
| `external-authentication/policies/default-roles` | update |
| `external-authentication/provider-trust` | override★ |
| `external-authentication/permission-grants` | delegate★, delegate-unrestricted★ |

**Connections have no hard delete.** `DELETE /connections/{connectionId}` maps to the archive permission and is paired with `restore`; enable and disable map to update. The absence of `delete` here is correct, not an omission.

`delegate` and `delegate-unrestricted` govern which Elsa permissions an external claim mapping may confer: `delegate` allows mapping only permissions the actor already holds, `delegate-unrestricted` lifts that restriction. They are a **privilege tier**, not sibling actions — `DefaultPermissionDelegationAuthorizer` computes `mayDelegate = unrestricted || hasDelegate`. That implication stays application logic in the module and is deliberately not modeled; FR-009's absence of verb implication is correct, not a gap. Both remain verbs because "unrestricted" is a mode of one action, not a thing being administered.

`policies/default-roles` is a sub-resource because the default-role list — the roles granted to a user auto-created for an unknown external identity — genuinely is a distinct thing being administered.

### Diagnostics, dashboard, and operations

| Resource | Verbs |
| --- | --- |
| `dashboard` | view |
| `diagnostics/console-logs` | view |
| `diagnostics/structured-logs` | view |
| `diagnostics/opentelemetry` | view |
| `resilience/retries` | view |
| `resilience/strategies` | view |
| `resilience/simulation` | execute |
| `alterations` | view, execute |
| `labels` | view, create, update, delete |

`ingest:diagnostics:opentelemetry` is dropped as declared-but-unused, consistent with D14.

### Platform

| Resource | Verbs |
| --- | --- |
| `tenants` | view, write, delete, refresh★ |
| `system/features` | view |
| `system/shells` | reload★ |
| `ai/chat` | execute |
| `ai/tools` | view |
| `ai/capabilities` | view |

`ai/proposals` and `ai:tools:manage` are not carried forward — they guard no endpoint. The AI module declares them when its endpoints ship ([research.md](../research.md) D14).

## Migration mapping

The source for [`docs/migrations/authorization-model.md`](../../../docs/migrations/authorization-model.md), which is the operator-facing guide and is published alongside this contract. Full legacy strings, so it is checkable mechanically.

**Several mappings expand rather than rename**, because some new sub-resources are granularity increases. A migration must expand, not substitute.

| Legacy permission | New permission(s) |
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

Note that `read:*` and `exec:*` become materially *more* powerful: today they authorize only the twelve and one endpoints that happen to list them, whereas `*:view` and `*:execute` work as their names always implied. Operators should review any role holding them.

## Naming rules

- Resource segments are lowercase, hyphenated, plural where they denote a collection.
- Verbs are lowercase, hyphenated, imperative.
- Depth reflects a distinction endpoints actually make, or a subtree an administrator would plausibly grant as a unit — not the module layout for its own sake.
- A resource is owned by exactly one module, which declares its constant and descriptor together in `Permissions/<Module>Permissions.cs`.
- Permission strings may never contain a comma, because the persistence converter joins collections with commas.

## Module-owner review outcomes

Resolved 2026-08-23.

1. **Workflow-definition upsert** — `POST /workflow-definitions` maps to a single `write` verb. Confirmed; see D17.
2. **External Authentication descriptors** — the six `/descriptors/*` endpoints get their own resource, `external-authentication/descriptors:view`, rather than folding into `connections:view`. One resource rather than six sub-nodes, because one legacy permission governs all six — the same principle that gives `workflows/descriptors/*` nine separate resources, since those were separately permissioned already. A read-only support role can now see which adapters and policies are installed without seeing connection configuration.
3. **`/external-authentication/user-options`** — stays on `external-authentication/identity-links:view`. It is a user *search* endpoint backing the link picker, returning a minimal projection (id and display name) scoped to the tenant, and the linking UI cannot function without it. **Recorded consequence:** holding identity-link rights therefore confers tenant-wide user enumeration in that reduced projection, without `identity/users:view`. Moving it to `identity/users:view` would either over-grant full user read on migration or break linking. Requiring both is the honest answer but needs conjunctive requirements, which the model does not support — see the note below.
4. **`roles:assign` descriptor** — corrected to describe what it actually guards: removing policy references to a role during role deletion. Setting `defaultRoleIds` is guarded by `RoleAuthorizationService.CanAssignRolesAsync`, the ordinary subset rule, so no escalation is possible either way. Whether it *should* additionally require this permission is filed separately as [#7977](https://github.com/elsa-workflows/elsa-core/issues/7977).
5. **`Broker/Logout.cs`** — the two endpoints declare differently. `Logout` is authenticated-only: it reads the external session claim from the principal, so it needs an identity but no permission. `ContinueLogout` is `AllowAnonymous`, matching every other broker callback — the route `handle` carries the authority, and the identity provider redirects the browser there during upstream logout, potentially after the Elsa session is gone. **`ContinueLogout` inheriting the authenticated default today is a probable live bug**, filed as [#7976](https://github.com/elsa-workflows/elsa-core/issues/7976).

### Two model implications from these outcomes

**A third declaration state is required.** `Logout` is deliberately authenticated-without-permission, which FR-019 and the coverage gate cannot currently express — they accept only "a permission" or "anonymous". An explicit authenticated-only marker is needed so the gate distinguishes a deliberate choice from an author's omission.

**Conjunctive requirements are not expressible.** An endpoint declares one resource and one verb, so "needs link rights *and* user read" cannot be stated declaratively — outcome 3 above is the first case to want it, and `ExternalAuthenticationRoleDeletionDependencyContributor` already does it imperatively across three permissions. Not needed for this work; recorded because the next such case should not be solved ad hoc.

# Tenant-agnostic workflows share one row per unscheduled trigger

Date: 2026-11-20

## Status

Accepted. Amends sections 1, 2, 4 and the first alternative of [Trigger and scheduling semantics for tenant-agnostic (`*`) workflows](2026-11-11-trigger-and-scheduling-semantics-for-tenant-agnostic-workflows.md).

## Context

[2026-11-11](2026-11-11-trigger-and-scheduling-semantics-for-tenant-agnostic-workflows.md) gives every tenant its own trigger rows for a `"*"` workflow definition. It rejects a single shared `"*"` row because the default scheduler keys its tasks by `trigger.Id` and does not know which tenant to fire in. That reason applies only to the triggers a scheduler schedules: `Timer`, `Cron` and `StartAt`. HTTP endpoints, events, webhooks and the message triggers of the extension modules are never scheduled. They are looked up when a request or stimulus arrives, in the tenant that receives it.

[2026-11-02](2026-11-02-a-host-may-register-the-asterisk-tenant.md) lets a host register a tenant with ID `"*"` to author shared definitions. On such a host, publishing or importing a `"*"` definition in the `"*"` tenant writes trigger rows stamped `"*"`, and fanning out writes another row for each tenant. A tenant's trigger lookup returns its own rows plus the `"*"` rows (2026-11-11 §4), so it finds two rows for the same trigger:

- `HttpWorkflowsMiddleware` answers 500 ("The call is ambiguous and matches multiple workflows") when more than one trigger matches.
- `StimulusSender` starts one instance per matching row, so the workflow starts twice.

The indexer makes this worse in both directions. `TriggerIndexer` reads the existing rows by `WorkflowDefinitionId` through the tenant's view (its own rows plus `"*"`), and `WorkflowTriggerEqualityComparer` ignores `TenantId`. A visible `"*"` row therefore counts as "unchanged" and no tenant row is written, or a tenant's re-index tries to remove rows that belong to `"*"` (the in-memory store removes them; EF Core removes nothing without a shared-write grant). The outcome depends on the store and on which tenant indexes first.

Full fan-out also scales with tenants times triggers. A host with 624 tenants and about 130 `"*"` triggers, almost all HTTP and event triggers, needs about 80,000 rows instead of about 130, and every publish of a shared definition re-indexes it 624 times.

Since [#9163](https://github.com/elsa-workflows/elsa-core/pull/9163), definition IDs are unique per tenant, so a tenant can have its own definition with the same definition ID as a `"*"` definition. Nothing in core says which of the two triggers apply in that tenant.

## Decision

### 1. Scheduled and unscheduled triggers

A trigger is **scheduled** if a trigger scheduler schedules it by its trigger ID. Every other trigger is **unscheduled**.

The runtime keeps one set of scheduled trigger names. The module that schedules a trigger type adds its name to that set, and the schedulers select the triggers they schedule from the same set. The scheduling module adds `Elsa.Timer`, `Elsa.Cron` and `Elsa.StartAt`. `DefaultTriggerScheduler` and `CreateSchedulesStartupTask` select by the set instead of their own lists, and `DeleteSchedules`, which has no list today, narrows its lookup by the set. Without the scheduling module, the set is empty and every trigger is unscheduled.

The set classifies and selects; it does not route. Each trigger scheduler adds the names it handles, loads only triggers whose name is in the set, and ignores the names it does not handle. Elsa has one trigger scheduler today, `DefaultTriggerScheduler`, which handles `Elsa.Timer`, `Elsa.Cron` and `Elsa.StartAt` by name; `CreateSchedulesStartupTask` and `DeleteSchedules` load the candidate triggers by the set and hand them to the trigger scheduler. `CreateSchedulesStartupTask` also loads `Delay` bookmarks today; `Delay` is a bookmark, not a trigger, so it stays in the bookmark filter and is not part of the set. Quartz and Hangfire run the jobs that the trigger scheduler creates and are not trigger schedulers themselves.

Because selecting and classifying use the same set, a new trigger type cannot end up on the wrong side. A type that no scheduler schedules is unscheduled, which is correct. A scheduler that handles a name it did not add to the set never loads those triggers, so the gap shows as triggers that are not scheduled, not as a silently shared trigger. A host or module with its own `ITriggerScheduler` must add the names of the triggers it schedules.

### 2. Rows of a `"*"` definition

- **Unscheduled triggers: one row, stamped `"*"`.** The indexer sets `TenantId = "*"` explicitly, whatever the ambient tenant is, and writes the row inside a `SharedTenantWriteScope` for `StoredTrigger`. On publish, the host enters `SharedTenantWriteScope` for both `WorkflowDefinition` and `StoredTrigger`, and the indexer does not enter it on that path ([2026-11-02](2026-11-02-a-host-may-register-the-asterisk-tenant.md): a `"*"` context alone grants no shared-write authority). Elsa enters the `StoredTrigger` scope itself only in the provider import, as `DefaultWorkflowDefinitionStorePopulator` already does, and in tenant activation (section 4), where it re-derives rows from `"*"` definitions that are already persisted. Both write only rows re-derived from `"*"` definitions that are already persisted, never content from a request, so neither adds a request-driven shared-write path, even when an admin API call triggers the activation. No tenant gets a copy. Every tenant sees the row through the visibility filter, and the instance it starts belongs to the invoking tenant (2026-11-11 §3, unchanged).
- **Scheduled triggers: one row per active tenant**, stamped with that tenant, as 2026-11-11 §1 and §6 decide. A registered `"*"` tenant does not get one: its rows would fire in a `"*"` context, and Elsa does not persist `"*"` instances ([2026-11-02](2026-11-02-a-host-may-register-the-asterisk-tenant.md)). A cron that fires once for all tenants is the global cron that 2026-11-11 leaves to a later ADR.

Definitions that a tenant owns are unchanged: all their triggers are stamped with that tenant.

### 3. Indexing reconciles each set against its own rows

When the indexer replaces the triggers of a `"*"` definition, it reads the rows it compares against by tenant, not through the ambient view:

- the unscheduled rows against the definition's existing `"*"` rows only;
- each tenant's scheduled rows against that tenant's rows of the definition only.

A row of another tenant, or a `"*"` row, never counts as the current row of a tenant, and a tenant's re-index never removes it. This applies to every store. Today the Dapper trigger store cannot delete `"*"` rows and ignores `TriggerFilter.TenantAgnostic`, so the implementation must fix both.

### 4. Lifecycle

This replaces the bullets of 2026-11-11 §2 for `"*"` definitions:

- **Publish, republish, provider import.** The unscheduled rows are replaced once, in whichever tenant the publish or import runs. The scheduled rows are reconciled for every active tenant except a registered `"*"` tenant and the tenants that override the definition (section 5), each in that tenant's context, so that `ScheduleWorkflows` schedules and unschedules there. Provider import runs on every tenant's activation, so the shared step repeats; with nothing changed, its diff is empty and it writes nothing.
- **Tenant activation.** First, the shared rows of every published `"*"` definition are reconciled, as the provider import already does for provider definitions; with nothing changed, this writes nothing. Then the tenant's scheduled rows of every `"*"` definition are reconciled. Before the tenant's schedules start (`CreateSchedulesStartupTask` runs after this), activation removes the tenant's scheduled rows whose `"*"` definition is no longer published, so a definition that was unpublished, retracted or deleted while the tenant was inactive does not fire again. The tenant's own copies of unscheduled triggers of `"*"` definitions are removed only after the shared rows exist, so a trigger is never missing (section 7). Activation writes the shared rows inside a `SharedTenantWriteScope` for `StoredTrigger`, as the provider import does (section 2).
- **Activation of a registered `"*"` tenant.** It reconciles the shared rows like any activation. It has no scheduled rows of `"*"` definitions (section 2): rows that exist from before the upgrade are removed and their jobs unscheduled.
- **Unpublish, retract, delete.** The `"*"` rows are removed once, and the scheduled rows are removed and unscheduled for every active tenant.
- **Tenant deactivation and deletion.** Unchanged: the tenant's schedules are stopped. The `"*"` rows stay.

### 5. A tenant's own definition wins over the `"*"` definition with the same ID

A tenant **overrides** a `"*"` definition while it has a published version of its own definition with the same definition ID. While it does:

- The trigger store's lookups in that tenant do not return the `"*"` rows of that definition ID. The store decides this itself, so every lookup is covered: HTTP, stimuli, and the trigger list endpoints. A decorator in the host is not needed.
- The fan-out of section 4 skips the tenant and removes the tenant's scheduled rows of the `"*"` definition. The tenant's own definition indexes its own triggers, as for any definition the tenant owns.

When the tenant retracts or deletes its last published version, the override ends: the `"*"` rows are visible to the tenant again, and its scheduled rows of the `"*"` definition are reconciled.

A draft alone does not override. Until the tenant publishes its own version, the shared definition keeps running for it.

### 6. HTTP and stimuli

This replaces the third sentence of 2026-11-11 §4. A tenant's lookup returns at most one row per trigger of a `"*"` definition: the shared `"*"` row of an unscheduled trigger, or, if the tenant overrides the definition, its own row. This holds with and without a registered `"*"` tenant. HTTP finds one workflow, and a stimulus starts one instance in the tenant that sent it.

### 7. Upgrading

Today, each tenant's activation writes tenant-stamped rows for the triggers of `"*"` definitions. After the upgrade:

- the first activation writes the shared rows of every published `"*"` definition, whether it comes from a provider or was authored in the database, and each tenant's first activation then removes its own rows of unscheduled triggers of `"*"` definitions (section 4). A copy is identified by its `WorkflowDefinitionVersionId`, which names a version of the `"*"` definition. The rows of a tenant's own definition with the same definition ID (section 5) name that definition's versions, so the cleanup keeps them;
- the first activation of a registered `"*"` tenant removes its scheduled rows of `"*"` definitions and unschedules their jobs;
- until a tenant's first activation after the upgrade, that tenant can see both its old row and the new shared row for the same trigger. HTTP then answers 500 for that trigger, and a stimulus starts two instances, one per row. Hosts that cannot accept this window delete the tenant-stamped rows of unscheduled triggers of `"*"` definitions before they start the new version; the first activation then writes the shared rows, and until it has, those triggers are not found rather than matched twice.

The migration note in `core/docs/migrations` describes these steps, the cleanup query, and the new rule for custom trigger schedulers (section 1).

## Consequences

### Positive

- One row per unscheduled trigger of a `"*"` definition, instead of one per tenant. Publishing a shared definition writes those rows once.
- On a host that registers `"*"`, HTTP and stimuli find exactly one trigger again.
- Timers, cron and `StartAt` keep the per-tenant semantics of 2026-11-11, including the tenant transport of [#9252](https://github.com/elsa-workflows/elsa-core/issues/9252).
- Which side a trigger type is on follows from whether it is scheduled. Nobody maintains a second list.
- Tenant overrides are defined in core and covered by every trigger lookup.

### Negative

- The indexer handles two kinds of row for `"*"` definitions, and each store must support explicit `"*"` writes and deletes and the tenant-agnostic filter, including Dapper.
- A trigger lookup must check for overriding definitions. The store can cache that check per tenant and definition ID, but it is an extra query on a cache miss.
- A custom `ITriggerScheduler` that schedules trigger types the scheduling module does not know must add their names to the set. Otherwise those triggers are classified as unscheduled, stored as one shared row, and never loaded by the scheduler, so they do not run.

### Neutral

- Bookmarks are unaffected: they belong to the instance's tenant (2026-11-11 §5).

## Alternatives considered

- **Full fan-out for every trigger (2026-11-11 as written).** Rejected for unscheduled triggers: it double-matches on a host that registers `"*"`, and its row count grows with tenants times triggers.
- **Remove the `"*"` row instead of fanning out less**, by never indexing in the `"*"` tenant. Rejected: the definitions authored in the `"*"` tenant would then have no row until each tenant re-indexes them, and the row count problem remains.
- **Classify trigger types by an attribute on the activity.** Rejected: a scheduled type whose attribute is missing would silently be shared. With the scheduler's own set, a type that is scheduled is classified as scheduled.
- **Hide overridden `"*"` triggers in a host decorator only.** Rejected: core would still double-match an overridden definition, and every host would have to reimplement the decorator.

## Related decisions

- [Trigger and scheduling semantics for tenant-agnostic (`*`) workflows](2026-11-11-trigger-and-scheduling-semantics-for-tenant-agnostic-workflows.md), amended by this ADR.
- [A host may register "*" as the tenant that authors tenant-agnostic entities](2026-11-02-a-host-may-register-the-asterisk-tenant.md)
- [0009-asterisk-sentinel-value-for-tenant-agnostic-entities](0009-asterisk-sentinel-value-for-tenant-agnostic-entities.md)
- Issue [#9254](https://github.com/elsa-workflows/elsa-core/issues/9254)

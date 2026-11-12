# Trigger and scheduling semantics for tenant-agnostic (`*`) workflows

Date: 2026-11-11

## Status

Proposed

## Context

ADR 0009 established `"*"` (`Tenant.AgnosticTenantId`) as the sentinel value for tenant-agnostic entities. Such entities are visible to every tenant but are owned by no specific tenant. The follow-up ADR [2026-11-02-a-host-may-register-the-asterisk-tenant](2026-11-02-a-host-may-register-the-asterisk-tenant.md) (PR #9171) clarified that a host *may* register a tenant whose ID is `"*"`, and that interrupted-workflow recovery for `"*"` instances uses that registered tenant when it exists, falling back to the default tenant otherwise.

What remained undecided was the runtime semantics of triggers, bookmarks, messages and scheduling for workflows whose `TenantId` is `"*"`. Specifically:

- Should a `"*"` workflow have one shared trigger row or one per tenant?
- When a tenant invokes a `"*"` trigger, what `TenantId` does the new instance carry?
- Do timers and cron fire once globally or once per tenant?
- Can a stimulus in tenant A resume an instance that belongs to tenant B?
- How do the default, Quartz and Hangfire schedulers participate?

This ADR records the decisions approved by the maintainers for Elsa 3.12.

## Decision

A `"*"` **workflow definition is shared**; its **triggers are tenant-scoped**; the **instances created from those triggers belong to the invoking tenant**; and **timers/cron run per tenant**. Recovery of `"*"` instances follows PR #9171.

### 1. Definitions, triggers and fan-out

A `WorkflowDefinition` whose `TenantId == "*"` is a shared definition. It is stored once and visible to every tenant through the tenant visibility filters (`TenantId == currentTenant OR TenantId == "*"`).

When a tenant activates, or when a `"*"` definition is published or republished, triggers are indexed **inside the ambient tenant context**. The `TriggerIndexer` creates `StoredTrigger` rows without an explicit `TenantId`; the trigger store then stamps each row with the current ambient tenant. The result is a fan-out: tenant A has its own trigger rows, tenant B has its own, and so on. The unique index `(WorkflowDefinitionId, Hash, ActivityId, TenantId)` prevents duplicate trigger rows within a single tenant.

This design is the only one that works correctly with the default in-memory scheduler, because that scheduler keys scheduled tasks by `trigger.Id`/`bookmark.Id` and does not capture or restore the ambient tenant context (see [Known limitations](#known-limitations) below).

### 2. Trigger lifecycle

- **Tenant activation.** `PopulateRegistriesStartupTask` runs in the tenant scope. `DefaultWorkflowDefinitionStorePopulator` imports `"*"` definitions (via `TenantAgnosticWorkflowsCache`) and calls `TriggerIndexer.IndexTriggersAsync`, which creates trigger rows stamped with the activating tenant.
- **Publish / republish.** When a `"*"` definition is published, `ScheduleWorkflows` handles `WorkflowTriggersIndexed` and schedules the new triggers in the current tenant context. Re-publishing a `"*"` definition while tenant A is active updates A's trigger rows; other tenants are unaffected until they next index the definition.
- **Tenant deactivation / deletion.** `UpdateTenantSchedules` handles `ITenantDeletedEvent` and unschedules (stops) the timer/cron schedules and bookmark schedules that belong to the deleted tenant. It does not delete persistent `StoredTrigger` rows; the host's tenant-cleanup strategy is responsible for removing those rows.

### 3. Instance ownership

When a tenant invokes a `"*"` trigger, the resulting workflow instance belongs to **that tenant**, not to `"*"`. The instance is created by `WorkflowInstanceFactory` without a `TenantId`; the store's `ApplyTenantId` handler (or `MemoryWorkflowInstanceStore.ApplyCurrentTenant`) stamps it with the ambient tenant before persistence.

This rule applies to:

- HTTP requests resolved by `HttpWorkflowsMiddleware`.
- Stimuli sent through `IStimulusSender`.
- Timers and cron fired by any scheduler for a tenant-scoped trigger.

The only way to obtain a `"*"` workflow instance is to create it explicitly under a `"*"` ambient tenant context (for example, via `IWorkflowRuntime` with `TenantAccessor.PushContext("*")`). Such instances are governed by the recovery rules in PR #9171.

### 4. HTTP and stimuli / messages

An HTTP request handled by `HttpWorkflowsMiddleware` resolves the tenant first (header, host or route prefix), then looks up triggers by hash. The trigger store returns triggers whose `TenantId` equals the request tenant plus any `"*"` triggers. Because `"*"` triggers are fan-out rows stamped with the request tenant, the lookup naturally returns the tenant's own trigger row. The middleware starts the workflow, and the instance is saved with the request tenant.

`IStimulusSender` follows the same pattern: it matches the stimulus hash against triggers visible to the ambient tenant. A stimulus in tenant A activates A's trigger row and creates an instance owned by A. It does **not** activate another tenant's trigger row, nor does it create an instance owned by another tenant.

### 5. Bookmarks

Bookmarks are created during workflow execution and must inherit the `TenantId` of the **persistent workflow instance** that owns them. The authoritative value is `WorkflowInstance.TenantId` (`Elsa.Workflows.Management.Entities.WorkflowInstance : Entity`); it is **not** `Workflow.Identity.TenantId`, because the definition may be `"*"` while the instance belongs to a concrete tenant.

Consequences of this rule:

- An instance created from a `"*"` trigger by tenant A has `WorkflowInstance.TenantId == "A"`; its bookmarks must also have `TenantId == "A"`. Tenant B cannot find or resume them.
- An instance explicitly created under the `"*"` ambient tenant has `WorkflowInstance.TenantId == "*"`; its bookmarks are also `"*"` and visible to every tenant.
- A tenant-specific instance has tenant-specific bookmarks.

The visibility rules are:

- A bookmark whose `TenantId` equals the ambient tenant is visible.
- A bookmark whose `TenantId` is `"*"` is visible to every tenant.
- A bookmark with a specific tenant is not visible to other tenants.

When a bookmark resumes an instance, the instance must also be visible to the caller. The `WorkflowInstanceStore` query filter prevents tenant A from loading an instance owned by tenant B. If both the bookmark and the instance are visible, resumption proceeds under the caller's tenant context.

**Current implementation gap.** `MapBookmark` (`BookmarkExtensions`) does not set `StoredBookmark.TenantId`. `WorkflowExecutionContext` is reconstructed from `WorkflowState`, which does not carry the persistent instance's `TenantId` (`LocalWorkflowClient.RunInstanceAsync` loads the `WorkflowInstance` and passes `workflowInstance.WorkflowState` to `WorkflowRunner`). As a result:

- With EF Core, `ApplyTenantId` stamps a null `TenantId` with the ambient tenant, so bookmarks inherit the caller's tenant instead of the instance's owner.
- With the in-memory store, the bookmark remains with a null `TenantId`; only the default tenant can see it (`WhereVisibleToTenant` treats `null` as visible only to the default tenant).

**Required change.** `MapBookmark` must be supplied with the persistent instance's `TenantId` (from `WorkflowInstance.TenantId`) and copy it into `StoredBookmark.TenantId`. This ensures that bookmarks of a tenant-A instance stay owned by A, even when the workflow definition is `"*"`.

### 6. Timers and cron

Timers and cron are **per tenant**.

- Each tenant has its own trigger rows for timer/cron activities inside `"*"` workflows.
- When a tenant activates, `CreateSchedulesStartupTask` schedules that tenant's timer/cron triggers through the configured `ITriggerScheduler`.
- `UpdateTenantSchedules` unschedules a tenant's timer/cron triggers when the tenant is deleted.
- The instance created when a timer fires belongs to the tenant whose trigger fired (see [Known limitations](#known-limitations) for the current default-scheduler exception).

Global scheduling (a single cron that fires once for all tenants) is **not** part of this design. It is noted only as future work under [Alternatives considered](#alternatives-considered).

### 7. Schedulers

| Scheduler | Role for `"*"` triggers |
|---|---|
| **Default (`LocalScheduler`)** | Tenant-scoped trigger rows are scheduled under the tenant's startup context. Because the scheduler is a singleton and `RunWorkflowTask` does not capture the ambient tenant, timer/cron tasks currently execute under the default tenant context. This is a known bug; see [Known limitations](#known-limitations). |
| **Quartz** | Captures the ambient tenant at scheduling time (`tenantAccessor.Tenant?.Id`) and restores it in `TenantAwareJobFactory`. Tenant-scoped trigger rows are grouped by tenant, so each fires in the correct tenant. |
| **Hangfire** | Captures the ambient tenant and passes it as a job argument; `RunWorkflowJob` restores it with `tenantAccessor.PushContext`. Tenant-scoped trigger rows are scheduled per tenant. |

### 8. Recovery

Recovery of `"*"` workflow instances follows the rules introduced by PR #9171 and recorded in [2026-11-02-a-host-may-register-the-asterisk-tenant](2026-11-02-a-host-may-register-the-asterisk-tenant.md):

- If the host registers a `"*"` tenant, recovery restarts the `"*"` instance under that tenant.
- If no `"*"` tenant is registered, recovery falls back to the default tenant (`""`).
- `TenantKey.From` continues to be used only for deduplication in the recovery scan, never for selecting the ambient execution context.

## Consequences

### Positive

- **Clear ownership.** Every workflow instance has a well-defined owning tenant, even when it was started from a shared definition.
- **Tenant isolation.** A tenant cannot create or resume instances whose `TenantId` belongs to another tenant simply because the definition is shared. Instances explicitly created with `TenantId = "*"` remain visible and resumable by all tenants.
- **Scheduler compatibility.** Fan-out of trigger rows allows the default scheduler, Quartz and Hangfire to coexist without inventing a second sentinel or special-casing `"*"` in every scheduler.
- **Predictable lifecycle.** Trigger rows appear when a tenant activates or a `"*"` definition is published, and disappear when the tenant is deleted.
- **Aligned with recovery.** Because instances created from `"*"` triggers belong to the invoking tenant, the recovery rules from PR #9171 apply only to instances that were explicitly created under `"*"`.

### Negative

- **Multiple trigger rows.** A `"*"` workflow with N tenants produces N trigger rows per startable activity. This is the cost of keeping the default scheduler correct.
- **Re-indexing on tenant add.** A new tenant does not get triggers for existing `"*"` workflows until it activates (or a republish occurs while that tenant is active).
- **No global cron.** Operators cannot yet declare a single cron that fires once for all tenants. This must be revisited in a later ADR if needed.

### Neutral

- `"*"` remains the only sentinel for tenant-agnostic entities. No second sentinel is introduced.

## Known limitations

The default `LocalScheduler` does not capture and restore the ambient tenant context when a scheduled task fires. `ScheduledCronTask` creates a new DI scope from the root `IServiceScopeFactory`, and `RunWorkflowTask` executes `CreateAndRunInstanceAsync` without pushing the tenant that scheduled the task. This means timer/cron triggers scheduled for a named tenant may execute under the default tenant context; the resulting workflow instance is therefore stamped with the default tenant rather than the tenant that owned the trigger.

This is an independent bug in the default scheduler, not a desired semantic of `"*"` workflows. It is tracked separately and should be fixed so that scheduled tasks run under the tenant context that scheduled them.

## Alternatives considered

- **Single shared `"*"` trigger row.** Rejected because the default scheduler keys tasks by `trigger.Id`; a single row would collide when multiple tenants try to schedule it, and the scheduler would not know which tenant context to use when firing.
- **Global timers/cron.** Rejected from this ADR's scope. If needed later, a separate ADR must answer: which tenant owns the resulting instance? Which schedulers can support exactly-once execution across the cluster? How does the feature interact with tenant activation and deactivation?
- **Instances created from `"*"` triggers also stamped `"*"`.** Rejected because it would make every shared workflow instance visible to every tenant, breaking isolation and making it impossible to attribute data and execution history to a specific tenant.

## Related decisions

- [0009-asterisk-sentinel-value-for-tenant-agnostic-entities](0009-asterisk-sentinel-value-for-tenant-agnostic-entities.md)
- [2026-11-02-a-host-may-register-the-asterisk-tenant](2026-11-02-a-host-may-register-the-asterisk-tenant.md)
- PR #9171 — recover `"*"` workflow instances under a registered `"*"` tenant.

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

Fan-out of trigger rows gives every tenant its own independent triggers and schedules. That is a separate concern from the tenant context used when a scheduled task eventually executes: the task must explicitly transport the `TenantId` of the scheduling context and restore it, otherwise the new workflow instance may be stamped with the wrong tenant. See [Known limitations and open work](#known-limitations-and-open-work) below.

### 2. Trigger lifecycle

- **Tenant activation.** `PopulateRegistriesStartupTask` runs in the tenant scope. `DefaultWorkflowDefinitionStorePopulator` imports `"*"` definitions (via `TenantAgnosticWorkflowsCache`) and calls `TriggerIndexer.IndexTriggersAsync`, which creates trigger rows stamped with the activating tenant.
- **Publish / republish.** When a `"*"` definition is published or republished, the host must reindex its triggers for **every active tenant**. The tenant registry (`ITenantService` / `DefaultTenantService`) enumerates active tenants; for each tenant, the system pushes that tenant context and invokes `TriggerIndexer.IndexTriggersAsync`. This ensures that added, removed or reconfigured start activities are reflected in every tenant's trigger rows and schedules. `ScheduleWorkflows` then unschedules removed triggers and schedules added ones in each tenant context. Without this fan-out, obsolete triggers and their schedules would remain after activities are deleted or reconfigured.
- **Unpublish.** When a `"*"` definition is unpublished, the host must delete all `StoredTrigger` rows for that definition across **every active tenant** and unschedule (stop) all associated timer/cron and bookmark schedules. This cleanup must cover both persistent store records and active scheduler entries; no timer or cron may continue to fire for a shared definition after it is retired.
- **Delete.** Deleting a `"*"` definition applies the same cleanup as unpublishing: all `StoredTrigger` rows for the definition are removed for every active tenant, and all active schedules for those triggers are stopped.
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

**Required change.** The persistent instance's `TenantId` (from `WorkflowInstance.TenantId`) must be preserved in the execution context when the instance is loaded or restored, so that `MapBookmark` can stamp `StoredBookmark.TenantId` with the owner tenant. This is preferable to adding an extra parameter to `MapBookmark` because it keeps bookmark creation aligned with the context's view of the instance. Once the execution context carries the owner tenant, `MapBookmark` can set `StoredBookmark.TenantId` from that value. This ensures that bookmarks of a tenant-A instance stay owned by A, even when the workflow definition is `"*"`.

### 6. Timers and cron

Timers and cron are **per tenant**.

- Each tenant has its own trigger rows for timer/cron activities inside `"*"` workflows.
- When a tenant activates, `CreateSchedulesStartupTask` schedules that tenant's timer/cron triggers through the configured `ITriggerScheduler`.
- `UpdateTenantSchedules` unschedules a tenant's timer/cron triggers when the tenant is deleted.
- The instance created when a timer fires belongs to the tenant whose trigger fired (see [Known limitations and open work](#known-limitations-and-open-work) for the current default-scheduler behavior).

Global scheduling (a single cron that fires once for all tenants) is **not** part of this design. It is noted only as future work under [Alternatives considered](#alternatives-considered).

### 7. Schedulers

| Scheduler | Role for `"*"` triggers |
|---|---|
| **Default (`LocalScheduler`)** | Tenant-scoped trigger rows are scheduled under the tenant's startup context. The scheduler stores `ITask` instances in memory and fires them from a new root DI scope. Whether the ambient tenant at schedule time is preserved depends on whether the scheduling request carries it and the task restores it. The approved design is to transport the tenant explicitly (see [Known limitations and open work](#known-limitations-and-open-work)). |
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
- **Predictable lifecycle.** Trigger rows appear when a tenant activates or a `"*"` definition is published, and disappear when the tenant is deleted, when a `"*"` definition is unpublished, or when a `"*"` definition is deleted.
- **Clean retirement.** Unpublishing or deleting a shared (`"*"`) workflow definition removes its triggers and schedules from every active tenant, so no timer or cron continues to fire after the definition is retired.
- **Aligned with recovery.** Because instances created from `"*"` triggers belong to the invoking tenant, the recovery rules from PR #9171 apply only to instances that were explicitly created under `"*"`.

### Negative

- **Multiple trigger rows.** A `"*"` workflow with N tenants produces N trigger rows per startable activity. This is the cost of maintaining independent trigger and schedule ownership for each tenant.
- **Re-indexing on tenant add.** A new tenant does not get triggers for existing `"*"` workflows until it activates (or a republish fans out to all active tenants).
- **Republish fan-out.** Publishing or republishing a `"*"` definition requires reindexing triggers for every active tenant. This is more work than a single-tenant definition but is necessary to keep each tenant's trigger rows and schedules consistent with the current definition.
- **No global cron.** Operators cannot yet declare a single cron that fires once for all tenants. This must be revisited in a later ADR if needed.

### Neutral

- `"*"` remains the only sentinel for tenant-agnostic entities. No second sentinel is introduced.

## Known limitations and open work

### Default scheduler tenant transport

The default scheduler relies on `System.Timers.Timer` callbacks that create a new DI scope via `IServiceScopeFactory` (`ScheduledCronTask`, `ScheduledRecurringTask`, `ScheduledSpecificInstantTask`). The scheduled task (`RunWorkflowTask` or `ResumeWorkflowTask`) is created at schedule time and stored as a field; it is executed in the new scope without any explicit tenant restoration today.

Whether the tenant context is lost in practice depends on whether `DefaultTenantAccessor`'s `AsyncLocal` happens to flow across the timer callback. Because the callback runs on a thread-pool thread after the scheduling scope has ended, production code should not rely on ambient flow. The approved design for Elsa 3.12 is:

1. Add a `TenantId` property to `ScheduleNewWorkflowInstanceRequest` and `ScheduleExistingWorkflowInstanceRequest`.
2. Capture the ambient tenant **from the scheduling context** in `DefaultTriggerScheduler` and `DefaultBookmarkScheduler`. The captured value must be the actual tenant under which the schedule is created:
   - A `"*"` workflow definition indexed and scheduled within tenant A must transport `"A"`.
   - An execution explicitly scheduled under the `"*"` ambient tenant context must transport `"*"`.

   The value must not be inferred from `WorkflowDefinition.TenantId` and must not be replaced by the default tenant.
3. Restore the captured tenant in `RunWorkflowTask` and `ResumeWorkflowTask` with `tenantAccessor.PushContext(tenantId)` before invoking the workflow runtime, so the tenant is preserved through the deferred execution.

This makes the default scheduler deterministic and aligned with Quartz/Hangfire.

Tracked by [#9252](https://github.com/elsa-workflows/elsa-core/issues/9252).

**Tests needed before claiming the behavior is fixed:**

- A timer/cron trigger scheduled for tenant A (or under `"*"`) fires and creates an instance whose `WorkflowInstance.TenantId` is that same scheduling tenant, not the default tenant.
- A bookmark scheduled for tenant A (or under `"*"`) resumes the instance under that same scheduling tenant.
- The same trigger definitions under different tenants produce independent schedules that do not collide.

### Bookmark instance-tenancy propagation

Bookmarks created during execution do not yet inherit `WorkflowInstance.TenantId`. The approved fix is to preserve the persistent instance's tenant in the execution context when the instance is loaded/restored, so that `MapBookmark` can stamp `StoredBookmark.TenantId` correctly. See [Bookmarks](#5-bookmarks).

Tracked by [#9253](https://github.com/elsa-workflows/elsa-core/issues/9253).

### Shared-definition republish fan-out

Publishing or republishing a `"*"` definition must reindex triggers for every active tenant and unschedule any triggers that have become obsolete (removed activities, changed hashes, or removed startable activities). The fan-out ensures that no tenant keeps stale trigger rows or schedules after the shared definition changes.

Tracked by [#9254](https://github.com/elsa-workflows/elsa-core/issues/9254).

## Alternatives considered

- **Single shared `"*"` trigger row.** Rejected because the default scheduler keys tasks by `trigger.Id`; a single row would collide when multiple tenants try to schedule it, and the scheduler would not know which tenant context to use when firing.
- **Global timers/cron.** Rejected from this ADR's scope. If needed later, a separate ADR must answer: which tenant owns the resulting instance? Which schedulers can support exactly-once execution across the cluster? How does the feature interact with tenant activation and deactivation?
- **Instances created from `"*"` triggers also stamped `"*"`.** Rejected because it would make every shared workflow instance visible to every tenant, breaking isolation and making it impossible to attribute data and execution history to a specific tenant.

## Related decisions

- [0009-asterisk-sentinel-value-for-tenant-agnostic-entities](0009-asterisk-sentinel-value-for-tenant-agnostic-entities.md)
- [2026-11-02-a-host-may-register-the-asterisk-tenant](2026-11-02-a-host-may-register-the-asterisk-tenant.md)
- PR #9171 — recover `"*"` workflow instances under a registered `"*"` tenant.

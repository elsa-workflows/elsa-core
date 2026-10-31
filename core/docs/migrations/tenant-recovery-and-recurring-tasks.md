# Interrupted-workflow recovery runs once for all tenants; recurring tasks share one timer per task type

**Behavior change.** Interrupted-workflow recovery runs once for all tenants instead of once per tenant, and recurring tasks are scheduled from one timer per task type instead of one timer per tenant.

## Interrupted-workflow recovery

- Recovery runs once after all tenants are activated. `ITenantService.ActivateTenantsAsync` raises the new `IAllTenantsActivatedEvent` and the scan uses one tenant-agnostic query.
- A tenant with any non-blank value under `ConnectionStrings` in `Tenant.Configuration`, including the default tenant, is assumed to have its own database. It is scanned separately in its own tenant scope, at startup and in each recurring cycle. Any connection string triggers this, also non-database ones such as RabbitMq or Redis, at the cost of one extra harmless query. An override equal to the default database causes no double restart. Hosts that switch databases per tenant some other way (Dapper, MongoDB) are not detected.
- Tenants activated later by `RefreshAsync` (added or re-enabled) are not scanned until the next host or shell activation.
- `RestartInterruptedWorkflowsTask` is now `[RunOnceForAllTenants]`: it runs once per cycle under the default tenant, or the lowest tenant id if there is no default tenant.

## Recurring tasks

- One timer per task type runs every active tenant. Runs per task type are bounded by `RecurringTaskOptions.MaxDegreeOfParallelism` (default 4).
- The interval is now measured between run starts, not from the end of the previous run. A run longer than the interval skips ticks and never overlaps itself for the same tenant.
- The new `[RunOnceForAllTenants]` attribute makes a task run once per cycle instead of once per tenant.
- `ProcessWorkflowDispatchOutboxRecurringTask` no longer takes a task-level single-node lock. It calls `IWorkflowDispatchOutboxProcessor.TryProcessAsync`, so a tick skips a tenant whose processor lock is held instead of waiting for it.

## What to do

- A custom `ITenantService` must call `TenantEventsManager.AllTenantsActivatedAsync` after activating tenants. Otherwise no recovery scan runs.
- A custom `IWorkflowInstanceStore` must honor `WorkflowInstanceFilter.TenantAgnostic` on reads by ignoring the tenant filter. Deletes stay tenant-scoped. Otherwise `RestartInterruptedWorkflowsTask` only sees one tenant's instances.
- A custom `IWorkflowDispatchOutboxProcessor` must lock itself (see the remark on the interface). Otherwise items are dispatched more than once in a cluster.
- A custom `TenantEventsManager` construction that uses the old constructor still compiles but is obsolete and raises no `IAllTenantsActivatedEvent` handlers; use the constructor that accepts them.

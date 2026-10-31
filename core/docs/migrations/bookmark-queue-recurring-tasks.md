# Bookmark queue recurring tasks run once for all tenants

**Behavior change.** The bookmark queue trigger and purge recurring tasks run once per cycle for all tenants instead of once per tenant.

## Trigger

- `TriggerBookmarkQueueRecurringTask` is now `[RunOnceForAllTenants]`.
- It signals only the workers of tenants that have bookmark queue items, using `IBookmarkQueueStore.ListTenantIdsAsync`. When the store returns null, it signals every active tenant.
- Every tenant's worker still starts.
- The task runs on every node, because each node signals its own workers.
- Items with a null, empty or `*` tenant id wake the default tenant's worker.
- The trigger assumes all tenants share one database. Tenants with their own databases are not covered.

## Purge

- `PurgeBookmarkQueueRecurringTask` is now `[RunOnceForAllTenants]`.
- It reads expired queue items, then expired dead letters, of all tenants in pages of `BatchSize` using tenant-agnostic queries, then dead-letters and deletes each tenant's items under that tenant.
- Items with a null, empty or `*` tenant id are handled under the default tenant.
- Items of tenants that are not active on this host are purged under a synthetic tenant with the same id. Their TTL has already elapsed, so they are dead-lettered or deleted like any other tenant's items.
- The purge assumes all tenants share one database. Tenants with their own databases are not covered.

## Fix

- `BookmarkQueueFilter.CreatedAtLessThan` was ignored by the Dapper bookmark queue store, so the purger treated every item as expired and dead-lettered fresh items. It is now applied.

## What to do

- A custom `IBookmarkQueueStore` should override `ListTenantIdsAsync`; the default returns null, and the trigger then wakes every tenant.
- A custom `IBookmarkQueueStore` must honor `BookmarkQueueFilter.TenantAgnostic` on `PageAsync` and `FindManyAsync` by ignoring the tenant filter. Deletes stay tenant-scoped. Otherwise the purger only sees one tenant's items.
- A custom `IBookmarkQueueDeadLetterStore` must honor `BookmarkQueueDeadLetterFilter.TenantAgnostic` on `PageAsync`.
- A custom `IBookmarkQueuePurger` is now called once per cycle, under one tenant, and must purge all tenants itself.
- Subclasses of, or factory registrations for, `TriggerBookmarkQueueRecurringTask` or `DefaultBookmarkQueuePurger` that use the `[Obsolete]` constructors only cover the executing tenant. `[RunOnceForAllTenants]` is inherited, so such a task runs once per cycle for one tenant only. Switch to the new constructors.
- Tenants with their own database are no longer purged or periodically triggered.

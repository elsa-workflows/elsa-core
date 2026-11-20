# Interrupted workflows are recovered each cycle, not only at startup

**Behavior change.** `RestartInterruptedWorkflowsTask` now also restarts `Running` instances in the `Interrupted` sub-status that were not updated within `RuntimeOptions.InactivityThreshold`.

## Why

A graceful drain marks the instances it force-cancels as `Running` / `Interrupted`. Before, only the startup scan (`RecoverInterruptedWorkflowsStartupTask`) restarted them. In a slot swap or a rolling deploy the new node starts and runs its startup scan before the old node drains, so the instances the old node interrupted stayed `Interrupted` until the next host or shell activation.

## What changes

- Each cycle first restarts stale executing instances, as before, and then stale `Interrupted` instances, with the same tenant handling, including tenants with their own database. An instance found by both queries is restarted once.
- Right before an `Interrupted` instance is restarted, its `UpdatedAt` is set to the time of that restart. The next cycle skips it while the restart is in flight. If the restarted run never updates the instance, it is restarted again once the inactivity threshold has passed.
- The cutoff uses `UpdatedAt`, which a drain does not change: it is the instance's last commit or heartbeat. With the defaults (5 minutes threshold, 5 minutes interval), an instance interrupted by another node's drain is restarted within about 10 minutes.
- The startup scan (`InterruptedRecoveryScanner`) stamps the instances it restarts the same way, so a recurring cycle on another node, or right after the scan, skips them while their restart is in flight. It finds the same instances as before.

## Fix

- On EF Core, `UpdateUpdatedTimestampAsync` (used by this stamp and by the workflow heartbeat) wrote the ambient tenant over the instance's `TenantId`, for example `"*"` or null became `""` under the default tenant. It now writes only `UpdatedAt`, and only on a row the ambient tenant may write: its own rows, and `"*"` rows only with shared-write authority (`SharedTenantWriteScope`, see [Tenant-owned persistence](tenant-owned-persistence.md)). The heartbeat and the recovery stamp run under the instance's tenant, so they stamp the instances of named tenants and of the default tenant. A legacy `"*"` instance from before 3.11 is not stamped without the shared-write grant, so each recovery scan finds it interrupted again. It stays interrupted until an operator re-stamps it and its runtime records to a tenant. Interrupted-workflow recovery then resumes it in that tenant, where it can be cancelled with the normal API (see [Tenant-owned persistence](tenant-owned-persistence.md)). Before this fix, with multitenancy enabled, every call threw "Writing a shared record requires trusted host authority." on 3.11.
- On Dapper, `UpdateUpdatedTimestampAsync` threw "A record with ID '...' already exists in another tenant" for a `"*"` instance, so the stamp failed and the instance was not restarted. It now writes `UpdatedAt` on `"*"` rows too and keeps their `TenantId`.

## What to do

- Nothing for most hosts.
- A custom `IWorkflowInstanceStore` must set `UpdatedAt` in `UpdateUpdatedTimestampAsync` under the instance's tenant, as the workflow heartbeat already requires. If it throws, the instance is not restarted in that scan or cycle and an error is logged. If it does nothing, a cycle can restart an instance whose restart is still in flight.
- Code that constructs `InterruptedRecoveryScanner` itself should use the new constructor that takes an `ISystemClock`. The previous constructors are obsolete; a scanner built with them restarts without stamping, as before.

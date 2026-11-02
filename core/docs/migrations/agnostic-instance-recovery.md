# Tenant-agnostic instances are recovered under the "*" tenant

**Behavior change.** Interrupted-workflow recovery restarts an instance whose `TenantId` is `"*"` under the `"*"` tenant when the host registers one. Before, it restarted it under the default tenant (`""`).

## Who is affected

Only hosts whose `ITenantsProvider` returns a tenant with id `"*"`, for example to run tenant-agnostic CLR workflows as `"*"`. Their `"*"` instances were restarted under `""`, so the resumed run could not replace the instance's existing `"*"` rows (activity execution records, for example). The EF Core store refused with `tenant ownership mismatch. Shared rows need TenantId '*'`, and the instance stayed interrupted. They now recover.

Hosts without a `"*"` tenant are unchanged: `"*"` instances still restart under the default tenant. Named tenants, the "tenant not found, skip with a warning" behavior and the once-per-run de-duplication are unchanged.

## What to do

Nothing, unless a custom `IWorkflowRestarter` or a workflow activity relied on `"*"` instances running under the default tenant after a recovery. They now run with `ITenantAccessor.Tenant.Id == "*"`, as they do when started normally.

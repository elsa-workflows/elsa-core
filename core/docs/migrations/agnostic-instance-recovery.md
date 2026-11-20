# Tenant-agnostic instances are recovered under the "*" tenant

**Behavior change.** Interrupted-workflow recovery restarts an instance whose `TenantId` is `"*"` under the `"*"` tenant when the host registers one. Before, it restarted it under the default tenant (`""`).

## Who is affected

Only hosts whose `ITenantsProvider` returns a tenant with id `"*"`, for example to author tenant-agnostic workflow definitions in Studio (see the ADR [A host may register "*" as the tenant that authors tenant-agnostic entities](../../../docs/adr/2026-11-02-a-host-may-register-the-asterisk-tenant.md)). Their `"*"` instances were restarted under `""`, so the resumed run could not replace the instance's existing `"*"` rows (activity execution records, for example). The EF Core store refused with `tenant ownership mismatch. Shared rows need TenantId '*'`, and the instance stayed interrupted. They now recover.

Hosts without a `"*"` tenant are unchanged: `"*"` instances still restart under the default tenant. Named tenants, the "tenant not found, skip with a warning" behavior and the once-per-run de-duplication are unchanged.

## Shared-write authority of the `"*"` tenant

Since 3.11, writing a `"*"` row requires shared-write authority (`SharedTenantWriteScope`, see [Tenant-owned persistence](tenant-owned-persistence.md)). Code that runs in the context of the `"*"` tenant now has that authority for every entity type, so a host that registers `"*"` can author shared definitions and start, resume and recover `"*"` instances without entering the scope. Other tenant contexts are unchanged and still need an explicit grant.

`HeaderTenantResolver` no longer resolves a `"*"` header; the request stays unresolved. A host that resolves `"*"` must do it in its own resolver, from a value the client cannot choose.

## What to do

Nothing, unless a custom `IWorkflowRestarter` or a workflow activity relied on `"*"` instances running under the default tenant after a recovery. They now run with `ITenantAccessor.Tenant.Id == "*"`, as they do when started normally.

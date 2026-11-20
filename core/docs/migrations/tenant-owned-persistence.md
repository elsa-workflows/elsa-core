# Tenant-owned persistence

## MySQL bulk upsert

MySQL bulk upserts match only the key selected by the caller. If another unique constraint conflicts, the database rejects the write. When Elsa owns the transaction it rolls back the batch. When a caller supplies the transaction, that caller owns the rollback. No schema migration is required.

## Shared EF Core records

With multitenancy enabled, records marked `TenantId = "*"` are visible across tenants. Writing or deleting them requires a trusted host operation. The default tenant and a missing tenant context confer no shared write authority. Host workflow provisioning, runtime pause persistence, and session revocation enter an entity-specific `SharedTenantWriteScope` when persisting these records. Custom host provisioning code can enter the same scope around its shared writes.

Existing rows stay readable and the upgrade requires no schema migration. Application code that manages shared records should enter the scope from a trusted host entry point after validating the operation, then dispose it after the write.

## Tenant-agnostic workflow instances

Workflow instances are never tenant-agnostic: a run of a `"*"` definition belongs to the tenant that invokes it, and Elsa does not persist `"*"` instances or their runtime records (ADR [A host may register "*" as the tenant that authors tenant-agnostic entities](../../../docs/adr/2026-11-02-a-host-may-register-the-asterisk-tenant.md)). A host that registers a `"*"` tenant and ran workflows in that tenant's context before 3.11 may have instances with `TenantId = "*"`. Those that were interrupted stay interrupted: recovering them would write `"*"` rows, which needs shared-write authority. Recovering them automatically is out of scope for 3.11. Operators re-stamp them and their runtime records to a tenant. Interrupted-workflow recovery then resumes them in that tenant, where they can be cancelled with the normal API.

## Dapper workflow definition versions

Management migration 10006 makes each workflow definition version unique within its tenant. A legacy NULL tenant ID and an empty tenant ID both denote the default tenant and share the same index key. Migration leaves the stored tenant values unchanged.

If existing rows conflict, the migration reports a tenant, definition, version, and row count before creating the index. Review the conflicting rows, back them up, and resolve their version identities before retrying. The migration deletes no workflow definitions. Downgrade removes its index and any computed key it added.

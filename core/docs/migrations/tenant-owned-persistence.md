# Tenant-owned persistence

## MySQL bulk upsert

MySQL bulk upserts match only the key selected by the caller. If another unique constraint conflicts, the database rejects the write. When Elsa owns the transaction it rolls back the batch. When a caller supplies the transaction, that caller owns the rollback. No schema migration is required.

## Shared EF Core records

With multitenancy enabled, records marked `TenantId = "*"` are visible across tenants. Writing or deleting them requires a trusted host operation. The default tenant and a missing tenant context confer no shared write authority. Host workflow provisioning, runtime pause persistence, and session revocation enter an entity-specific `SharedTenantWriteScope` when persisting these records. Custom host provisioning code can enter the same scope around its shared writes.

Existing rows stay readable and the upgrade requires no schema migration. Application code that manages shared records should enter the scope from a trusted host entry point after validating the operation, then dispose it after the write.

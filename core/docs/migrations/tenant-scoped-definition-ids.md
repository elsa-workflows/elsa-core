# Workflow definition ids are unique per tenant

**Schema change.** With a shared database, the same workflow definition can now exist in more than one tenant. The unique index on `WorkflowDefinitions` includes `TenantId`.

## EF Core

- `IX_WorkflowDefinition_DefinitionId_Version` on `(DefinitionId, Version)` is replaced by `IX_WorkflowDefinition_DefinitionId_TenantId_Version` on `(DefinitionId, TenantId, Version)`, still unique.
- The new `TenantScopedDefinitionIds` migration in the Management context drops and recreates the index for SQL Server, PostgreSQL, MySQL, Oracle and SQLite. Existing rows satisfy the wider key, so `Up()` cannot fail on data.
- The default tenant is `""`, not `NULL`, like the label uniqueness migration (`PerTenantLabelUniqueness`). PostgreSQL, MySQL and SQLite treat `NULL`s as distinct in a unique index, so `Up()` stamps leftover `NULL` `TenantId` values to `""` there (and on SQL Server), and `EFCoreWorkflowDefinitionStore` now stores `""` instead of `NULL` when multitenancy is off. Without this, two concurrent imports of the same new definition could both create version 1.
- Oracle stores `''` as `NULL`, but treats rows with equal non-null columns and a `NULL` `TenantId` as duplicates in a unique index, so the index stays unique for the default tenant without stamping. SQL Server also treats `NULL`s as equal. The index has no filter.
- On SQL Server the key is `nvarchar(450)` + `nvarchar(450)` + `int`, up to 1804 bytes, above the 1700-byte limit for a nonclustered index key. The migration succeeds with a warning, and an insert fails only when `DefinitionId` and `TenantId` together exceed about 848 characters.
- `Down()` restores the old index. It fails while two tenants hold the same `(DefinitionId, Version)`; remove or rename one of them first. On Oracle and MySQL, where DDL commits implicitly, `Down()` checks for such duplicates before dropping the new index, so a failed downgrade leaves the unique index in place.

## Import

- `WorkflowDefinitionImporter` keeps the exported version id (`model.Id`, the primary key) only when no tenant uses it. Otherwise the new definition gets a generated version id and keeps its definition id.
- Re-importing into the tenant that already has the definition updates the existing draft and is unchanged.

## What to do

- A custom construction of `WorkflowDefinitionImporter` that uses the old constructor still compiles but is obsolete and keeps the exported version id even when another tenant uses it; use the constructor that accepts an `IWorkflowDefinitionStore`.
- MongoDB needs no manual step. At startup it creates the unique index `DefinitionId_1_TenantId_1_Version_1` on `(DefinitionId, TenantId, Version)` first, then drops the legacy store-wide `DefinitionId_1_Version_1`. Default-tenant rows have a null `TenantId`, so they stay unique per `(DefinitionId, Version)`.
- A MongoDB warning is logged, and the index is kept, when a store-wide unique `(DefinitionId, Version)` index under another name exists (tenants still cannot share a definition until you drop it), or when a non-unique `(DefinitionId, TenantId, Version)` index blocks the new one while the legacy index remains (drop the non-unique one and restart). If that non-unique index exists and nothing else keeps definition versions unique, startup fails with the same instruction.
- Dapper has no unique index on `(DefinitionId, Version)`, so no schema change is needed. `DapperWorkflowDefinitionStore.AnyAsync` now honors `WorkflowDefinitionFilter.TenantAgnostic`, which the importer relies on to see version ids used by other tenants.

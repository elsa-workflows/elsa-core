# Persistence vNext Operations

Issue: #7620

## Diagnostics

Use `GET /diagnostics/modular-persistence` to inspect the current process view of modular persistence:

- selected provider and startup materialization setting
- registered providers and whether they are selected
- registered manifests and materialization records
- physicalization plans and unsupported physicalization items
- materialization failures with provider, schema, version, timestamp, error type, and message

Startup materialization failures are also wrapped in `StorageManifestMaterializationException`, which includes provider, schema, version, attempt count, and retry guidance.

## Schema History Inspection

Relational providers store applied manifest versions in `ModularPersistenceSchemaHistory`.

SQLite/PostgreSQL:

```sql
SELECT SchemaName, Version, AppliedAt
FROM ModularPersistenceSchemaHistory
ORDER BY SchemaName, Version;
```

SQL Server:

```sql
SELECT SchemaName, Version, AppliedAt
FROM dbo.ModularPersistenceSchemaHistory
ORDER BY SchemaName, Version;
```

MongoDB does not maintain a relational schema history table. Inspect materialized state by listing the configured collections and indexes.

## Repair Rules

Repair is intentionally manual in vNext MVP. Apply these rules before editing schema history:

- Back up the database first.
- Confirm the physical document tables/collections and indexes exist.
- Confirm the manifest version in source code matches the history row being repaired.
- Prefer rerunning startup materialization before editing history.
- Never mark a version as applied if a required table, collection, or index is missing.
- Record the repair in deployment notes with schema name, version, provider, timestamp, and operator.

Safe repair examples:

SQLite/PostgreSQL:

```sql
INSERT INTO ModularPersistenceSchemaHistory (SchemaName, Version, AppliedAt)
VALUES ('sample.schema', '1.0.0', '2026-06-01T00:00:00.0000000+00:00')
ON CONFLICT (SchemaName, Version) DO NOTHING;
```

SQL Server:

```sql
IF NOT EXISTS (
    SELECT 1
    FROM dbo.ModularPersistenceSchemaHistory
    WHERE SchemaName = N'sample.schema' AND Version = N'1.0.0'
)
BEGIN
    INSERT INTO dbo.ModularPersistenceSchemaHistory (SchemaName, Version, AppliedAt)
    VALUES (N'sample.schema', N'1.0.0', N'2026-06-01T00:00:00.0000000+00:00');
END;
```

Use delete repair only when a history row exists but the physical schema was rolled back and startup materialization must apply it again:

```sql
DELETE FROM ModularPersistenceSchemaHistory
WHERE SchemaName = 'sample.schema' AND Version = '1.0.0';
```

## Tenant Isolation

Portable documents include `TenantId` in the document primary key and in declared index rows. Queries can be tenant-scoped through `DocumentQuery.TenantId` and runtime entity query requests can pass `TenantId`.

Operational rules:

- Pass `TenantId` for tenant-scoped runtime entity query endpoints.
- Treat missing tenant scope as an intentional cross-tenant operator query.
- Keep tenant IDs stable and normalized before writing documents.
- Test custom providers for load, query, update, and delete behavior across at least two tenants.

## Runtime Schema Security

Runtime storage definitions declare `RequiredPermissions`. Runtime entity data operations enforce those permissions through `RuntimeStorageOperationContext`.

Endpoint-level permissions protect the admin API:

- Read runtime definitions: `read:modular-persistence:runtime-storage-definitions`
- Write runtime definitions: `write:modular-persistence:runtime-storage-definitions`
- Publish runtime definitions: `publish:modular-persistence:runtime-storage-definitions`
- Delete runtime definitions: `delete:modular-persistence:runtime-storage-definitions`
- Read runtime entities: `read:modular-persistence:runtime-entities`
- Write runtime entities: `write:modular-persistence:runtime-entities`
- Delete runtime entities: `delete:modular-persistence:runtime-entities`

Do not publish runtime definitions with broad required permissions unless the entity is explicitly operator-only.

## Backup And Restore

Back up these provider resources together:

- Relational providers: `ModularPersistenceDocuments`, `ModularPersistenceDocumentIndexes`, and `ModularPersistenceSchemaHistory`.
- MongoDB shared collection mode: the shared document collection and its indexes.
- MongoDB collection-per-type mode: all generated type collections and their indexes.

After restore:

1. Run startup materialization with the same provider options.
2. Check `/diagnostics/modular-persistence` for failures.
3. Inspect schema history or MongoDB indexes.
4. Run smoke tests for create, load, indexed query, update, and delete on a non-production entity.

## Roll-Forward Guidance

Prefer roll-forward repair over rollback. Materializers use idempotent table/index creation and idempotent schema-history writes, so repeated startup materialization should be safe.

When a deployment fails during materialization:

1. Capture the `StorageManifestMaterializationException` details.
2. Inspect the diagnostics endpoint.
3. Confirm whether the provider transaction rolled back.
4. Fix the manifest/provider option issue.
5. Rerun startup materialization.
6. Only edit schema history manually if the physical schema and history table disagree after verification.

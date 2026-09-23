# Reversible bridge design and current-target requirements

The first section records the historical 3.8.1 → 3.8.4 fixture contract. It does not define the current Core target: Core 3.8.4 has no native `TenantId`, while the pinned target Core does. The current-target section below is grounded in Core commit [`7b06b82d0ea89c12d49c3c28da8d770bfca13faf`](https://github.com/elsa-workflows/elsa-core/tree/7b06b82d0ea89c12d49c3c28da8d770bfca13faf). Neither section authorizes or implements production DDL. A future task must turn the current-target requirements into reviewed migration code and integration tests before any customer database is touched.

## Historical 3.8.1 → 3.8.4 fixture only

The separate-database recommendation below applies only to the pinned historical pair characterized by [#8276](https://github.com/elsa-workflows/elsa-core/issues/8276). Treat its statements about Core fields, schema, tenancy blockers, and compatibility as specific to Core 3.8.4. In particular, its no-native-tenant-field conclusion must not be carried forward to current Core.

## Preserve source rows and identity

Use a **separate Core SQLite database file** for the first bridge. The two products both use a `Secrets` table, so an in-place table rename/drop/overwrite risks destroying the only copy and confuses the two migration histories. Keep the old database immutable and apply the Core 3.8.4 migrations to the new file.

Add one namespaced compatibility table to the new file. Store every original row, including the old ciphertext, plus an explicit pointer to the Core aggregate/version:

```sql
CREATE TABLE ElsaSecretsLegacyV381 (
    LegacyId TEXT NOT NULL PRIMARY KEY,
    LegacySecretId TEXT NOT NULL,
    LegacyName TEXT NOT NULL,
    LegacyScope TEXT NULL,
    LegacyEncryptedValue TEXT NOT NULL,
    LegacyDescription TEXT NOT NULL,
    LegacyVersion INTEGER NOT NULL,
    LegacyIsLatest INTEGER NOT NULL CHECK (LegacyIsLatest IN (0, 1)),
    LegacyStatus INTEGER NOT NULL,
    LegacyExpiresIn TEXT NULL,
    LegacyExpiresAt TEXT NULL,
    LegacyLastAccessedAt TEXT NULL,
    LegacyTenantId TEXT NULL,
    LegacyCreatedAt TEXT NOT NULL,
    LegacyUpdatedAt TEXT NOT NULL,
    LegacyOwner TEXT NULL,
    CoreSecretId TEXT NOT NULL,
    CoreVersion INTEGER NOT NULL,
    MigrationBatchId TEXT NOT NULL,
    UNIQUE (CoreSecretId, CoreVersion)
);
CREATE INDEX IX_ElsaSecretsLegacyV381_SecretId
    ON ElsaSecretsLegacyV381 (LegacySecretId);
CREATE INDEX IX_ElsaSecretsLegacyV381_TenantOwner
    ON ElsaSecretsLegacyV381 (LegacyTenantId, LegacyOwner);
```

`LegacyId` is the old per-version primary key. `LegacySecretId` remains the old logical aggregate ID and is the candidate Core `Secret.Id`. `CoreSecretId` and `CoreVersion` form the bridge lookup to the JSON-serialized Core version. There is no SQLite foreign key into a JSON array; the migration verifies the Core aggregate and matching version before committing. `MigrationBatchId` identifies the single atomic batch and supports audit/recovery.

The sidecar is authoritative for legacy-only metadata and exact rollback data. Core `SecretVersion.Payload.Metadata` needs only a stable `legacy.bridgeRowId` link, plus a schema/version marker. This avoids copying tenant or owner labels into general-purpose Core metadata that APIs may return without applying legacy authorization. The synthetic Python projection also shows field-by-field candidate placement; production code should use the sidecar as the system of record for all source-row fields that Core cannot represent natively.

The old `EncryptedValue` remains encrypted under the old Data Protection key ring. Protect the sidecar with the same access controls and backup protections as the secret store. It must not be returned from Core APIs, diagnostic output, audit logs, or the compatibility API. Retain the original database snapshot and old key ring through the rollback window even though the sidecar makes reconstruction possible.

## Conversion transaction and failure conditions

1. Take a consistent read-only snapshot of the source. Confirm the exact pinned schema and migration history, no active EF migration lock, source counts, and backup readability. Preserve the old Data Protection key ring, application name, and `Elsa.Secrets.Encryption` purpose.
2. Preflight every row without changing the target: validate IDs, versions, status codes, dates, name validator results, normalized-name uniqueness, and the exactly-one-latest rule. Reject any source schema/history drift, unavailable old key, missing/invalid Core key, or existing target aggregate/name collision.
3. Run Core's own migrations against the separate destination file. Do not copy the Extensions migration history row.
4. In one destination transaction, decrypt each source value in memory using the exact old protector; write Core versions through `EncryptedSecretStore` so they receive Core AES-GCM `protectedValue`; write the full source row and Core pointer to the sidecar. Never stage plaintext in a file, log, metadata table, or report.
5. Before commit, read each newly written Core version back, compare a one-way plaintext hash in memory, and verify Core key rejection behavior. Check row/version counts, source-to-sidecar field equality, mapping uniqueness, and full aggregate/latest/expiry state. Any mismatch rolls back the destination transaction and leaves the source untouched.
6. Keep the old database and both key configurations available until API, security, and operational acceptance has completed. Roll back by switching to the immutable old database snapshot; if reconstruction is needed, rebuild old `Secrets` rows from the sidecar and restore the original migration history. Do not destroy the sidecar or old keys during the rollback window.

The two-process fixture in this PR verifies only steps around controlled decrypt/re-encrypt and Core read-back with synthetic keys. It does not perform SQLite DDL, transaction/rollback tests, source snapshots, target collision checks, or customer key discovery.

## Compatibility gate before cutover

The sidecar preserves tenant, owner, per-version IDs, status values, names, timestamps, expiry duration, last-access time, and exact old ciphertext. **Preservation alone does not reproduce behavior.** An opt-in compatibility service must be implemented and reviewed before cutover:

- It resolves old per-version IDs through `LegacyId` and maps them to a Core aggregate/version.
- It enforces the old tenant and owner predicates on every read, write, list, and delete. A tenant-bearing dataset cannot be exposed through unfiltered Core 3.8.4 endpoints; either use isolated per-tenant database boundaries that are proven at the host layer, or add supported tenant authorization to the Core access path.
- It handles legacy route and client collisions explicitly. The existing route shapes cannot be presumed to keep their old meaning when the Core API is enabled; use a versioned/selected adapter route or a separately reviewed routing decision.
- It updates the Core aggregate and sidecar in one transaction for new versions so `LegacyIsLatest`, status and mapping pointers do not drift. If owner/tenant change semantics differ from Core, writes fail closed until a rule exists.
- It preserves metadata-only behavior (`ExpiresIn`, `LastAccessedAt`, per-version `UpdatedAt`) through the sidecar-backed adapter or explicitly retires those behaviors with a compatibility plan.

Until that adapter and its authorization tests exist, a sidecar can preserve source data but must not be treated as proof that a tenant-bearing or ID-addressed application is safe to cut over. This is a direct-upgrade no-go, not a permanent block on a reversible, compatibility-backed migration.

## Current Core target requirements

The verified target commit is [`7b06b82d0ea89c12d49c3c28da8d770bfca13faf`](https://github.com/elsa-workflows/elsa-core/tree/7b06b82d0ea89c12d49c3c28da8d770bfca13faf). `Secret` inherits `Id` and nullable `TenantId` from [`Entity`](https://github.com/elsa-workflows/elsa-core/blob/7b06b82d0ea89c12d49c3c28da8d770bfca13faf/src/modules/Elsa.Common/Entities/Entity.cs); it has no legacy `Owner` field. It now has `ManagedOwnerId` and `ManagedGenerationId` for lifecycle-owned generations, added by [`ManagedSecretOwnership`](https://github.com/elsa-workflows/elsa-core/blob/7b06b82d0ea89c12d49c3c28da8d770bfca13faf/src/modules/Elsa.Secrets.Persistence.EFCore.Sqlite/Migrations/Secrets/20260923164123_ManagedSecretOwnership.cs). Those fields are not equivalent to the legacy owner marker and must remain unset unless a separately reviewed ownership mapping proves equivalence. [`SecretVersion`](https://github.com/elsa-workflows/elsa-core/blob/7b06b82d0ea89c12d49c3c28da8d770bfca13faf/src/modules/Elsa.Secrets/Models/SecretVersion.cs) has no legacy per-version ID, `UpdatedAt`, `ExpiresIn`, or `LastAccessedAt` fields. Those legacy-only values still need a compatibility sidecar, and their old behavior is not restored by storing them there.

Current SQLite persistence has a tenant-aware unique `(TenantId, NormalizedName)` index. [`SecretTenancy`](https://github.com/elsa-workflows/elsa-core/blob/7b06b82d0ea89c12d49c3c28da8d770bfca13faf/src/modules/Elsa.Secrets.Persistence.EFCore.Sqlite/Migrations/Secrets/20260825230122_SecretTenancy.cs) adds the nullable column and replaces the old global unique index. [`SecretDefaultTenantUniqueness`](https://github.com/elsa-workflows/elsa-core/blob/7b06b82d0ea89c12d49c3c28da8d770bfca13faf/src/modules/Elsa.Secrets.Persistence.EFCore.Sqlite/Migrations/Secrets/20260914120000_SecretDefaultTenantUniqueness.cs) converts remaining null tenant IDs to the default tenant ID `""` before recreating that index. [`ManagedSecretOwnership`](https://github.com/elsa-workflows/elsa-core/blob/7b06b82d0ea89c12d49c3c28da8d770bfca13faf/src/modules/Elsa.Secrets.Persistence.EFCore.Sqlite/Migrations/Secrets/20260923164123_ManagedSecretOwnership.cs) adds nullable lifecycle ownership markers. The [`EF Core repository`](https://github.com/elsa-workflows/elsa-core/blob/7b06b82d0ea89c12d49c3c28da8d770bfca13faf/src/modules/Elsa.Secrets.Persistence.EFCore/Repositories/EFCoreSecretRepository.cs) stamps default-tenant rows and applies tenant ownership rules through its configured tenancy behavior. This is the pinned target path to test; Core 3.8.4 is not.

A bounded current-target proof should therefore:

1. Create a synthetic source database from the pinned Extensions schema and a fresh destination by running the target Core migrations. Keep the source immutable and verify its bytes and migration history remain unchanged.
2. Preserve non-empty legacy `TenantId` values exactly in Core's native `TenantId`, after verifying those identifiers resolve to the same tenant context at the host boundary. Map null/empty legacy tenant values to `""` only after verifying that the source represented them as default-tenant rows. Fail closed on unknown tenant semantics, missing tenant context, or any target uniqueness collision. Exercise reads and writes in at least two named tenants and the default tenant.
3. Preserve each legacy row ID, owner, and every field with no native Core equivalent in a namespaced sidecar keyed to the source aggregate/version. Core has no `Owner` field, so tenant filtering alone does not preserve owner authorization: no legacy route may be cut over until an adapter checks the preserved owner on every applicable operation. Sidecar preservation does not itself prove route, client, owner, or metadata-behavior compatibility.
4. Keep the legacy logical aggregate ID as the Core aggregate ID when it passes validation and does not collide. Link each old per-version ID to its Core aggregate/version in the sidecar; test that every source row maps once and that every target version has exactly one source pointer. Preserve `ExpiresIn`, `LastAccessedAt`, per-version `UpdatedAt`, old ciphertext, and unsupported status details verbatim in the sidecar unless a separately verified native mapping exists.
5. Perform destination writes and sidecar creation atomically. Inject validation, encryption, uniqueness, and write failures and verify rollback leaves the destination unchanged and the old source readable. Verify encrypted values only through the configured Core store; never place plaintext or keys in SQLite sidecar data, logs, or reports.
6. Report this as a synthetic SQLite conversion proof only. Keep host route selection, API/client compatibility, Owner authorization adapter, provider coverage beyond SQLite, and production cutover as separate acceptance gates.

The synthetic current-Core proof in [#8290](https://github.com/elsa-workflows/elsa-core/issues/8290) exercises actual SQLite rows from the pinned Extensions 3.8.1 migration against the target Core SQLite schema at [`7b06b82d0ea89c12d49c3c28da8d770bfca13faf`](https://github.com/elsa-workflows/elsa-core/tree/7b06b82d0ea89c12d49c3c28da8d770bfca13faf). It preserves every source row in the sidecar within the target transaction, verifies re-encrypted values after reloading target rows through the EF repository, exercises default and named-tenant visibility, leaves lifecycle ownership markers unset, and proves fail-closed behavior for source drift, collisions, and injected rollback. The historical source fixture records the ordinary `MigrateAsync` pending-model warning separately and does not suppress it.

This closes only the synthetic SQLite data path. The sidecar does not prove old owner authorization, tenant-host identity resolution, legacy route or API behavior, compatibility-adapter writes, or production rollback operations. Keep cutover disabled until those behaviors are implemented and reviewed against a separately authorized environment.

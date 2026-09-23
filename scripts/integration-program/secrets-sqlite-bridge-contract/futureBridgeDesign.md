# Smallest reversible bridge design

This design is a bounded recommendation from the pinned 3.8.1 → 3.8.4 evidence. It does not authorize or implement production DDL. A future task should turn it into reviewed migration code and integration tests before any customer database is touched.

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

# 11. Persistence vNext First Module Candidate

Date: 2026-06-01

## Status

Accepted

## Context

Persistence vNext needs a first production module candidate before replacing any live stores. Issue #7594 inventoried the current persistence surface and classified stores into document-friendly, relational-specific, and hot-path groups. Its key finding was that Secrets and workflow definitions are both plausible document-oriented candidates, while workflow instances, triggers, bookmarks, queues, and execution logs should stay out of the first migration because they are runtime hot paths.

The inventory also found that workflow definitions are attractive because their persisted shape already separates scalar metadata from serialized workflow payload data. However, workflow definitions are central to Studio, import/export, versioning, publishing, reference graphs, workflow activity descriptors, trigger indexing, cache invalidation, and runtime refresh behavior. `ManagementElsaDbContext` also currently hosts workflow definitions and workflow instances together, so migrating only definitions requires partial EF coexistence inside a high-traffic module.

Secrets have a smaller persistence surface:

- `ISecretRepository` currently needs get-by-name, list, add, replace-deleted, and save operations.
- `Secret` is naturally document-shaped with scalar metadata and a version collection.
- List and picker endpoints filter by search text, type, store, scope, and status, then sort by name.
- Runtime resolution reads by immutable normalized technical name and then resolves the latest active version through the configured secret store.
- The module is security-sensitive but not part of the workflow execution hot path.

## Decision

Secrets is the first Persistence vNext production module candidate.

Workflow definitions remain a strong follow-up candidate, but they are deferred until the vNext document/query contracts and coexistence pattern are proven with Secrets. Hot-path runtime stores remain explicitly out of scope for the first module migration.

The first Secrets manifest is defined in `src/modules/Elsa.Secrets/Storage/SecretsStorageManifest.cs` with schema name `elsa.secrets` and version `1.0.0`.

### Manifest Shape

The manifest contains one document storage unit, `Secrets`.

Scalar fields:

- `Id`
- `Name`
- `DisplayName`
- `Description`
- `TypeName`
- `StoreName`
- `Scope`
- `Status`
- `CreatedAt`
- `UpdatedAt`
- `CurrentVersion`
- `CurrentVersionExpiresAt`

JSON fields:

- `Tags`
- `Versions`

Keys and indexes:

- Primary key: `PK_Secrets` on `Id`
- Unique key: `UK_Secrets_Name` on normalized immutable `Name`
- Unique index: `UX_Secrets_Name` on `Name`
- Query indexes for `Status + Name`, `TypeName + Name`, `StoreName + Name`, and `Scope + Name`

### Tenant Behavior

Current Secrets persistence is tenant-agnostic: `Secret` has no tenant property and repository lookups use normalized `Name` only. The vNext document store should therefore write Secrets with the default empty tenant id. Tenant-scoped secrets require an explicit product decision and a manifest version bump because they change uniqueness from `Name` to `TenantId + Name`.

### Existing API Behavior To Preserve

- Technical names are normalized, immutable, and unique.
- Deleted secrets are hidden from get/list behavior, but `TryAddOrReplaceDeletedAsync` may replace a deleted record with the same name.
- List/picker behavior excludes deleted records, supports filtering by type, store, scope, and status, applies search over name/display name/description, sorts by name, and pages with a maximum page size of 200.
- Create/rotate store payloads must preserve no-reveal behavior: protected payload metadata can be persisted, but cleartext values must not be stored in general document fields, logs, or API responses.
- Resolution returns the latest active, non-expired version for active secrets only.

### Migration And Coexistence Constraints

- The first implementation should add a vNext-backed `ISecretRepository` without removing the existing file and in-memory repositories.
- Existing file-backed secrets should remain readable until a migration/import path is explicitly implemented.
- Configuration-backed secrets remain a store concern; the repository manifest describes Elsa-managed secret metadata and protected version payloads.
- Search text should be handled as a portable fallback unless provider-specific full-text capabilities are explicitly added later.
- The vNext repository must keep the security boundary: secret values are only revealed through store resolution paths that already enforce type/store rules.

## Consequences

### Positive

- The first production migration exercises document shape, unique keys, scalar query metadata, JSON payloads, and security-sensitive no-reveal behavior without moving runtime hot paths.
- The manifest is small enough to cover with focused unit and repository contract tests.
- Provider behavior can be verified with SQLite first, then relational providers and MongoDB.

### Negative

- Secrets are security-sensitive, so repository mistakes have high impact even though the operational surface is small.
- Full current search semantics include substring matching over name/display name/description, which the portable manifest does not make a native full-text guarantee.
- Tenant-scoped secrets remain a future decision and cannot be silently introduced without changing uniqueness semantics.

### Neutral

- Workflow definitions remain the next likely candidate after Secrets.
- Runtime workflow instances, triggers, bookmarks, queues, and execution logs remain out of scope for the first module migration.

# Persistence vNext Extraction Boundaries

Issue: #7621

## Decision

Persistence vNext remains Elsa-internal for this development branch, but the reusable boundary is finalized so it can be extracted without redesign.

The current Elsa repository package names stay `Elsa.ModularPersistence*`. If extracted, the reusable packages should drop the Elsa prefix and keep the same conceptual names.

## Final Package Names

Elsa-internal package names:

- `Elsa.ModularPersistence`
- `Elsa.ModularPersistence.Relational`
- `Elsa.ModularPersistence.Sqlite`
- `Elsa.ModularPersistence.SqlServer`
- `Elsa.ModularPersistence.PostgreSql`
- `Elsa.ModularPersistence.MongoDb`

External reusable package names after extraction:

- `ModularPersistence.Abstractions`
- `ModularPersistence.Documents`
- `ModularPersistence.Relational`
- `ModularPersistence.Sqlite`
- `ModularPersistence.SqlServer`
- `ModularPersistence.PostgreSql`
- `ModularPersistence.MongoDb`

Elsa integration packages after extraction:

- `Elsa.Persistence.Modular`
- `Elsa.Persistence.Modular.RuntimeEntities`
- `Elsa.Persistence.Modular.Secrets`

## Ownership Model

Reusable layer ownership:

- Storage manifest descriptors.
- Document envelope, document session, and document store contracts.
- Portable index-first query model.
- Physicalization descriptors and provider capability model.
- Relational connection/dialect helpers.
- Provider document stores and materializers.

Elsa integration ownership:

- Elsa feature and shell feature registration.
- FastEndpoints admin and diagnostics endpoints.
- Elsa permissions.
- Runtime storage definition lifecycle and audit trail.
- Runtime entity admin API.
- Module-specific manifests such as Secrets.

## Current Coupling Review

Reusable concepts already avoid depending on EF Core or Elsa workflow runtime concepts. The reusable boundary is represented by these folders:

- `Descriptors`
- `Documents`
- `Queries`
- `Planning`
- `Validation`
- `Relational.Contracts`
- provider materializers and document sessions

Elsa-specific concepts intentionally remain in the Elsa package:

- `Features`
- `ShellFeatures`
- `Endpoints`
- `Permissions`
- `Runtime`
- `Services.ModularPersistenceMaterializationStartupTask`
- `Services.ModularPersistenceDiagnosticsService`

The current project still packages reusable and Elsa integration code together under `Elsa.ModularPersistence`. That is acceptable for the vNext branch, but extraction should split the folders above into the external package names before publishing outside Elsa.

## Public API Risks

Review these APIs before external publishing:

- `StorageManifestDescriptor`, `StorageUnitDescriptor`, and descriptor constructor shapes.
- `StorageManifestVersion` version formatting and comparison policy.
- `PhysicalizationIntent` values and whether `NativePhysicalized` needs provider-specific options.
- `DocumentQuery`, especially tenant scoping, paging, sorting, and unsupported operator behavior.
- `DocumentQueryValue` numeric representation, currently decimal-backed for numeric query values.
- `DocumentEnvelope.Metadata` string-only metadata policy.
- `ExpectedDocumentVersion` concurrency semantics.
- `IDocumentSession` transaction/session lifetime semantics.
- Provider option names for optimized indexes and collection strategies.

Breaking these after extraction would affect non-Elsa consumers. Treat them as pre-1.0 until the extraction package split is complete.

## Extraction Checklist

Before external publishing:

1. Move descriptors, documents, queries, planning, validation, and provider-neutral contracts into `ModularPersistence.Abstractions` and `ModularPersistence.Documents`.
2. Move relational contracts and helpers into `ModularPersistence.Relational`.
3. Keep SQLite, SQL Server, PostgreSQL, and MongoDB providers free of Elsa feature, endpoint, and permission dependencies.
4. Move Elsa feature registration, diagnostics endpoint, runtime entity APIs, and module manifests into `Elsa.Persistence.Modular*`.
5. Add compatibility tests that reference only the external package set.
6. Run a public API diff before publishing.

## Non-Goals For This Branch

- Do not rename repository projects during the vNext integration branch.
- Do not publish external packages from this branch.
- Do not remove Elsa integration APIs from `Elsa.ModularPersistence` until the package split is scheduled.

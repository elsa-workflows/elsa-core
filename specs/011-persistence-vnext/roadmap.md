# Persistence vNext Executable Roadmap

Parent issue: [#7580](https://github.com/elsa-workflows/elsa-core/issues/7580)

## S1 - Core Manifest And Planner Foundations

Outcome: Modules can declare provider-neutral storage intent once, and relational/document planners can consume that same manifest.

Scope:

- Core descriptors.
- Provider extension contracts.
- Relational planner.
- Document planner.
- Secrets sample manifest.
- SQLite and SQL Server render proof.

Non-scope:

- Full document store.
- Runtime entity UI.
- Production provider packages.

Acceptance criteria:

- Same Secrets manifest produces relational and document plans.
- SQLite DDL can execute.
- SQL Server DDL can render.
- Schema history records applied version.

Validation:

- `dotnet test test/unit/Elsa.Persistence.VNext.UnitTests/Elsa.Persistence.VNext.UnitTests.csproj`

GitHub issue mapping: [#7670](https://github.com/elsa-workflows/elsa-core/issues/7670)

## S2 - Portable SQLite Document Store MVP

Outcome: A provider-neutral document store can save, load, delete, index, and query documents on SQLite.

Scope:

- Document envelope.
- SQLite document table.
- Generic field index table.
- Save/load/delete.
- Index maintenance.
- Basic query by declared indexes.
- Optimistic concurrency.

Non-scope:

- SQL Server/MongoDB providers.
- Runtime entity admin UI.
- Physicalization policy.

Acceptance criteria:

- Documents can be saved and loaded.
- Indexed fields are maintained transactionally.
- Queries by declared indexes return expected documents.
- Unindexed queries fail clearly.

Validation:

- Targeted unit and SQLite integration tests.

GitHub issue mapping: [#7671](https://github.com/elsa-workflows/elsa-core/issues/7671)

## S3 - SQL Server And PostgreSQL Relational Document Providers

Outcome: The portable document/index contract runs on SQL Server and PostgreSQL.

Scope:

- SQL Server document store.
- PostgreSQL document store.
- Generic field indexes.
- Startup locking/materialization.
- Provider integration tests.

Non-scope:

- MongoDB.
- Runtime entity UI.
- Advanced physicalization.

Acceptance criteria:

- Same document/index contract tests pass on SQLite, SQL Server, and PostgreSQL.
- Provider differences remain isolated to provider packages.

GitHub issue mapping: [#7672](https://github.com/elsa-workflows/elsa-core/issues/7672)

Local progress note: provider packages and SQL dialect tests are implemented locally. Live SQL Server/PostgreSQL contract tests require a running Docker/Testcontainers environment or CI worker.

## S4 - MongoDB Document Provider

Outcome: The same storage/index model works on MongoDB using native collections and indexes.

Scope:

- MongoDB document store.
- Native collection/index materialization.
- Query translation for declared indexes.
- Optimistic concurrency.
- Provider capability validation.

Non-scope:

- Aggregation pipelines beyond portable query contract.
- Runtime entity UI.

Acceptance criteria:

- Same document/index contract tests pass on MongoDB.
- Native indexes are created and used for declared indexes.

GitHub issue mapping: [#7673](https://github.com/elsa-workflows/elsa-core/issues/7673)

Local progress note: MongoDB provider package, native collection/index planner, query translation, and expected-version write/delete filters are implemented locally. Live MongoDB contract tests require a running MongoDB service or CI worker.

## S5 - Elsa Integration And First Module Migration

Outcome: Elsa consumes vNext persistence and one real module no longer needs EF Core migrations.

Scope:

- Elsa registration package.
- Shell feature/options.
- Startup materialization.
- Diagnostics.
- Secrets module vNext store.
- EF Core import path if required.

Non-scope:

- Workflow runtime hot paths.
- Runtime-defined entities.

Acceptance criteria:

- Secrets works on vNext persistence.
- Provider-specific EF Core migration packages are not required for Secrets vNext.
- Existing behavior is preserved.

GitHub issue mapping: [#7674](https://github.com/elsa-workflows/elsa-core/issues/7674)

Local progress note: opt-in Elsa integration package, startup materialization task, status snapshot, and Secrets vNext repository are implemented locally. Existing Secrets persistence remains intact; import/migration from existing stores is intentionally left open until the storage model is proven further.

## S6 - Runtime-Defined Entities

Outcome: Admin-defined entities can be published, persisted, and queried through the document/index model.

Scope:

- Runtime entity definitions.
- Draft/published/retired lifecycle.
- Validation.
- Index definitions.
- CRUD/API integration.
- Audit trail.

Non-scope:

- Full Studio UI polish.
- Native physicalization.

Acceptance criteria:

- Admin-defined entity can be published.
- Instances can be created and queried by indexed fields.
- No physical table per runtime entity is required by default.

GitHub issue mapping: [#7675](https://github.com/elsa-workflows/elsa-core/issues/7675)

## S7 - Physicalization And Performance

Outcome: Hot entities and indexes can opt into provider-optimized physical storage.

Scope:

- Storage policies.
- Optimized indexes.
- Native physicalization hooks.
- Benchmarks.
- Provider capability validation.

Non-scope:

- Universal auto-optimization.

Acceptance criteria:

- At least one relational provider supports optimized indexes.
- At least one document provider supports provider-native optimization.
- Portable default remains unchanged.

GitHub issue mapping: [#7676](https://github.com/elsa-workflows/elsa-core/issues/7676)

## S8 - Workflow Runtime Evaluation And Hardening

Outcome: vNext is evaluated against workflow runtime hot paths and hardened for production.

Scope:

- Runtime store benchmark.
- Concurrency/locking validation.
- Retry and diagnostics.
- Backup/recovery guidance.
- Provider status reporting.

Non-scope:

- Premature runtime migration without benchmark evidence.

Acceptance criteria:

- Clear go/no-go decision for workflow runtime stores.
- Production operational risks are documented and mitigated.

GitHub issue mapping: [#7677](https://github.com/elsa-workflows/elsa-core/issues/7677)

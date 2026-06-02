# Persistence vNext Roadmap

## Vision

Persistence vNext should let modular applications declare persistence intent once and let database providers materialize that intent using the best physical model available to them.

For Elsa, this means modules can contribute storage manifests, indexes, and runtime entity definitions without owning SQL Server, SQLite, PostgreSQL, Oracle, MySQL, MongoDB, or EF Core migration projects.

The long-term model is:

- portable by default
- provider-optimized when needed
- document-first for dynamic/runtime entities
- explicit-index driven for portable queries
- extensible enough for relational and document databases
- independent enough to become reusable outside Elsa

## Non-Goals

Persistence vNext should not become:

- a universal ORM
- a LINQ provider
- an EF Core replacement for all data access
- a lowest-common-denominator database API
- a reporting/BI engine
- a magic cross-database query translator

Its job is to own modular persistence manifests, schema/index materialization, versioning, and a focused document/index query model.

## Design Principles

1. Modules declare intent, providers own physical shape.
2. Schema authority is the manifest, not EF Core migrations.
3. Runtime-defined entities use document storage, not table-per-entity by default.
4. Portable queries run through declared indexes.
5. Advanced provider behavior is exposed through capabilities and opt-in policies.
6. Provider-specific escape hatches are acceptable when explicit.
7. The core abstractions must not depend on Elsa, EF Core, MongoDB, or any one provider.
8. Start with simple, reliable append-only versioning before automatic diffing.
9. Optimize hot paths without sacrificing the default portable path.

## Target Package Shape

### General-Purpose Packages

- `ModularPersistence.Abstractions`
  - storage manifests
  - storage units
  - fields
  - indexes
  - versions
  - provider capabilities
  - planner, renderer, migrator contracts

- `ModularPersistence.Document`
  - document descriptors
  - collection/index plans
  - portable document session abstractions
  - index-first query model

- `ModularPersistence.Relational`
  - relational plans
  - SQL operation model
  - relational schema history conventions
  - SQL execution abstractions

- `ModularPersistence.Sqlite`
  - SQLite type mapping
  - SQL rendering
  - schema history
  - portable document store implementation

- `ModularPersistence.SqlServer`
  - SQL Server type mapping
  - SQL rendering
  - schema history
  - optimized index strategies

- `ModularPersistence.PostgreSql`
  - PostgreSQL type mapping
  - JSONB options
  - expression/generated indexes where useful

- `ModularPersistence.MongoDb`
  - collection/index planning
  - native MongoDB indexes
  - document session implementation
  - optional schema validation

### Elsa Packages

- `Elsa.Persistence.VNext`
  - Elsa module integration
  - shell feature wiring
  - options
  - module discovery
  - startup migration orchestration

- `Elsa.Persistence.VNext.RuntimeEntities`
  - runtime entity definitions
  - admin-defined schema lifecycle
  - generated API/UI integration points
  - permissions and audit hooks

## Core Concepts

### Storage Manifest

A module or runtime admin defines:

- schema name
- schema version
- storage units
- fields
- keys
- indexes
- validation metadata
- provider requirements
- optional physicalization policy

### Storage Unit

A storage unit is provider-neutral. Providers map it differently:

- relational: table or document table/index table
- MongoDB: collection or shared collection
- Cosmos DB: container/partition strategy
- in-memory: dictionary plus indexes

### Document Store

Runtime entities and many module aggregates should be stored as documents:

- `Id`
- `Type`
- `TenantId`
- `Version`
- `CreatedAt`
- `UpdatedAt`
- `Data`
- optional metadata

### Indexes

Indexes are the portable query surface.

Portable queries should only target declared indexes. Providers decide how to implement them:

- relational generic field index table
- relational typed index table
- SQL generated/computed column
- MongoDB native index
- in-memory lookup table

### Physicalization Policy

Each storage unit or index can choose a policy:

- `PortableDocument`
  - safest default
  - shared document table/collection plus generic indexes

- `OptimizedIndexes`
  - provider creates stronger typed/materialized indexes
  - still document-backed

- `NativePhysicalized`
  - provider may create typed tables, collection-per-entity, generated columns, JSONB expression indexes, etc.
  - opt-in for hot paths

## Roadmap

## Phase 0: Research And Validation

Goal: validate the design against Elsa's real persistence surface.

Tasks:

- Inventory current EF Core modules, stores, migrations, and provider packages.
- Classify stores into document-friendly, relational-specific, and high-performance hot paths.
- Identify query patterns per module.
- Compare YesSQL, Marten, MongoDB, EF Core, FluentMigrator, Liquibase, and Orchard Core data migrations.
- Decide which Elsa module is the first production candidate.

Exit criteria:

- Persistence inventory completed.
- First production module candidate selected.
- Provider priorities selected.

Recommended first candidates:

- Secrets
- Labels
- Tenants
- simple diagnostics stores

Avoid first:

- workflow runtime hot paths
- workflow instance queries
- execution log history

## Phase 1: Extract Clean Core Abstractions

Goal: turn the POC into clean reusable contracts.

Tasks:

- Rename POC concepts into neutral package names.
- Separate core abstractions from Elsa.
- Finalize descriptor vocabulary:
  - manifest
  - storage unit
  - field
  - key
  - index
  - version
  - capability
- Add provider capability checks.
- Add validation errors and diagnostics.
- Add contract tests for descriptors and planners.

Exit criteria:

- No Elsa dependencies in core packages.
- Descriptors support relational and document planning.
- Provider capability validation exists.
- SQLite and SQL Server render tests still pass.

## Phase 2: Portable Document Store MVP

Goal: implement a YesSQL-like portable document store over SQLite first.

Tasks:

- Define `IDocumentSession`.
- Define `IDocumentStore`.
- Define document envelope:
  - `Id`
  - `Type`
  - `TenantId`
  - `Version`
  - `Data`
  - timestamps
- Implement SQLite document table.
- Implement generic field index table.
- Implement save/load/delete.
- Implement index maintenance in the same transaction.
- Implement basic optimistic concurrency.

Exit criteria:

- Can store and load documents.
- Can maintain declared indexes.
- Can query by declared indexes.
- Startup materialization is idempotent.

## Phase 3: Portable Query Model

Goal: define useful queries without becoming an ORM.

Tasks:

- Define index-first query API.
- Support:
  - equals
  - not equals
  - in
  - range
  - starts-with where supported
  - null checks
  - sorting
  - paging
- Add provider capability validation for unsupported operations.
- Add query-plan diagnostics.
- Add tests ensuring unindexed portable queries fail clearly.

Exit criteria:

- Modules can query common indexes portably.
- Unsupported query shapes produce clear errors.
- Query API does not expose provider-specific behavior by accident.

## Phase 4: SQL Server And PostgreSQL Providers

Goal: prove the relational provider model across serious relational engines.

Tasks:

- Implement SQL Server document store.
- Implement PostgreSQL document store.
- Support generic indexes.
- Add provider-specific optimized index options:
  - SQL Server computed columns where useful
  - PostgreSQL JSONB/expression indexes where useful
- Add transaction and locking strategy.
- Add provider integration tests.

Exit criteria:

- Same document/index tests pass on SQLite, SQL Server, and PostgreSQL.
- Provider differences are isolated to provider packages.
- Startup race conditions are handled.

## Phase 5: MongoDB Provider

Goal: prove native document database support.

Tasks:

- Implement MongoDB document store.
- Decide shared collection versus collection-per-type defaults.
- Map storage units to collections.
- Map declared indexes to MongoDB indexes.
- Add optimistic concurrency.
- Add basic transaction support when available.
- Add provider capability checks for unsupported relational assumptions.

Exit criteria:

- Same document/index tests pass on MongoDB.
- MongoDB uses native indexes.
- No relational tables/index assumptions leak into the document provider.

## Phase 6: Elsa Integration

Goal: consume the general packages from Elsa.

Tasks:

- Add Elsa-specific registration package.
- Let modules register manifests through module features.
- Add startup materialization service.
- Add shell feature support.
- Add options for selected provider and connection settings.
- Add diagnostics endpoint for applied manifests and provider status.

Exit criteria:

- An Elsa module can declare storage intent without EF Core.
- Elsa startup materializes the selected provider.
- Applied versions are observable.

## Phase 7: First Elsa Module Migration

Goal: migrate one real module safely.

Recommended module: Secrets.

Tasks:

- Implement Secrets document store.
- Preserve existing API behavior.
- Add import path from EF Core Secrets tables if needed.
- Add dual-read or explicit migration utility if required.
- Add provider tests for SQLite, SQL Server, and MongoDB.

Exit criteria:

- Secrets works without EF Core migrations.
- Existing tests pass.
- Provider-specific persistence packages are no longer needed for Secrets vNext.

## Phase 8: Runtime-Defined Entities

Goal: let admins define entities at runtime.

Tasks:

- Define `RuntimeEntityDefinition`.
- Add draft/published/retired lifecycle.
- Add validation rules.
- Add index definitions.
- Add provider capability checks before publish.
- Materialize published definitions.
- Generate CRUD endpoints.
- Expose admin UI integration points.
- Add audit trail for schema changes.

Exit criteria:

- Admin can define an entity.
- System validates and publishes it.
- Data can be created, queried by indexed fields, updated, and deleted.
- No physical table per entity is required by default.

## Phase 9: Physicalization And Performance

Goal: optimize hot entities and indexes without giving up portability.

Tasks:

- Add storage policy:
  - `PortableDocument`
  - `OptimizedIndexes`
  - `NativePhysicalized`
- Add provider-specific physicalization planners.
- Add operational commands to promote/demote indexes.
- Add benchmarks.
- Add guidance for when to physicalize.

Exit criteria:

- Portable defaults remain simple.
- Hot-path indexes can be optimized.
- Provider-specific behavior is explicit and observable.

## Phase 10: Workflow Runtime Evaluation

Goal: determine whether high-throughput workflow runtime persistence should use vNext.

Tasks:

- Benchmark workflow instance queries.
- Benchmark bookmark and trigger queries.
- Evaluate lock/concurrency semantics.
- Evaluate event/log volume storage.
- Decide where specialized persistence remains better.

Exit criteria:

- Clear decision for runtime stores.
- No premature migration of hot workflow internals.

Likely outcome:

- Management/catalog/metadata stores are good candidates.
- Runtime execution hot paths may need specialized providers or optimized physicalization.

## Phase 11: Hardening

Goal: make the framework production-safe.

Tasks:

- Startup locking.
- Transaction strategy.
- Retry strategy.
- Idempotency guarantees.
- Version history repair tools.
- Provider diagnostics.
- Backup/restore guidance.
- Multi-tenant isolation model.
- Security and permission model for runtime schemas.
- Roll-forward migration guidance.

Exit criteria:

- Safe concurrent startup.
- Clear recovery story.
- Provider failures are diagnosable.

## Phase 12: General-Purpose OSS Extraction

Goal: publish the reusable framework outside Elsa.

Tasks:

- Finalize package names.
- Remove Elsa-specific concepts.
- Add examples:
  - ASP.NET Core modular monolith
  - SQLite
  - SQL Server
  - MongoDB
- Add documentation.
- Add provider compatibility matrix.
- Define semantic versioning policy.

Exit criteria:

- Elsa consumes the general packages.
- Non-Elsa sample app works.
- Public API is stable enough for external users.

## Migration Strategy For Elsa

1. Keep current EF Core persistence intact.
2. Introduce vNext side-by-side.
3. Use vNext for new experimental modules first.
4. Migrate small modules next.
5. Provide import utilities for existing data.
6. Avoid dual-write unless absolutely necessary.
7. Do not migrate runtime hot paths until benchmarks justify it.

## Major Risks

- Query abstraction grows too large.
- Provider capabilities become hard to reason about.
- Document indexes become expensive on relational providers.
- Runtime-defined entities introduce product/security complexity.
- Automatic diffs become unreliable.
- Physicalization policies create operational burden.
- MongoDB and relational transaction semantics diverge.

## Key Decisions To Make Early

- Package naming and ownership.
- Whether this starts as Elsa-internal or separate repo.
- First production Elsa module.
- Provider priority order.
- Whether PostgreSQL or SQL Server follows SQLite.
- MongoDB shared collection versus collection-per-type default.
- How much query expressiveness is allowed in portable mode.
- Runtime entity UI scope.

## Suggested Execution Order

1. Finish core abstractions in the POC.
2. Build SQLite document store.
3. Convert Secrets to use the document store.
4. Add SQL Server provider.
5. Add MongoDB provider.
6. Add Elsa integration.
7. Add runtime-defined entities.
8. Add physicalization policies.
9. Decide whether to extract as OSS.

## Success Criteria

This initiative succeeds if:

- modules declare persistence once
- provider-specific migrations are no longer required per module
- relational and MongoDB providers both work
- runtime-defined entities can be stored without table-per-entity
- portable queries are predictable and index-driven
- hot paths have opt-in physicalization
- Elsa remains database agnostic
- the reusable core stays small enough to understand

# Persistence vNext POC

## Goal

This proof of concept explores whether Elsa modules can declare persistence intent once and let database providers own physical schema materialization. The immediate target is reducing the maintenance cost of per-module EF Core migrations across SQLite, SQL Server, PostgreSQL, MySQL, and Oracle.

## Current Pain

The existing EF Core stack keeps logical module configuration in module packages, while migrations live in provider packages. That works, but every schema change fans out into provider-specific migrations and model snapshots. The Secrets module is a compact example: a single logical `Secrets` table is represented by one EF model configuration and five provider-specific initial migrations.

## Proposed Model

The POC introduces four layers:

1. Module descriptors: modules expose `IPersistenceSchemaProvider` and return versioned provider-neutral storage-unit, field, key, and index descriptors.
2. Provider-family planning: a planner turns the same descriptors into a family-specific plan. The POC has relational and document planners.
3. Provider rendering/materialization: a provider renders or executes physical operations. The POC implements SQLite SQL rendering/execution and SQL Server render-only output.
4. Schema history: a provider records applied descriptor versions so startup materialization can be idempotent.

The core extension points are intentionally small:

- `IPersistenceSchemaProvider`
- `IPersistenceSchemaPlanner<TPlan>`
- `IPersistenceSchemaRenderer<TPlan>`
- `IPersistenceSchemaMigrator<TContext>`

In this model, the module owns storage intent:

- storage unit name
- namespace
- schema version
- fields
- logical types
- nullability
- lengths
- keys
- indexes

The provider owns physical details:

- whether a storage unit becomes a table, collection, bucket, container, or something else
- SQL type names
- identifier quoting
- whether schemas are supported
- document collection/index options
- BSON/JSON naming conventions
- generated DDL or migration operations
- execution strategy
- schema history storage

## POC Scope

The POC models the Secrets storage unit through `SecretPersistenceSchemaProvider`. It does not replace the current EF Core repository or migrations. Tests prove that the Secrets module can declare storage intent once, SQLite can render and execute a relational table, SQL Server can render provider-specific relational DDL from the same descriptor, a document planner can produce a collection/index plan, and SQLite can record applied schema versions.

SQLite records versions in `ElsaSchemaVersions`:

- `SchemaName`
- `Version`
- `AppliedAt`

`SqliteSchemaVersionRunner` creates the history table if needed, checks the highest version for the descriptor schema name, executes rendered DDL only when the descriptor version is newer, and records the applied version in the same transaction.

## Document Databases

Document database providers should not consume relational table descriptors directly. They should consume the provider-neutral storage-unit descriptors and produce a document plan:

- collection name
- namespace/database/tenant partition where applicable
- document fields
- identity field
- secondary indexes
- unique indexes
- provider-specific options such as TTL, collation, shard keys, or JSON/BSON schema validation

MongoDB, for example, would implement a planner/materializer that maps `PersistenceStorageUnit` to collection and index creation. Data-shape changes would be handled by versioned migration operations, not by relational DDL.

## General-Purpose Reuse

This could be extracted into a reusable framework, but the reusable boundary should be below Elsa-specific module wiring:

- a core package for descriptors, version metadata, planning interfaces, and migration result contracts
- provider-family packages for relational and document planning primitives
- provider packages for SQLite, SQL Server, MongoDB, PostgreSQL, etc.
- optional host integration packages for Elsa, ASP.NET Core, or worker services

The reusable core should not depend on Elsa features, EF Core, CShells, or any specific database driver. Elsa would then be a consumer that registers schema providers from modules and selects provider implementations.

## Known Gaps

This POC intentionally does not solve schema diffing, data migrations, provider-specific advanced indexes, foreign keys, optimistic concurrency, or integration with Elsa shell features. The descriptor now includes a schema version and SQLite consumes it, but the runner applies the current full descriptor rather than a chain of per-version operations.

## Viability

The approach looks viable for simple module-owned storage units and indexes. Rendering both SQLite and SQL Server plus planning a document collection from the same Secrets descriptor suggests the module/provider boundary is useful. The SQLite version runner shows that provider-owned materialization can be idempotent without EF Core migrations. The main challenge remains evolution: Elsa would need a versioned descriptor/migration story so providers can apply each version safely, including non-idempotent changes and data backfills, before attempting automatic diffs.

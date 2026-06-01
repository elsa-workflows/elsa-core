# Persistence

Elsa uses replaceable stores. Most features default to memory stores, then provider packages replace those stores with EF Core or other persistence implementations. Diagnostics structured logs also have a separate relational/SQLite persistence path that deliberately does not use EF Core.

## Store Replacement Pattern

Feature classes expose store factories. Persistence features replace those factories during `Configure()`.

Example from [EFCoreWorkflowRuntimePersistenceFeature](../../src/modules/Elsa.Persistence.EFCore/Modules/Runtime/WorkflowRuntimePersistenceFeature.cs):

- replace `WorkflowRuntimeFeature.TriggerStore`
- replace `BookmarkStore`
- replace `BookmarkQueueStore`
- replace `WorkflowExecutionLogStore`
- replace `ActivityExecutionLogStore`
- replace key-value store

Management persistence follows the same idea for definition and instance stores.

## EF Core Shared Infrastructure

Shared EF Core infrastructure lives in [Elsa.Persistence.EFCore.Common](../../src/modules/Elsa.Persistence.EFCore.Common) and [Elsa.Persistence.EFCore](../../src/modules/Elsa.Persistence.EFCore).

Important base types:

- [PersistenceFeatureBase](../../src/modules/Elsa.Persistence.EFCore.Common/PersistenceFeatureBase.cs)
- [PersistenceShellFeatureBase](../../src/modules/Elsa.Persistence.EFCore.Common/PersistenceShellFeatureBase.cs)
- [CombinedPersistenceShellFeatureBase](../../src/modules/Elsa.Persistence.EFCore.Common/CombinedPersistenceShellFeatureBase.cs)
- [ElsaDbContextBase](../../src/modules/Elsa.Persistence.EFCore.Common/ElsaDbContextBase.cs)

`PersistenceFeatureBase` registers `IDbContextFactory<TDbContext>`, migration options, tenant-aware context factory decoration, and tenant model handlers.

## EF Core Module Slices

The shared EF Core module contains slices for:

- [Management](../../src/modules/Elsa.Persistence.EFCore/Modules/Management)
- [Runtime](../../src/modules/Elsa.Persistence.EFCore/Modules/Runtime)
- [Identity](../../src/modules/Elsa.Persistence.EFCore/Modules/Identity)
- [Tenants](../../src/modules/Elsa.Persistence.EFCore/Modules/Tenants)
- [Labels](../../src/modules/Elsa.Persistence.EFCore/Modules/Labels)
- [Alterations](../../src/modules/Elsa.Persistence.EFCore/Modules/Alterations)

Each slice has a DbContext, configurations, store implementations, feature classes, and shell feature classes.

## Provider Packages

Provider packages configure database-specific EF Core options and migrations:

- [Elsa.Persistence.EFCore.Sqlite](../../src/modules/Elsa.Persistence.EFCore.Sqlite)
- [Elsa.Persistence.EFCore.SqlServer](../../src/modules/Elsa.Persistence.EFCore.SqlServer)
- [Elsa.Persistence.EFCore.PostgreSql](../../src/modules/Elsa.Persistence.EFCore.PostgreSql)
- [Elsa.Persistence.EFCore.MySql](../../src/modules/Elsa.Persistence.EFCore.MySql)
- [Elsa.Persistence.EFCore.Oracle](../../src/modules/Elsa.Persistence.EFCore.Oracle)

Combined provider shell features such as `SqliteWorkflowPersistenceShellFeature` let modular hosts configure workflow persistence once and share settings with dependent definition, instance, and runtime persistence features.

## Typical Host Configuration

The reference server configures SQLite persistence for management and runtime separately:

```csharp
elsa.UseWorkflowManagement(management =>
{
    management.UseEntityFrameworkCore(ef => ef.UseSqlite());
});

elsa.UseWorkflowRuntime(runtime =>
{
    runtime.UseEntityFrameworkCore(ef => ef.UseSqlite());
});
```

See [src/apps/Elsa.Server.Web/Program.cs](../../src/apps/Elsa.Server.Web/Program.cs).

## Migrations

EF Core migrations are controlled by feature options such as `RunMigrations`. The base persistence feature registers startup tasks that run migrations when enabled.

Migration-related files:

- [MigrationOptions](../../src/modules/Elsa.Persistence.EFCore.Common/MigrationOptions.cs)
- [RunMigrationsStartupTask](../../src/modules/Elsa.Persistence.EFCore.Common/RunMigrationsStartupTask.cs)
- [scripts/migrations/README.md](../../scripts/migrations/README.md)

When adding an entity or changing persisted shape, check every provider package and test provider-specific migration behavior where practical.

## Tenant Awareness

EF Core persistence decorates `IDbContextFactory<TDbContext>` with `TenantAwareDbContextFactory<TDbContext>` and registers model/saving handlers:

- `ApplyTenantId`
- `SetTenantIdFilter`

Tenant conventions are documented in ADRs:

- [ADR 0008: Empty String As Default Tenant ID](../adr/0008-empty-string-as-default-tenant-id.md)
- [ADR 0009: Asterisk Sentinel Value For Tenant-Agnostic Entities](../adr/0009-asterisk-sentinel-value-for-tenant-agnostic-entities.md)

## Structured Log Persistence

Structured log persistence is intentionally separate from EF Core. The active feature plan is [005 structured log persistence](../../specs/005-structured-log-persistence/plan.md).

Packages:

- [Elsa.Diagnostics.StructuredLogs](../../src/modules/Elsa.Diagnostics.StructuredLogs): core capture, API, hub, provider/store contracts, default in-memory store.
- [Elsa.Diagnostics.StructuredLogs.Persistence.Relational](../../src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational): provider-neutral relational store, SQL builder, mapper, retention service, write buffer.
- [Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite](../../src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite): SQLite connection factory, dialect, FluentMigrator runner, startup migration/cleanup service.

This path uses explicit SQL and FluentMigrator. It stores timestamps as UTC ISO-8601 text and JSON payloads as text in SQLite.

## Modular Persistence vNext

Modular persistence vNext uses explicit document stores and provider packages rather than EF Core. The portable relational schema stores document payloads in `ModularPersistenceDocuments` and declared index values in `ModularPersistenceDocumentIndexes`.

Relational provider packages create the portable tables and indexes by default:

- [Elsa.ModularPersistence.Sqlite](../../src/modules/Elsa.ModularPersistence.Sqlite)
- [Elsa.ModularPersistence.SqlServer](../../src/modules/Elsa.ModularPersistence.SqlServer)
- [Elsa.ModularPersistence.PostgreSql](../../src/modules/Elsa.ModularPersistence.PostgreSql)

SQL Server and PostgreSQL expose provider-specific optimized index switches. These are off by default and require both provider opt-in and a manifest index with `PhysicalizationIntent.OptimizedIndexes`.

- SQL Server: `SqlServerModularPersistenceOptions.UseOptimizedIndexes`
- PostgreSQL: `PostgreSqlModularPersistenceOptions.UseOptimizedJsonbIndexes`

Schema materialization runs in a transaction. SQL Server takes a transaction-scoped `sp_getapplock`; PostgreSQL takes a transaction-scoped advisory lock. The locks serialize startup materialization for the shared portable schema. PostgreSQL optimized JSONB expression indexes are created inside the same transaction, so concurrent index creation is intentionally not used by the materializer.

Shared relational contract tests live in [Elsa.ModularPersistence.Relational.IntegrationTests](../../test/integration/Elsa.ModularPersistence.Relational.IntegrationTests). SQLite runs locally by default. SQL Server and PostgreSQL contract rows compile by default and execute only when these environment variables point at dedicated test databases:

- `ELSA_MODULAR_PERSISTENCE_SQLSERVER_CONNECTION_STRING`
- `ELSA_MODULAR_PERSISTENCE_POSTGRESQL_CONNECTION_STRING`

The live contract fixtures drop the modular persistence test tables before each test.

The MongoDB provider is non-relational and does not use the relational document/index tables:

- [Elsa.ModularPersistence.MongoDb](../../src/modules/Elsa.ModularPersistence.MongoDb)

`MongoDbModularPersistenceOptions.CollectionStrategy` defaults to `SharedCollection`, which stores all document types in `SharedCollectionName` and discriminates documents with the `Type` field. `CollectionPerType` stores each document type in a sanitized collection named with `CollectionPerTypePrefix`.

MongoDB stores the original document JSON in `DataJson` for round-tripping and stores parsed BSON in `Data` for native filtering and indexing. Declared storage indexes become native MongoDB indexes over `Data.<field>` paths, prefixed by `Type` and `TenantId` in shared collection mode and by `TenantId` in collection-per-type mode. This keeps MongoDB-facing contracts free of relational table and generic-index assumptions.

MongoDB write operations use single-document atomic writes by default. `MongoDbModularPersistenceOptions.TransactionMode` can be set to `TransactionPerWrite` for deployments that support MongoDB transactions.

MongoDB integration contract tests compile by default and execute only when `ELSA_MODULAR_PERSISTENCE_MONGODB_CONNECTION_STRING` points at a MongoDB instance. The tests create and drop a unique test database.

## Adding A Store

When adding a new store implementation:

1. Identify the feature contract that owns the store.
2. Keep the core module provider-neutral.
3. Add the concrete store in the persistence/provider module.
4. Replace the feature's store factory in the persistence feature.
5. Add unit tests for store-specific query behavior if logic is nontrivial.
6. Add integration tests for provider behavior, migrations, and multi-target concerns where practical.

## Persistence Risk Checklist

- Does the change affect multiple target frameworks?
- Does it need provider-specific migrations?
- Does it preserve tenant filtering?
- Does it update both definition and instance stores if both shapes changed?
- Does it require API/client DTO updates?
- Does it alter runtime recovery, bookmark, or trigger semantics?
- Does it need retention, cleanup, or migration documentation?

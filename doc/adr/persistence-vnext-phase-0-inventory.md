# Persistence vNext Phase 0 Inventory

Issue: https://github.com/elsa-workflows/elsa-core/issues/7580

This inventory records the initial Phase 0 findings for Persistence vNext. It is intentionally scoped to the current Elsa persistence surface and the first implementation risks that should shape the vNext contracts.

## Current Persistence Surface

The shared EF Core persistence base is under `src/modules/Elsa.Persistence.EFCore.Common`:

- `Store.cs`
- `EntityStore.cs`
- `ElsaDbContextBase.cs`

The current pattern is short-lived EF contexts from `IDbContextFactory`, `IQueryable` filter delegates, `AsNoTracking`, `ExecuteDeleteAsync`, and provider-specific bulk insert/upsert support.

The EF-backed module stores live under `src/modules/Elsa.Persistence.EFCore/Modules`:

- `Management`
- `Runtime`
- `Identity`
- `Labels`
- `Alterations`
- `Tenants`

Provider packages currently include:

- `src/modules/Elsa.Persistence.EFCore.Sqlite`
- `src/modules/Elsa.Persistence.EFCore.SqlServer`
- `src/modules/Elsa.Persistence.EFCore.PostgreSql`
- `src/modules/Elsa.Persistence.EFCore.MySql`
- `src/modules/Elsa.Persistence.EFCore.Oracle`
- `src/modules/Elsa.Persistence.EFCore.Common`

Provider migrations are duplicated per provider and module, especially for Alterations, Identity, Labels, Management, Runtime, and Tenants.

## Non-EF Relational Precedent

The strongest existing non-EF relational precedent is diagnostics structured log persistence:

- `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational`
- `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite`

Useful concepts to generalize into Persistence vNext:

- ADO.NET connection factory
- SQL dialect abstraction
- Raw command/parameter construction
- FluentMigrator-based schema setup
- SQLite startup service
- Temp SQLite integration test host with `IAsyncDisposable`

The structured-log write buffer, retention behavior, and query builder are domain-specific and should not be generalized in the first slice.

## Store Classification

Document-friendly candidates:

- Workflow definitions: scalar metadata plus serialized `Data`; bounded queries; has memory-store comparison.
- Alteration plans/jobs: serialized alterations, filters, and logs.
- Secrets: naturally document-shaped, but encryption/no-reveal behavior adds product risk.
- Workflow instances: document-shaped state, but runtime write path is hotter.

Relational-specific candidates:

- Labels, especially workflow-definition label joins.
- Identity users, roles, and applications.
- Tenants.
- Provider-specific workflow reference query services.

Hot-path candidates to avoid first:

- Runtime triggers.
- Bookmarks and bookmark queues.
- Dead-letter queues.
- Workflow and activity execution logs.
- Key-value dispatch/outbox paths.
- Workflow instance update/save paths.

## First Module Candidate

The roadmap names Secrets as the recommended first candidate. Phase 0 inspection found a strong alternative: `IWorkflowDefinitionStore` / `EFCoreWorkflowDefinitionStore`.

Workflow definitions are attractive because they are document-friendly, central to Elsa, less write-hot than workflow instances/bookmarks/logs, and already separate scalar query metadata from serialized payload state. The existing memory store gives a comparison point for behavior.

Relevant paths:

- `src/modules/Elsa.Workflows.Management/Contracts/IWorkflowDefinitionStore.cs`
- `src/modules/Elsa.Workflows.Management/Stores/MemoryWorkflowDefinitionStore.cs`
- `src/modules/Elsa.Persistence.EFCore/Modules/Management/WorkflowDefinitionStore.cs`
- `src/modules/Elsa.Persistence.EFCore/Modules/Management/Configurations.cs`
- `src/modules/Elsa.Persistence.EFCore/Modules/Management/DbContext.cs`

Decision needed: keep Secrets as the first migration target from the roadmap, or select workflow definitions as the first production module candidate based on the Phase 0 evidence.

## Early Risks

- `ManagementElsaDbContext` currently covers both workflow definitions and workflow instances, so migrating only definitions needs a partial coexistence strategy.
- Current store filters are `IQueryable`-centric and must become declared portable indexes or provider-specific adapters.
- Tenant behavior currently relies on EF query filters and save handlers; vNext must model tenant isolation explicitly.
- EF shadow properties such as `Data` and computed metadata need first-class document metadata.
- Provider search semantics differ. Existing `ToLower().Contains(...)` behavior should become an explicit query capability or module-specific fallback.
- Bulk upsert/delete and duplicate-key handling differ by provider and should not be assumed portable until contract tests prove it.
- Hot workflow runtime paths need benchmarks before migration.

## Initial Provider Order

Recommended order:

1. SQLite, to prove the local document/index MVP.
2. SQL Server or PostgreSQL, to prove a major relational provider after the SQLite contract stabilizes.
3. MongoDB, to prove native document support after query and capability contracts are stable.

SQL Server and PostgreSQL can be parallelized only after the shared document/query contract tests are stable.

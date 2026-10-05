# Extensions release reconciliation and Core catchup

Tracker: Elsa Core #8623. The reconciled Extensions source preserves both original parents through a normal merge of release/3.9 into main. The subsequent cleanup removes one duplicate Dapper friend-assembly item introduced by the automatic project merge.

Source main: `98a185a303b6c96347d25c780528ce37f2f864a3`.
Release/3.9: `89d4eb9b739ae135604aac2a1de18653a289bdb0`.
Reconciled source: `b76f8fd216fdc529d41fbf39fd712d3fcd4e80fd`.
Previous imported source: `3cf502955791cbd20375022ef4597f0e195faf8a`.
Core mapping base: `91a1c0723`.

All stable 3.8 tags (3.8.0, 3.8.1, 3.8.2, 3.8.4), their release branch tips, and release/3.9 are ancestors of the reconciled source. No 3.8.3 tag exists in the inspected source repository. Main's package workflow and Directory.Build.props are unchanged, retaining base_version 3.10.0 and Elsa dependencies 3.10.0-preview.5760 / Studio 3.10.0-preview.1799.

## Source conflict dispositions

| Conflict path | Resolution |
| --- | --- |
| .github/workflows/packages.yml | Keep main publication settings and 3.10 base version. |
| Directory.Build.props | Keep main 3.10 preview dependency pins. |
| Dapper Modules/Runtime/Stores/KeyValueStore.cs | Keep main atomic delete method and preview-compatible documentation. |
| MongoDb Modules/Runtime/KeyValueStore.cs | Keep main scoped atomic delete method and preview-compatible documentation. |
| MongoDb Modules/Management/WorkflowInstanceStore.cs | Apply release scoped interruption through MongoDbStore.UpdateOneAsync while retaining finished-row refusal. |
| Elsa.Dapper.UnitTests/DapperWorkflowInstanceStoreTests.cs | Combine release cutoff/filter cases with main stronger cross-tenant regression and parameterized tenant seed helper. |
| Elsa.MongoDb.UnitTests/MongoKeyValueStoreTryDeleteTests.cs | Keep main concrete-store calls and deterministic historical race baseline compatible with source preview dependency. |
| Elsa.MongoDb.UnitTests/MongoWorkflowInstanceStoreTests.cs | Keep main exact recursive Id-filter assertions. |
| Elsa.Persistence.Dapper.UnitTests/DapperKeyValueStoreTryDeletePostgreSqlTests.cs | Keep main concrete-store coverage against migration-built PostgreSQL. |
| Elsa.Persistence.Dapper.UnitTests/DapperKeyValueStoreTryDeleteTests.cs | Keep main concrete-store and SQLite race baseline coverage. |
| Elsa.Persistence.Dapper.UnitTests/DapperPostgreSqlMigrationTests.cs | Include release BeforeLastUpdated coverage. |
| Elsa.Persistence.Dapper.UnitTests/NonPgQuerySqlSnapshotTests.cs | Include release exclusive less-than snapshots. |
| Elsa.Persistence.Dapper.UnitTests/PostgreSqlDialectTests.cs | Include release quoted less-than assertions. |

## Core mapping

`extensions-catchup-dispositions.json` accounts for every changed source path using the established destination mapping in `scripts/integration-program/rehearse-import.py`. Root-level settings, workflows, solution metadata, and instructions are refreshed only as inert `.source` snapshots. Current Core build/publication configuration remains authoritative.

The live slice adds the existence-guarded Dapper runtime migration 20008 (KeyValues and BookmarkQueueItems.SerializedOptions), atomic tenant-scoped TryDelete implementations, default-tenant NULL/empty compatibility, corrected prefix parameter binding, BeforeLastUpdated filtering, Mongo interruption/summary tenant scoping, DI-backed variable serialization, and JsonNode subtype/legacy compatibility. Imported regression coverage exercises migration-built SQLite/PostgreSQL and MongoDB containers. Core atomic-delete tests dispatch through IKeyValueStore because Core already supplies that API.

Existing Core project-reference/SSH.NET compatibility adjustments and MigrationDatabases.QuotedIdentifiers/UnquotedIdentifiers are preserved. Identity V3_10 remains untouched. Files already equal to the reconciled source are recorded without rewriting them.

Verification is recorded by the integrating lead after source and mapped Core test execution. No publication or cutover is performed by this local slice.

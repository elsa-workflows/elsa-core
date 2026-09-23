# Dapper atomic workflow updates in the consolidated build

Program #8194, story #8286, task #8293. This is a source patch for the disposable consolidated build, to be incorporated into the history-preserving import. It is not a published package or a completed source import. Apply it after #8291's build preparation, against its exact Core/Extensions/Studio pins.

## Behavior

`DapperWorkflowDefinitionStore.TryUpdateLatestAsync` loads the latest matching row inside a SERIALIZABLE transaction, checks the caller's expected state and invokes its update callback on that loaded row. It updates the same row or unmarks the former latest row and inserts a new draft in that transaction. Failed insertion rolls back the unmark. The selected row's tenant and logical definition cannot change. `ToolVersion`, which is absent from the public workflow entity, is preserved.

The newly injected tenant accessor is optional. A DI regression verifies default-tenant CAS updates and denial of another tenant when that service is absent. The generic store retains its existing registration contract.

The read uses the existing Dapper tenant convention (ambient tenant ID, or SQL NULL for the default context); an explicit tenant-agnostic filter can select another tenant but cannot move it. No new persisted concurrency stamp is required of older writers. SERIALIZABLE protects this operation from existing `Store.SaveAsync` writes during its transaction. An unconditional stale save issued *after* this operation commits remains unconditional; this method does not retrofit optimistic concurrency onto every legacy API.

SQLite reserves the writer before invoking the callback. SQL Server holds read/range locks through commit; competing upgrades can deadlock, aborting one transaction. PostgreSQL can allow a competing legacy writer to commit and then reject the serializable writer. SQLite BUSY/LOCKED, SQL Server deadlock victim 1205, and PostgreSQL serialization/deadlock aborts 40001/40P01 become `Conflict` after transaction disposal. Callback exceptions are not normalized. There is no automatic retry, especially after an ambiguous commit. Timeouts, transport failures, uniqueness errors and cancellation propagate. Database lock waits use the configured command/provider timeouts; `Conflict` does not imply a wait-free database implementation.

Official references, checked 2026-09-23:

- [Microsoft.Data.Sqlite transactions](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions).
- [SQL Server SERIALIZABLE locks](https://learn.microsoft.com/en-us/sql/t-sql/statements/set-transaction-isolation-level-transact-sql?view=sql-server-ver17).
- [PostgreSQL isolation](https://www.postgresql.org/docs/17/transaction-iso.html) and [serialization failure handling](https://www.postgresql.org/docs/18/mvcc-serialization-failure-handling.html).

## Existing PostgreSQL defects exposed by execution

The real PostgreSQL test initially failed because `PostgreSqlDialect.Upsert` omitted the primary key from INSERT, although the query builder supplies its parameter. The patch includes it. Version selection also emitted integer literals against Boolean columns; it now binds Boolean parameters for draft/latest/published and combined options. Public package identities remain unchanged.

Evidence receipt: [dapper-atomic-updates-evidence.json](dapper-atomic-updates-evidence.json), including exact source/file/patch hashes and test counts. A clean patch replay produced identical source bytes and passed all 29 SQLite tests.

## Verification

Each contract test creates a uniquely named synthetic table and drops only that table. SQLite uses a fresh temporary database. PostgreSQL and SQL Server use dedicated loopback Docker instances with synthetic credentials. The table fixture models workflow record columns; it does not establish migration compatibility for all historical schemas.

| Check | Result |
|---|---|
| Dapper project build, net8/net9/net10 | Passed, 0 errors, 30 warnings |
| Existing Dapper tests plus new cases, SQLite net10 | 29 passed, 0 skipped |
| PostgreSQL 17 contract cases, net10 | 16 passed, 0 skipped |
| SQL Server 2022 contract cases, net10 | 16 passed, 0 skipped |

Cases cover current metadata and `ToolVersion`, failed preconditions, missing rows, new drafts, rollback after duplicate insertion, a legacy writer during the transaction, two-worker contention, tenant isolation/preservation, tenant-change rejection, and callback-thrown database errors, and all five version-filter modes. A SQL Server run with a one-second command timeout propagated the timeout before deadlock detection; a 15-second bound passed. The final two-worker test uses a barrier on PostgreSQL/SQL Server so both callbacks see the old row before either writes, directly exercising an aborted-writer Conflict. The separate legacy-writer case asserts timeout -2 while SQL Server holds the read locks. No timeout was relabeled as safe contention.

Images:

- PostgreSQL: `postgres@sha256:67f41722b7a8cbdb868a44a4995c846eddfdc2973bccb291ce937dce88ad5675` (arm64).
- SQL Server: `mcr.microsoft.com/mssql/server@sha256:4402d880dd4c34bfa7d8705e56a86cd6c88da80a1f6bbbe741f999e76264a090` (linux/amd64).

Warnings include existing nullable findings and missing SourceLink in the intentionally remote-free rehearsal. These tests do not pack/publish packages. Oracle/MySQL/custom Dapper providers remain unverified; their serializable transaction and exception behavior require evidence before equivalent support is claimed.

## Reproduce

Prepare a fresh disposable consolidated rehearsal using #8291. From that repository, apply the artifact using its absolute path:

```sh
git apply --check /path/to/elsa-core/scripts/integration-program/consolidated-build/dapper-atomic-updates.patch
git apply /path/to/elsa-core/scripts/integration-program/consolidated-build/dapper-atomic-updates.patch
dotnet build src/extensions/persistence/Elsa.Persistence.Dapper/Elsa.Persistence.Dapper.csproj
dotnet test test/extensions/modules/persistence/Elsa.Persistence.Dapper.UnitTests/Elsa.Persistence.Dapper.UnitTests.csproj -f net10.0
```

The suite defaults to SQLite. For a dedicated synthetic PostgreSQL instance set `ELSA_DAPPER_CAS_PROVIDER=postgres` and `ELSA_DAPPER_CAS_CONNECTION` to its connection string. For dedicated synthetic SQL Server use `sqlserver` and `Command Timeout=15` or the normal larger bound. Run the same test command with `--filter FullyQualifiedName~DapperWorkflowDefinitionStoreTests`. Never commit real connection strings. A requested unavailable provider fails rather than silently skipping.

## Integration gate

Keep #8293/#8287 open until this patch is incorporated into actual consolidated source, the complete declared-framework solution builds with Mongo's implementation, and provider tests pass in that final layout. The Core caller fix is separate (#8292/#8295). Do not publish the synthetic rehearsal commit: the real import must retain original ancestors and reviewed transformations.

# Dapper atomic workflow updates in the draft import

Program #8194, story #8286, task #8293. This records how the reviewed [Dapper atomic-update patch](dapper-atomic-updates.md) from PR #8297 was incorporated into the history-preserving draft import. It is not a package release. No package was published and no publisher workflow changed.

## Starting point

The draft import head is `1976dd2d4781bad44f13384881e3f00605ba44f9`. At that head, `DapperWorkflowDefinitionStore` already had a `TryUpdateLatestAsync` from Extensions `ba8b71d91` (#216). The imported files match the Extensions `807cd893` tip byte-for-byte; the [fifth source-tip receipt](source-tip-refresh-2026-09-26-r5.json) pins that tip.

The whole reviewed patch does not apply to this head, but only the store hunk actually fails. The dialect, query-extension and new-test hunks apply cleanly on their own; the store hunk fails in both directions because #216 had already added a method in the same place.

The reviewed result was rebuilt by applying the patch to the Extensions `33fa0bfd2` sources it was made against. All four resulting file hashes match [the reviewed evidence](dapper-atomic-updates-evidence.json).

## Differences between the imported code and the reviewed result

Classes: (a) harmless adaptation to later upstream changes, (b) reviewed behaviour missing from the import, (c) new behaviour nobody reviewed.

| File | Imported code compared with the reviewed result | Class | Now |
|---|---|---|---|
| `PostgreSqlDialect` | Doc comment says "SQL Server dialect"; upsert leaves the primary key out of the INSERT | b | Reviewed bytes |
| `ParameterizedQueryBuilderExtensions` | Version filters compare with integer literals, not Boolean parameters | b | Reviewed hunk |
| `ParameterizedQueryBuilderExtensions` | Adds an `Update(table, record, fields)` overload (Extensions #231, with #237/`807cd893` dialect support) | a | Kept |
| Store constructor | No `IDbConnectionProvider`; no optional `ITenantAccessor` | b | Reviewed |
| Store transaction | Read and writes use separate connections with no transaction. Two writers can both pass the check, and a failed draft insert leaves no latest row | b | One SERIALIZABLE transaction |
| Store selection | Ignores `TenantAgnostic`; no explicit tenant predicate | b | Reviewed |
| Store writes | Uses `SaveAsync` upserts: replaces the row's tenant with the ambient one, writes the whole stale row back, and silently overwrites an existing draft ID | b | Conditional UPDATE, or unmark plus INSERT |
| Store errors | No Binary parameter typing, no contention-to-Conflict mapping, no cancellation check before commit | b | Reviewed |
| Store selection | A superseded version returns Conflict, not NotFound | c | Kept (below) |
| Tenant/definition guard, `ToolVersion` preservation | Same behaviour | – | Reviewed |
| Tests | Reviewed test file missing | b | Ported |

Kept (c) behaviour: the memory, EF Core and MongoDB stores return Conflict when a filter matches a version that is no longer latest. The imported upstream test `DapperWorkflowDefinitionStoreCompareAndSwapTests` asserts the same thing.

The reviewed latest-only read is unchanged. When it finds nothing, a second read in the same transaction and tenant scope, without the latest-only condition, decides the result: Conflict if a superseded version matches, NotFound otherwise. A `VersionOptions.Latest` filter, which the BPMN document PUT uses, behaves exactly as reviewed. The added test `SupersededVersionIsConflictOnlyInsideTenantScope` checks both directions: a superseded version in scope gives Conflict, and one outside the tenant gives NotFound.

## Changes

- Commit `2d5d7d1e08773f20edb89c6ccad25b8c81a2e743` makes the store, dialect and query-extension changes above.
  - It ports the reviewed tests to their reviewed path in `test/extensions/modules/persistence/Elsa.Persistence.Dapper.UnitTests`. That project is part of `Elsa.sln` and `Elsa.Extensions.slnf`.
  - Without the one added case, the ported file is byte-identical to the reviewed file (SHA-256 `0ce45370…`).
  - The upstream compare-and-swap test only gains the store's connection provider and tenant accessor as constructor arguments. Its assertions are unchanged.
- Commit `89162ff0e` adds the [sixth source-tip receipt](source-tip-refresh-2026-09-27-r6.json) and [verifier](../../../scripts/integration-program/verify_import_source_tip_refresh_r6.py). The delta changes two files that the first and second receipts pin at `HEAD`, and this receipt takes over their current bytes, following the precedent of #8501.
  - The verifier rebuilds the reviewed result from Extensions `33fa0bfd2` and the patch, and checks it against the recorded evidence hashes.
  - It then derives each changed file: the reviewed patch applied to the imported bytes, the superseded-version read, the constructor arguments, and the one added test case.
  - It pins the before and after blobs at the base, the delta and `HEAD`, the prior receipt digests, the publisher workflows, and solution membership.
  - The first verifier's `HEAD` check and the fifth verifier's prior-mapping check now skip only those two named paths. Those older receipts still pin them at their reviewed commits.
  - The r1, r2 and r5 command lines, and the mapped Slack proof, also run the sixth check.

## Verification

Results are recorded in [dapper-atomic-updates-import-evidence.json](dapper-atomic-updates-import-evidence.json), and the full output is in [the log directory](dapper-atomic-updates-import-logs/).

- PostgreSQL ran in `postgres@sha256:67f41722…` (17.11, linux/arm64).
- SQL Server ran in `mcr.microsoft.com/mssql/server@sha256:4402d880…` (2022 CU27, linux/amd64 emulated), with `Command Timeout=15`.
- Both were fresh loopback containers with synthetic credentials, removed after the runs, and no tables were left behind.

| Check | net10.0 | net9.0 |
|---|---|---|
| `Elsa.Persistence.Dapper.UnitTests`, SQLite (18 existing, 16 reviewed, 1 added) | 35/35 | 35/35 |
| Reviewed store cases plus the added case, PostgreSQL | 17/17 | 17/17 |
| Reviewed store cases plus the added case, SQL Server | 17/17 | 17/17 |
| `Elsa.Dapper.UnitTests`, no regression | 11/11 | – |
| `Elsa.Persistence.Dapper` build for net8.0, net9.0 and net10.0 | 0 errors, 65 existing warnings, none in changed files | |

Before the change, the two test projects passed 18/18 and 11/11 on net10.0. Earlier runs on the same source bytes passed PostgreSQL and SQL Server 17/17 five times on net10.0.

Two negative controls show the tests catch the old behaviour:
- SQLite with the imported method body and the new constructor: 4 of 35 fail (single winner, rollback, tenant scope, serializable read).
- PostgreSQL with the imported dialect and query extensions: all 17 fail, 16 with SQLSTATE 23502 (missing Id on insert) and 1 with 42883 (boolean = integer).

The net8.0 tests cannot run. The test projects target net10.0, and `Microsoft.AspNetCore.Mvc.Testing` 9.0.17, which `test/extensions/modules/Directory.Build.props` pulls in, does not support net8.0 (NU1202). net8.0 is covered by the project build only.

All six `verify_import_source_tip_refresh*.py` verifiers and all 30 `test_import_source_tip_refresh*` tests pass under both normal and `-O` Python. A throwaway commit on a disposable clone confirmed each gate fails when it should:
- Editing an r6-only file fails r1, r2, r5 and r6.
- Editing a non-superseded second-receipt file fails r5, and r1, r2 and r6 through the chain.
- Editing a non-superseded first-receipt file fails r1, and the others through the chain.
- Editing either superseded file fails through the r6 checks.

## Reproduce

```sh
python3 scripts/integration-program/verify_import_source_tip_refresh_r6.py
python3 -m unittest discover -s scripts/integration-program -p 'test_import_source_tip_refresh*.py'
P=test/extensions/modules/persistence/Elsa.Persistence.Dapper.UnitTests/Elsa.Persistence.Dapper.UnitTests.csproj
dotnet test $P -f net10.0
ELSA_DAPPER_CAS_PROVIDER=postgres ELSA_DAPPER_CAS_CONNECTION='<synthetic PostgreSQL>' \
  dotnet test $P -f net10.0 --filter 'FullyQualifiedName~DapperWorkflowDefinitionStoreTests'
ELSA_DAPPER_CAS_PROVIDER=sqlserver ELSA_DAPPER_CAS_CONNECTION='<synthetic SQL Server>;Command Timeout=15' \
  dotnet test $P -f net10.0 --filter 'FullyQualifiedName~DapperWorkflowDefinitionStoreTests'
dotnet restore $P -p:TargetFramework=net9.0 && dotnet test $P --no-restore -p:TargetFramework=net9.0
```

Never commit real connection strings. A requested provider that is unavailable fails rather than skipping.

## Limits

- The tests use a synthetic table that models the workflow record columns. They do not replay historical Dapper migrations.
- Oracle, MySQL and custom Dapper providers are unverified.
- The sixth receipt pins delta commit `2d5d7d1e`. If that commit is squashed or rebased away when the import lands, the receipt fails, and the landing change must record a new one.
- Metadata preservation in the BPMN caller relies on its full-snapshot predicate, which belongs to Core (#8292/#8295).
- The complete consolidated NUKE run and the mapped package proof at the final import head are separate gates.

# Issue #8101: TUnit/MTP component-test performance checkpoint

Date: 2026-09-14

Branch: `spike/8101-tunit-mtp`

Baseline commit: `bf5c9dd041e58ffafe9ccbfe1c8effec89d959ef`

## Executive result

This checkpoint addresses the dominant component-test setup cost without reducing TUnit's default parallelism or sharing mutable test state:

- SQL Server is started once per TUnit session.
- The four component schemas are migrated once into a session template, backed up, verified, and then restored into a unique physical database for every expanded test case.
- Each case still owns its host, service provider, scopes, clients, `DbContext` instances, files, locks, trackers, and database lifecycle.
- The 36 host-method metadata tests now materialize one immutable descriptor snapshot per class and dispose their application host before any test body runs.
- Three workflow-deletion tests release only their own orphaned execution-cycle handles after verifying the corresponding workflow instance is absent, removing a repeated 30-second host-drain delay.
- Unused global `Moq` and MVC Testing references were removed. Projects using TUnit web testing continue to receive MVC Testing through `TUnit.AspNetCore`; the mediator unit tests now declare the two `Microsoft.Extensions.*` packages they actually use.

Targeted validation is green, including concurrent database-isolation stress, multitenancy-provider semantics, host-method tests, deletion cleanup, and the known slow multi-pod test. A full Release solution build also succeeds with zero warnings and zero errors.

At the user's request, work stopped at this reviewable checkpoint before the post-change 205-test component run and exact solution-wide PR test command. Consequently, this document does **not** claim a final full-suite speedup or final 4,417-test result. Those two commands remain the release gate described below.

TUnit remains pinned at `1.66.27`; the resolved TRX reporter remains `2.3.3`. There are no `src/` changes, test caps, limiters, `NotInParallel` annotations, or broad serialization in this checkpoint.

## Measurement environment

Measurements were taken with warm package/build caches on:

- macOS 26.6.2 (25G83), Apple silicon `arm64`
- Apple M5 Pro, 18 logical/physical CPUs, 48 GiB memory
- .NET SDK 10.0.400 and runtime 10.0.11
- Docker client/server 29.7.2, Linux `arm64` containers
- TUnit 1.66.27 using Microsoft Testing Platform
- Europe/Minsk timezone

All reported TUnit durations come from native TUnit JSON reports. External wall time and peak RSS were collected with `/usr/bin/time -l` where noted. Raw local evidence was kept under `/tmp/elsa-tunit-perf-8101-20260914`; that location is intentionally ephemeral and is not part of the commit.

## Baseline

Two uncontaminated, no-build, full component samples completed with the expected 205 outcomes: 202 passed and 3 intentionally skipped.

| Sample | TUnit duration | External wall time | Peak RSS | Outcome |
|---|---:|---:|---:|---|
| Clean 1 | 453.808 s | 454.13 s | 977,092,608 B | 202 passed, 3 skipped |
| Clean 3 | 442.725 s | 443.89 s | 978,337,792 B | 202 passed, 3 skipped |

A second attempted sample overlapped with an external statistics process and was invalidated; a fourth was interrupted. Neither is included in the baseline.

The runner boundary is not the material cost: direct executable versus `dotnet test` overhead was about 0.30 seconds for a single component test, and external versus TUnit-reported time differed by 1.165 seconds (0.26%) in the clean full sample.

Representative non-component project medians were much smaller: 0.93 seconds for a 3-test unit project, 1.65 seconds for a 279-test unit project, and 4.67 seconds for a 305-test integration project. This supported prioritizing the component project.

## Bottleneck analysis

The clean 442.725-second report contained 202 executed tests. Its cumulative test lifetimes totalled 25,150,110 milliseconds because many tests ran concurrently. The cumulative time split was:

| Phase | Cumulative duration | Share | Median per test |
|---|---:|---:|---:|
| `App` before hooks | 24,043,303.905 ms | 95.599% | 143.081 s |
| Test bodies | 412,114.918 ms | 1.639% | about 0.143 s |
| Cleanup | 694,257.047 ms | 2.760% | 3.092 s |

There were 195 `App`-backed cases in 49 classes. The old model created 195 databases, 195 primary hosts, and ran four EF migration sets per case: 780 migration invocations. Two multi-pod tests created four additional hosts. TUnit's ASP.NET Core support admits server initialization through a process-wide eight-slot semaphore, so repeated setup progressed in waves; the median eight-case wave was 16.582 seconds. Up to 72 `App` setup spans overlapped while work queued behind that gate.

The two apparent 156–157-second test bodies were concurrency artefacts in the full run. Running the three-pod test alone produced a 13.147-second baseline body; with the template implementation it remained 13.222 seconds. The workflow behavior itself was not regressed.

Three `RelatedRecordsDeletionTests` cleanups each took about 33.08 seconds. Cancellation creates a replacement execution pipeline, while deletion removes the instance before that pipeline has another commit point at which its execution-cycle handle can be released. Host shutdown therefore waited for the default 30-second graceful-drain timeout.

## Reference architecture and adaptation

The read-only reference at `/Users/dendeline/github/flexitech/projects/wfe-tree/platform_orchestrator` uses TUnit 1.61.38 and session-scoped Redis, Kafka, S3, and PostgreSQL container fixtures. Every test still obtains unique resource names, creates its own PostgreSQL database, and owns fresh host/provider/scope/client state. Cleanup removes each test's topic, keys, bucket, database, and identity resources. It does not use Respawn, transactions, parallelism caps, or `NotInParallel`.

This checkpoint keeps that proven isolation boundary and changes only how the unique database is provisioned: Elsa restores a schema-only, migrated session image rather than creating an empty database and running four schema migrations for every test.

## Implemented database lifecycle

The session fixture now performs this sequence:

1. Start one SQL Server Testcontainer.
2. Create a dedicated template catalog.
3. Start a bootstrap-only component host that keeps pre-existing framework hosted services plus only the added catalog-provisioning, component-migration, and Elsa module-registry-cleanup services.
4. Confirm exactly one migration each for Identity, Management, Runtime, and Alterations.
5. Dispose the template host and clear its SQL connection pool.
6. Create a `COPY_ONLY`, `INIT`, `CHECKSUM` backup; run `RESTORE VERIFYONLY`; read all logical files with `RESTORE FILELISTONLY`.
7. Drop the live template catalog so normal tests cannot connect to it.
8. For each test, restore the backup with `CHECKSUM`, `RECOVERY`, `NEW_BROKER`, and `MOVE` every logical file to a GUID-based physical path.
9. Start the test's normal full component host against only that case's restored database.
10. On case teardown, clear that catalog's pool, force any online database to single-user, and drop it. Session teardown then removes the container and backup.

Once SQL Server begins a restore, command cancellation is deliberately ignored so a cancelled test cannot leave a database in `RESTORING`; the SQL command retains a 120-second hard timeout. Initialization and cleanup failures are aggregated so cleanup does not hide the primary failure.

No mutable host, provider, scope, client, `DbContext`, catalog, tracker, lock directory, or HTTP cache directory is shared across cases. The only reused database object is an offline backup image made after its bootstrap host has stopped.

## Other validated improvements

### Host-method metadata

The 36 host-method tests queried immutable registry metadata but formerly paid for 36 complete component hosts. A per-class fixture now creates one real host, copies only the relevant descriptors and port types into immutable records, and disposes the scope and host before test bodies execute. Tests share only the immutable value snapshot.

### Deterministic workflow-deletion teardown

Each related-records test records the workflow instance IDs it created. Before normal host drain, it enumerates active execution cycles, selects only matching IDs, verifies each instance is absent from the store, and disposes only those handles. It cannot affect cycles owned by another test or a still-persisted workflow.

### Multitenancy semantics

The existing database-backed multitenancy test now resolves the real `ITenantsProvider` from a normal per-case host. It verifies the exact provider result (`""`, `Tenant1`, `Tenant2`, `Tenant3`) and `FindAsync` semantics for `Tenant2`. This guards against accidentally replacing Elsa's real provider while constructing the bootstrap host.

### Test dependency scope

Static usage and restored asset-file audits found no Moq use in the test tree. Nine web-test projects directly reference `TUnit.AspNetCore`, which resolves MVC Testing 10.0.11 transitively through `TUnit.AspNetCore.Core`. Removing the two global references is projected to remove 713 files and 45,050,794 bytes (42.96 MiB) from clean Release outputs across the test tree. This is a dependency-graph projection, not a measured clean-directory delta; NuGet cache storage is unaffected.

The solution build exposed that `Elsa.Mediator.UnitTests` had relied on the old transitive closure for concrete dependency-injection and logging APIs. It now declares `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Logging` directly.

## Targeted experiment results

| Experiment | TUnit duration | Outcome | Finding |
|---|---:|---|---|
| One template-backed test | 24.601 s | 1/1 passed | Container startup dominates a one-test session; restore was 360 ms. |
| Nine HTTP cases, before | 40.913 s | 9/9 passed | Comparison cohort before session-template provisioning. |
| Nine HTTP cases, schema-only template | 37.642 s | 9/9 passed | Wall time fell 7.3%; mean setup fell from 21.404 s to 17.923 s (16.3%). |
| Nine HTTP cases, seeded template | 44.608 s | 9/9 passed | 7.06 s slower than schema-only; rejected and reverted. |
| Temporary 16-case isolation stress | 58.106 s | 16/16 passed | Concurrent identical table/PK writes each observed only its own owner and one row. Temporary test source was removed. |
| Three-pod concurrency scenario | 39.164 s session; 13.222 s body | 1/1 passed | Preserved intrinsic behavior versus 13.147 s baseline body. |
| Host-method class before snapshot | 86.063 s | 36/36 passed | Each case created a host. |
| Host-method class after snapshot | 28.786 s | 36/36 passed | Runner time fell 66.6%; external wall time fell 65.6%, and RSS fell about 38.5%. |
| Multitenancy class | 30.808 s | 7/7 passed | Real provider and database-backed cases remained green. |
| Related-records deletion class | 26.882 s | 3/3 passed | Cleanup was 3.050–3.294 s instead of about 33.08 s per case. |

During the 16-case stress run all cases created the same table and primary key and waited on a 16-way barrier. Every assertion observed its own database name, exactly one row, and its own owner value. Restore median/p95/max were 907/1,197/1,197 milliseconds. The stress test itself was intentionally not committed because it was experimental scaffolding.

The full solution build after dependency pruning passed with 0 warnings and 0 errors. It was repeated after removing the temporary benchmark probes and passed again in 10.51 seconds using `--no-restore`. A focused Release build of `Elsa.Mediator.UnitTests` also passed with 0 warnings and 0 errors.

## Deferred release gates

The following were intentionally not run after the last implementation change because the user requested a checkpoint commit:

1. Full component project, default parallelism, expecting exactly 205 outcomes: 202 passed and 3 intentionally skipped.
2. The exact PR command, with no added filters, caps, or runner arguments:

```bash
dotnet test --solution Elsa.sln --configuration Release --no-build
```

The expected solution outcome remains 4,417 total: 4,270 passed and 147 intentionally skipped. That expected count is an acceptance criterion, not a post-change result in this checkpoint.

For CI confidence, collect multiple uncontaminated post-change full component samples after those gates pass; the current post-change subset timings are single local samples.

## Rejected approaches

- **Shared mutable test database, Respawn, or transactions:** rejected because parallel tests can observe or reset one another's rows, background work can outlive a transaction, and database-level behavior would no longer be representative.
- **Seeded application-data template:** measured slower than the schema-only template for the HTTP cohort and risks cross-test assumptions; reverted.
- **Parallelism limits, semaphores, limiters, or broad serialization:** rejected because they hide setup contention and violate the required default-parallel execution model.
- **Sharing application hosts/providers/scopes/clients:** rejected. The host-method optimization shares only immutable DTO snapshots after its host has been disposed.
- **Aspire telemetry backchannel:** the component suite is an in-process `WebApplication`/TestServer setup, not an Aspire AppHost, so Aspire CLI resource/log/OTel attachment is not available. Native TUnit HTML/JSON spans and MTP diagnostics supplied the phase evidence instead.
- **Suite-wide NativeAOT:** rejected for this change. `dotnet test` remains a managed test-host workflow, while this suite uses dynamic-code-sensitive paths including proxy generation, `WebApplicationFactory`, EF migration APIs, Roslyn/Jint, and runtime assembly loading. An AOT pilot would require a separately scoped compatibility matrix and runner design; no AOT publish result is claimed here.

## Reproduction commands

Baseline/full component command shape (run without competing component sessions):

```bash
/usr/bin/time -l dotnet test \
  --project test/component/Elsa.Workflows.ComponentTests/Elsa.Workflows.ComponentTests.csproj \
  --configuration Release \
  --no-build \
  --minimum-expected-tests 205 \
  --results-directory /tmp/elsa-tunit-perf-8101-20260914/final-component \
  --report-html \
  --report-html-filename /tmp/elsa-tunit-perf-8101-20260914/final-component/component.html \
  --diagnostic \
  --diagnostic-output-directory /tmp/elsa-tunit-perf-8101-20260914/final-component \
  --diagnostic-verbosity Information \
  --progress off \
  --ansi off \
  --show-test-results none \
  --show-slowest-tests 10
```

Build validation performed at the checkpoint:

```bash
dotnet build Elsa.sln --configuration Release --verbosity minimal --no-restore
dotnet build test/unit/Elsa.Mediator.UnitTests/Elsa.Mediator.UnitTests.csproj --configuration Release
```

Required PR gate, still pending:

```bash
dotnet test --solution Elsa.sln --configuration Release --no-build
```

## Supporting documentation

- [TUnit performance guidance](https://github.com/thomhurst/TUnit/blob/v1.66.27/docs/docs/guides/performance.md#L12-L30)
- [TUnit ASP.NET Core initialization semaphore](https://github.com/thomhurst/TUnit/blob/v1.66.27/src/TUnit.AspNetCore.Core/WebApplicationTest.cs#L10-L19)
- [TUnit ASP.NET Core server initialization path](https://github.com/thomhurst/TUnit/blob/v1.66.27/src/TUnit.AspNetCore.Core/WebApplicationTest.cs#L130-L140)
- [TUnit engine-mode defaults](https://github.com/thomhurst/TUnit/blob/v1.66.27/src/TUnit.Core/TUnit.Core.props#L26-L34)
- [TUnit test scheduler](https://github.com/thomhurst/TUnit/blob/v1.66.27/src/TUnit.Engine/Scheduling/TestScheduler.cs#L552-L614)
- [Microsoft: restore a SQL Server backup in a Linux container](https://learn.microsoft.com/en-us/sql/linux/tutorial-restore-backup-in-sql-server-container?view=sql-server-ver17)
- [Microsoft: restore a database to new files with `MOVE`](https://learn.microsoft.com/en-us/sql/relational-databases/backup-restore/restore-a-database-to-a-new-location-sql-server?view=sql-server-ver17)
- [Microsoft: Service Broker identities](https://learn.microsoft.com/en-us/sql/database-engine/service-broker/managing-service-broker-identities)
- [Microsoft: `RESTORE` arguments including `NEW_BROKER`](https://learn.microsoft.com/en-us/sql/t-sql/statements/restore-statements-arguments-transact-sql)
- [Microsoft: Native AOT deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [Microsoft: run Microsoft Testing Platform tests](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-run-and-debug)

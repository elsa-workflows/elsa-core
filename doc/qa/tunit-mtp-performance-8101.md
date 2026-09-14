# Issue #8101: TUnit/MTP component-test performance checkpoint

Date: 2026-09-14

Branch: `spike/8101-tunit-mtp`

Baseline commit: `bf5c9dd041e58ffafe9ccbfe1c8effec89d959ef`

## Executive result

The accepted component architecture restores the lifetime and scheduling semantics of the former xUnit `AppCollection`: one App is shared across the TUnit test session, and every App consumer is serialized with a native keyed constraint.

- `AppComponentTest` receives `App` from `[ClassDataSource<App>(Shared = SharedType.PerTestSession)]`.
- `[NotInParallel(nameof(AppComponentTest))]` serializes all 198 App-bound outcomes: 195 executed cases and three expected skips.
- `HostMethodActivityTests` derives from `AppComponentTest`; it no longer owns a separate fixture or immutable snapshot.
- The seven outcomes that do not consume App remain unconstrained and retain TUnit's default scheduling.
- Per-test cleanup drains work and releases test-local scopes, clients, tenant state, mocks, signals, and subscriptions, while the App, cluster, database, and host remain session-owned.
- There is no custom semaphore, parallel limiter, runner-wide cap, NUKE test target, or `src/**` change.

Three complete Release component runs passed the exact 205-outcome contract: 202 passed, three expected skips, and zero failed. Native TUnit durations were 70.876, 68.798, and 74.570 seconds; external wall times recorded for the second and rollback-confirmation samples were 69.57 and 75.71 seconds. Each finalized report contains exactly one `initialize App` span and one `initialize Infrastructure` span; the rollback-confirmation report also contains all 195 expected App-test cleanup hooks, with a measured peak of one concurrent App-bound test lifecycle.

The 442.725/453.808-second unconstrained per-invocation samples and both four-slot experiments below are retained as **historical design evidence**. They compare component resource topologies and scheduling policies within the already-migrated TUnit suite; none is a TUnit-versus-xUnit framework comparison. The exact ordinary PR workflow still needs to measure the final pushed commit.

TUnit remains pinned at `1.66.27`; the resolved TRX reporter remains `2.3.3`.

## Measurement environment

Measurements were taken with warm package/build caches on:

- macOS 26.6.2 (25G83), Apple silicon `arm64`
- Apple M5 Pro, 18 logical/physical CPUs, 48 GiB memory
- .NET SDK 10.0.400 and runtime 10.0.11
- Docker client/server 29.7.2, Linux `arm64` containers
- TUnit 1.66.27 using Microsoft Testing Platform
- Europe/Minsk timezone

All reported TUnit durations come from native TUnit JSON reports. External wall time and peak RSS were collected with `/usr/bin/time -l` where noted. Raw local evidence is kept under ephemeral `/tmp` result directories and is not part of the commit.

## Current shared-App architecture

The accepted session topology is:

1. TUnit constructs one `App` for the test session and injects that same instance through the inherited `AppComponentTest` data source.
2. That App owns one `Infrastructure`, one `Cluster`, one component database, and the normal primary host; extra pods remain lazy.
3. The keyed `[NotInParallel(nameof(AppComponentTest))]` constraint wraps the complete lifecycle of every App-bound outcome.
4. `HostMethodActivityTests` derives from `AppComponentTest`, so its 36 outcomes use the same App and the same serialization key rather than a separate snapshot fixture.
5. Per-test cleanup drains workflow work and releases test-local state without disposing the shared App or cluster. TUnit disposes the session data source after its consumers complete, and `App` memoizes its disposal path.

The keyed constraint is intentionally narrower than process-wide serialization. The seven non-App outcomes have no matching key and remain eligible for TUnit's default scheduling. This reproduces the former collection boundary: App consumers serialize around one fixture while unrelated tests remain independent.

Validation record:

| Sample | Native TUnit duration | External wall time | App initialization spans | Infrastructure initialization spans | Outcome |
|---|---:|---:|---:|---:|---|
| Shared App 1 | 70.876 s | not separately recorded | 1 | 1 | 202 passed, 3 skipped |
| Shared App 2 | 68.798 s | 69.57 s | 1 | 1 | 202 passed, 3 skipped |
| Shared App rollback confirmation | 74.570 s | 75.71 s | 1 | 1 | 202 passed, 3 skipped |

A focused cohort covering host-method, dynamic-endpoint, distributed-lock-resilience, and workflow-instance-deletion paths also passed 49/49 in 40.827 seconds of console time (39.970 seconds native).

## Historical unconstrained per-invocation baseline (superseded policy)

Before the template/lifecycle experiments and accepted shared-App policy, two uncontaminated, no-build, unconstrained per-invocation samples completed with the expected 205 outcomes: 202 passed and 3 intentionally skipped. These measurements characterize the superseded scheduling policy and earlier setup cost, not the final implementation.

| Sample | TUnit duration | External wall time | Peak RSS | Outcome |
|---|---:|---:|---:|---|
| Clean 1 | 453.808 s | 454.13 s | 977,092,608 B | 202 passed, 3 skipped |
| Clean 3 | 442.725 s | 443.89 s | 978,337,792 B | 202 passed, 3 skipped |

A second attempted sample overlapped with an external statistics process and was invalidated; a fourth was interrupted. Neither is included in the baseline.

The runner boundary is not the material cost: direct executable versus `dotnet test` overhead was about 0.30 seconds for a single component test, and external versus TUnit-reported time differed by 1.165 seconds (0.26%) in the clean full sample.

Representative non-component project medians were much smaller: 0.93 seconds for a 3-test unit project, 1.65 seconds for a 279-test unit project, and 4.67 seconds for a 305-test integration project. This supported prioritizing the component project.

## Historical bottleneck analysis

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

The intermediate per-invocation implementation kept that isolation boundary and changed how each unique database was provisioned: Elsa restored a schema-only, migrated session image rather than creating an empty database and running four schema migrations for every test. The accepted implementation makes a different tradeoff by sharing the complete App and serializing every consumer, matching the former xUnit collection lifetime.

## Historical per-invocation database lifecycle (superseded)

The discarded per-invocation implementation performed this sequence:

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

At that historical checkpoint, no mutable host, provider, scope, client, `DbContext`, catalog, tracker, lock directory, or HTTP cache directory was shared across cases. The only reused database object was an offline backup image made after its bootstrap host stopped. This statement does not describe the accepted shared-App topology.

## Other validated improvements

### Historical host-method metadata optimization (superseded)

The 36 host-method tests query immutable registry metadata but formerly paid for 36 complete component hosts. The intermediate design added a per-class fixture that copied descriptors into immutable records and disposed its App before the test bodies executed. The accepted shared-App design removes that fixture: `HostMethodActivityTests` derives from `AppComponentTest` and uses the session App like every other App consumer.

### Deterministic workflow-deletion teardown

Each related-records test records the workflow instance IDs it created. Before normal host drain, it enumerates active execution cycles, selects only matching IDs, verifies each instance is absent from the store, and disposes only those handles. It cannot affect cycles owned by another test or a still-persisted workflow.

### Historical multitenancy/template validation

The database-backed multitenancy test resolved the real `ITenantsProvider` during the template experiment. It verified the exact provider result (`""`, `Tenant1`, `Tenant2`, `Tenant3`) and `FindAsync` semantics for `Tenant2`, guarding against accidentally replacing Elsa's real provider while constructing the bootstrap host.

### Test dependency scope

Static usage and restored asset-file audits found no Moq use in the test tree. Nine web-test projects directly reference `TUnit.AspNetCore`, which resolves MVC Testing 10.0.11 transitively through `TUnit.AspNetCore.Core`. Removing the two global references is projected to remove 713 files and 45,050,794 bytes (42.96 MiB) from clean Release outputs across the test tree. This is a dependency-graph projection, not a measured clean-directory delta; NuGet cache storage is unaffected.

The solution build exposed that `Elsa.Mediator.UnitTests` had relied on the old transitive closure for concrete dependency-injection and logging APIs. It now declares `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Logging` directly.

## Historical targeted experiment results

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

The full solution build after dependency pruning passed with 0 warnings and 0 errors. It was repeated after removing the temporary benchmark probes and passed again in 10.51 seconds using `--no-restore`. A focused Release build of `Elsa.Mediator.UnitTests` also passed with 0 warnings and 0 errors. These build and experiment timings predate the accepted shared-App policy and are retained as historical context.

## Current validation and remaining CI measurement

The accepted shared-App, keyed-serialization policy passed three complete component runs with 205 total outcomes: 202 passed, the same three expected skips, and zero failed. Native durations were 70.876, 68.798, and 74.570 seconds; recorded external wall times were 69.57 and 75.71 seconds. All three reports recorded exactly one App and one Infrastructure initialization span. The final rollback-confirmation report recorded all 195 expected App-test cleanup hooks, and an interval sweep over its App-bound root spans measured a maximum concurrency of exactly one.

The rejected invocation-isolated, native-limit-4 policy also passed 205/202/3, but took 7m12.188s native, 7m12.727s in the console summary, and 433.02 seconds externally. It preserved isolation but provided essentially the earlier per-invocation wall-time profile. A shared-App-plus-four experiment failed 14 cases through cross-test state interference, so concurrent consumers of one mutable App were discarded rather than masked.

The remaining measurement is the ordinary PR workflow at the **final pushed commit**, with no custom cap or runner policy:

```bash
./build.cmd Compile
dotnet test --solution Elsa.sln --configuration Release --no-build
```

Capture the Restore, Compile, and Test step wall times; confirm the existing 4,417-outcome solution contract; and record the component test application's duration from the same run. The 4,417 count is an acceptance criterion until this post-change CI run completes, not a result claimed for the accepted shared-App checkpoint.

Use CI run `34858013234` only as the same-workflow, earlier per-invocation scheduling comparison: it reported 4,417 outcomes, a 10:16 Test step, and a 9:34 component application. Retain upstream xUnit run `34861447592` as unmatched historical context: Restore 0:44, Compile 3:07, Test 7:30, and component 2:47. The two historical CI runs do not form a controlled framework benchmark.

## Architecture decisions

- **Respawn or transaction-based reset around a shared database:** rejected because background work can outlive a transaction and database-level behavior would no longer be representative. The accepted App database is instead protected by complete lifecycle serialization.
- **Seeded application-data template:** measured slower than the schema-only template for the HTTP cohort and risks cross-test assumptions; reverted.
- **One session App plus keyed native serialization:** selected. It restores the former xUnit fixture lifetime and collection-wide App-consumer serialization while leaving seven unrelated outcomes unconstrained.
- **One session App plus a four-slot limiter:** rejected after 14 failures demonstrated cross-test state interference when the shared mutable graph ran concurrently.
- **Per-invocation Apps plus a four-slot native TUnit limiter:** passed, but rejected after taking 7m12.188s native and 433.02 seconds externally; it did not recover the former fixture lifetime or setup cost.
- **Custom semaphores, parallel limiters, and a runner-wide maximum:** rejected for the accepted policy. The native keyed constraint states the actual shared-App boundary directly.
- **Aspire telemetry backchannel:** the component suite is an in-process `WebApplication`/TestServer setup, not an Aspire AppHost, so Aspire CLI resource/log/OTel attachment is not available. Native TUnit HTML/JSON spans and MTP diagnostics supplied the phase evidence instead.
- **Suite-wide NativeAOT:** rejected for this change. `dotnet test` remains a managed test-host workflow, while this suite uses dynamic-code-sensitive paths including proxy generation, `WebApplicationFactory`, EF migration APIs, Roslyn/Jint, and runtime assembly loading. An AOT pilot would require a separately scoped compatibility matrix and runner design; no AOT publish result is claimed here.

## Reproduction commands

Current full component command shape (run without competing component sessions):

```bash
TUNIT_OTEL_RECEIVER=0 dotnet test \
  --project test/component/Elsa.Workflows.ComponentTests/Elsa.Workflows.ComponentTests.csproj \
  --configuration Release \
  --no-build \
  -- \
  --minimum-expected-tests 202 \
  --results-directory /tmp/elsa-8101-shared-app-component \
  --report-trx \
  --report-trx-filename component.trx \
  --timeout 20m \
  --progress off \
  --ansi off \
  --show-test-results none
```

The minimum is 202 because the MTP policy counts executed cases; verify the exact 205 total independently in the finalized TUnit JSON or TRX report.

Build validation command shape:

```bash
dotnet build test/component/Elsa.Workflows.ComponentTests/Elsa.Workflows.ComponentTests.csproj \
  --configuration Release \
  --no-restore
```

Required ordinary PR measurement, still pending for the final pushed commit:

```bash
./build.cmd Compile
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

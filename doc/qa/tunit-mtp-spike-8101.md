# Issue #8101: TUnit migration research and rollout plan

Status: **all 60 active test projects migrated and root MTP/CI cutover implemented; post-upstream-sync validation complete, with focused lifetime/sentinel, failure-path, and IDE checks remaining**
Recorded: 2026-09-13
Updated: 2026-09-14
Issue: [elsa-workflows/elsa-core#8101][s1]  
Comparison base: `37b1a453169201e577ef6db0bfd0348b08070211`  
Isolated branch: `spike/8101-tunit-mtp`  
Isolated worktree: `elsa-core-tunit-8101`

## Executive finding

Issue #8101 begins as a controlled framework-and-runner spike. The research established that a native TUnit + Microsoft Testing Platform migration is feasible, identified the compatibility work, and exposed isolation as the main correctness risk. After reviewing that result, the project explicitly selected the expanded outcome: migrate every active test project, migrate the tests and assertions themselves, remove all xUnit dependencies and compatibility shims, isolate mutable resources, and enable TUnit's native parallel execution.

That project decision intentionally goes beyond the issue's original experiment boundary. The A/B/C design and baseline evidence remain in this report so the decision is reproducible, but they no longer gate implementation on this branch. The implementation target is now unambiguous:

1. every active Elsa test is discovered and run by TUnit on MTP;
2. no xUnit framework, assertion, runner, discoverer, output-helper, package, configuration, or compatibility alias remains;
3. independent tests run concurrently by default;
4. shared mutable resources use invocation-unique identities, while irreducible process-global hazards use narrowly scoped native TUnit constraints;
5. discovery counts, intended skips, reports, coverage enforcement, diagnostics, and supported developer commands are preserved or replaced by an explicit native contract.

The accepted implementation now selects MTP from the repository-root `global.json`, runs individual projects with `dotnet test --project`, and runs checked-in solution or solution-filter scopes with `dotnet test --solution`. All 60 active test projects are native TUnit projects. NUKE remains responsible for compile/package work but no longer owns a test target; PR and package workflows invoke MTP directly. Package coverage is native Cobertura, merged into one repository-wide 10% line gate, with TRX and five-minute supported mini-dump diagnostics retained.

At the comparison base, the most important risks were behavioral rather than syntactic:

- xUnit v2 and TUnit do not have the same default parallel-execution model;
- Elsa has process-global test state, shared infrastructure, and public test helpers coupled to xUnit types;
- custom conformance discovery deliberately suppresses unavailable persistence providers and cannot be converted as ordinary data;
- the baseline CI depended on Coverlet thresholds, three coverage formats, GitHub annotations, TRX, and hang dumps, each of which needed an explicit MTP replacement and a failure-path test;
- a partial MTP migration cannot safely change runner selection at the repository root because a single `dotnet test` invocation cannot include projects that only support different test platforms.

The issue's original recommendation was to finish a controlled A/B/C comparison before deciding:

1. xUnit v2 + VSTest, the then-current baseline;
2. xUnit v3 + MTP, the platform-only control tracked by #8050;
3. TUnit + MTP, initially retaining `xunit.assert` only to isolate runner cost.

That comparison remains useful historical evidence, but the accepted implementation does not retain `xunit.assert`: assertions are migrated to native TUnit APIs and every analyzer-generated rewrite is compiled and behaviorally reviewed. TUnit's unconstrained default is enabled only after the isolation audit and repeat-run gate. [1][s1] [7][s7]

> **Historical record:** The original issue scope, isolated-runner design, A/B/C commands, baseline architecture, and rollout checkpoints below record how the decision was reached. They intentionally retain obsolete VSTest, positional `dotnet test`, and direct `dotnet run` examples and are not current developer guidance. See “Current implementation: repository-root MTP cutover” for the active contract.

## Original issue scope and approved expansion

The issue defines a deliberately small experiment. The project subsequently and explicitly expanded this branch to the complete migration described above. The lists below preserve the original issue boundary so reviewers can distinguish issue evidence from the approved production work. [1][s1]

### In scope

- Benchmark the current xUnit v2 + VSTest baseline.
- Benchmark an xUnit v3 + MTP control so that an MTP improvement is not misattributed to TUnit.
- Benchmark a TUnit + MTP candidate.
- Retain `xunit.assert` in the first TUnit pilot so runner/lifecycle cost is measured separately from assertion-rewrite cost.
- Use one representative fixture-light unit-test project and one representative integration-heavy project.
- Check the component-test project for compatibility, fixture lifetime, serialization, reporting, and diagnostics; a full component migration is not required for the spike.
- Hold effective concurrency equivalent across variants.
- Measure clean build/test, incremental build/test, no-build execution, coverage execution, wall time, peak memory, and discovered/passed/failed/skipped counts.
- Validate GitHub annotations, TRX, hang diagnostics, coverage formats and thresholds, and IDE discovery/debugging.
- Run at least five paired CI samples for each variant and ten consecutive runs of each migrated pilot without count drift or new failures.

### Originally out of scope (now superseded where noted)

- A repository-wide production migration. **Now explicitly approved.**
- Rewriting all assertions to TUnit assertions. **Now explicitly required.**
- Native AOT work.
- Treating TUnit's unconstrained default parallelism as the baseline.
- Test sharding or unrelated CI restructuring.
- Fixing existing flaky tests as part of the framework comparison.
- Product changes or other changes that confound the timings.

These boundaries matter. A faster run with different concurrency, fewer discovered cases, missing coverage enforcement, or weaker diagnostics is not a successful migration.

## Isolation model

“Isolation” has three distinct meanings in this spike. All three must hold.

### 1. Repository isolation

The spike is based on immutable commit `37b1a453169201e577ef6db0bfd0348b08070211` in a separate Git worktree and branch. The pre-existing checkout, its branch, and its uncommitted changes are outside this work. Each A/B/C variant should remain independently reproducible, preferably as a separate worktree or as a pinned commit whose outputs are written to a variant-specific artifact directory.

Do not rebase a variant in the middle of a paired experiment. If the base changes, discard that sample set and rerun every variant from the new common base.

### 2. Runner-configuration isolation

.NET 10 selects MTP through the `test.runner` setting in `global.json`; MTP mode also changes explicit project syntax from `dotnet test project.csproj` to `dotnet test --project project.csproj`. A project that supports only VSTest is an error in an MTP invocation. [3][s3] [4][s4]

The pilot therefore keeps the repository root on its existing VSTest behavior and puts the MTP selector under:

```text
eng/tunit-spike/mtp/global.json
```

MTP commands run with that directory as the working directory, or run the generated test executable through `dotnet run`. The root `global.json` must not be changed while xUnit v2-only projects remain in the solution. This prevents an isolated pilot from breaking unrelated tests.

### 3. Test-state and resource isolation

xUnit v2 ordinarily serializes test methods that belong to the same test class/collection while allowing separate collections to execute concurrently. TUnit makes every test method eligible to run concurrently unless constraints are applied. [7][s7] Comparing those defaults would change semantics and invalidate both performance and reliability conclusions.

The first controlled baseline is deliberately serial: set the effective maximum to one active test for **all three** A/B/C variants. For TUnit the delivered pilot uses `--maximum-parallel-tests 1`; the xUnit controls must use an equivalent variant-local runner setting. This is more restrictive than Elsa's current xUnit default, but it is equivalent across variants and prevents a parallelism change from being reported as a framework speedup. The five exploratory xUnit measurements later in this document used the current default and therefore are not comparisons to the serial TUnit pilot.

The delivered pilot does not add a compatibility alias or a custom discovery-time concurrency shim. It uses explicit native TUnit constraints on known hazards and keeps the whole initial pilot serial until project-local isolation has been audited. TUnit supports keyed `[NotInParallel]`, global `[NotInParallel]`, parallel groups, parallel limiters, and a global maximum through `--maximum-parallel-tests`. [7][s7]

Only after the serial correctness and tooling gates pass should a separate concurrency experiment raise the cap. Before doing that:

- inventory each explicit xUnit collection and the state it shares;
- add native keyed `[NotInParallel]` constraints where tests share mutable state;
- use unkeyed `[NotInParallel]` for process-global state that must run completely alone;
- isolate project-local mutable resources per invocation;
- apply the same audited cap and grouping policy to each A/B/C variant;
- verify the policy with sentinel tests that fail if protected cases overlap and demonstrate that independent cases can overlap.

The concurrency experiment must be reported separately from the serial framework comparison. A production rollout cannot enable TUnit's default parallelism until that audit is complete.

Parallel constraints are the last line of defense, not the primary isolation strategy. Mutable resources should be unique per test invocation:

- database or schema names;
- message topics, queues, subscriptions, and consumer groups;
- cache keys and blob paths;
- workflow definition, instance, correlation, tenant, and user identifiers;
- files, directories, and ports.

TUnit exposes an invocation-specific identity and helpers such as `TestContext.Current.Isolation.GetIsolatedName(...)` and `GetIsolatedPrefix(...)`. [8][s8] Use those names in setup, pass them into the system under test, and delete exactly those resources in idempotent cleanup.

Process-global state cannot be made safe merely by choosing unique data. Tests that mutate any of the following must be made exclusive or must restore state reliably in `finally`/cleanup:

- `Console.SetIn`, `Console.SetOut`, or console-stream hooks;
- environment variables;
- FastEndpoints security defaults such as `EndpointSecurityOptions.SecurityIsEnabled`;
- global `ActivityListener`, `MeterListener`, and instrumentation registration;
- static clocks, registries, and service locators.

Issue #7965 is a concrete warning: a process-global FastEndpoints security flag raced between tests. [16][s16] The component suite also has shared SQL Server, PostgreSQL, RabbitMQ, host, and tenant lifecycle, so it must remain serialized until those resources are partitioned and its fixture lifetime is proven. Existing flake #7404 should be tracked as a baseline condition rather than silently attributed to or fixed by this spike. [17][s17]

## Historical baseline: Elsa test architecture at `37b1a453`

At the pinned base, test defaults came from `test/Directory.Build.props` and package versions from `Directory.Packages.props`. The baseline stack was:

| Concern | Historical value |
| --- | --- |
| Framework | xUnit `2.9.3` |
| Runner adapter | `xunit.runner.visualstudio` `3.1.5` |
| Test SDK | `Microsoft.NET.Test.Sdk` `18.0.1` |
| Platform | VSTest through `dotnet test` |
| Assertions | bundled xUnit v2 assertions |
| Coverage | `coverlet.msbuild` and `coverlet.collector` `6.0.4` |
| Default coverage output | Cobertura, LCOV, and OpenCover |
| Default threshold | 10% total line coverage |
| Component threshold | 23% on `Elsa.Workflows.ComponentTests` |
| GitHub reporting | `GitHubActionsTestLogger` `3.0.4` |
| Component diagnostics | TRX plus VSTest `--blame-hang`, two-minute mini dump |
| Target framework | `net10.0` |

The package workflow discovers every `*.csproj` under `test/unit` and `test/integration` from the filesystem, builds each project, then runs it with coverage. Component tests run in a separate job with coverage, TRX, and hang diagnostics. This means a project does not have to be included in `Elsa.sln` to affect CI. In particular, `Elsa.Mediator.UnitTests` is outside the solution but still included by filesystem discovery. [18][s18] [19][s19]

### Static inventory at `37b1a453`

The inventory below is a source scan, not a runner-discovery count. Parameterized attributes can expand to multiple cases at discovery time.

| Inventory item | Count |
| --- | ---: |
| Active xUnit projects | 55 |
| Unit projects | 37 |
| Integration projects | 17 |
| Component projects | 1 |
| `[Fact]` methods | 2,565 in 572 files |
| `[Theory]` methods | 324 in 171 files |
| Custom `[ConformanceFact]` methods | 41 in 4 files |
| Custom `[ConformanceTheory]` methods | 1 |
| Attributed test methods before theory expansion | 2,931 in 607 files |
| `[InlineData]` rows | 1,009 in 157 files |
| `[MemberData]` uses | 35 in 21 files |
| `TheoryData` uses | 16 in 12 files |
| `IEnumerable<object[]>` data sources | 14 in 9 files |
| `[ClassData]` uses | 0 |
| `[Trait]` uses | 0 |
| Explicit `Fact(Skip = ...)` uses | 3 |
| `ITestOutputHelper` references | 166 occurrences in 154 files |
| `IAsyncLifetime` references | 31 occurrences in 29 files |
| Collection-related attributes/references | 40 occurrences in 20 files |
| `Xunit.Sdk` references | 5 occurrences in 4 files |
| `Assert.*` calls | 7,620 in 597 files |

The largest assertion families are `Equal` (3,221), `Contains` (845), `True` (645), `NotNull` (499), and `Single` (479). This volume is why #8101 correctly separates runner/lifecycle migration from assertion migration.

### Public and shared xUnit coupling

Three reusable projects require extra care:

- `src/common/Elsa.Testing.Shared`
- `src/common/Elsa.Testing.Shared.Integration`
- `src/common/Elsa.Testing.Shared.Component`

Their surface includes types such as `XunitConsoleTextWriter`, `XunitLogger`, `XunitLoggerProvider`, `TestApplicationBuilder(ITestOutputHelper)`, and `WorkflowTestFixture(ITestOutputHelper)`. Replacing these signatures in a framework-migration PR can break source or binary consumers. The pilot should keep these libraries framework-neutral where possible and adapt TUnit output at the leaf test project. If a shared library only needs TUnit contracts later, reference `TUnit.Core`, not the TUnit meta-package that turns a test project into an executable.

### Custom discovery blocker

`test/unit/Elsa.UserTasks.Persistence.ConformanceTests/Infrastructure/ConformanceFactAttribute.cs` does more than rename `[Fact]`. Its discoverer avoids enumerating tests for unavailable persistence providers. A mechanical `[Test]`/data conversion can activate fixtures that were previously suppressed, change discovered counts, or fail before the skip decision is made.

Treat the 41 conformance facts and one conformance theory as a separate extensibility workstream. Build a TUnit-native discovery/data-source equivalent, exercise both available and unavailable providers, and assert exact discovered/skipped counts before migrating those projects.

## Representative projects

The initial pair is intentionally large enough to expose real migration costs while remaining narrower than the full suite.

| Role | Project | Baseline cases | Why selected |
| --- | --- | ---: | --- |
| Fixture-light unit | `test/unit/Elsa.Workflows.Core.UnitTests/Elsa.Workflows.Core.UnitTests.csproj` | 274 | Fast, broad core coverage; exercises inline/member data and a process-global instrumentation collection without the full shared-fixture surface. |
| Integration-heavy | `test/integration/Elsa.Workflows.IntegrationTests/Elsa.Workflows.IntegrationTests.csproj` | 305 | Broad workflow hosting/integration behavior and heavy `ITestOutputHelper` use; exposes shared-helper, output, and lifecycle adaptation cost. |
| Component compatibility | `test/component/Elsa.Workflows.ComponentTests/Elsa.Workflows.ComponentTests.csproj` | Record during spike | Shared SQL/Rabbit/host/tenant fixture; validates lifetime, serialization, reporting, coverage threshold, and hang-dump compatibility without enabling unrestricted parallelism. |

The baseline case counts are local runner-discovery results and must be reconfirmed on Linux CI for every variant. A matching total alone is insufficient: the names and skip reasons should be diffed as well.

## Framework and runner analysis

### Variant A: xUnit v2 + VSTest

This was Elsa's comparison-base behavior and the reference for source compatibility. It retained the baseline packages, VSTest logging, Coverlet collection, and `xunit.runner.json` semantics. Its main purpose in the experiment was to establish complete-job and project-level timings.

### Variant B: xUnit v3 + MTP

This is the platform control. xUnit v3 supports standalone executable test projects and has an MTP-specific package, `xunit.v3.mtp-v2`; version `4.0.0` was current when this spike was recorded. [5][s5] [15][s15] The project must use the same nested MTP selection and reporting/coverage extensions as the TUnit variant.

Variant B is essential because otherwise a faster variant C cannot tell us whether the gain came from MTP or TUnit. It also supplies implementation evidence for #8050. [2][s2]

### Variant C: TUnit + MTP

TUnit is MTP-native and makes test projects executable. The spike pins TUnit `1.66.27`, current at the time of the experiment, rather than copying the older reference-repository version. [14][s14] The first pilot:

- conditionally removes xUnit framework, VSTest adapter, Test SDK, and VSTest-only coverage/logger dependencies from opted-in projects;
- adds TUnit and `xunit.assert`;
- uses `OutputType=Exe`;
- disables TUnit assertion implicit usings to keep `Assert` bound to `Xunit.Assert`;
- opts in only selected projects through `UseTUnit=true`;
- keeps MTP runner selection under `eng/tunit-spike/mtp`.

The delivered pilot converts source to native TUnit names: `[Test]`, `[Arguments]`, and `[MethodDataSource]`. It does not use compatibility aliases, because aliases can hide which framework owns discovery and would leave production-unrepresentative source behind. Only assertions stay on xUnit, explicitly through `Xunit.Assert`, as required by the issue's isolation of runner/lifecycle cost. The migration must not apply TUnit's `TUXU0001` fixer across the repository during the performance experiment because that fixer also rewrites assertions and changes the question being measured. TUnit's official xUnit guide confirms the attribute, fixture, output, lifecycle, and data-source mappings and identifies the cases that require manual work. [6][s6]

## Behavioral migration map

| xUnit behavior | TUnit candidate | Elsa-specific validation |
| --- | --- | --- |
| `[Fact]` | `[Test]` | Same fully qualified test identity and pass/fail/skip state. |
| `[Theory]` + `[InlineData]` | `[Test]` + `[Arguments]` | Same number and order-independent set of expanded cases; source-generated argument types must be accessible. |
| `[MemberData]` / `TheoryData` / `object[]` | `[MethodDataSource]` with typed tuples or factories | Convert deliberately; compare the expanded display-name set, nullable/generic values, and deferred factories. TUnit's method-data rules are compile-time checked. [9][s9] |
| `Fact.Skip` | TUnit skip facility | Preserve skip reason and skipped count; ensure CI annotation remains non-failing. |
| `DisplayName` | Native TUnit display-name support | Preserve useful parameter text and ensure TRX names remain distinguishable. |
| Constructor setup | Constructor or `[Before(Test)]` | Prove a fresh test-class instance per invocation before relying on mutable fields. |
| `IDisposable` | `IDisposable` or `[After(Test)]` | Ensure cleanup runs after assertion/setup failure. |
| `IAsyncLifetime` | TUnit async initializer/disposer or hooks | Verify exact per-test/per-class/session timing and cancellation. TUnit documents async lifecycle hooks. [10][s10] |
| `IClassFixture<T>` | `[ClassDataSource<T>(Shared = SharedType.PerClass)]` | Assert initialization count, sharing scope, disposal count, and failure propagation. |
| `ICollectionFixture<T>` / `[Collection]` | keyed shared `ClassDataSource` plus parallel constraint | Sharing and serialization are separate requirements; prove both. |
| `ITestOutputHelper` | `TestContext` output or adapter | Preserve output on pass/fail, concurrent line integrity, GitHub annotations, and TRX capture. |
| `Xunit.Sdk` discoverer | TUnit discovery/data-source extension | Required for provider-aware conformance tests; do not mechanically replace. |
| xUnit assertions | `xunit.assert` during spike | Keeps assertion behavior and rewrite cost outside the runner comparison. |

TUnit source generation can expose accessibility problems that reflection-based discovery tolerated. For example, types supplied through `[Arguments]` or method data may need to be visible to generated code. These are real migration costs and should be recorded, not worked around by dropping cases.

## Reference implementation findings

The read-only `elsa-xrm-extensions` checkout supplied for this spike was audited at commit `b5dee621169f0492b864f93282816ec07304e961`. It is useful prior art, not evidence that Elsa Core will behave identically.

Observed patterns:

- centrally pinned TUnit and TUnit.AspNetCore `1.54.0`;
- root MTP runner selection;
- shared TUnit/Moq test props with no xUnit, VSTest Test SDK, or Coverlet packages;
- 494 `[Test]` attributes whose methods all return `async Task`;
- expensive fixtures exposed through required `[ClassDataSource<Fixture>(Shared = SharedType.PerTestSession)]` properties;
- fixtures implementing async initialization and disposal;
- shared infrastructure containers but per-test mutable databases, topics, consumer groups, and identifiers;
- invocation-specific resource names derived from `TestContext` identity/isolation helpers;
- `[NotInParallel]` reserved for process-global state;
- an assembly `ParallelLimiter` for resource-heavy suites;
- CI execution through MTP with Cobertura output.

Useful paths in that checkout include:

- `Directory.Packages.props`
- `global.json`
- `test/Directory.Build.props`
- `test/integration/XRM.Elsa.Activities.IntegrationTests/ActivitiesTestBase.cs`
- `test/integration/XRM.Elsa.ServiceBus.Kafka.IntegrationTests/Hosting/KafkaIntegrationTestBase.cs`
- `test/integration/XRM.Elsa.ServiceBus.Kafka.IntegrationTests/Helpers/KafkaTestResourceSet.cs`
- `test/integration/XRM.Elsa.ServiceBus.Kafka.IntegrationTests/README.md`
- `test/integration/XRM.Elsa.ServiceBus.Kafka.Cluster.IntegrationTests/Program.cs`

The patterns to reuse are “share expensive immutable infrastructure” and “isolate mutable resources per invocation.” The version and root-runner configuration should not be copied: Elsa Core is a mixed-runner repository during the spike and uses a newer TUnit package.

## Experiment design

### Controlled variables

Record these values with every run:

- source commit and variant commit;
- OS image and architecture;
- exact `dotnet --info`, SDK, host, and runtime versions;
- exact package lock/restore state;
- project and target framework;
- runner/framework versions;
- effective test parallelism and fixture-sharing policy;
- coverage/report/diagnostic extensions and switches;
- whether the NuGet cache, build outputs, and container images were warm;
- CPU and memory limits;
- discovered, started, passed, failed, skipped, and timed-out counts;
- wall-clock duration, runner-reported duration, and peak resident memory;
- artifact paths and hashes.

Do not compare a clean variant to a warm variant, or a coverage run to a non-coverage run.

### Run classes

For each representative project and each A/B/C variant, measure these separately:

1. **Clean build + test**: no project `bin`/`obj`; restore state stated explicitly.
2. **Incremental build + test**: outputs already present; invoke the ordinary build/test path.
3. **No-build test**: build once, then invoke only the runner.
4. **Coverage test**: collect the production-equivalent coverage artifact and enforce the threshold.
5. **Complete CI job**: include project discovery, build, test, reporting, coverage upload preparation, and relevant service/container startup.

The decision gate applies to the complete unit/integration CI job, not just the framework's internal “test duration.” Runner-reported duration is still retained to explain where a gain or regression originates.

### Repetition and statistics

- Run ten consecutive pilot executions per migrated project to establish stability.
- Run at least five paired CI repetitions per variant on the same runner class.
- Interleave or randomize variants (`A-B-C`, then `C-A-B`, and so on) so time-of-day and host drift do not consistently favor one candidate.
- Prefer a dedicated workflow matrix whose variants use the same base and services.
- Report every sample, median, p95, minimum, maximum, and count outcome.
- With only five samples, nearest-rank p95 is the maximum; label it as low-confidence and do not over-interpret it.
- Preserve raw logs and machine-readable sample data as artifacts.

If any variant changes case count, skip count, concurrency, coverage denominator, or external-service topology, stop the comparison and explain the difference before collecting more timing samples.

## Historical spike commands

The commands below are preserved templates for reproducing the original isolated experiment. They are not current developer guidance. Artifact directories must remain variant-specific. Run from the repository root unless the command changes directory.

### Environment record

```bash
git rev-parse HEAD
git status --short --branch
dotnet --info
dotnet --list-sdks
dotnet --list-runtimes
```

On Linux CI, wrap the measured invocation in `/usr/bin/time -v` and retain “Maximum resident set size.” On macOS, use `/usr/bin/time -lp`. Do not use the shell's abbreviated `time` output for peak-memory claims.

### Variant A: xUnit v2 + VSTest

```bash
project=test/unit/Elsa.Workflows.Core.UnitTests/Elsa.Workflows.Core.UnitTests.csproj

dotnet build "$project" --configuration Release --framework net10.0 \
  -p:CollectCoverage=false

/usr/bin/time -lp dotnet test "$project" \
  --configuration Release \
  --framework net10.0 \
  --no-build \
  --logger trx \
  --results-directory artifacts/tunit-spike/xunit2/unit \
  -p:CollectCoverage=false
```

Repeat with the integration project and its own result directory. The CI coverage form remains `/p:CollectCoverage=true` for the baseline.

### Variants B and C: MTP through the isolated selector

From the nested MTP directory, .NET 10 uses `--project` rather than a positional project argument. [4][s4]

```bash
cd eng/tunit-spike/mtp

dotnet test \
  --project ../../../test/unit/Elsa.Workflows.Core.UnitTests/Elsa.Workflows.Core.UnitTests.csproj \
  --configuration Release \
  --framework net10.0 \
  --no-build \
  --results-directory ../../../artifacts/tunit-spike/tunit/unit \
  -- \
  --maximum-parallel-tests 1 \
  --report-trx
```

For a single executable TUnit project, this direct form is useful while diagnosing runner selection:

```bash
dotnet run \
  --project test/unit/Elsa.Workflows.Core.UnitTests/Elsa.Workflows.Core.UnitTests.csproj \
  --configuration Release \
  --no-build \
  -- \
  --list-tests
```

Before using any report, coverage, or diagnostic switch, prove that the owning extension was registered:

```bash
dotnet run \
  --project test/unit/Elsa.Workflows.Core.UnitTests/Elsa.Workflows.Core.UnitTests.csproj \
  --configuration Release \
  --no-build \
  -- --help
```

MTP report switches are not part of the platform core; for example, `--report-trx` requires `Microsoft.Testing.Extensions.TrxReport`. [11][s11]

### Coverage candidate

Microsoft's MTP coverage extension supports `coverage`, `xml`, and `cobertura`, while Elsa currently emits Cobertura, LCOV, and OpenCover through Coverlet. The native `coverlet.MTP` extension supports JSON, LCOV, OpenCover, Cobertura, and TeamCity output as well as include/exclude filters. [13][s13] The spike must evaluate either:

- `Microsoft.Testing.Extensions.CodeCoverage`, retaining Cobertura and explicitly deciding how to replace LCOV/OpenCover; or
- `coverlet.MTP`, the leading format-parity candidate, with separate proof of threshold failure because the documented MTP option table does not itself establish Elsa's current MSBuild-threshold behavior.

A Microsoft-extension proof command has this shape after the package is registered:

```bash
dotnet run \
  --project test/unit/Elsa.Workflows.Core.UnitTests/Elsa.Workflows.Core.UnitTests.csproj \
  --configuration Release \
  --no-build \
  -- \
  --coverage \
  --coverage-output artifacts/tunit-spike/tunit/unit/coverage.cobertura.xml \
  --coverage-output-format cobertura
```

For the selected tool, validate all of the following:

- exclude `[*Elsa.Testing.Shared*]*` as the current build does;
- compare covered/valid line and branch counts, not just percentages;
- enforce project-specific thresholds, including 10% default and 23% component;
- deliberately set an impossible threshold and prove the process exits non-zero;
- preserve artifact discovery by the existing coverage-report job;
- document any intentional loss or conversion of LCOV/OpenCover.

### Reports and diagnostics

For MTP, the first-party replacements are explicit extensions:

```text
Microsoft.Testing.Extensions.TrxReport              -> --report-trx
Microsoft.Testing.Extensions.GitHubActionsReport    -> --report-gh
Microsoft.Testing.Extensions.HangDump               -> --hangdump --hangdump-timeout 2m --hangdump-type Mini
```

The GitHub extension emits annotations and a job summary only on GitHub Actions and only when `--report-gh` is passed. `GitHubActionsTestLogger` uses the different `--report-github` switch; the switches are not aliases. [11][s11] The MTP hang extension requires its own NuGet package. [12][s12]

Test the failure paths, not only a green run:

1. Add or select a deliberately failing pilot test and confirm a source-linked GitHub annotation, useful stack trace, and non-zero exit.
2. Generate TRX and compare totals, names, outcomes, duration, output, and attachments to the baseline.
3. Run a bounded hang sentinel, confirm the two-minute timeout is honored, and upload a non-empty mini dump.
4. Remove/disable an extension once and confirm the workflow fails clearly instead of silently omitting an artifact.

## Local evidence collected

These are exploratory measurements on macOS 26.6 arm64 with .NET SDK `10.0.400` and runtime `10.0.11`. Coverage was disabled and build outputs were already present. They establish stable variant-A baselines only; they do not satisfy the paired CI gate.

### xUnit v2 no-build samples

| Project | Result | Wall-time samples (seconds) | Median | Nearest-rank p95 / max | Runner-reported samples |
| --- | --- | --- | ---: | ---: | --- |
| `Elsa.Workflows.Core.UnitTests` | 274/274 passed | 0.734154667, 0.704059166, 0.691976291, 0.694611375, 0.693532833 | 0.694611375 | 0.734154667 | 154, 154, 148, 148, 143 ms |
| `Elsa.Workflows.IntegrationTests` | 305/305 passed | 4.228720417, 4.594373917, 4.291529166, 4.598988791, 4.453444542 | 4.453444542 | 4.598988791 | 3, 3, 3, 4, 3 s |

Also proven locally:

- the selected unit project builds and passes under the current xUnit v2 stack;
- the selected integration project builds and passes under the current xUnit v2 stack;
- baseline TRX was generated for the 274-case unit project;
- TUnit `1.66.27` restores in the isolated worktree;
- the repository root runner was not changed.

Not yet proven by these samples:

- an xUnit v3 + MTP pass;
- a complete TUnit + MTP pass for both pilots;
- exact A/B/C concurrency equivalence;
- clean, incremental, coverage, or peak-memory comparisons;
- ten consecutive TUnit passes;
- five paired CI runs;
- Linux/container behavior;
- GitHub annotations, TRX parity, hang dumps, or coverage-threshold failures under MTP;
- Visual Studio, Rider, or VS Code discovery/debugging.

## Evidence matrix

Use “proven” only when the raw artifact is attached. “Mapped” means the expected implementation is known but unverified.

| Requirement | xUnit v2 + VSTest | xUnit v3 + MTP | TUnit + MTP | Gate |
| --- | --- | --- | --- | --- |
| Unit build and pass | **Proven locally**, 274/274 | Not run | All 37 native unit/conformance executables re-listed successfully at 2,645 cases; the final six-project tranche recorded 593 passed, 126 intentionally skipped, and zero failed | Same intended case/name/outcome set, with reviewed discovery deltas |
| Integration build and pass | **Proven locally**, 305/305 | Not run | **Proven locally in Release**: 1,126 discovered cases across all 17 native integration projects, with zero failed or skipped | Same case/name/outcome set |
| Ten consecutive pilot runs | Not required for baseline; 5 timing samples recorded | Not run | All 17 native integration projects completed 10/10 Release runs under their final native scheduling policy (170/170 project runs), with stable case identities and no failure, skip, timeout, or flake; there is no runner-wide cap, and Hosts alone has a narrow keyed constraint on four entry-point cases | No failure or count drift |
| Initial serial policy | Five local samples used current defaults; serial control not yet captured | Not run | Planned with `--maximum-parallel-tests 1` | Exactly one active case in every A/B/C variant |
| Same-class safety after raising cap | Current framework behavior | Project audit required | Project audit required; no custom shim | Native constraints and sentinel pass |
| Unrelated-class concurrency after audit | Current framework behavior | Not in initial serial run | Not in initial serial run | Separate matched-cap experiment |
| Explicit collection semantics | Current framework behavior | Must audit | Native fixture sharing and constraints required | Sharing and non-overlap proven |
| Process-global exclusivity | Known risk; instrumentation collection exists | Must verify | Explicit native constraint begun | No overlap in sentinel/logs |
| Clean build/test timing | Not captured | Not captured | Not captured | Five paired CI samples |
| Incremental build/test timing | Not captured | Not captured | Not captured | Five paired CI samples |
| No-build timing | **5 local samples** | Not captured | Not captured | Five paired CI samples |
| Coverage timing | Not captured | Not captured | Not captured | Five paired CI samples |
| Peak RSS | Not captured | Not captured | Not captured | `/usr/bin/time -v` artifacts |
| Coverage content | Current Coverlet baseline available in CI | Not verified | Tool choice pending | Equivalent denominator/filter |
| Threshold failure | Current MSBuild threshold path | Not verified | Not verified | Deliberate high-threshold non-zero exit |
| Cobertura | Current | Not verified | Supported by candidate MS extension | Parsable and uploaded |
| LCOV/OpenCover | Current | Not verified | Available through `coverlet.MTP`, not the MS extension | Generate and diff or document another decision |
| TRX | Unit local + component CI baseline | Not verified | MTP extension mapped | Totals/names/output/attachments parity |
| GitHub annotations | Current logger in CI | Not verified | MTP extension mapped | Deliberate failure annotation |
| Hang mini dump | Current component VSTest path | Not verified | MTP extension mapped | Bounded hang yields uploaded dump |
| IDE discovery/debug | Current contributor experience | Not verified | Not verified | Exact supported IDE versions recorded |
| Shared helper compatibility | Inventory complete | Not tested | Framework-neutral shared-helper conversion is committed in `48337b14734d995312ba8ff3090e210c582641d2`; dependent integration builds and xUnit scans are clean | No public API regression |
| Conformance discovery | Current custom discoverer | Not tested | One native list: 249 unique cases; each of 10 default-environment runs: 249 total, 123 passed, 126 provider-gated skips, and zero failed | Available/unavailable provider counts match |
| Component fixture lifetime | Current serialized baseline | Not tested | Compatibility analysis pending | Init/dispose/serialization/reporting proven |
| Complete CI job speed | Baseline workflow exists | Not measured | Not measured | Candidate median improves by >=10% |

## Staged implementation plan

Each step should be a small, single-purpose PR or spike commit with a rollback point. Do not combine timing, assertion rewrites, fixture redesign, and CI restructuring.

### Step 1: Freeze the experiment and preserve the baseline

- Pin a common source commit and package versions.
- Record original worktree status and keep all spike changes in separate worktrees.
- Capture current project lists, package graphs, xUnit runner configuration, discovered names/counts, and CI commands.
- Add concurrency sentinels before changing frameworks.
- Store the A/B/C experiment protocol and raw-result schema.

Exit: all variants can be recreated from the same commit and the baseline artifacts are immutable.

### Step 2: Add opt-in test infrastructure

- Introduce central package pins for the chosen TUnit and xUnit v3 packages.
- Add an opt-in `UseTUnit` condition in test build props; do not flip every project.
- Make an opted-in TUnit project executable.
- Retain `xunit.assert` and disable assertion-name conflicts.
- Add the nested MTP `global.json`; do not change root runner selection.
- Add variant-specific artifact directories and a sample-recording harness.

Exit: a non-opted-in xUnit v2 project is unchanged and an empty opted-in TUnit sentinel discovers/runs through MTP.

### Step 3: Migrate and validate the unit pilot

- Convert `Elsa.Workflows.Core.UnitTests` attributes/data only as required to compile.
- Preserve all 274 cases and their meaningful display identities.
- Convert data sources to strongly typed tuples/factories where source generation requires it.
- Isolate process-global instrumentation tests.
- Run the initial A/B/C correctness and timing controls with one active test; add explicit native constraints for known process-global hazards.
- Audit class/collection state before any separate higher-concurrency experiment; do not add a custom compatibility shim.
- Run list/discovery diff, ten consecutive runs, failure-output check, and cancellation/cleanup check.

Exit: 274/274 passes ten times with no count/name drift and concurrency sentinels pass.

### Step 4: Decouple output without breaking public helpers

- Add a leaf-project adapter from TUnit `TestContext` output to the existing helper abstraction.
- Avoid changing public shared-helper signatures during the spike.
- Prove output is associated with the correct test under concurrency.
- Verify output on pass, failure, TRX, and GitHub Actions.
- Design a separate public API migration only if TUnit is selected.

Exit: no lost/interleaved output and no public-package compatibility change.

### Step 5: Migrate and validate the integration pilot

- Migrate `Elsa.Workflows.IntegrationTests` lifecycle and output usage.
- Share only expensive immutable host/container infrastructure.
- Generate unique mutable resource identifiers per invocation.
- Make teardown idempotent and prove it runs after setup/test failure.
- Run 305-case discovery diff and ten consecutive controlled-concurrency passes.

Exit: 305/305 passes ten times with clean resource teardown and no count/name drift.

### Step 6: Build the xUnit v3 + MTP control

- In its own isolated variant, migrate the same two projects to `xunit.v3.mtp-v2`.
- Apply the same MTP version, report/coverage extensions, runner invocation, artifact paths, and concurrency cap as variant C.
- Resolve only xUnit v3 compatibility changes; do not redesign tests.
- Run the same discovery, stability, output, and lifecycle checks.

Exit: variant B is behaviorally equivalent and can distinguish platform gain from framework gain.

### Step 7: Prove reporting, coverage, diagnostics, and IDE support

- Wire TRX and compare contents.
- Wire the GitHub report extension and exercise a deliberate failure.
- Wire hang dumps and exercise a bounded hang sentinel.
- Select the MTP coverage implementation, reproduce filters/denominator, and prove threshold failure.
- Decide how LCOV and OpenCover are retained, converted, or retired.
- Validate discovery, run, debug, filtering, cancellation, and output in each supported IDE and record exact versions.

Exit: no regression in required CI and contributor tooling.

### Step 8: Check component compatibility

- Keep the component suite serialized initially.
- Verify application/collection fixture initialization and disposal counts.
- Verify SQL Server, PostgreSQL, RabbitMQ, host, and tenant state do not leak between cases.
- Verify serialized test arguments and display names.
- Exercise TRX, coverage threshold 23%, GitHub failure annotation, and two-minute mini hang dump.
- Track known #7404 behavior separately.

Exit: a written compatibility result exists; full component conversion remains out of scope unless explicitly approved.

### Step 9: Run paired CI A/B/C experiments

- Use one workflow matrix and identical runner/service configuration.
- Run at least five paired samples, interleaving variant order.
- Attach raw times, peak RSS, case manifests, reports, coverage, and logs.
- Compare complete job medians and project-level breakdowns.
- Attribute gains between A→B (platform) and B→C (framework).

Exit: enough evidence exists to apply the #8101 decision gate.

### Step 10: Make the decision

Choose exactly one outcome:

- **Adopt TUnit**: behavior/tooling gates pass and the complete unit/integration job median improves by at least 10%.
- **Adopt xUnit v3 + MTP only**: most benefit is A→B, while B→C is too small or adds unacceptable migration/maintenance cost.
- **Stay on xUnit v2 temporarily**: neither candidate clears reliability/tooling/performance gates; record blockers and a review date.

Do not infer “adopt” merely because the pilot compiles or because a no-build microbenchmark is faster.

## Accepted production migration plan and commit ledger

The complete conversion is intentionally split into independently reviewable commits. Transitional commits may keep the root runner on VSTest while unmigrated projects remain, but no compatibility shim is permitted and the final tree must contain no xUnit dependency.

### Historical verified rollout snapshot (2026-09-13)

At code commit `f434da9d66c4173c3247fcb52d1352f6c4475300`, 50 of the fixed 55 active test projects recorded at comparison base `37b1a453169201e577ef6db0bfd0348b08070211` have committed native TUnit opt-ins: all 37 unit projects, the already-native `Elsa.Bpmn.Interchange.IntegrationTests`, and 12 newly migrated integration projects. That frozen comparison-base filesystem inventory comprises 37 unit, 17 integration, and one component project; it excludes `test/TlsSmoke` and the BenchmarkDotNet performance project and is not recomputed from every project elsewhere under `test`.

The preceding verified unit/conformance wave is:

| Project | Commit | Verified discovered cases |
| --- | --- | ---: |
| `Elsa.Identity.UnitTests` | `fef6786b196db5e3d0377d858b1ddb9315018576` | 140 |
| `Elsa.Persistence.VNext.UnitTests` | `8905908414675a63e06254185d252e7852f4f541` | 50 |
| `Elsa.Resilience.Core.UnitTests` | `fcc1726158f23dd61652d19f030270cce43ecde2` | 96 |
| `Elsa.Expressions.UnitTests` | `29ef0764c0c60b7ca657ab11632c66cb9127fb95` | 20 |
| `Elsa.Features.UnitTests` | `c70162b58bb183f94832cd76392e73a5faa4c50d` | 10 |
| `Elsa.Hosting.Management.UnitTests` | `02dae9390118c29c14ff6d93f872ade2128c91a9` | 16 |
| `Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests` | `f79ca2d07f88e266a3ffc9b38797749481a547e7` | 18 |
| `Elsa.Diagnostics.StructuredLogs.UnitTests` | `a54f71f020dc43a4247b44dc48dafec6d0b6bd37` | 36 |
| `Elsa.ExternalAuthentication.UnitTests` | `553710d56610890ca331d07ad0ca72315c908447` | 205 |
| `Elsa.Secrets.UnitTests` | `62cee9cc7d5e62de954e3acddc7ddb4553b61806` | 65 |
| `Elsa.UserTasks.Persistence.EFCore.UnitTests` | `a8d0ee2743a7424a01e6386c25686649616b90b3` | 4 |
| `Elsa.UserTasks.UnitTests` | `6ab15bfa95044bfc4540b731272dead4dea8941a` | 103 |
| `Elsa.Workflows.Api.UnitTests` | `071591a1db795663069432953358cacbea6305ef` | 34 |
| `Elsa.Workflows.Management.UnitTests` | `3e5f91a7d85ee90b49c1751b88bce59c3ca5ba99` | 105 |
| `Elsa.Workflows.Runtime.UnitTests` | `e14eeb72439b55013f1e59d526cd6ded8b92318b` | 288 |
| `Elsa.Http.UnitTests` | `2891148701f31445edc304f370357c31def314db` | 37 |
| `Elsa.Shells.Api.Tests` | `e84b5f757daea6f1d0718124dd3caca3c5b220e4` | 6 |
| `Elsa.UserTasks.Persistence.ConformanceTests` | `6a6619626c0619343648abdccdf00433b7905a28` | 249 |
| **Wave total** | **18 projects** | **1,482** |

Together with the preceding 20-project snapshot's 1,284 targeted cases, that earlier rollout covered 38 projects and 2,766 targeted discovered cases: 2,645 unit/conformance cases plus the already-native BPMN Interchange project's 121 cases. The final six-project unit tranche contributed 719 discoveries: 593 passed, 126 intentionally skipped, and zero failed. Each of those six projects completed its focused build, exact native discovery check, semantic conversion audit, and ten consecutive successful runs under TUnit's default parallel policy; no Release configuration is claimed for that earlier tranche.

The `48337b147` integration tranche adds 695 newly migrated cases:

| Project | Verified discovered cases | Ten Release runs |
| --- | ---: | ---: |
| `Elsa.Activities.IntegrationTests` | 123 | 10/10 |
| `Elsa.Bpmn.IntegrationTests` | 63 | 10/10 |
| `Elsa.Dsl.ElsaScript.IntegrationTests` | 23 | 10/10 |
| `Elsa.Http.IntegrationTests` | 2 | 10/10 |
| `Elsa.Alterations.IntegrationTests` | 16 | 10/10 |
| `Elsa.JavaScript.IntegrationTests` | 151 | 10/10 |
| `Elsa.Resilience.IntegrationTests` | 12 | 10/10 |
| `Elsa.Workflows.IntegrationTests` | 305 | 10/10 |
| **Tranche total** | **695** | **80/80 project runs** |

All eight native executables built in Release, matched the exact discovery totals above, reported zero failures and zero skips, and passed ten consecutive runs under TUnit's unconstrained default parallel policy. Their discovery manifests contained no duplicate test UIDs and no duplicate display names within a method. The already-native `Elsa.Bpmn.Interchange.IntegrationTests` was independently reverified at 121 cases for ten Release runs with unique UIDs; its three pre-existing parameterized methods intentionally reuse display labels across rows. Thus the combined integration verification exercised 816 cases per pass, while only 695 are new migration cases. The cumulative committed rollout is 46 projects and 3,461 targeted discovered cases.

#### Follow-on four-project checkpoint

The next project-scoped sequence adds 60 native integration cases without changing the root runner, CI, shared libraries, product code, or any remaining xUnit project:

| Project | Signed code commit | Verified discovered cases | Release build | Default-parallel Release runs |
| --- | --- | ---: | ---: | ---: |
| `Elsa.Common.IntegrationTests` | `8cdb500bb22010c4e70618cdd91f7bfffd758d8f` | 9 | 0 warnings/errors | 10/10 |
| `Elsa.Diagnostics.StructuredLogs.IntegrationTests` | `49913ced0fbf78ce869f0a00d24b36444c57f76a` | 6 | 0 warnings/errors | 10/10 |
| `Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests` | `f47bce7c080f50f625664e565db42684846ec895` | 19 | 0 warnings/errors | 10/10 |
| `Elsa.Diagnostics.ConsoleLogs.IntegrationTests` | `f434da9d66c4173c3247fcb52d1352f6c4475300` | 26 | 0 warnings/errors | 10/10 |
| **Checkpoint total** | **4 signed commits** | **60** | **4/4** | **40/40 project runs** |

Each row was built and executed separately from the isolated worktree root. `<project>` below was the row's exact `test/integration/<Project>/<Project>.csproj` path, `<expected>` was respectively 9, 6, 19, or 26, and every `<unique-run-dir>` was freshly created for that one invocation:

```bash
dotnet build <project> -c Release -f net10.0 -p:CollectCoverage=false --disable-build-servers -m:1
dotnet run --project <project> -c Release -f net10.0 --no-build -- --list-tests json
dotnet run --project <project> -c Release -f net10.0 --no-build -- --minimum-expected-tests <expected> --results-directory <unique-run-dir> --no-ansi --progress off --output Minimal --timeout 2m
```

The run command intentionally contains no `--maximum-parallel-tests` or other parallel limiter. Every one of the 40 reports had exactly the table's total, all unique case IDs, all `passed`, and summary fields `failed=0`, `skipped=0`, `cancelled=0`, `timedOut=0`, and `flaky=0`. Sorted `{id, displayName, status}` manifests matched run 1 through run 10 for every project.

The nine pre-existing custom display identities in `Elsa.Common.IntegrationTests` were preserved exactly:

```text
String is preserved as-is
Byte array is serialized as base64 string
Integer array is serialized as JSON array
String array is serialized as JSON array
String array with multiple elements is serialized as JSON array
Custom class array is serialized as JSON array
List of integers is serialized as JSON array
List with different values is serialized as JSON array
Null returns null
```

The Structured Logs and SQLite discovery manifests matched their six and 19 declared test-method names one-for-one. Console Logs expanded 20 `[Test]` methods and ten `[Arguments]` declarations to 26 unique cases. Its parameterized display identities were exactly:

```text
HubSubscribe_WithConsoleLogsPermission_AllowsAccess(diagnostics/console-logs:view)
HubSubscribe_WithConsoleLogsPermission_AllowsAccess(*)
HubSubscribe_WithConsoleLogsPermission_AllowsAccess(*:view)
RestEndpoints_RequireConsoleLogsPermission(Elsa·Diagnostics·ConsoleLogs·Endpoints·ConsoleLogs·Recent·Endpoint)
RestEndpoints_RequireConsoleLogsPermission(Elsa·Diagnostics·ConsoleLogs·Endpoints·ConsoleLogs·Sources·Endpoint)
RecentEndpoint_MapsLowercaseStreamFilter(stdout, Stdout)
RecentEndpoint_MapsLowercaseStreamFilter(stderr, Stderr)
RecentEndpoint_MapsAllStreamFilterToNull(null)
RecentEndpoint_MapsAllStreamFilterToNull()
RecentEndpoint_MapsAllStreamFilterToNull(all)
```

The SQLite verification harness's first execution itself passed 19/19 and its JSON report passed the exact summary, unique-ID, and manifest checks. The wrapper then returned nonzero because its next bookkeeping operation attempted to copy the run-01 manifest onto the same path. No test invocation intervened: that validated run-01 manifest became the baseline, and runs 02 through 10 all passed 19/19 and matched it exactly. The ten-test-run gate is therefore run 01 plus runs 02-10; the bookkeeping error is not counted as a test failure or as an extra run.

The migration review preserved framework semantics and tightened only project-local ownership. Common's xUnit default string comparisons are explicit `CurrentCulture`; Structured Logs disposes its built service provider and uses ordered collection equivalence; every SQLite host already owns a GUID-named directory and now disables connection pooling so disposal releases its database before directory deletion; Console Logs explicitly owns and disposes every subscription manager. The five reflection-based Console Logs endpoint calls assert that the reflected result is a `Task`, cast it, and await it. Exact exception types, reference identity, collection membership, assertion messages, null/empty checks, and helper assertion ordering were retained. None of the four projects adds a parallel constraint.

#### Final four-project integration tranche

The final integration sequence adds 250 native cases in four project-scoped signed commits. Each project was built separately for `net10.0` in Release, re-listed at its exact expected discovery count, and then executed ten times with a fresh result directory for every invocation:

| Project | Signed code commit | Cases | Run-manifest SHA-256 | Release build | Release runs |
| --- | --- | ---: | --- | ---: | ---: |
| `Elsa.AI.IntegrationTests` | `2ae4fbbc5e79ade082959401c8e5128583143377` | 69 | `2f065b10e7b3ea214ba3dba01c4e59f6cf72d5c845186f93c4ad1c55dab6ff6d` | 0 warnings/errors | 10/10 |
| `Elsa.Diagnostics.OpenTelemetry.IntegrationTests` | `0dfe15206fdd6e29db3b597e9a9b43e21f52bf00` | 23 | `aa896503897fb0c165658b88d50cddb370b3588e3dba0a321c32a99103a11da5` | 0 warnings/errors | 10/10 |
| `Elsa.ExternalAuthentication.IntegrationTests` | `4700ce6362a3b83d4e63e2af9b907ad427fe5989` | 150 | `947964395cb3342d642ff7d33ee63552573c782ed7e401fe4cc3c2d581989cc0` | 0 warnings/errors | 10/10 |
| `Elsa.Hosts.SmokeTests` | `d65a6e74d9be026e55ee0ab2c5d818fee06ea3db` | 8 | `aac333cb36377ae1d59a32c89ca0fc48510f7904eabf157766b600064a9a625a` | 0 warnings/errors | 10/10 |
| **Tranche total** | **4 signed commits** | **250** | **Stable within each project** | **4/4** | **40/40 project runs** |

All 40 reports contained their project's exact case total and only `passed` outcomes; every summary recorded zero failed, skipped, cancelled, timed-out, or flaky cases. The SHA-256 values above are the stable hashes of each project's sorted compact `{id, displayName, status}` run manifest, identical from run 1 through run 10. The commands were project-serial and contained no runner-wide maximum or other project-wide parallel limiter. AI, OpenTelemetry, and External Authentication used TUnit's unconstrained default scheduling. Hosts alone applies `[NotInParallel("ElsaHostsEntryPointProcessState")]` to its four inherited Classic/Modular entry-point cases because the real Programs mutate `ObjectConverter.StrictMode` and console-stream process state; its other four cases remain eligible to overlap.

The migration retained project-specific behavior rather than replacing it with test-only substitutes. OpenTelemetry uses a real TUnit ASP.NET Core factory with automatic test-runner OpenTelemetry and HTTP propagation disabled, and its FIFO sentinel is deterministic rather than timing-based. External Authentication retains exact exception-type, reference, ordered-collection, authorization-matrix, and response-redaction checks while giving every SQLite host an invocation-owned `Pooling=False` database. Hosts boots both real top-level Programs, supplies startup-sensitive settings before their synchronous reads, loads the Modular settings from an invocation-owned JSON overlay so CShells retains object-map shape, and disposes each host before restoring process globals and deleting its temporary root.

One separate supporting metadata commit, `900d7dc4775b3e2d13ad2ef10c54e3e8b6a26054`, aligns `Microsoft.Extensions.Logging.Abstractions` from `10.0.9` to `10.0.11`; it adds no test project or case.

The final-four scoped audit found no xUnit source/configuration references or legacy attributes and no un-awaited TUnit assertions. All four restored graphs contain TUnit `1.66.27` and no xUnit or `Microsoft.NET.Test.Sdk` library; their Release outputs contain no xUnit-named file or `xunit` string in the primary test assembly. The host project contains exactly six declared `[Test]` methods plus two `[InheritsTests]` expansions, 15 awaited assertion sites, and only the two matching keyed constraints described above.

#### Final 54-project native boundary (pre-component checkpoint)

A final native discovery audit re-listed all 54 converted executables successfully. The 37 unit/conformance projects were re-listed from their already-verified Debug outputs and returned 2,645 cases; all 17 integration projects were re-listed from Release outputs and returned 1,126 cases. The combined manifest contains exactly 3,771 cases and 3,771 unique UIDs. The verified per-project outcomes aggregate to 3,645 passed, 126 intentionally provider-gated skips, and zero failed. This is a targeted aggregation, not a claim that one monolithic repository command ran every project. Display names are not claimed to be globally unique: BPMN Interchange intentionally reuses labels across rows in three pre-existing parameterized methods.

The 3,771 figure assumes `ELSA_USERTASKS_TEST_SQLSERVER`, `ELSA_USERTASKS_TEST_POSTGRES`, and `ELSA_USERTASKS_TEST_ORACLE` are unset, as they were during the audit. If `k` of those optional providers is configured, the intended aggregate becomes `3,771 + 7k` discoveries, `3,645 + 49k` passes, and `126 - 42k` skips. The fixed comparison-base inventory now resolves to 54 native projects and one residual component project out of 55.

#### Component migration checkpoint (2026-09-14, before root cutover)

Signed code commit `8a9bc8988ff43608c82a6ba3700761adbe311406` migrates the final active project, `Elsa.Workflows.ComponentTests`, to native TUnit. The fixed comparison-base inventory therefore resolves to 55 of 55 active projects on TUnit. The new `Elsa.Workflows.ComponentTests.Host` project is a non-test dependency and is not a 56th project in that denominator. Adding the component result to the preceding targeted evidence yields 3,974 discoveries: 3,845 passed, 129 intentionally skipped, and zero failed. This remains a per-project evidence aggregation, not a claim that one monolithic command executed all 55 projects.

The component host is a dedicated non-test ASP.NET Core project under `test/component` and uses ordinary `WebApplication.CreateBuilder(args)`. Its entry point records the base Elsa module configuration without applying it. Each case-owned TUnit.AspNetCore factory appends invocation configuration, leases that host's exact process-wide Elsa module-registry entry, and calls `Apply()` exactly once. There is no static test hook or `IsTesting` branch.

One session-shared SQL Server Testcontainer is the only shared component infrastructure. Every expanded invocation receives a fresh `App`, SQL catalog, temporary/cache directories, service graph, and primary pod; Pod2 and Pod3 are created lazily. Catalog creation and the four distinct Elsa schema migrations run once per case inside the native TUnit.AspNetCore server-initialization gate, while all four baseline tenants still activate. Cleanup stops all materialized hosts before dropping that case's catalog and files. No `[NotInParallel]`, `ParallelLimiter`, `IParallelLimit`, runner-wide maximum, or disguised suite serialization is present.

The final default-parallel Release invocation was:

```bash
TUNIT_OTEL_RECEIVER=0 dotnet run --project test/component/Elsa.Workflows.ComponentTests/Elsa.Workflows.ComponentTests.csproj -c Release -f net10.0 --no-build -- --minimum-expected-tests 203 --results-directory /tmp/elsa-8101-tunit-full-final-01 --report-trx --report-trx-filename full.trx --timeout 20m
```

TUnit reported `Passed` with 203 total, 200 passed, three intended skips, zero failed, zero cancelled, zero timed out, and zero flaky in 6m27.8s; its finalized JSON records `totalDurationMs = 387784.054` and 203 unique case IDs. The retained skips are `ActivityRegistrySyncTests.ImportWorkflowActivity_ShouldUpdateOtherPods`, `InputOutputLoggingTests.WorkflowAsActivityInternal_ShouldHonorSettings_WhenExecuting`, and `DeleteWorkflowTests.DeleteWorkflow_Clustered`. The TRX counters are `total=203`, `executed=200`, `passed=200`, `failed=0`, and `notExecuted=3`. The process exit code was 9 only because the directly launched Microsoft Testing Platform 2.4.0 test application applied `--minimum-expected-tests` to executed cases: the supplied value 203 exceeded the 200 non-skipped executions. It was a `MinimumExpectedTestsPolicyViolation`, not a test failure.

Discovery/report parity and execution-floor enforcement are therefore two distinct gates. Component runs must use `--minimum-expected-tests 200`, while the finalized TUnit JSON must independently be checked for exactly 203 total cases. `TUNIT_OTEL_RECEIVER=0` only disables the optional test-runner OTLP receiver in a restricted local environment; it does not alter scheduling or application telemetry. The focused four-tenant cohort also passed 41 of 41 in 1m36.786s after catalog migration was reduced to once per case.

The component project restored successfully outside the restricted sandbox and built in Release with zero warnings and zero errors. Its restored graph contains TUnit 1.66.27 and no xUnit or `Microsoft.NET.Test.Sdk`; the dedicated host graph contains neither test framework. The component source/configuration audit found no legacy xUnit attributes or APIs, and the migration has no `src/**` diff. The previously started ten-cycle component repeat was force-stopped and is invalid evidence; no component repeat-run claim is made.

At the 50-project checkpoint, the scoped audit covered the then-native 13 integration projects and all three shared testing libraries. It found zero xUnit source/configuration references, zero legacy xUnit test attributes, 1,919 `Assert.That` sites and 1,919 matching awaited sites, zero restored xUnit libraries in all 13 `project.assets.json` graphs, TUnit `1.66.27` in all 13 graphs, zero xUnit-named Release files, and zero `xunit` strings in each primary test assembly. Central `TUnit` and `TUnit.AspNetCore` pins remain `1.66.27`.

The shared testing libraries are now framework-neutral and contain no xUnit dependency. `ITestOutputHelper`-based constructors and the public `XunitConsoleTextWriter`, `XunitLogger`, and `XunitLoggerProvider` types were removed in favor of `TextWriter` APIs. This is an intentional source and binary compatibility break with no shim because the accepted migration is atomic within this boundary. Fixture initialization now publishes a provider only after successful activation, partial initialization is disposed, multi-resource teardown attempts every resource in reverse order and aggregates failures, and workflow dispatch hosts are stopped and disposed explicitly. A temporary blocking-workflow sentinel proved that a completion timeout still reaches `StopAsync` and surfaces a shutdown failure; the sentinel was removed after verification.

The container-serialization equivalence call uses a TUnit source-generated custom assertion following TUnit's documented extension model. `EquivalencyAssertionExtensions.cs` declares `EquivalencyAssertionGeneration.IsEquivalentTo<TActual>(actual, expected, strict)` in a `file static` authoring class with `[GenerateAssertion]`; the required `strict` argument avoids colliding with TUnit's built-in overload, and the method delegates to the framework-neutral `TestAssert.Equivalent`. This preserves the existing recursive partial-equivalence semantics; TUnit 1.66.27's built-in enumerable equivalence remains positional and exact-count for this case. The generated overload and its call-site binding were inspected after compilation. [20][s20] [21][s21]

Two deterministic defects exposed by repeated parallel execution were fixed at their source. `ElsaScriptCompiler` now keeps the original reflected `ParameterInfo[]` with each candidate constructor instead of calling `GetParameters()` again and relying on wrapper identity; a cold concurrent production-shaped probe reproduced the old failure in all ten processes and the corrected ElsaScript suite passed ten of ten runs without serialization or warm-up. `ShortGuid` assertions now validate the actual contract—one to 22 alphanumeric characters after removing `/`, `+`, and `=` from Base64—instead of incorrectly requiring at least 19 characters.

`Elsa.UserTasks.Persistence.ConformanceTests` retained exact same-environment discovery parity. Its native list contained 249 unique cases with no duplicate names, and each of ten default-environment runs reported 249 total: 123 passed and 126 were skipped with actionable provider-specific reasons. Available and covered were InMemory, EFCore.Sqlite, and VNext.Sqlite (repository only for VNext). EFCore.SqlServer, EFCore.PostgreSql, and EFCore.Oracle were not covered; each contributed 42 skips. EFCore.MySql has no executable suite and contributes no discovered cases because Pomelo's EF Core 9 dependency is incompatible with this repository's EF Core 10 line. The unavailable cursor theory deliberately emits one inert skipped row, preserving xUnit's collapsed discovery count. If `k` of the three optional providers is configured, the intended totals are `249 + 7k` discovered, `123 + 49k` passed, and `126 - 42k` skipped; that seven-case expansion per available provider already existed under xUnit and is not a TUnit discovery delta.

Parallel constraints remain narrow in the earlier unit/conformance rollout. `StructuredLogSourceRegistryTests` is constrained because it mutates fixed process-environment keys. The conformance suites use provider-keyed fixture sharing and matching keyed `[NotInParallel]` constraints, so tests sharing one provider fixture serialize while different providers and ordinary tests remain eligible to run in parallel. Neither the earlier eight-project integration tranche nor the follow-on four-project checkpoint contains `[NotInParallel]`, a parallel limiter, a parallel group, a runner-wide maximum, or another parallelism cap. In the final four-project tranche, AI, OpenTelemetry, and External Authentication likewise add no constraint; Hosts alone uses the shared named key for its four process-global entry-point cases, without serializing its other cases or the project as a whole.

Three discovery deltas in the 18-project wave are intentional and reviewed rather than duplicate execution. `Elsa.Identity.UnitTests` discovers 140 cases instead of the xUnit runner's 126 because TUnit enumerates all eight non-serializable `Action` rows in each of two `InvalidConfigurations` theories; xUnit aggregated each theory at discovery. `Elsa.Resilience.Core.UnitTests` similarly discovers 96 cases instead of 94 because two deferred non-serializable data rows are enumerated. `Elsa.Workflows.Runtime.UnitTests` discovers 288 cases instead of 282 because `GracefulShutdownOptionsValidationTests.RejectsInvalidConfiguration` and `RejectsInvalidConfigurationDuringStartupValidation` each use four non-serializable `Action<GracefulShutdownOptions>` rows from `InvalidConfigurations`; xUnit aggregated each theory as one discovery case, while TUnit statically enumerates all four, adding three cases per theory and six overall. The same eight behaviors execute, so this is neither duplicate execution nor new coverage. The other fifteen projects match their expected discovered totals in the same environment.

The earlier supporting package-alignment commits are `7696f3d0e5b1390569a6add6133f17897a9caa8a` and `7f3cd8475362dc51fef3acdb73e6d7ef96ddbfa2`; the final tranche's separate dependency-alignment commit is `900d7dc4775b3e2d13ad2ef10c54e3e8b6a26054`. None adds a test project or case.

The baseline's 2,931 figure is a source count of attributed test methods before theory expansion, not a repository-wide runner-discovered case count. Neither the 1,482-case rollout wave, the earlier cumulative 2,766 discoveries, nor the final 3,771 targeted discoveries may be presented as a percentage of 2,931 or as repository-wide discovery parity.

At that checkpoint, the fixed 55-project migration boundary was no longer mixed-runner, but repository-root runner selection and CI had not been switched. There was no root `global.json`; MTP selection remained scoped to `eng/tunit-spike/mtp/global.json`. Targeted native executables were the verified path for the converted projects. These counts are not evidence of a root-level or full-suite MTP pass.

At the component checkpoint, no active test-project execution boundary remains on xUnit: the component project uses TUnit and TUnit.AspNetCore, and its `xunit.runner.json` is removed. Conditional central package/runner wiring still needs removal during the final repository and CI cutover. The performance props still mention removing inherited xUnit, but performance and `test/TlsSmoke` are outside the fixed 55-project denominator.

At the earlier 46-project integration checkpoint, the protected 15-path framework-neutral shared-helper patch was incorporated into `48337b147` together with the eight dependent integration projects. The three shared libraries and all eight migrated projects build for every targeted framework/configuration used by that tranche. A post-restore audit found zero resolved xUnit packages in their `project.assets.json` graphs, zero xUnit-named files in their build output, and zero xUnit strings in 42 scanned first-party DLLs. Source audits found zero xUnit references, zero potentially un-awaited `Assert.That` calls among 1,450 assertion sites, zero temporary trace/sentinel references, and no changed paths outside the accepted tranche plus the `ElsaScriptCompiler` race fix and that evidence.

That checkpoint closed the component-project commit boundary at 55 of 55 active projects and 3,974 targeted discoveries in the default provider environment. It did not switch the repository root or CI to MTP, remove the remaining conditional central xUnit wiring, close the coverage/diagnostics/IDE gates, or claim one repository-wide execution. The current implementation below supersedes that pre-cutover state without changing its recorded evidence.

1. **`docs: research TUnit migration and isolation plan`** — freeze the base SHA, inventories, baseline counts/timings, primary-source findings, accepted scope, risks, and reproducible validation matrix.
2. **`test: add opt-in TUnit MTP infrastructure`** — central package versions, conditional test-project wiring, and a directory-scoped MTP selector. Exit: converted and unconverted projects can coexist without changing the root runner.
3. **`test: decouple shared testing infrastructure from xUnit`** — replace `ITestOutputHelper` and xUnit logger/assertion dependencies with `TextWriter` and framework-neutral shared helpers. Exit: all three shared libraries build for every target framework with zero xUnit references.
4. **`test: migrate unit suites to native TUnit (wave 1)`** — fixture-light and representative core projects; convert attributes, data sources, lifecycle, output, and assertions. Exit: exact discovery parity, no un-awaited assertions, and ten stable default-parallel runs per representative suite.
5. **`test: migrate unit suites to native TUnit (wave 2)`** — remaining unit projects, including data-heavy and global-state cases. Exit: all unit projects pass with native assertions; global hazards have narrow constraints and ordinary tests remain parallel.
6. **`test: migrate integration suites to native TUnit`** — convert output/lifecycle and allocate unique databases, tenants, workflow identities, queues, files, and other mutable resources per invocation. Exit: exact case parity and repeated parallel runs with idempotent cleanup.
7. **`test: migrate conformance and component suites to TUnit`** — completed by the earlier conformance commits and signed component commit `8a9bc8988ff43608c82a6ba3700761adbe311406`; unavailable-provider suppression, component counts/skips, native host lifetime, invocation isolation, and teardown are verified.
8. **`test: enforce isolated parallel execution`** — complete the repository hazard audit, add isolation sentinels, use keyed constraints for genuinely shared resources and unkeyed `[NotInParallel]` only for irreducible process-global state. Exit: independent sentinels overlap, protected sentinels do not, and repeated full runs show no count drift or cross-test contamination.
9. **`ci: switch Elsa tests to MTP`** — implemented in the current worktree with repository-root runner selection, direct native CI commands, TRX, native Cobertura, an aggregate threshold gate, and hang diagnostics. The success path is verified; deliberate coverage-failure and hang-sentinel artifact checks remain.
10. **`chore: remove the final xUnit surface`** — implemented for the active test/build configuration and developer guidance. Historical issue evidence deliberately retains xUnit terminology; fresh restored-graph and built-artifact audits are clean.

The rollout evidence above was gated by focused builds and test runs. Every project batch additionally records discovered/passed/skipped totals and is rerun under its final parallel policy. The final merge gate remains stricter: unchanged intended case set, stable repeated runs, coverage threshold enforcement, reports and diagnostics present, and a zero-xUnit source/package audit.

### Current implementation: repository-root MTP cutover (2026-09-14)

The root `global.json` selects `Microsoft.Testing.Platform`. `Elsa.sln` contains all 60 active test projects. Two checked-in solution filters provide the package-workflow lanes: `Elsa.UnitIntegration.Tests.slnf` contains the 59 unit/integration projects, and `Elsa.Component.Tests.slnf` contains the component project. Current root commands are:

```bash
dotnet test --solution Elsa.sln
dotnet test --solution Elsa.UnitIntegration.Tests.slnf
dotnet test --solution Elsa.Component.Tests.slnf
dotnet test --project test/unit/Elsa.Workflows.Core.UnitTests/Elsa.Workflows.Core.UnitTests.csproj
```

Outer SDK options belong before the literal `--`; options after it are forwarded to each TUnit test application. NUKE no longer exposes or selects a `Test` target. The PR workflow runs `./build.cmd Compile` and then invokes `dotnet test --solution Elsa.sln --configuration Release --no-build` directly. The package workflow restores and tests the two solution filters directly. The workflows remain the source of truth for detailed runner switches while developer examples intentionally stay minimal.

SDK 10.0.400's outer `--minimum-expected-tests` option was verified as an aggregate floor whose count includes all discovered cases. It is not equivalent to the 59-unit/integration and one-component coverage-shard checks and was removed from the active commands under the minimal-command policy. The shard checks prove that the expected test applications emitted coverage, while normal MTP exit behavior still handles zero discovery. CI therefore no longer enforces an exact aggregate case count, and these solution/filter runs do not by themselves prove exact discovery parity.

The accepted coverage contract is Microsoft Testing Platform native Cobertura with `test/coverage.settings.xml`. Each test application writes a collision-safe GUID-named Cobertura shard into its lane-specific results directory; CI requires 59 unit/integration shards and one component shard. The raw lane artifacts remain separate, then ReportGenerator merges all 60 shards and enforces one repository-wide 10% line threshold. This deliberately retires LCOV/OpenCover and replaces the historical per-project 10% default and 23% component thresholds with one aggregate gate.

TUnit's built-in GitHub Actions reporter auto-activates in GitHub Actions, so no external GitHub reporter package or switch is required. Both package lanes retain TRX, while only the component lane enables a five-minute supported mini-dump policy; their result directories are uploaded on every outcome. The five-minute component hang window and aggregate 10% coverage gate supersede the earlier two-minute dump and per-project threshold passages retained above as historical evidence.

Before the upstream synchronization, post-cutover validation completed outside the restricted sandbox. Restore succeeded for both solution filters and `Elsa.sln`, and a Release `--no-restore` solution build completed with zero errors. The exact PR command passed all 3,974 cases (3,845 passed, 129 intentionally skipped, zero failed). The native unit/integration lane passed 3,771 cases (3,645 passed, 126 intentionally skipped, zero failed) and emitted all 54 then-expected Cobertura shards. The native component lane passed 203 cases (200 passed, three intended skips, zero failed) and emitted its one expected shard. ReportGenerator 5.5.11, restored from the repository-local .NET tool manifest, merged all 55 fresh shards without a downstream assembly filter and passed the aggregate gate at 63.1% line coverage. The collector output contains neither `Elsa.*Tests*`/`Elsa.Testing.Shared*` packages nor `Elsa.Workflows.ComponentTests.Host`.

### Post-upstream-sync inventory (2026-09-14)

Synchronizing upstream `main` at `507522469ba810ee8b618ebd60294823436d6820` adds five active test projects: `Elsa.Alterations.Core.UnitTests`, `Elsa.Labels.UnitTests`, `Elsa.Alterations.Persistence.ConformanceTests`, `Elsa.Labels.Persistence.ConformanceTests`, and `Elsa.Workflows.Persistence.ConformanceTests`. The active inventory is now 60 projects: 39 unit, 20 integration, and one component project. The component host remains a non-test dependency, while the performance benchmark and `test/TlsSmoke` remain outside this inventory. `Elsa.sln` and the two solution filters carry the exact 60-project split, and package CI expects 59 unit/integration Cobertura shards plus one component shard. The preceding 3,974-case and 55-shard results remain explicitly pre-sync evidence.

Fresh post-sync validation completed outside the restricted sandbox. `dotnet restore Elsa.sln` evaluated all 168 solution projects successfully, restoring 22 while 146 were already current, and the Release solution build completed with zero errors. The five new projects passed focused native runs of 38, 31, 39, 12, and 16 cases respectively. The exact PR command, `dotnet test --solution Elsa.sln --configuration Release --no-build`, then passed all 4,417 discovered cases: 4,270 succeeded, 147 were intentionally skipped, and none failed. All 60 active restored test graphs resolve TUnit 1.66.27 and its intentional TRX 2.3.3 dependency, with no xUnit or `Microsoft.NET.Test.Sdk`; the filter and workflow audit confirms the 59-plus-one coverage-shard contract and the single aggregate 10% line gate.

## Final migration checklist

Checked items have either the rollout evidence recorded above or completed cutover wiring. Unchecked items still require post-cutover verification before the migration is closed:

- [x] Every active unit, integration, and component project uses TUnit on MTP.
- [x] Historical project-local case manifests match the reviewed intended case set; current solution/filter CI does not enforce exact aggregate discovery parity.
- [x] Skip reasons and display identities match or have reviewed mappings.
- [x] Ten consecutive default-parallel unit-pilot runs pass without count drift.
- [x] Ten consecutive default-parallel integration-pilot runs pass without count drift.
- [ ] Shared fixture initialization/disposal counts match intended lifetime.
- [x] Mutable databases, queues, topics, keys, files, workflows, and tenants are isolated per invocation.
- [x] Process-global tests are exclusive and restore state.
- [ ] Independent parallelism sentinels overlap while constrained sentinels never overlap.
- [x] `ITestOutputHelper` replacement preserves concurrent/failure output.
- [x] Conformance discovery preserves unavailable-provider suppression.
- [x] Native Cobertura uses the translated shared filters and CI guards the expected 59 plus one report shards.
- [x] LCOV/OpenCover are deliberately retired in favor of Cobertura.
- [x] ReportGenerator merges every lane and enforces the single repository-wide 10% line threshold.
- [x] TUnit's built-in GitHub Actions reporter replaces the external reporter package and switch.
- [x] TRX is configured for the native CI lanes.
- [x] The package workflow requests a supported mini dump after a five-minute hang and uploads each lane result directory on every outcome.
- [ ] Post-cutover success, deliberate coverage-failure, and hang-sentinel paths produce the expected artifacts and exit codes.
- [ ] Supported IDEs discover, filter, run, debug, cancel, and show output.
- [x] Root and CI test commands use native MTP `dotnet test --solution` or `dotnet test --project` syntax.
- [x] The active test/build source and configuration contain no xUnit surface; historical issue/specification prose is retained as historical evidence.
- [x] Restored dependency graphs and built artifacts contain no xUnit assemblies.
- [x] Original checkout and unrelated user changes remain untouched.

## Sources

1. [Elsa Core issue #8101 — xUnit v2/VSTest vs xUnit v3/MTP vs TUnit/MTP spike][s1]
2. [Elsa Core issue #8050 — xUnit v3/MTP migration control][s2]
3. [Microsoft Learn — .NET test platform overview][s3]
4. [Microsoft Learn — testing with `dotnet test`, including .NET 10 MTP mode and `--project` syntax][s4]
5. [xUnit.net — Microsoft Testing Platform with xUnit v3][s5]
6. [TUnit — migrating from xUnit.net][s6]
7. [TUnit — controlling parallelism][s7]
8. [TUnit — `TestContext` and test-isolation helpers][s8]
9. [TUnit — method data sources][s9]
10. [TUnit — test lifecycle][s10]
11. [Microsoft Learn — MTP test reports: TRX and GitHub Actions][s11]
12. [Microsoft Learn — MTP crash and hang dumps][s12]
13. [Microsoft Learn — MTP code coverage][s13]
14. [NuGet — TUnit 1.66.27][s14]
15. [NuGet — xunit.v3.mtp-v2 4.0.0][s15]
16. [Elsa Core issue #7965 — process-global security-state race][s16]
17. [Elsa Core issue #7404 — known component-test flake][s17]
18. [Elsa Core baseline test props at `37b1a453`][s18]
19. [Elsa Core baseline package-test workflow at `37b1a453`][s19]
20. [TUnit — source-generator assertions][s20]
21. [TUnit — custom assertions][s21]

[s1]: https://github.com/elsa-workflows/elsa-core/issues/8101
[s2]: https://github.com/elsa-workflows/elsa-core/issues/8050
[s3]: https://learn.microsoft.com/en-us/dotnet/core/testing/test-platforms-overview
[s4]: https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test
[s5]: https://xunit.net/docs/getting-started/v3/microsoft-testing-platform
[s6]: https://tunit.dev/docs/migration/xunit/
[s7]: https://tunit.dev/docs/execution/parallelism/
[s8]: https://tunit.dev/docs/writing-tests/test-context/
[s9]: https://tunit.dev/docs/writing-tests/method-data-source/
[s10]: https://tunit.dev/docs/writing-tests/lifecycle/
[s11]: https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-test-reports
[s12]: https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-extensions-diagnostics
[s13]: https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-code-coverage
[s14]: https://www.nuget.org/packages/TUnit/1.66.27
[s15]: https://www.nuget.org/packages/xunit.v3.mtp-v2/4.0.0
[s16]: https://github.com/elsa-workflows/elsa-core/issues/7965
[s17]: https://github.com/elsa-workflows/elsa-core/issues/7404
[s18]: https://github.com/elsa-workflows/elsa-core/blob/37b1a453169201e577ef6db0bfd0348b08070211/test/Directory.Build.props
[s19]: https://github.com/elsa-workflows/elsa-core/blob/37b1a453169201e577ef6db0bfd0348b08070211/.github/workflows/packages.yml
[s20]: https://tunit.dev/docs/assertions/extensibility/source-generator-assertions/
[s21]: https://tunit.dev/docs/assertions/extensibility/custom-assertions/

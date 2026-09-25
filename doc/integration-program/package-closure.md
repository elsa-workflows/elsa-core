# Inventory-pinned package dependency closure

This rehearsal compares two source-change shapes at the integration-program inventory pins. A change to Core's `src/modules/Elsa/Elsa.csproj` reaches 51 test/build inputs; a Slack module-only change reaches only `Elsa.Slack.Tests`. Both select `Elsa.Slack` as the release unit, and `Elsa.Mqtt` is asserted absent as the unchanged connector control. The inventory graph records zero ambiguous package edges for these paths. These are current-source impact checks, not the separately documented 3.8.4 artifact-proof source commits.

The `paired_selector_assertions` receipt now makes both scenarios a fail-closed CI check: Slack-only must match the manifest's focused tests and select exactly `Elsa.Slack`; the shared Core change must include those Slack tests and expand beyond that focused closure. Its package list is explicitly the selected Slack release unit, not a plan for packages required by a real Core release. The PR workflow uploads this receipt, then runs the mapped local package/consumer proof from the same PR head only after selector success. Manual dispatch still runs the mapped proof if the selector fails, preserving a separate diagnostic path. Neither job publishes packages.

[`release-units.json`](release-units.json) is the validated per-package manifest for the bounded Slack unit. It records the source and mapped project/test paths, framework matrix, tested artifact dependency baseline, sole current publisher, and independent per-package SemVer 2 policy. The inventory graph's source `Elsa` dependency is `3.8.0-preview.5557`; the tested artifact compatibility baseline is `Elsa` `3.8.4`. Those graphs serve different checks. The local `3.8.5-proof` identity is not a release allocation and cannot be published. This manifest does not change the current Extensions publisher or the repository's shared release workflows.

## Clean-run limitation discovered on 2026-09-23

[Manual CI run 35908901399](https://github.com/elsa-workflows/elsa-core/actions/runs/35908901399) passed the released-artifact proof and selector, but failed the source closure. Five Extensions projects (Logging, LDAP, MQTT, Quartz and Service Bus) could not restore explicit `Elsa.*` package references to `3.8.0-preview.5557`. `UseProjectReferences=true` does not replace those test/host package edges. The clean CI run recorded 3,717 passed tests, 3 skips and 5 project restore failures; missing TRX files are reported as failures, never passing tests. The fourth baseline skip belongs to the Service Bus project that did not execute.

Earlier local counts below remain historical execution with a populated package cache. They **do not establish clean pinned-source closure**. The source gate is not accepted. Corrected preflight now rejects source-owned package edges before test execution and records the exact project/package references in its failure receipt. A live check of the clean pinned sources stops at Logging's `Elsa.Testing.Shared`, `Elsa.Testing.Shared.Integration`, and `Elsa.Workflows.Core` package references. No packages were published to make this test pass, and source pins were not silently rewritten.

The final closure proof must run against the reviewed consolidated source graph after #8287, including the actual source-reference transformations and a fresh CI environment. A disposable, inventory-pinned source-binding overlay now provides a separate way to execute the historical two-repository graph without treating cached package restores as source execution. It does not substitute for the final imported graph or complete #8260.

The four recorded baseline cases remain unverified: two Core component tests, the Azure Service Bus topic component test, and Slack `CreateChannelTests.ExecuteAsync`. The earlier #8337 manual run selected 51 inputs, executed 50, and had zero failed projects, but returned incomplete status because those cases were NotExecuted and the performance input was not run; it was also run on an earlier PR head. Final imported-source dependency closure and remote SourceLink validation remain open. This PR gate proves mapped-source package scope and consumers, plus selector expansion; it does not close those gates.

### Disposable source-binding overlay

`source_bindings.py` requires clean Core `610790ec` and Extensions `33fa0bfd` checkouts. It creates a detached Extensions worktree beside the Core checkout and rewrites only the reviewed source-bound edges: 17 reachable Extensions projects reference 26 uniquely owned Core package IDs. Two additional MassTransit projects have no Core package conversion but carry explicit CShells source edges. Across four projects, the external `CShells.Abstractions` project edge becomes the centrally declared `0.0.28` package dependency. The overlay therefore changes 19 project files in total. It retains every original checkout and commit untouched, writes before/after SHA-256 values and exact package-to-project mappings, and rejects an altered overlay or receipt before and after test execution. The conversion preserves each existing conditional `ItemGroup`; unsupported package-reference shapes fail rather than being guessed.

The runner then evaluates outer builds and every declared TFM recursively. Any source-owned package edge, missing project, or reference outside the two supplied source trees still fails before tests. A successful `--preflight-only` command means that graph evaluation passed; it never marks test closure complete. The nonpublishing manual workflow uses the same overlay and uploads its receipt.

On 2026-09-24, the local `--preflight-only` run against those exact pins passed 175 distinct project/property-mode nodes and found zero remaining source-owned package references. It selected the recorded 51 test/build inputs and left `correctness_closure_complete=false` because it did not execute them. This is evaluated source-binding evidence only; the historical 51-input execution, its four known skips, final imported-source closure and publisher cutover remain separate gates.

A focused `--run --only elsa-extensions:test/modules/diagnostics/Elsa.Logging.Core.IntegrationTests/Elsa.Logging.Core.IntegrationTests.csproj` repeated the same passing 175-node preflight and then executed the first formerly blocked project: 2 passed, 0 failed or skipped, with the original Core checkout still clean and the overlay matching its 19-file receipt after execution. The runner returned 3 because 49 required test/build inputs were deliberately deferred by `--only`; it did not report a complete closure. Fresh manual CI remains necessary for the full pinned-source lane.

```sh
python3 scripts/integration-program/source_bindings.py \
  --inventory doc/integration-program/inventory/inventory.json \
  --core /path/to/sources/elsa-core \
  --extensions /path/to/sources/elsa-extensions \
  --overlay /path/to/sources/source-bound-extensions \
  --receipt /tmp/source-binding.json
python3 scripts/integration-program/package_closure.py \
  --inventory doc/integration-program/inventory/inventory.json \
  --source elsa-core=/path/to/sources/elsa-core \
  --source elsa-extensions=/path/to/sources/source-bound-extensions \
  --source-binding-receipt /tmp/source-binding.json \
  --preflight-only --output /tmp/source-bound-preflight.json
```

The mapped-source artifact job also writes a path-and-framework plan for all selector results into its impact receipt. For Extensions paths, each inventory project is mapped through the recorded import relocation rows; Core paths remain in place. The plan records both inventory and mapped source pins and whether each pin matches. It is explicitly not test evidence: no test is counted as executed unless a later TRX receipt matches the mapped path, framework, source revision, and evaluated inputs.

The planner classifies the selected inputs from their recorded project metadata:

- 47 local-test candidates have `Microsoft.NET.Test.Sdk` and no declared Testcontainers package.
- 2 service-backed component projects declare Testcontainers dependencies: Core workflow components, and Extensions Azure Service Bus components.
- `Elsa.TestServer.Web` is a build-only host despite appearing as a test candidate in the inventory; it has no `Microsoft.NET.Test.Sdk` reference.
- The Core performance project is kept in a separate benchmark lane and is not run by this correctness rehearsal.

The local-test classification means only that the project file has no Testcontainers dependency. Test execution can still reveal another runtime requirement. A green planner does not claim every selected project ran. Before execution, the runner evaluates package and project references for all declared frameworks and recursively checks reachable projects. It rejects source-owned package references even if cached, and rejects project paths outside the pinned trees. The Slack release project must resolve `Elsa.csproj` from the supplied Core checkout. Core roots explicitly use `UseProjectReferences=false`; Extensions roots and their graph use `true`, matching execution. This is an evaluated-input preflight, not a substitute for compiling/testing the resulting graph or auditing arbitrary custom build targets. The execution command writes project logs, TRX receipts, source commit receipts and its final working-tree check to the selected output directory.

Every selected test/build input in this inventory declares `net10.0`, so this current-source closure executes that target. If inventory data later declares another target for a selected project, the receipt lists it as untested and the correctness gate remains incomplete. The separate released-package proof exercises the package library framework matrix; this closure does not substitute for it.

Run selector and planner tests in both interpreter modes:

```sh
python -m unittest discover -s scripts/integration-program -p 'test_*.py'
python -O -m unittest discover -s scripts/integration-program -p 'test_*.py'
python scripts/integration-program/package_closure.py \
  --inventory doc/integration-program/inventory/inventory.json \
  --output /tmp/package-closure-plan.json
```

To reproduce the historical unmodified baseline (an expected preflight failure), check out the Core and Extensions commits listed in `inventory.json` as sibling directories named `elsa-core` and `elsa-extensions`, then run:

```sh
python scripts/integration-program/package_closure.py \
  --inventory doc/integration-program/inventory/inventory.json \
  --source elsa-core=/path/to/sources/elsa-core \
  --source elsa-extensions=/path/to/sources/elsa-extensions \
  --run --include-docker \
  --output /tmp/package-closure.json
```

The runner checks both source HEADs and clean tracked/untracked status before and after execution, runs .NET 10 tests sequentially, emits TRX receipts, builds the host input, and enables Extensions project references so Slack resolves to the pinned Core checkout. Each MSBuild evaluation and each build/test command has a 30-minute timeout; the manual workflow also caps the whole source job at 90 minutes. `--include-docker` exercises the two container-backed component projects; omit it to defer them, which makes the result explicitly incomplete. A focused `--only` run reports every omitted local, container-backed, or build-only input as deferred. Benchmarks remain excluded and are reported separately.

Historical cached local execution, not accepted clean-source proof: on 2026-09-23, the full non-Docker local lane ran 47 test projects plus the build-only host at the pinned source SHAs: 3,621 passed, 1 skipped, 0 failed; the host build passed. The Slack project restored and built through its Core project reference, but its sole `CreateChannelTests.ExecuteAsync` test was skipped as “Not implemented yet.” Running both container-backed projects against local Docker produced 203 passed and two skipped in Core workflow components, plus one skipped and zero executed in the ServiceBus component. The component skips report “Clustered tests are interfering with other event driven tests,” “Not yet implemented,” and “TODO.” Across these 49 test-project runs, 3,824 tests passed, 4 were skipped, and none failed. The runner records only the four exact source-pinned `NotExecuted` cases as `known-baseline-skip`, includes their names and reasons in the receipt, and returns exit code 3; closure stays incomplete until the skips have meaningful executed evidence. It does not treat a skipped test as passing. The package-consumer activity smoke in the separate released-artifact proof is evidence for that released source baseline only; it is not substituted for this inventory-pinned closure.

This is a local/CI source-test rehearsal only. It does not pack/publish packages, deploy, or enable package-publishing workflows. Restore reads the configured feeds; feed access is not publication. Builds write ignored `bin`, `obj`, and coverage outputs under the disposable pinned source checkouts.

### Incomplete inventory identities

Preflight also rejects any inventory project marked packable (or with unknown packability) whose package ID is missing. Review exposed an inventory omission for packable `test/TlsSmoke/TlsSmoke.csproj`. Evaluating `PackageId,IsPackable` against the exact recorded Core commit returned `TlsSmoke,true`; that identity is now recorded, and a regression restores the omission to prove it fails before graph evaluation. The receipt retains the intermediate missing-identity failure as historical evidence. The remaining transitive feed/cache references still block source closure; correcting one inventory row does not establish full consolidated package compatibility.

## Prepared canonical 95a project graph

[`canonical-95a-impact-closure.json`](consolidation/canonical-95a-impact-closure.json) refreshes project impact against the prepared canonical `Elsa.sln` and reconciles it with the exact canonical run evidence. Run the preparer from the reviewed tooling checkout, then pass the separate prepared source checkout as `--rehearsal`; the source checkout supplies the solution, project files, build configuration, restore assets, and source receipts. The tooling checkout supplies the CLI, shared closure code, inventory, release-unit manifest, and canonical run receipt. The checked-in 95a source tree still contains an older inert copy of `source-integration.patch`; the receipt separately records the reviewed tooling patch hash and that inert source-copy hash. The selected inputs are Core `95a658b96107ad4dbb280a13972479af74bc6a30`, Extensions `33fa0bfd28c7585240e3d4f665058c067b17e287`, and Studio `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822`, plus the four supplemental patches recorded in the run evidence.

The graph is built from each restored project's `project.assets.json` framework metadata. Every project and assets file, canonical solution, applicable ancestor `Directory.Build.*`, `Directory.Packages.props`, NuGet configuration and canonical NUKE input is hashed in the receipt. Direct project edges retain both the consuming and selected target TFM; this matters because four `net10.0` references resolve to `net9.0`. This is the source-mode Restore graph captured for the rehearsal, not a package-mode graph. External user NuGet configuration and SDK/environment inputs are not included in the source-tree hash set.

Canonical `Elsa.sln` contains 343 project entries and NUKE selected 95 project names ending in `Tests`. The current Core root reaches 70 projects and 52 NUKE-selected inputs on `net10.0`: 49 test projects have passing framework summaries, Azure Service Bus and Slack each have one explicitly skipped case with zero execution, and the performance project was built without a test SDK and has no test run. The `net8.0` and `net9.0` Core closures each reach 13 projects but no selected test projects. These per-TFM outcomes are cross-checked against the committed console summaries and retained TRX records; the global canonical run reports 6,222 framework-summary passes and 146 skipped cases, while the retained TRX set reports 6,056 passes and 148 skips because per-project filenames retain only one of the three Designer framework TRXs and two selected projects contain only a zero-execution skip. The receipt does not treat either skipped test as passing.

The current release-unit manifest declares only Elsa.Slack. Its mapped source graph selects one `net10.0` test project, whose retained result is the explicit “Not implemented yet” skip with zero executed cases. The separate mapped Slack local-package consumer proof remains independent artifact evidence; it does not change that test result. The manifest does not classify the other solution projects as independent release units.

The older inventory plan still contains 51 planned paths. All 51 map into canonical `Elsa.sln`; 50 test/benchmark paths overlap the current `net10.0` Core graph, its `Elsa.TestServer.Web` path is a build-only host, and the canonical graph adds the Connections unit and Secrets API integration tests. This is a comparison only: the old 51-project inventory plan was not the executed canonical run. The canonical NUKE run selected 95 project paths, and the separate receipt records which affected project/TFM entries actually passed or skipped.

After the four supplemental patches, the Workbench sample alone received six MSBuild property evaluations for its declared `net10.0` TFM: Debug/Release × default/source/package-reference property settings. `PackageId` stayed `Elsa.Server.Web`, with `IsPackable=false` and `GeneratePackageOnBuild=false` in every case. `UseProjectReferences=false` changed the property but left the sample's 62 explicit project references and 14 package references unchanged. This is not package-mode restore or Pack evidence. The 2,586-case property matrix remains the earlier pre-patch result; this follow-up does not extend that matrix to all imported projects.

To refresh the receipt, execute from the reviewed tooling checkout and point `--rehearsal` at the read-only prepared 95a source checkout:

```sh
cd /path/to/reviewed-tooling-checkout
PYTHONDONTWRITEBYTECODE=1 python3 scripts/integration-program/refresh_canonical_dependency_graph.py \
  --rehearsal /path/to/prepared-canonical-95a-source \
  --output /tmp/canonical-95a-impact-closure.json
```

The receipt validates the accepted preparation hash from the source-profile ledger and fails on missing projects/assets, stale selected-path receipts, unknown graph targets, altered patch or tool pins, or missing affected project/TFM outcomes. It does not establish clean package-only source closure, public package compatibility, frontend runtime behavior, or successful history-bearing import. In particular, a separate browser observation tracks a missing `DomInterop` bundle under [#8326](https://github.com/elsa-workflows/elsa-core/issues/8326); this .NET graph and NUKE run do not claim that UI runtime path works. The earlier package-source preflight remains blocked and no package was published. No Pack, Push, Publish, deployment, or migration action ran.

## Current-tip pinned canonical closure

The later current-tip source profile pins Core `c4b3ce150160e3c9062b57f7b158fd6b968e1631`, Extensions `ba8b71d91c15ffe5be4b2c539cf9f712e74af775`, and Studio `20ceaeeed7e671f0c9662003e82063026f2216de`. The accepted [mapped build receipt](consolidation/current-tip-c4b3-evidence/mapped-solution-build.json) records five exact supplemental patches and a passing 343-project compile. The independent [local Slack artifact proof](https://github.com/elsa-workflows/elsa-core/pull/8396) packed one synthetic `Elsa.Slack` unit and restored net8.0/net9.0/net10.0 package-only consumers without publishing.

`refresh_canonical_dependency_graph.py --source-profile current-tip-c4b3` evaluates the prepared current-tip graph without borrowing the historical 95a Test receipt. It checks exact source and preparation receipts, all reviewed overlay bytes and their reversed content against the prepared source, and restored project references. The graph contains 343 solution projects plus a Connections credential worker that is restored transitively but is not a NUKE-selected test project. The [six-patch overlay receipt](consolidation/current-tip-c4b3-evidence/reviewed-overlays-six.json) records exact patch names and hashes. No publication target is invoked by the graph refresh.

The first full Test rehearsal **failed** in 21 Studio source-file contract tests that still searched the standalone repository layout. Its [failure receipt](consolidation/current-tip-c4b3-evidence/full-suite-first-run-failure.json) remains historical evidence. A sixth patch made those test-path helpers accept the relocated layout without weakening assertions. A clean full NUKE rerun then passed Restore, Compile and Test: **6,136 passed, 147 skipped, zero failed**, with 95 selected project commands and 94 retained TRX files. The [run evidence and graph receipt](consolidation/current-tip-c4b3-evidence/README.md) reconcile exact source/overlay pins and the NUKE/retained-TRX counters. For a Core source change, the graph reaches 53 selected `net10.0` test projects: 50 passed, two explicitly selected-but-skipped with zero cases executed (Slack and Azure Service Bus), and the performance project was built but has no Test SDK or TRX. The declared Slack unit's selected test is skipped; its separate package-consumer proof is not substituted for that test. This pinned rehearsal does not prove a history-bearing import on current Core `main`, final remote-backed SourceLink, package-mode dependency closure or the sole-publisher cutover. No package was published.

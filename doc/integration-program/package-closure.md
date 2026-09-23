# Inventory-pinned package dependency closure

This rehearsal compares two source-change shapes at the integration-program inventory pins. A change to Core's `src/modules/Elsa/Elsa.csproj` reaches 51 test/build inputs; a Slack module-only change reaches only `Elsa.Slack.Tests`. Both select `Elsa.Slack` as the release unit, and `Elsa.Mqtt` is asserted absent as the unchanged connector control. The inventory graph records zero ambiguous package edges for these paths. These are current-source impact checks, not the separately documented 3.8.4 artifact-proof source commits.

## Clean-run limitation discovered on 2026-09-23

[Manual CI run 35908901399](https://github.com/elsa-workflows/elsa-core/actions/runs/35908901399) passed the released-artifact proof and selector, but failed the source closure. Five Extensions projects (Logging, LDAP, MQTT, Quartz and Service Bus) could not restore explicit `Elsa.*` package references to `3.8.0-preview.5557`. `UseProjectReferences=true` does not replace those test/host package edges. The clean CI run recorded 3,717 passed tests, 3 skips and 5 project restore failures; missing TRX files are reported as failures, never passing tests. The fourth baseline skip belongs to the Service Bus project that did not execute.

Earlier local counts below remain historical execution with a populated package cache. They **do not establish clean pinned-source closure**. The source gate is not accepted. Corrected preflight now rejects source-owned package edges before test execution and records the exact project/package references in its failure receipt. A live check of the clean pinned sources stops at Logging's `Elsa.Testing.Shared`, `Elsa.Testing.Shared.Integration`, and `Elsa.Workflows.Core` package references. No packages were published to make this test pass, and source pins were not silently rewritten.

The final closure proof must run against the reviewed consolidated source graph after #8287, including the actual source-reference transformations and a fresh CI environment. Until then, the historical two-repository baseline remains blocked. This fail-closed characterization does not complete #8260.

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

To reproduce the historical baseline (currently an expected preflight failure), check out the Core and Extensions commits listed in `inventory.json` as sibling directories named `elsa-core` and `elsa-extensions`, then run:

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

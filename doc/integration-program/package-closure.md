# Inventory-pinned package dependency closure

This rehearsal compares two source-change shapes at the integration-program inventory pins. A change to Core's `src/modules/Elsa/Elsa.csproj` reaches 51 test/build inputs; a Slack module-only change reaches only `Elsa.Slack.Tests`. Both select `Elsa.Slack` as the release unit, and `Elsa.Mqtt` is asserted absent as the unchanged connector control. The inventory graph records zero ambiguous package edges for these paths. These are current-source impact checks, not the separately documented 3.8.4 artifact-proof source commits.

The planner classifies the selected inputs from their recorded project metadata:

- 47 local-test candidates have `Microsoft.NET.Test.Sdk` and no declared Testcontainers package.
- 2 service-backed component projects declare Testcontainers dependencies: Core workflow components, and Extensions Azure Service Bus components.
- `Elsa.TestServer.Web` is a build-only host despite appearing as a test candidate in the inventory; it has no `Microsoft.NET.Test.Sdk` reference.
- The Core performance project is kept in a separate benchmark lane and is not run by this correctness rehearsal.

The local-test classification means only that the project file has no Testcontainers dependency. Test execution can still reveal another runtime requirement. A green planner does not claim every selected project ran. Before execution, the runner evaluates Extension project references with MSBuild and fails unless they resolve within the pinned Core or Extensions trees; the Slack release project must resolve `Elsa.csproj` from the supplied Core checkout. The execution command writes project logs, TRX receipts, source commit receipts and its final working-tree check to the selected output directory.

Every selected test/build input in this inventory declares `net10.0`, so this current-source closure executes that target. If inventory data later declares another target for a selected project, the receipt lists it as untested and the correctness gate remains incomplete. The separate released-package proof exercises the package library framework matrix; this closure does not substitute for it.

Run selector and planner tests in both interpreter modes:

```sh
python -m unittest discover -s scripts/integration-program -p 'test_*.py'
python -O -m unittest discover -s scripts/integration-program -p 'test_*.py'
python scripts/integration-program/package_closure.py \
  --inventory doc/integration-program/inventory/inventory.json \
  --output /tmp/package-closure-plan.json
```

To execute against the exact source graph, check out the Core and Extensions commits listed in `inventory.json` as sibling directories named `elsa-core` and `elsa-extensions`, then run:

```sh
python scripts/integration-program/package_closure.py \
  --inventory doc/integration-program/inventory/inventory.json \
  --source elsa-core=/path/to/sources/elsa-core \
  --source elsa-extensions=/path/to/sources/elsa-extensions \
  --run --include-docker \
  --output /tmp/package-closure.json
```

The runner checks both source HEADs and clean tracked/untracked status before and after execution, runs .NET 10 tests sequentially, emits TRX receipts, builds the host input, and enables Extensions project references so Slack resolves to the pinned Core checkout. Each MSBuild evaluation and each build/test command has a 30-minute timeout; the manual workflow also caps the whole source job at 90 minutes. `--include-docker` exercises the two container-backed component projects; omit it to defer them, which makes the result explicitly incomplete. A focused `--only` run reports every omitted local, container-backed, or build-only input as deferred. Benchmarks remain excluded and are reported separately.

On 2026-09-23, the full non-Docker local lane ran 47 test projects plus the build-only host at the pinned source SHAs: 3,621 passed, 1 skipped, 0 failed; the host build passed. The Slack project restored and built through its Core project reference, but its sole `CreateChannelTests.ExecuteAsync` test was skipped as “Not implemented yet.” Running both container-backed projects against local Docker produced 203 passed and two skipped in Core workflow components, plus one skipped and zero executed in the ServiceBus component. The component skips report “Clustered tests are interfering with other event driven tests,” “Not yet implemented,” and “TODO.” Across these 49 test-project runs, 3,824 tests passed, 4 were skipped, and none failed. The runner records only the four exact source-pinned `NotExecuted` cases as `known-baseline-skip`, includes their names and reasons in the receipt, and returns exit code 3; closure stays incomplete until the skips have meaningful executed evidence. It does not treat a skipped test as passing. The package-consumer activity smoke in the separate released-artifact proof is evidence for that released source baseline only; it is not substituted for this inventory-pinned closure.

This is a local/CI source-test rehearsal only. It does not pack packages, access a feed, publish, deploy, or enable package-publishing workflows. Builds write ignored `bin`, `obj`, and coverage outputs under the disposable pinned source checkouts.

# Activity compatibility on the history-preserving import

Program #8194, tasks #8313 and #8325. This reruns the offline activity probe against the actual history-preserving import, not the mapped rehearsal behind the [static](activity-compatibility.md), [generated](generated-activity-compatibility.md) and [GitHub activity-ID](github-activity-id-compatibility.md) evidence. No activity body executes. The probe makes no provider call, uses no credential, and does not pack or publish a package. The earlier rehearsal evidence is unchanged and remains the historical record.

## Source under test

- Import head `ce1111e4421b0e2af9852d55d9e2ebde937fcab5` (draft consolidated import). Its Extensions parent is `ba8b71d91c15ffe5be4b2c539cf9f712e74af775`: the rehearsal pin `33fa0bfd` plus one `base_version` commit that does not touch the GitHub module. The imported `Elsa.DevOps.GitHub` sources equal that parent except for the import's `.csproj` edit.
- At that head, #8325 existed only as Core-side tooling: the stored patch, the offline migration utility (including the #8338 nested-container guard), the ADR and the mapped-source evidence. All of it was byte-identical to the #8338 merge. The imported GitHub module had no version-2 classes and no identity tests.
- `1774859de4b1fc8444981c0a34b128b60e0277a6` ports the reviewed patch into the imported layout. `3add8104f72986f44adcb496a880a9c1db656f63` adds the reviewed matrix entry and its guard. Every .NET run below used a clean detached clone of `3add8104f` outside the worktree; each receipt records an empty `git status`.
- The released baseline is unchanged: exact NuGet 3.8.4 packages for 16 assemblies, restored from nuget.org and the local package cache. `Elsa.Ldap` and `Elsa.Mqtt` still have no released baseline. SDK 10.0.300 is pinned.

The harness targets the imported layout directly. `matrix.json` already used `src/extensions/...` project paths, and all 18 resolve in the import, so no path mapping was needed.

## Porting #8325 into the import

The four version-2 activities (`DeleteCommentV2`, `GetCommentV2`, `UpdateCommentV2`, `GetGistV2`) and the focused identity tests come from [extensions-v2.patch](../../../scripts/integration-program/github-activity-id-compatibility/extensions-v2.patch). Two path mappings were applied: `src/modules/devops/` to `src/extensions/devops/`, and `test/modules/devops/` to `test/extensions/modules/devops/`. The test project's GitHub `ProjectReference` gains one `../` because the imported test folder is one directory deeper. The project is registered in `Elsa.sln`, using the deterministic GUID scheme from `prepare_consolidated_build.py`, and in `Elsa.Extensions.slnf`. The version-1 classes are unchanged, as the [ADR](../../adr/2026-09-24-version-github-activities-with-provider-ids.md) requires.

Byte-order marks follow each directory. The four activity files carry a BOM, like every neighbouring imported activity file. The new test project has none, like every `test/extensions/modules` project file and most of their sources.

The historical runner reproduces the mapped-source proof and stays unchanged. [run_imported_github_activity_id_tests.py](../../../scripts/integration-program/run_imported_github_activity_id_tests.py) targets the import instead:

1. It requires a clean checkout.
2. It checks that the six imported files equal the reviewed patch after the documented mapping; only a leading BOM may differ.
3. It runs the focused tests in place and requires the tree to stay clean afterwards.

`test_run_imported_github_activity_id_tests.py` binds the committed port to the reviewed patch.

The [focused receipt](activity-compatibility-evidence-imported-ce1111e4/github-id-focused-receipt.json) records **6/6 tests passed on net10.0**. All six files match the patch, and the source was clean before and after. `test/extensions/modules/Directory.Build.props` pins net10.0 as the only test target. Version-2 behavior on net8.0 and net9.0 therefore rests on the probe below, not on these tests.

## Results

Frameworks: net10.0, net9.0 and net8.0, each run against source `3add8104f`. The released host uses the 3.8.4 packages; the source host uses the imported projects. Full data is in the [summary](activity-compatibility-evidence-imported-ce1111e4/summary.json).

| Per framework | Released host | Imported source |
| --- | ---: | ---: |
| Descriptors (static + generated) | 134 (118 + 16) | 147 (131 + 16) |
| Activities in the round-tripped workflow | 130 | 143 |
| Same-host round-trip failures | 5 | 4 |
| Abstract base classes excluded | 13 | 13 |
| Build errors / warnings | 0 / 0 | 0 / 103 |
| Probe exit code | 1 | 1 |

All three frameworks give identical numbers. The descriptors that are new in the source host fall into three groups:

- the nine reviewed LDAP/MQTT contracts, which have no released baseline;
- the four reviewed GitHub version-2 contracts;
- nothing else: there are no unexpected additions, missing descriptors or changed descriptors.

The released workflow is imported into the source host in both its first-write and canonical forms. Contracts, identities and typed inputs are preserved. The six negative probes reject each mutation on every framework: type, ID, version, literal, first-write ID and generated literal, for 18 rejections in total. All 7,854 hashed source inputs stay unchanged during every run. Each `run.py` exits 1 because failures remain, as described below; `verify_probe.py` exits 0.

Generated descriptors come from the real providers under the controlled fixtures: 1 Agent, 4 Orchard, 2 MassTransit and 9 Telnyx webhook descriptors, in both hosts. All 16 round trips pass in the source host. The Agent root still fails in the released host.

## Accounting for every difference

1. **Four GitHub version-1 identity failures: kept, not allowlisted.** `Comments.DeleteComment`, `Comments.GetComment`, `Comments.UpdateComment` and `Gists.GetGist` at version 1 fail the same-host round trip in both hosts on every framework. The imported source serializes each version-1 payload byte-identically to the released 3.8.4 host. The version-1 contract is therefore unchanged, and so is its defect: the serialized `id` holds the provider `Input`, so the Elsa activity ID is not preserved. These activities stay in the descriptor and failure evidence. The released-host failures are historical evidence and have not been relabeled. The supported path is the ADR's version-2 activities together with the bounded offline [migration utility](../../../scripts/integration-program/github_activity_id_migration.py). That utility requires an operator-approved new activity ID for each affected activity, bound to the SHA-256 of the original file. It rejects `Elsa.For`, `Elsa.StateMachine`, flowchart topology and other unsupported containers without writing partial output. A historical document that has already lost its activity ID cannot be recovered; the migration needs explicit input.
2. **Four GitHub version-2 descriptors: approved only by a separate, reviewed record.** `Elsa.DevOps.GitHub` has a released baseline, so the existing source-only allowlist does not admit additions to it. The matrix row now carries `reviewedReleasedAssemblyAdditions` (review `elsa-core#8325`, revision 1), listing exactly the four normalized contracts. The receipts report these four under `reviewedReleasedAssemblyAddedDescriptors`. Any other addition, a different assembly, or any change to their CLR type, kind, inputs, outputs or ports is still unexpected. Each version-2 contract differs from its version-1 contract only in the provider input name: `Id` becomes `CommentId` or `GistId`, with the same contract type. On every framework, workflow ID, type, version and typed provider literal survive serialization, restoration and reserialization, individually and inside the source workflow (143 = 139 + 4).
3. **Generated Agent root: fixed in the source host only.** `Elsa.Agents.AgentActivity.Summarize` still loses its synthetic inputs when serialized directly as a root in the released 3.8.4 host. The import preserves them through the [#8323 serializer change](root-synthetic-activity-serialization.md), whose `JsonActivitySerializer.cs` is unchanged since `95a658b96`. This is the only reason the source host has four failures against the released host's five. The released-host failure remains historical #8320 evidence.
4. **Rehearsal to import: no relocation contract change.** On all three frameworks, the released-host contracts equal the retained rehearsal evidence. The source-host contracts differ from the mapped rehearsal only by the four version-2 additions; none are removed or changed (`historicalComparison` in the summary). The [guard replay](activity-compatibility-evidence-imported-ce1111e4/historical-guard-replay.json) runs the new guard over the retained static and generated rehearsal evidence. It classifies exactly as before: nine allowed, zero reviewed, zero unexpected. The pre-change harness remains at `ce1111e4` (`run.py` `78fa4b26…`, `matrix.json` `5eb6437d…`).
5. **Build warnings: 92 to 103.** This is not a compatibility result, and neither count is a security approval. The rehearsal logs contain 46 NU1902 warnings and 46 SourceLink "repository has no remote" warnings, and no compiler diagnostics at all. This run compiled a clean clone that has Git metadata. It records:
   - 46 NU1902 warnings;
   - 53 compiler and trimming warnings, including 4 CS0108 warnings for the hidden version-1 `Id` members and 17 CS0618 obsolete-API warnings;
   - 4 Fody "no configuration for ConfigureAwait" warnings.
6. **Source input count: 7,694 to 7,854.** The imported tree contains more build inputs than the rehearsal, from the import integration, later Core merges and this port. The runner hashes all of them before and after each run.

## Verification

- Python tests pass in normal and optimized mode: the harness (15, including 5 new tests for the reviewed-addition guard) and the new imported-layout runner (7). The integration-program suite runs 361 tests in each mode, and 358 pass. The three failures all report the same ledger pin, described in the last bullet. At `ce1111e4` those three tests pass.
- The source-tip verifiers `verify_import_source_tip_refresh.py` and `_r2` through `_r6` pass on the import head before and after the port.
- `validate_legacy_asset_dispositions.py`, `test_legacy_asset_dispositions.py` (2 tests) and `test_current_tip_legacy_assets.py` (1 test) now fail with one error: `completed asset active blob or mode changed: extensions/Elsa.Extensions.sln`. The legacy ledger pins `Elsa.Extensions.slnf` at blob `ccbdccee…` (#8496). Registering the test project in `Elsa.sln` requires the matching filter entry; `test_product_solution_filters.py` enforces this. That entry changes the pinned blob. As with #8480, the ledger completion must be repointed to the pull request that lands this change, with its merge commit. That evidence does not exist yet, so the ledger is left untouched.

## Remaining gaps and limits

- Configuration-generated identities cover one controlled Agent schema, one Orchard content type, one MassTransit CLR message and the current Telnyx attributed payloads. The following are not proven: arbitrary Agent schemas, Orchard content types or MassTransit messages; historical tenant configurations; final-host DI registration.
- Provider behavior, live dispatch, webhooks, external permissions, durable execution and a production workflow corpus are outside this probe. So are SourceLink identity, final package versions (this is an unversioned development build) and release packaging.
- The migration utility follows only `Root`/`root` and `Elsa.Sequence`. Other topologies need a reviewed migration.
- NuGet restore used nuget.org and the ambient package cache. This is not a fresh-cache or feed-signature attestation.
- As in the historical TRX, the focused TRX keeps the test host's computer name. Sanitization replaces machine-local paths only (`$SOURCE`, `$EVIDENCE`, `$OUTPUT`).
- Two gates remain: final CI on the import pull request, and landing the import branch itself.

## Evidence and reproduction

The [evidence directory](activity-compatibility-evidence-imported-ce1111e4/) keeps per-framework receipts, host results, build and probe logs, and negative-probe inputs, results and logs, all gzip-compressed. It also keeps the source-input hashes (identical for all frameworks), the focused-test receipt, log and TRX, and the guard replay. For every artifact, `summary.json` records the original, sanitized and compressed SHA-256.

```sh
git clone --no-checkout /path/to/import-worktree /tmp/import-src
git -C /tmp/import-src checkout --detach 3add8104f72986f44adcb496a880a9c1db656f63
H=/tmp/import-src/scripts/integration-program
for fw in net10.0 net9.0 net8.0; do
  python3 $H/activity-compatibility/run.py --source-tree /tmp/import-src \
    --output /tmp/activity-proof-$fw --framework $fw
  python3 $H/activity-compatibility/verify_probe.py \
    --evidence /tmp/activity-proof-$fw --output /tmp/activity-negative-$fw
done
(cd $H && python3 run_imported_github_activity_id_tests.py \
  --source-tree /tmp/import-src --output-dir /tmp/github-id-imported)
python3 -m unittest discover -s scripts/integration-program/activity-compatibility -p 'test_*.py'
python3 -O -m unittest discover -s scripts/integration-program/activity-compatibility -p 'test_*.py'
```

Run one source build at a time; the proof hosts share the referenced projects' output directories. Output paths must be new and outside the source tree. Each `run.py` is expected to exit 1 while the four version-1 GitHub failures remain.

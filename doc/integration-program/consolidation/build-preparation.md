# Consolidated source build preparation

Program #8194, feature #8214, story #8286, task #8287. This is a disposable build experiment after the history rehearsal, not the history-bearing import or a release candidate.

## Recorded inputs and active work

The experiment uses Core `8e893e02c4ac089d526b0a0d294a8546f021d072`, Extensions `33fa0bfd28c7585240e3d4f665058c067b17e287`, and Studio `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822`. The [upstream work ledger](upstream-work.json) records a later read of repository tips and open PR heads. Those later tips are not silently substituted into these observations. Refresh and reconcile that ledger before preparing the real import; preserve original ancestry and active contributor branches.

The rehearsal preserved 9,635 file blobs/modes (5,864 Core, 1,665 Extensions, 2,106 Studio) and all three original histories. Five colliding Secrets projects remain inert `.source` evidence under the existing relocation policy. The remaining legacy Secrets API/Core/Models/Management/Scripting projects are retained for compatibility work. A successful build does not authorize registering both sets of endpoints or switching existing databases.

## Repeat the preparation

Create a new rehearsal with [rehearse-import.py](../../../scripts/integration-program/rehearse-import.py), using a clean Core checkout at the exact Core commit above and full source histories at the Extensions/Studio pins. Then materialize that disposable repository and apply the reviewed integration patch:

```sh
git -C /path/to/new-rehearsal checkout rehearsal
git -C /path/to/new-rehearsal restore --source=HEAD --worktree .
python3 scripts/integration-program/prepare_consolidated_build.py \
  --rehearsal /path/to/new-rehearsal
```

The preparer recomputes the relocation map from pinned Git trees, verifies all parent commits and the exact rehearsal tree, and rejects tracked edits, remotes, unexpected files (including ignored files), receipt drift, and different source pins. It never resets or deletes user work. It applies the checked-in patch only after `git apply --check` passes, generates stable solution project GUIDs, and writes `consolidated-build-receipt.json` with the patch digest and resulting file hashes. It does not invoke .NET, commit, push, pack or publish. Re-running against an already prepared checkout is rejected: keep the log and use another disposable rehearsal when the patch changes.

This tool trusts the local Git installation and the reviewed tool/patch. Builds additionally trust the installed SDK, environment, user NuGet configuration and caches. Use a trusted development account; this preparation is not a sandbox or a hermetic release proof.

The patch integrates 166 imported projects and one new regression-test project with Core's existing solution. It rewrites references to local Core/Studio/Extensions projects, retains separate upstream central dependency baselines, keeps external CShells dependencies as packages, repairs relocated assets, and scopes build properties/targets. Test/sample central-version wrappers import their corresponding source baseline instead of duplicating it. A static comparison also confirms that all 166 imported project files retain their explicit `PackageId`, `AssemblyName` and `RootNamespace` declarations; evaluated and packed public identity checks remain a later gate. Imported projects are nonpackable and automatic generation of packages is disabled; original upstream publishing workflows remain inert. Fody behavior remains scoped to its original product instead of silently inheriting Core's different guard.

Two diagnosed compatibility changes are explicit in the patch:

- Agents no longer relies on Studio transitively supplying archived Blazored.FluentValidation. Both button and form submission await the existing async name validator, using a component-owned message store, stale-response revision guard and disposal guard. Six new tests cover pending uniqueness, duplicate messages, field changes including change-back, direct name changes, competing responses and disposal.
- The `BlazorApp1` sample clears inherited `TargetFrameworks` while retaining its declared `net8.0`. Previously the combination restored multiple framework asset versions into a single-framework build and caused a CustomElements static-asset conflict.

## Validation and remaining gates

Run builds with each project's declared target frameworks. Do not force every project to net10: Core's MySQL provider currently declares only net8/net9.

```sh
cd /path/to/new-rehearsal
dotnet build Consolidated.sln -p:UseProjectReferences=true \
  -p:IsPackable=false -p:GeneratePackageOnBuild=false -m:1
dotnet test test/extensions/modules/agents/Elsa.Studio.Agents.Tests/Elsa.Studio.Agents.Tests.csproj \
  -p:UseProjectReferences=true -p:IsPackable=false -p:GeneratePackageOnBuild=false -m:1
```

Observed on 2026-09-23 in the original disposable experiment:

| Check | Result |
| --- | --- |
| WorkflowContexts Studio module, mapped source, net10 | Build passed, 32 warnings |
| Full solution using declared frameworks, before compatibility fixes | Failed: 25 errors, 10m30s; no full-build success claim |
| Agents module after compatibility fix, net8/net9/net10 | Build passed, 122 warnings |
| New Agents submission regressions, net10 | 6 passed, 0 failed, 0 skipped |
| Fixed net8 Studio sample | Build passed, 70 warnings |
| Preparation replay in a second clean rehearsal | Patch applied and receipt generated; 6 Agents tests passed and net8 sample rebuilt (0 errors, 70 warnings) |
| Preparation safety regressions, normal and optimized Python | 7 passed in each mode |

The full build exposed missing `IWorkflowDefinitionStore.TryUpdateLatestAsync` implementations in Dapper and MongoDB. These remain blockers, tracked separately in #8293 (Dapper) and #8294 (MongoDB). They require real tenant-scoped database compare-and-swap behavior, including atomic old/new version transitions; a throwing stub or process-local lock would not satisfy the contract. #8292 separately fixes a document-edit caller that could overwrite intervening metadata before the store loaded its compare-and-swap snapshot. The first forced-net10 diagnostic was invalid for the MySQL target matrix and was stopped; its partial output is not validation evidence.

Warnings include existing nullable/analyzer findings, dependency advisories, and expected empty SourceLink metadata in the repository with no remote. Keep these visible and assess release-blocking advisories before publication. This experiment does not establish complete-solution tests, public package compatibility, publishable SourceLink, migrated-host authentication, browser/debugger behavior in the final layout, or Secrets database/endpoint compatibility. Those remain program integration gates.

Do not push the synthetic rehearsal commit. The real import must preserve source ancestors with a normal merge after the compatibility and publishing gates are accepted. Package publication, production/cutover and repository archival retain their explicit approval boundaries.

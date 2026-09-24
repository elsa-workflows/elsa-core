# Canonical solution build preparation

Program #8194, feature #8214, story #8286, task #8287. This is a disposable build experiment after the history rehearsal, not the history-bearing import or a release candidate.

This preparation adds imported projects to Core's canonical `Elsa.sln` in the disposable rehearsal. The earlier `Consolidated.sln` build and targeted tests remain separate historical evidence. A canonical NUKE `Test` run has now completed successfully for the selected 95a profile and its four recorded supplemental patches; this does not validate current Core `main` or the history-bearing import.

## Recorded inputs and active work

The original rehearsal profile uses Core `8e893e02c4ac089d526b0a0d294a8546f021d072`, Extensions `33fa0bfd28c7585240e3d4f665058c067b17e287`, and Studio `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822`. The [upstream work ledger](upstream-work.json) records a later read of repository tips and open PR heads. Those later tips are not silently substituted into these observations. Refresh and reconcile that ledger before preparing the real import; preserve original ancestry and active contributor branches.

The rehearsal preserved 9,635 file blobs/modes (5,864 Core, 1,665 Extensions, 2,106 Studio) and all three original histories. Five colliding Secrets projects remain inert `.source` evidence under the existing relocation policy. The remaining legacy Secrets API/Core/Models/Management/Scripting projects are retained for compatibility work. A successful build does not authorize registering both sets of endpoints or switching existing databases.

## Current Core integration profile

The preparer retains Core `076f022cc174d497af26fc8e26414970e61a79b1` with the same recorded Extensions and Studio commits. This explicitly reviewed profile includes the merged credential lifecycle and workflow bindings plus EF/BPMN compare-and-swap corrections. The source-profile ledger records a follow-up Core pin at `95a658b96107ad4dbb280a13972479af74bc6a30`, which was verified as Core `main` when selected; the ledger separately records the later verified Core `main` tip `77f3ca92eb3e514af959404f67ea538b07cba98f`. Extensions and Studio `main` remained at their recorded pins in both checks. Core 076 is an ancestor of 95a. Their tracked `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, `NuGet.Config`, `src/Directory.Build.props`, and `build/Build.cs` blobs match; neither pin has a root `global.json`. The 95a solution adds the PostgreSQL credentials persistence project and the PostgreSQL-credentials and Secrets API integration-test projects. The integration-program CI workflows also changed; the package publisher workflow is unchanged. A path-only screen found no overlap between the 185 source-integration patch paths and the 277 Core paths changed since 076. Since 95a, verified upstream main has advanced through #8318 package-boundary tooling, #8321 PostgreSQL worker integration fixtures, and #8324 canonical preparation tooling; the ledger records these deltas and distinguishes the current tip from the historical rehearsal pin. These build-input and solution-membership comparisons complement, but do not replace, the canonical 95a run evidence below.

All three reviewed profiles retain the same source-integration patch. The original 8e baseline and reviewed 076 profile remain available; 95a is an explicitly allowlisted follow-up pin, not current Core main. Its fresh raw rehearsal preserves all 9,993 source blobs and modes plus the three original histories. The canonical preparer evaluated a 167-project imported/added property matrix; canonical `Elsa.sln` contains 343 project entries overall. Four additional current-main compatibility/test-layout patches were then applied to the disposable rehearsal before the canonical NUKE run. Arbitrary Core commits remain rejected; source trees, original parents, complete relocation mapping, and clean workspace are verified for the selected profile. The receipt records the actual selected commits. The [current-Core build proof](current-core-build.md) records a successful 340-project `Consolidated.sln` build and 214 targeted tests on 076; that historical result is not canonical `Elsa.sln` validation. The [source-profile ledger](canonical-source-profile-ledger.json) preserves exact pins, raw rehearsal evidence, current-main delta, and the bounded build-input comparison.

## Repeat the preparation

Create a new rehearsal with [rehearse-import.py](../../../scripts/integration-program/rehearse-import.py), using a clean Core checkout at the selected exact Core commit and full source histories at the Extensions/Studio pins. For the follow-up profile, checkout Core `95a658b96107ad4dbb280a13972479af74bc6a30`, Extensions `33fa0bfd28c7585240e3d4f665058c067b17e287`, and Studio `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822`; then materialize that disposable repository and apply the reviewed integration patch:

```sh
git -C /path/to/new-rehearsal checkout rehearsal
git -C /path/to/new-rehearsal restore --source=HEAD --worktree .
python3 scripts/integration-program/prepare_consolidated_build.py \
  --rehearsal /path/to/new-rehearsal
```

The preparer recomputes the relocation map from pinned Git trees, verifies all parent commits and the exact rehearsal tree, and rejects tracked edits, remotes, unexpected files (including ignored files), receipt drift, and different source pins. It never resets or deletes user work. It applies the checked-in patch only after `git apply --check` passes, generates stable solution project GUIDs in `Elsa.sln`, and writes the established `consolidated-build-receipt.json` filename plus `canonical-packability-report.json`. The receipt retains the existing `buildCompatibilityVerified: false` field for downstream readers and adds canonical-solution-specific fields; `canonicalBuildAndTestsVerified` remains false until the canonical build and test gate runs. The report records effective `PackageId`, `IsPackable`, and `GeneratePackageOnBuild` for every mapped imported project and the added test project, for each evaluated target framework in Debug and Release, with default, source-reference (`UseProjectReferences=true`), and package-reference (`UseProjectReferences=false`) property modes. It fails unless every imported project is non-packable and disables package generation on build. These are property-evaluation checks, not a compile, test, pack or publisher execution. SDK selection is rooted at the disposable rehearsal and each MSBuild evaluation runs from its project directory. The preparer does not commit, push or publish. Re-running against an already prepared checkout is rejected: keep the log and use another disposable rehearsal when the patch changes.

This tool trusts the local Git installation and the reviewed tool/patch. Builds additionally trust the installed SDK, environment, user NuGet configuration and caches. Use a trusted development account; this preparation is not a sandbox or a hermetic release proof.

The patch integrates 166 mapped imported projects and one new regression-test project into Core's canonical solution. It rewrites references to local Core/Studio/Extensions projects, retains separate upstream central dependency baselines, keeps external CShells dependencies as packages, repairs relocated assets, and scopes build properties/targets. Test/sample central-version wrappers import their corresponding source baseline instead of duplicating it. The preparation matrix evaluates effective package identity and exclusion properties per project/TFM/configuration/reference mode; it does not create or inspect `.nupkg` files. The repository's original publishing workflows remain inert in this disposable rehearsal and are not enabled by this change. Fody behavior remains scoped to its original product instead of silently inheriting Core's different guard.

The imported `.csproj` inventory under `test/extensions` and `test/studio` is also checked against NUKE's `TestProjects` selector in `build/Build.cs`, which selects solution projects whose names end in `Tests`. The pinned inventory has 17 test-tree projects: 16 match the test selector, and `Elsa.TestServer.Web` is the one test-tree project intentionally excluded from test execution because it is a host. The canonical NUKE run selected 95 solution projects, including projects outside those two inventory trees. All added solution projects remain included in the default solution build. Exact selected paths and observed project/framework outcomes are in [canonical-95a-nuke-test-evidence.json](canonical-95a-nuke-test-evidence.json).

The Slack relocation keeps its upstream two-mode Elsa dependency. With `UseProjectReferences=false`, `Elsa.Slack.csproj` retains `PackageReference Include="Elsa"`; with `UseProjectReferences=true`, it uses the mapped Core project at `../../../modules/Elsa/Elsa.csproj`. The package reference is versioned by the imported Extensions central package baseline (`ElsaVersion` is `3.8.0-preview.5557` at the recorded source pin). This matters for package verification: evaluating or building the project-reference mode does not establish the package-mode dependency. After preparation, inspect both evaluated modes with:

```sh
dotnet msbuild src/extensions/communication/Elsa.Slack/Elsa.Slack.csproj \
  -getProperty:PackageId,AssemblyName,RootNamespace,ElsaVersion,ManagePackageVersionsCentrally \
  -getItem:PackageReference,ProjectReference -p:UseProjectReferences=false

dotnet msbuild src/extensions/communication/Elsa.Slack/Elsa.Slack.csproj \
  -getProperty:PackageId,AssemblyName,RootNamespace,ElsaVersion,ManagePackageVersionsCentrally \
  -getItem:PackageReference,ProjectReference -p:UseProjectReferences=true
```

On 2026-09-24, this check passed against a fresh preparation using Core `8e893e02c4ac089d526b0a0d294a8546f021d072`, Extensions `33fa0bfd28c7585240e3d4f665058c067b17e287` and Studio `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822`. The package mode evaluated to `PackageId`, `AssemblyName` and `RootNamespace` `Elsa.Slack`, with the `Elsa` package reference and no project references. The source mode evaluated to the same identity and mapped Core project reference, with no `Elsa` package reference. This was MSBuild evaluation only; no restore, build or pack was performed, so it does not substitute for checking the generated `.nuspec` in the package artifact proof. See [NuGet PackageReference conditions](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files#adding-a-packagereference-condition) for the MSBuild condition semantics.

The subsequent [mapped Slack package proof](mapped-slack-package-proof.md) packs only `Elsa.Slack` from this source-mapped layout into a local feed, inspects its `.nuspec`, consumes it in clean package-only projects, and checks embedded C# source against Extensions `33fa0bfd28c7585240e3d4f665058c067b17e287`. This package proof is separate from the public 3.8.4 artifact comparison, which uses Core `33181ae3048f628f591a0155b5665a8e4d1bcea2` and Extensions `154ba15fb4da85b4bebecfbe43639579cbda1d0d`.

Two diagnosed compatibility changes are explicit in the patch:

- Agents no longer relies on Studio transitively supplying archived Blazored.FluentValidation. Both button and form submission await the existing async name validator, using a component-owned message store, stale-response revision guard and disposal guard. Six new tests cover pending uniqueness, duplicate messages, field changes including change-back, direct name changes, competing responses and disposal.
- The `BlazorApp1` sample clears inherited `TargetFrameworks` while retaining its declared `net8.0`. Previously the combination restored multiple framework asset versions into a single-framework build and caused a CustomElements static-asset conflict.

## Validation and remaining gates

The existing older build receipts below preserve results from `Consolidated.sln`; do not use them as canonical solution evidence. The 95a profile's root contributor `Test` target, including Restore, Compile, and Test, succeeded against canonical `Elsa.sln` with each project's declared target frameworks. Core's MySQL provider remains on its declared net8/net9 frameworks. An exact-head hosted PR CI run is still required. Do not infer successful root `Pack` or publication from the property matrix or this `Test` target.

A local property-only matrix was run against the already-prepared Core `076f022cc174d497af26fc8e26414970e61a79b1`, Extensions `33fa0bfd28c7585240e3d4f665058c067b17e287`, and upstream-main Studio `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822` rehearsal using SDK 10.0.300. It evaluated all 167 imported/added projects across 2,586 framework/configuration/reference-mode cases; every effective `PackageId` was non-empty and stable, and every case had `IsPackable=false` and `GeneratePackageOnBuild=false`. The rehearsal's Git status was unchanged. The [compressed property evidence](canonical-packability-076-evidence.json.gz) includes every project/mode/framework/configuration value, the rehearsal and source-receipt pins, and its SHA-256 is `640afa1a61b472fb2f054312168a11c6ba85595a312441976785dd04ac7537a7`. This is the retained 076 profile evidence and is not the 95a canonical proof.

The fresh 95a preparation used Core `95a658b96107ad4dbb280a13972479af74bc6a30`, Extensions `33fa0bfd28c7585240e3d4f665058c067b17e287`, and Studio `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822`. Its canonical `Elsa.sln` contains 343 total project entries; the preparer property matrix covers the 167 imported/added projects. SDK 10.0.300 evaluated 2,586 Debug/Release, default/source/package-reference cases; all effective package IDs were stable and non-empty, and all cases had `IsPackable=false` and `GeneratePackageOnBuild=false`. The canonical packability report SHA-256 is `5b720229d7f58da91a4c62aaf19670acea6212638c013dfcc676713fba6b5d18`; the preparation receipt SHA-256 is `5fd5df724698e010932a741633936a12d39e506743195482b33e68d9154d0238`. This evaluation preceded the four listed compatibility/test-layout patch applications, so it does not independently establish their effective package properties. The exact patches are listed in the [source-profile ledger](canonical-source-profile-ledger.json).

The canonical NUKE command `env -u NUKE_ENTERPRISE_TOKEN ./build.sh --host Terminal --target Test` exited 0 on 2026-09-24. Restore succeeded in 19 seconds, Compile in 4:53 with 0 errors and 1,570 warnings, and Test in 9:10; NUKE reported 6,056 passed and 148 skipped. The full output is preserved at `/tmp/elsa-8194/95a-canonical-compile-test.log` (SHA-256 `39b8e102204e1f2a02029acc3589d3b8d982ec94fdffd61f5a547e7e393ffe2b`). Its selected-project inventory includes 95 unique project command paths and 94 retained TRX files. Console summaries show 6,222 passes across declared target frameworks; the net8/net9/net10 Designer runs each passed 83 tests, but NUKE's fixed per-project TRX filename retained only the net9 TRX. Two retained test-result files contain zero executed cases (Azure Service Bus's explicitly skipped TODO and Slack's unimplemented skipped test). `Elsa.Workflows.PerformanceTests` is selected and built but has no test SDK and produced no test run/TRX. The machine-readable evidence records each selected path, per-assembly/framework result, retained TRX counters, these exceptions, and the historical count reconciliation.

The current run's 6,222 all-framework passes are 53 more than the historical 076 `Consolidated.sln` count of 6,169: 21 formerly failing Studio path-resolution tests now pass, and 32 cases were added across Connections, Secrets, Workflow Core, Credentials PostgreSQL integration, and Secrets API integration. Both profiles report 148 skipped cases; these are different pinned source/solution profiles and the counts do not imply identical coverage. The 1,570 compile warnings include dependency security advisories (NU1902/NU1903), Pomelo EF version constraint NU1608, NETSDK1206, XML cref, nullable/compiler, trimming, xUnit/MudBlazor analyzer, and remote-free SourceLink warnings. The referenced advisories remain unresolved; no dependency changes were included in this evidence run. No Pack, Push, or Publish target ran.

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

A subsequent [provider and sample build proof](provider-build.md) records a complete successful rehearsal build after the Dapper/Mongo patches and canonical Secrets sample correction. It retains exact input revisions, the failed diagnostic, warnings and remaining final-import/test gates.

The [Studio source-test layout correction](studio-test-layout.md) retains the subsequent full test run (6,169 passed, 21 path-resolution failures, 148 skipped) and the 103 passing tests after repairing the three affected projects. It does not relabel the initial full run as successful.

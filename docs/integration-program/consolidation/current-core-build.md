# Current-Core consolidated source build proof

Program #8194, story #8286, task #8287. This advances the disposable source rehearsal to Core `076f022cc174d497af26fc8e26414970e61a79b1`, including credential lifecycle/workflow bindings and the merged EF/BPMN compare-and-swap changes. Extensions remains `33fa0bfd28c7585240e3d4f665058c067b17e287`; Studio remains `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822`. Later main commits are not silently substituted.

## Result

The 340-project combined solution built with its declared target frameworks: **0 errors, 1,867 warnings**, using SDK 10.0.300 on macOS arm64. The successful invocation took 17m51.54s and reused the first attempt's outputs. The initial attempt failed because the disk filled during Copilot CLI extraction; it is retained as failed evidence. After deleting only owned disposable build outputs and the partial extraction, the same source and command succeeded.

The freshly built net10.0 outputs passed **214 targeted tests, zero failures/skips**: Connections 30, BPMN interchange unit 21, BPMN unit 30, BPMN interchange integration 127, and imported Studio Agents 6. The Connections baseline predates the separate offboarding PR; those 30 tests do not prove the later changes. These are focused integration checks, not the full combined test suite.

The [machine-readable evidence](current-core-build-evidence.json) records exact source/artifact commits, input hashes, build attempts, test counters and compressed/raw log hashes. Logs are in [current-core-build-logs](current-core-build-logs/). The rehearsal verified 9,749 original file blobs/modes (Core 5,978, Extensions 1,665, Studio 2,106), original source ancestors and Git object integrity before applying compatibility patches.

## Repeat

1. Create a clean history rehearsal using the three exact source pins above and `scripts/integration-program/rehearse-import.py`. Never push its synthetic rehearsal commit.
2. Run the updated `prepare_consolidated_build.py` from this change against that clean rehearsal. Its explicit allowlist accepts this Core profile and the original profile; arbitrary source revisions remain rejected.
3. Apply the Dapper artifact from `9fee71f1634c5f29905335f0fa7835ea8bf1daff`, reviewed Mongo artifact from `4a24f416b8f082922ad78538594700674f02cf04`, and workbench canonical-Secrets artifact from `a754a1cfc9c1b2aa94788db522a062c6e953aff5`, all under `scripts/integration-program/consolidated-build/`. Verify the patch hashes against the evidence. The workbench artifact is from PR #8306, which has its own review/merge gate.
4. Run `dotnet build Consolidated.sln -p:UseProjectReferences=true -p:IsPackable=false -p:GeneratePackageOnBuild=false -m:1` with SDK 10.0.300. Do not force a framework across projects: MySQL retains its declared net8/net9 matrix.
5. Run `dotnet test <project> --no-build --no-restore -f net10.0` for the five projects recorded in the test logs. The no-build commands require the completed build from step 4.

## Remaining gates

Warnings include existing dependency advisories, nullable/analyzer findings, SourceLink metadata absent from the remote-free rehearsal, and a Pomelo 8/EF Relational 10 version-constraint warning in net10 closures. Compilation does not establish runtime compatibility for that MySQL combination or accept vulnerabilities for release.

Public package identity evaluation, a current mapped-source selective packaging proof, runtime Secrets API/Studio compatibility and final-layout debugging remain separate gates. No packages were generated/published, no production state changed, and no repository was archived. The actual import must preserve original histories through a normal merge after its gates pass; this rehearsal is not that import.

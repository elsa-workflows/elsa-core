# Core c4b3ce consolidated source rehearsal

Program #8194; story #8286; task #8287. This is a disposable local rehearsal with no remote. It is not the canonical source import, package release, or publisher cutover.

The selected source commits are Core `c4b3ce150160e3c9062b57f7b158fd6b968e1631`, Extensions `ba8b71d91c15ffe5be4b2c539cf9f712e74af775`, and Studio `20ceaeeed7e671f0c9662003e82063026f2216de`. The [import receipt](import-receipt.json.gz) records synthetic commit `cfa5e7d6d6ccc5f849163b3f6e0db651b6062b38`: 6,420 Core files plus 3,771 byte-and-mode-preserved Extensions/Studio files, 10,191 total. Root independently confirmed that all three source commits remain reachable from that synthetic commit. The uncompressed receipt SHA-256 is `dee44fd3d8027765e2cc8107a3b6e9970797cc0ec74cd484c21d7985f931cea8`.

The [upstream PR refresh](upstream-pr-refresh.json) records the selected tips still matching both source repositories' `main` at its checked time. It paginates the open PR collections and retains 18 Extensions and 15 Studio PR heads for owner review. None is silently imported, merged, reparented or abandoned by this rehearsal. Repeat this refresh before creating the actual import candidate.

The reviewed source-integration patch is unchanged at SHA-256 `b091576a7f8d51c8b469645eea4b868556ebfadb084d7afbc4287ea36155433d`. From the earlier `a13ac7a` Core profile to `c4b3ce1`, 109 Core paths changed; none overlaps the patch's 185 paths. The [preparation receipt](consolidated-build-receipt.json.gz), uncompressed SHA-256 `80396b7354bf42761e31853d683887c5336898db5aca9696a6c5613576d4bd26`, records the actual patch application and prepared file hashes. The mapped `Elsa.sln` has 343 `.csproj` entries.

The [packability report](canonical-packability-report.json.gz), uncompressed SHA-256 `5b720229d7f58da91a4c62aaf19670acea6212638c013dfcc676713fba6b5d18`, evaluates 167 imported/test projects in Debug and Release across declared frameworks and default/source/package-reference modes. All 2,586 evaluated cases have `IsPackable=false` and `GeneratePackageOnBuild=false`. The receipt itself correctly retains `buildCompatibilityVerified=false`, `canonicalBuildAndTestsVerified=false`, and `publicationAuthorized=false`. The three compressed receipts were scanned for absolute local path values; none were found.

Repeat from a reviewed tooling checkout with full-history source repositories at those exact commits:

```text
python3 scripts/integration-program/rehearse-import.py --core <core-checkout> --extensions <extensions-checkout> --studio <studio-checkout> --output <new-disposable-directory> --source-profile current-tip
python3 scripts/integration-program/prepare_consolidated_build.py --rehearsal <new-disposable-directory>
python3 -m unittest discover -s scripts/integration-program -p test_prepare_consolidated_build.py
```

This preparation proves exact relocation, source ancestry, patch applicability and packaging exclusion. By itself it does not prove the combined source compiles, final package dependency closure, Studio browser behavior, SourceLink provenance or supported Secrets upgrades. Build and test results are recorded separately below with their exact source and overlay state. Never push the synthetic rehearsal commit or publish packages from it.

## Subsequent mapped build and focused tests

The separate [build receipt](mapped-solution-build.json) records both runs against this same local rehearsal. A clean prepared-source build finished in 5 minutes 11 seconds with four Workbench Secrets compiler errors and 1,919 warnings. The errors were one ambiguous `UseSecrets` call and three missing `UseEntityFrameworkCore` extensions in the unpatched sample host. No other compiler errors occurred. This is a failed baseline, not a passing build.

Five already reviewed Workbench/Studio fixture patches were then applied in order in the disposable tree; the receipt records each patch hash. The same 343-project `Elsa.sln` source-reference build passed in 1 minute 50 seconds with zero errors and 1,556 warnings. The warm rerun benefited from the first build's restored and compiled artifacts. Its no-remote SourceLink remains empty, and inherited dependency advisories and analyzer warnings remain; this is not a release or clean-consumer proof.

Targeted `net10.0` tests against that overlaid source passed: Management 127/127, Core Secrets 141/141, Studio Designer 83/83, Studio Secrets menu 11/11, and Studio Agents 6/6, with no skips. The Studio Secrets test project is added by a later fixture patch and is not part of the prepared 343-project solution; its first `--no-build` attempt failed because its DLL was absent, then its normal targeted build/test passed. No full combined test suite or actual Studio browser workflow is claimed. Raw local build/test logs were retained privately; the public receipt contains hashes and summarized results without local paths or credentials.

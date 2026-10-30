# Studio source-contract tests in the consolidated layout

Program #8194, build task #8287. The broader test run against the current-Core source rehearsal found 21 failures because Studio tests still searched for `src/framework`, `src/modules`, or the standalone `Elsa.Studio.sln`. The imported tree uses `src/studio/`; those assertions could not reach the files they were intended to verify.

The [integration patch](../../../scripts/integration-program/consolidated-build/studio-test-layout.patch) adds one shared source-file resolver linked into three existing test projects. It locates Studio's source tree by the actual Core project marker in either layout. CSS, host authentication/theme, and module dependency assertions remain unchanged. No test was disabled and no production behavior changed.

## Verification

The initial combined run used the source/artifact inputs from [PR #8312](https://github.com/elsa-workflows/elsa-core/pull/8312), SDK 10.0.300, and:

```sh
dotnet test Consolidated.sln --no-build --no-restore \
  -p:UseProjectReferences=true -p:IsPackable=false \
  -p:GeneratePackageOnBuild=false -m:1 --logger trx
```

It exited 1. Its 94 TRX reports contain **6,169 passed, 21 failed and 148 not executed/skipped results**. Two wholly skipped test runs have no `Passed!`/`Failed!` console summary, so the structured results, not only console totals, establish that skip count. All 21 failures were source-root resolution failures in the following projects.

After applying the patch, the three affected projects were rebuilt and rerun with `dotnet test <project> --no-restore -f net10.0 -p:UseProjectReferences=true -p:IsPackable=false -p:GeneratePackageOnBuild=false -m:1`:

| Project | Result |
| --- | --- |
| `src/studio/framework/Elsa.Studio.Core.Tests` | 28 passed |
| `src/studio/modules/Elsa.Studio.Authentication.UI.Tests` | 25 passed |
| `src/studio/modules/Elsa.Studio.Dashboard.Tests` | 50 passed |

The **103 targeted tests passed with zero failures/skips**. The complete combined run was not repeated after this test-only fix; its failed evidence is preserved rather than relabeled. Existing skips remain unverified behavior.

A separate clean seven-file patch replay passed and reproduced every recorded resulting SHA-256 value. [Evidence](studio-test-layout-evidence.json) records inputs, before/after hashes, the failed full-run matrix, passing focused counters and compressed/raw log hashes. The [logs](studio-test-layout-logs/) retain both phases. This extends the local rehearsal evidence; it does not replace hosted CI or final imported-source verification.

## Apply and integrate

Apply the patch after the source preparation, Dapper/Mongo compatibility artifacts and canonical-Secrets workbench patch from PR #8312's recorded profile. Run `git apply --check` first. The patch is specific to that mapped source, and the seven resulting file hashes in the evidence make drift visible. Retain the linked shared helper when importing the source; do not create a dummy solution file or weaken the tests to make path detection pass.

The actual history import, package/activity compatibility, final debugging workflow and publisher cutover remain separate gates. This change publishes no package and changes no production environment.

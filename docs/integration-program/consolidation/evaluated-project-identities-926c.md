# Evaluated identities for the 926c consolidation baseline

This evidence compares the evaluated MSBuild project identities for the 166 active Extensions and Studio projects in the clean consolidated baseline `926c91327efaecce286d50b31f19db376239f05e` with the pinned source projects. The source pins are Extensions `33fa0bfd28c7585240e3d4f665058c067b17e287` and Studio `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822`; both import and build receipts refer to rehearsal commit `9c264cfbe03dbe27e1de2792e441ec2db65f3fea`. The inventory snapshot date is 2026-09-23.

The [machine-readable report](evaluated-project-identities-926c.json) records 166/166 evaluations, hashes for 541 evaluated inputs, six quarantined source projects, and no evaluation warnings, errors, or failures. The audit compares outer properties and each declared target framework. It invokes no target, restore, build, test, pack, publish, or deployment command against the inspected projects. Its own .NET 10.0.300 tool was restored and built separately. Imports under the selected SDK and its sibling workload-manifest directory are hashed and recorded. The evaluator clears inherited environment variables except `PATH`, temporary-directory variables, and `SystemRoot`, then sets explicit SDK paths and disables system/global Git configuration. Imported MSBuild expressions still execute during evaluation; this is not a security sandbox.

The 512 property differences separate into 508 rehearsal `IsPackable` differences and four framework differences in one Studio sample. The consolidated `src/extensions/Directory.Build.props` and `src/studio/Directory.Build.props` explicitly set `IsPackable=false` for the rehearsal; this does not establish release package eligibility. There were no differences in `PackageId`, `AssemblyName`, `RootNamespace`, `Version`, or `PackageVersion`.

The four framework differences are all for `samples/BlazorApp1/BlazorApp1.csproj`:

1. At outer evaluation, source `TargetFrameworks` is `net8.0;net9.0;net10.0`; the consolidated property is empty.
2. At `net8.0` inner evaluation, source `TargetFrameworks` is still `net8.0;net9.0;net10.0`; the consolidated property is empty.
3. The source declares `net9.0`, while the consolidated project does not.
4. The source declares `net10.0`, while the consolidated project does not.

This is the sample-framework correction already explained in [consolidated source build preparation](build-preparation.md): its explicit `net8.0` is retained while inherited multi-targeting is cleared to avoid conflicting static-asset framework versions. The report confirms the evaluated difference; it does not generalize it to shipped package identities.

The six quarantined project mappings are five colliding Secrets projects and the Extensions `build/_build.csproj`. They are listed individually in the JSON report; the build script is not a Secrets project.

## Reproduce

Use clean checkouts at the exact source pins above and the prepared consolidation checkout at `926c91327efaecce286d50b31f19db376239f05e`. The evaluator rejects any other consolidated commit, source pin, dirty tracked input, untracked imported build input, changed receipt hash, or unexpected imported path. The report output must be a new file outside all three inspected roots.

```sh
dotnet build scripts/integration-program/consolidation-identities/EvaluatedIdentityAudit.csproj \
  --configuration Release --ignore-failed-sources

AUDIT_OUTPUT_DIR="$(mktemp -d /tmp/elsa-identity-report.XXXXXX)"
dotnet scripts/integration-program/consolidation-identities/bin/Release/net10.0/EvaluatedIdentityAudit.dll \
  --consolidated-root /tmp/elsa-8287-consolidated-identities-926c \
  --extensions-root /tmp/elsa-integration-8251/extensions \
  --studio-root /tmp/elsa-integration-8251/studio \
  --import-receipt /tmp/elsa-8287-consolidated-identities-926c/import-receipt.json \
  --build-receipt /tmp/elsa-8287-consolidated-identities-926c/consolidated-build-receipt.json \
  --inventory /tmp/elsa-8287-consolidated-identities-926c/doc/integration-program/inventory/inventory.json \
  --output "$AUDIT_OUTPUT_DIR/evaluated-project-identities-926c.json" \
  --configuration Release

python3 scripts/integration-program/consolidation-identities/verify-guards.py \
  --audit-dll scripts/integration-program/consolidation-identities/bin/Release/net10.0/EvaluatedIdentityAudit.dll \
  --consolidated-root /tmp/elsa-8287-consolidated-identities-926c \
  --extensions-root /tmp/elsa-integration-8251/extensions \
  --studio-root /tmp/elsa-integration-8251/studio \
  --import-receipt /tmp/elsa-8287-consolidated-identities-926c/import-receipt.json \
  --build-receipt /tmp/elsa-8287-consolidated-identities-926c/consolidated-build-receipt.json \
  --inventory /tmp/elsa-8287-consolidated-identities-926c/doc/integration-program/inventory/inventory.json
```

The guard probe verifies rejection of an output path under a pinned source root and a modified `Directory.Build.props` in a disposable Git worktree. It does not alter the pinned source checkouts. The successful evaluation and the guard probes do not establish source build/test compatibility, package-consumer compatibility, release readiness, or publication authorization.

The report intentionally uses the clean prepared 926c tree. A later dirty current-rehearsal checkout has a separate sample reference/API correction in `Elsa.Server.Web`; that tree and its build evidence are not represented by this identity report.

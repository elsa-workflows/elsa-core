# Consolidated package recovery preflight

[Task #8645](https://github.com/elsa-workflows/elsa-core/issues/8645) prepares read-only reconciliation for the original 3.10.0 candidate accepted by [#8631](https://github.com/elsa-workflows/elsa-core/issues/8631). It does not publish packages, complete the release, or authorize publisher cutover.

The planner always covers the complete retained manifest: 225 NuGet packages and their paired symbol archives, plus 124 exclusion rows representing 123 package IDs. Exclusions retain project identity. There is no package subset, version override or feed override. Maintenance release publishers remain unchanged.

## Original inputs

Use the original archive from Core run `37456860080`, attempt `1`, artifact `11412848210`, source `ba5b348aa2414fdf7c19d9d8806e87b7c91a4205`. The archive SHA-256 is `ace85260c3389916fe3dcd7ee9cd5b766e4030dd6cee13c7f89285ada79c944f` and its size is 92,659,861 bytes. It expires on 2026-11-05 at 12:13:30 UTC. Expiry or deletion blocks this candidate; it never authorizes rebuilding equivalent-looking bytes.

The input directory contains exactly:

- `candidate.zip`: the original GitHub artifact download, without repacking.
- `artifact.json`: fresh GitHub artifact metadata.
- `producer-run.json`: metadata for the original producer attempt.
- `live-retrieval.json`: schema, artifact ID, original archive SHA-256 and retrieval timestamp.

The checked-in historical envelope remains byte-identical. Fresh availability is a separate observation. The [rehearsal workflow](../../.github/workflows/consolidated-package-recovery-preflight.yml) demonstrates retrieval: the job with `actions: read` executes fixed requests without checking out repository code; verification executes in a separate job without the artifact-reading token. Editable workflow YAML is not an independent security boundary: trusted repository-write authority and review remain required.

Input verification binds the original envelope, producer attempt, archive and every sealed file before extraction. It checks the full inventory and package metadata, raw hashes, framework dependency groups and symbol pairs against the retained producer evidence. It does not rerun a package build or turn producer evidence into a new source build claim. Paths must have no symlink ancestors; use canonical absolute paths (on macOS, `/private/tmp` rather than the `/tmp` alias). Verification destinations must be new.

## Read-only command

From the reviewed planner checkout, after retrieving the four inputs:

```bash
python3 scripts/integration-program/consolidated_package_recovery.py \
  --inputs /absolute/recovery-inputs \
  --verified-root /absolute/new-verified-candidate \
  --output /absolute/new-recovery-ledger.json \
  --planner-source "$(git rev-parse HEAD)"
```

Hosted execution additionally supplies `--planner-run` and `--planner-attempt` together. The ledger distinguishes this planner identity from the original candidate producer.

The default access mode is anonymous. If a separately authorized operator supplies a full Authorization header through an existing environment variable, select it with `--authorization-env VARIABLE_NAME`. The planner does not discover credentials, create tokens or print the header. A supplied credential establishes only `credential_present_unverified_scope`; it does not prove publisher rights or intended-reader access. Feedz documents NuGet authentication as a token used as the password with any nonempty username; operators must supply the appropriate complete header without embedding it in command arguments. See [Feedz NuGet documentation](https://feedz.io/docs/package-formats/nuget).

Production reads are fixed to the Elsa Feedz repository. GETs reject redirects and foreign origins, do not discover proxies, and bound response size and elapsed time. No HEAD response or cached invocation certifies absence. A complete 404 is a visibility observation under the declared access mode, not proof that an authenticated publisher or intended reader sees the same state.

## Ledger interpretation

| Classification | Meaning |
| --- | --- |
| `missing` | A complete GET reported absence under the declared access mode. |
| `matching` | Identity, source, dependency metadata and signature-aware archive payload match the original package. |
| `conflicting` | Readable remote bytes differ from the original package. |
| `unverifiable` | Authentication, transport, timeout, ambiguous response, malformed content or input verification prevented a conclusion. |

Raw local and remote archive SHA-256 values are distinct from signature-aware payload hashes. Signature normalization reuses the maintenance verifier and does not verify a signing certificate. Symbol archive hashes identify the original local pairs; remote symbol publication and retrieval remain unverified.

Any conflict or unverifiable package blocks reconciliation. `classification_complete` means every package has a classification other than unverifiable, so a complete ledger can still contain blocking conflicts. `content_converged` requires all 225 packages to match. Even a fully matching ledger always has `publication_ready: false` and `publication_performed: false`.

Intended-reader access, remote symbols, publisher authority, publisher quiescence, publication approval and full compatibility remain separate pending gates. The planner never treats a push exit status, `--skip-duplicate`, an anonymous absence or a previous successful invocation as permission to publish. Receipts retain fixed diagnostic categories and hashes, not credentials, response headers, raw server errors or private paths.

## Local recovery rehearsal

Use the same original inputs with a second fresh verification destination:

```bash
python3 scripts/integration-program/run_consolidated_recovery_fixture.py \
  --inputs /absolute/recovery-inputs \
  --verified-root /absolute/new-rehearsal-candidate \
  --output /absolute/new-rehearsal-ledger.json \
  --planner-source "$(git rev-parse HEAD)"
```

The one simulated feed covers all missing, all matching, a noncontiguous partial set, interruption after simulated acceptance, uncertain outcomes, delayed visibility, conflicting bytes, duplicate archive identity, unreadable packages, ambiguous feed metadata, interrupted observation and eventual full-set convergence. Every invocation re-reads simulated remote state. Altered bytes exist only in simulated remote responses; the fixture rechecks all original package and symbol hashes afterward.

This is local simulation evidence, not a live recovery or Feedz publication result. The workflow retains its full portable ledger and runs focused contracts both normally and with Python optimization enabled. The regular integration tooling workflow also runs the maintenance verifier and recovery policy contracts.

## Artifact-only executor

[Task #8649](https://github.com/elsa-workflows/elsa-core/issues/8649) adds an executor for this exact candidate. Its default operation verifies and rehearses; publication is blocked while the reviewed operational policy is unset. The [executor workflow](../../.github/workflows/publish-consolidated-original-candidate.yml) reuses the recovery workflow's fixed retrieval and verification jobs. It never builds or packs candidate projects. The only compiled project is the symbol inspection helper.

From the reviewed executor checkout, compile the helper and use new destinations for each invocation:

```bash
dotnet build scripts/integration-program/VerifyPackageSymbolPair/VerifyPackageSymbolPair.csproj \
  --configuration Release --output /absolute/symbol-inspector

python3 scripts/integration-program/consolidated_package_executor.py verify \
  --inputs /absolute/recovery-inputs \
  --verified-root /absolute/new-executor-candidate \
  --inspector /absolute/symbol-inspector/VerifyPackageSymbolPair.dll \
  --output /absolute/new-executor-verification.json \
  --executor-source "$(git rev-parse HEAD)"
```

Use `rehearse` instead of `verify`, with separate new destinations, to exercise simulated uploads and recovery with the original complete candidate. Hosted commands additionally bind `--executor-run` and `--executor-attempt`. The workflow retains separate verification, rehearsal, admission and publication artifacts identified by the GitHub run and attempt. Before scheduling native approval, its job summary binds the exact executor revision, original producer/artifact/archive/manifest, observed feed index, complete same-run verification and GitHub-reported verification artifact ID/digest. The summary links the retained evidence and distinguishes verification with missing content from release acceptance.

A successful `verify` exit means original inputs and every required local DLL/PDB association passed, and remote classifications are complete and nonconflicting. Missing packages or PDBs are allowed in this nonpublishing result. It is not release acceptance. A successful `rehearse` must demonstrate complete simulated convergence and interruption/resume across all 225 pairs, without writing to Feedz. Neither command grants upload authority.

The symbol inspector preserves the existing SourceLink/document checks and emits the Portable PDB key, GUID, stamp, DLL-declared checksum and unchanged PDB hash. Readback compares actual remote PDB bytes with the corresponding original `.snupkg` member. The normalized DLL checksum and raw PDB hash are distinct. Deduplicated symbol requests must retain every package/framework association; assembly-free entries and inconsistent duplicate keys cannot be silently omitted. Full acceptance needs 225 matching packages and every expected symbol association. Original `.snupkg` archive readback remains a separate, unproven contract.

## Publication admission and execution

An operation input, receipt or `approved` flag is not permission to publish. A manual request must pass fresh admission before GitHub schedules the fixed `elsa-3-10-feedz` environment job. Missing environment identity, required reviewer/ref/bypass protections or reviewed policy blocks that job. Scheduling eligibility is public and does not claim credential isolation; complete secret-name provenance must pass after native approval, before the publisher credential is exposed. This avoids accidentally creating an unprotected environment by naming one in a workflow. Native approval must apply to this same run, and the executor checks admission again before using the dedicated `ELSA_CONSOLIDATED_FEEDZ_PUBLISH_KEY` credential. There is no repository/organization credential fallback. Version one accepts manual dispatch from Core `refs/heads/main`, attempt 1 only. A request from another ref fails before admission checks out code; verification remains available on the implementation branch. Recovery requires a fresh approved dispatch, not reusing approval history by rerunning an old job. Any later tag or other-ref publication path requires a reviewed change. Editable workflow code is not an independent security boundary against a writer who can change it: trusted repository review and the real native environment restrictions remain required.

The current policy is unset. No environment, reviewer IDs, allowed refs or credential access are provisioned by these workflows. Public environment metadata does not prove secret isolation. Execution uses a separate environment-scoped `ELSA_CONSOLIDATED_METADATA_READ_TOKEN` with only repository Secrets:read and Environments:read permissions, covering environment, repository and repository-available organization secret names. It must have no Actions/artifact-reading, Contents or organization-administration permissions. Verify those actual permissions during setup; a variable name is not scope evidence. The metadata-only token is supplied only after native approval, restricted to the exact GitHub metadata endpoints, and excluded from Git identity checks and inspector subprocesses. Namespace checks cover both dedicated credentials. Accepted rejection of inherited `GH_TOKEN`, `GITHUB_TOKEN` and `ACTIONS_READ_TOKEN` remains unchanged; do not rename an artifact-reader token to bypass it. No metadata token is provisioned or used by implementation or verification. Unavailable, partial or changed metadata blocks publisher-key use. Rechecks narrow the race but do not make provider configuration atomic: stable reviewed environment administration remains required during execution.

Before requesting operational approval, complete the [publisher handoff packet](consolidated-publisher-handoff.md), including actual provider overwrite controls, maintenance/npm replacement, containment of already-loaded old credentials, fresh publisher quiescence and full compatibility. Bind the reviewed executor revision, original candidate hashes, intended feed/reader, environment identities and stop/recovery instructions. Merge of the executor does not authorize those actions.

After those exact settings and publication are approved, dispatch the workflow on the reviewed permitted ref with `operation=publish`. Verification finishes before native environment approval is requested; the protected job revalidates the same-run original archive and the complete remote inventory. Only verified missing content may be uploaded, with explicit symbol handling through the advertised symbol-publish resource. The package-read, package-publish and symbol-publish resources must match the fixed Feedz endpoints; package reconciliation must observe the same index digest before upload and during final readback. A changed index blocks admission or content acceptance. A success status or HTTP 409 does not prove matching content. Any uncertain acceptance, conflict or unreadable content stops further upload; retain the ledger and reconcile from scratch before a new approved dispatch. Never rebuild, overwrite, delete or substitute bytes as an automatic recovery action.

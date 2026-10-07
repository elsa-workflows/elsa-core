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

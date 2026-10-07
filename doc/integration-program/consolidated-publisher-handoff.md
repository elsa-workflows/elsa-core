# Consolidated publisher handoff preflight

This is the preparation packet for #8647 under #8220 and program #8194. It changes no publisher, credential, environment or package. The [accepted release ADR](../adr/2026-09-28-lockstep-consolidated-release-and-publisher-cutover.md) makes Core the sole NuGet publisher for 3.10 and later, with the first consolidated release at 3.10.0 on Feedz. Studio and Extensions retain their 3.8/3.9 maintenance releases. npm ownership remains a separate decision under #8216.

## Run the read-only observation

From a reviewed checkout on macOS or Linux with Python and an authenticated GitHub CLI (the bounded transport uses POSIX pipes):

```sh
python3 scripts/integration-program/consolidated_publisher_handoff.py \
  --inventory doc/integration-program/consolidated-publisher-inventory.json \
  --output /absolute/new/path/publisher-handoff.json
```

The command only reads GitHub metadata and source identities. It never requests secret values, dispatches or cancels a workflow, changes repository settings, or pushes packages. Use a new output file and retain the exact inventory with the receipt. The inventory has an explicit review expiry: a later operator must refresh the facts and review them, not merely extend the timestamp. A fixture using simulated responses is not a live observation.

The receipt separates these questions:

| Result | Meaning |
| --- | --- |
| Observation complete | Required API reads and pagination completed within the declared bounds. |
| Inventory verified | Observed identities agree with the reviewed scope; no unresolved scope has been silently accepted. |
| Snapshot quiescent | No relevant nonterminal run was observed in the consistent observation window. This does not lock GitHub or prevent another run starting afterward. |
| Authority gaps | Missing protections, legacy credential access, unknown token rights and other unverified release conditions remain explicit. |

`publication_performed`, `live_publisher_changed` and `publication_ready` remain false. A complete observation of an unsafe or active publisher is useful evidence, not release approval. Unknown runs, changed refs, inaccessible metadata or capped/inconsistent pagination cannot establish quiescence or authority.

## Current source boundaries

The accompanying inventory pins the package workflows on main, the Core program branch and all advertised 3.8/3.9 maintenance branches observed on 2026-10-07. It also names all 56 Actions workflow identities then advertised across the three repositories. Naming a workflow does not review its source or prove it cannot obtain credentials: other repository workflows, historical versions and platform-managed `dynamic/*` workflows remain explicitly unverified. Complete branch/tag observations include refs outside the reviewed source subset.

| Repository / reviewed revision | Observed package publication behavior |
| --- | --- |
| [Core main](https://github.com/elsa-workflows/elsa-core/blob/b38e7523f79297076e51701fd951c426145aa99c/.github/workflows/packages.yml) | Push/release events do not publish. Dispatch options default off; 3.10.x tags are Feedz-only. The publisher jobs have no protected environment. |
| [Core 3.8 maintenance](https://github.com/elsa-workflows/elsa-core/blob/33181ae3048f628f591a0155b5665a8e4d1bcea2/.github/workflows/packages.yml) and [3.9 maintenance](https://github.com/elsa-workflows/elsa-core/blob/5d3582b6309a2fd8ea33ebdb8c39fb040bfdc514/.github/workflows/packages.yml) | Feedz publication runs on push/release. The 3.8 workflow pushes directly; 3.9 uses the shared composite action. These revisions do not inherit main's dispatch gates. |
| [Studio main](https://github.com/elsa-workflows/elsa-studio/blob/3d028a4e9a3267b291f3d43d8064b5c81a6f2759/.github/workflows/packages.yml) | NuGet and npm packages publish to Feedz on push/release, using a 3.10.0 base version. Public-registry dispatch gates do not disable this Feedz path. |
| [Studio 3.8 maintenance](https://github.com/elsa-workflows/elsa-studio/blob/9bff3f785fd13bd80a3a7ecf88fec4aec8eef7ae/.github/workflows/packages.yml) and [3.9 maintenance](https://github.com/elsa-workflows/elsa-studio/blob/a30ed7c997dfb3cff1d5d095a4ee19ff03c7fe42/.github/workflows/packages.yml) | The four mutable 3.8.1/3.8.2/3.8.4/3.9.0 branches permit dispatch with `publish` and a syntactically valid version override. That syntax does not restrict the version to 3.8/3.9. |
| [Extensions main](https://github.com/elsa-workflows/elsa-extensions/blob/e189390b3ea10b730c4d66bf40e357f7621374ab/.github/workflows/packages.yml) | Feedz publication runs on push/release, using a 3.10.0 base version. Its NuGet.org 3.10 restriction does not guard Feedz. |
| [Extensions 3.8 maintenance](https://github.com/elsa-workflows/elsa-extensions/blob/154ba15fb4da85b4bebecfbe43639579cbda1d0d/.github/workflows/packages.yml) and [3.9 maintenance](https://github.com/elsa-workflows/elsa-extensions/blob/89d4eb9b739ae135604aac2a1de18653a289bdb0/.github/workflows/packages.yml) | Feedz publication runs on push/release; release events derive package versions from the tag. A branch name alone is not a package-version restriction. |

The existing `check_publisher_handoff.py` validates a bounded Slack-only simulated transition. It remains useful within that contract; it is not consolidated live cutover evidence. Core's `release-elsa-slack.yml` currently rehearses local CI artifacts and publishes nothing. The current Core package push action retries with `--skip-duplicate`; that exit status does not establish that an existing remote package matches the immutable candidate. Use the [content recovery preparation](consolidated-package-recovery.md) for that comparison.

The inventory pins current package workflow bodies and selected indirect Core files. It does not assert that every historical workflow, external action revision, build script or reusable call was audited. Old Core `publish-latest*.yml` workflow identities remain advertised by Actions even when a path is absent from current main. Their presence cannot be treated as harmless deletion without examining the executable historical paths and retiring their old authority.

## Credential and environment boundary

The 2026-10-07 metadata observation found no reviewer-protected package environment in any repository. Core exposed `copilot` and `github-pages` environments, the latter with a branch policy only; Studio and Extensions exposed only `copilot`. Studio and Extensions had no repository-level secrets, but organization-level `FEEDZ_API_KEY` and `FEEDZ_API_KEY_BASE64` names were available to all three repositories. Core also retained repository-level Feedz credential names.

No secret value or token permission was inspected. A name, encoding, existing npm use, or successful metadata response does not prove token scope. In particular, Studio passes `FEEDZ_API_KEY_BASE64` to its npm publisher; that does not prove the underlying credential cannot publish NuGet packages. The repository-level organization-secret listing can establish that a credential name is available to that repository even when the organization-wide policy endpoint is inaccessible. The latter returned HTTP 403 during the preparation audit; full organization visibility/selected-repository policy remains unverified, and receipt completeness remains false.

GitHub [environment protection](https://docs.github.com/en/actions/reference/workflows-and-actions/deployments-and-environments) controls jobs that reference the environment and access its secrets. It does not remove repository or organization credentials from old workflow revisions. A newly named environment alone does not establish required reviewers, ref restrictions, or disabled administrator bypass. Missing API fields are unknown, not proof that bypass is disabled.

GitHub [loads repository and organization secrets when a run is queued](https://docs.github.com/en/actions/reference/security/secrets), and environment secrets when the referencing job starts. Removing or relocating a secret therefore does not establish that a queued or running workflow has lost its already-loaded credential. The approved handoff must account for those copies through provider-side revocation/rotation or verified exhaustion of every affected run's authority. Prepare and verify replacement maintenance/npm access first; metadata changes and workflow cancellation alone are not credential-revocation evidence.

The receipt retains configured reviewer, wait-timer and branch/tag policy metadata. It does not approve the allowed refs: a wildcard can be broad, and GitHub's protected-branches-only option permits all branches when no branch protection exists. Effective ref scope therefore remains an explicit review gap until the actual execution paths and restrictions are approved.

Feedz [package-management permissions](https://feedz.io/docs/access/permissions) include upload, replacement and deletion within the selected repository. The documented permission model does not establish isolation by version line. The future executor must restrict its own complete validated inventory and avoid arbitrary file/feed/version inputs; the credential still needs a reviewed protected boundary. Metadata alone cannot establish the feed's actual overwrite settings or intended-reader access.

The proposed environment names in this packet are `elsa-3-10-feedz` in Core and `elsa-3-maintenance-feedz` in each source repository. These are proposed configuration targets, not existing or approved environments. Require a named maintainer/team reviewer, prevent self-review and administrator bypass, and restrict allowed branches/tags to the exact reviewed execution paths. Reviewer identities and exact allowed refs belong in the final approval packet; no identity is invented here. Use a dedicated environment-scoped credential name for each executor and verify that name has no repository/organization fallback.

## Ordered cutover packet

1. **Finish the evidence before requesting execution.** Retain the original candidate from #8631 (artifact 11412848210, archive SHA-256 `ace85260c3389916fe3dcd7ee9cd5b766e4030dd6cee13c7f89285ada79c944f`, expiry 2026-11-05T12:13:30Z), its complete 225 package/symbol pairs and exclusions. Complete #8643's browser/profile acceptance and the remaining compatibility checks. Reconfirm intended-reader access, symbol upload/readback, all original bytes and candidate availability. Never substitute a rebuild under the same version.
2. **Prepare the exact code and settings changes.** Review a Core artifact-only executor that consumes the verified original bytes, never packs, and refuses conflicts/uncertainty before further uploads. Prepare source publisher guards that validate actual package identity/version, accepting only the approved 3.8/3.9 maintenance route and refusing 3.10+ on every executable source path. Include dispatch overrides and release/tag/rerun behavior, not just main push conditions. Bind all changes to reviewed commits, workflow digests and configured environment IDs/ref policies.
3. **Prepare replacement authority before retirement.** Specify dedicated environment credentials and a reviewed source maintenance route. Prove the separate npm replacement path required before retiring its old credential access; #8216's ownership decision is not implied by this NuGet handoff. Audit every repository and encoded/plain credential alias that can reach the shared Feedz repository. Preserve a verified maintenance procedure without retaining an unrestricted historical workflow bypass.
4. **Request one concrete operational approval.** Present the exact reviewer/bypass/ref configuration, credential creation/retirement plan, affected repositories and maintenance/npm paths, reviewed executor and source guard commits, candidate hashes, compatibility/symbol evidence, expected feed/version, and stop/recovery instructions. This document and a green PR do not authorize those live actions.
5. **Execute the approved handoff in order.** Freeze affected publication through the approved authority change, including credentials already loaded by queued/running jobs, then obtain a fresh complete run/job observation across all three repositories. Wait for or separately resolve any active/uncertain publisher; this preflight cancels nothing. Retain evidence that the old provider credential can no longer publish, or that all affected runs have ended and no retained execution path can use that authority. Recheck source refs and credentials after the observation. Activate only the reviewed Core execution path after old overlapping authority is contained. An Actions concurrency group in one repository does not serialize another repository.
6. **Publish and verify only the approved bytes.** Refresh content reconciliation under the approved access identity; publish only verified missing content through the approved executor. Stop on conflicts, unknown outcomes or changed evidence. Verify full package contents and symbol behavior afterward, record all receipts and source ownership, and retain the explicit remaining gates until proven. Publication is not complete merely because a push step exited successfully.

The concrete configuration and source patches are not yet applied by this task. The protected artifact-only executor remains required under #8220; this preflight does not replace it.

## Symbols and recovery limits

Feedz [documents symbol-package support](https://feedz.io/docs/package-formats/symbols), but the documented IDE symbol retrieval path does not establish an original `.snupkg` archive round trip. A read-only probe of the current feed returned ordinary package bytes and an ordinary `.nupkg` disposition for a `.snupkg`-looking flat-container URL. Therefore neither that URL suffix nor HTTP 200 proves symbol delivery. Verify the returned format and expected PDB identities/bytes through a supported symbol-reader contract. Resolve any required archive-level comparison explicitly; do not silently equate PDB lookup with full archive readback.

An anonymous 2026-10-07 probe retrieved the released `Elsa.Workflows.Core` 3.9.0 Portable PDBs for net8.0, net9.0 and net10.0 from the exact `elsa-workflows/elsa-3` symbol endpoint. Each matched its released DLL's GUID, stamp and normalized checksum; a wrong-GUID control returned HTTP 404. The [portable receipt](https://github.com/elsa-workflows/elsa-core/issues/8220#issuecomment-6036229105) SHA-256 is `3c01540055ef72945ba26c0d0d59ddc589a676a90780ce53fb58c020c047ad1a`. This establishes a usable public PDB-reader path for that sampled release, not 3.10 candidate acceptance or private-reader rights. The next executor must compare every returned PDB's unchanged bytes against the corresponding original candidate `.snupkg` member, preserving the existing association and SourceLink checks. Use the pinned [.NET Portable PDB key algorithm](https://github.com/dotnet/symstore/blob/d66992e7c2f32288fbf1acf08cdea43098025c7c/src/Microsoft.SymbolStore/KeyGenerators/PortablePDBFileKeyGenerator.cs) and [checksum specification](https://github.com/dotnet/runtime/blob/ea01cd94f644df0c4247f3a1bd995d4edbaf9245/docs/design/specs/PE-COFF.md#portable-pdb-checksum): the DLL checksum zeroes the 20-byte PDB ID before hashing and is distinct from the raw PDB hash. Original `.snupkg` archive readback remains unproven.

Before any package upload, stopping the approved workflow and retaining the old reviewed ownership configuration can prevent publication. Once a package or symbol upload may have been accepted, cancellation is not rollback. Reconcile remote content using the same original candidate; do not overwrite, delete, repack or allocate replacement bytes at that identity as an automatic repair. If code/configuration must be reverted, keep competing publishers contained and re-establish one reviewed owner before restoring maintenance access. Re-enabling old broadly scoped credentials is not a safe default rollback.

## Verification

Run the focused contracts normally and with optimized Python, plus the existing Slack handoff contracts:

```sh
cd scripts/integration-program
python3 -m unittest test_consolidated_publisher_handoff test_publisher_handoff
python3 -O -m unittest test_consolidated_publisher_handoff test_publisher_handoff
```

Retain a live receipt and an independent API comparison before accepting this preparation. Expected current authority gaps must remain visible. Exact-head repository CI, independent review and Greptile are required before merge. Acceptance of #8647 will close only this read-only preparation; live cutover, actual publication/recovery, symbols and the complete program remain open.

# Feedz publish 3.10.1 operational packet (unsigned)

This packet records the unpublished 3.10.1 consolidated candidate identity for a
later, separately reviewed Feedz decision. It does **not** replace
`docs/integration-program/feedz-publish-operational-packet.md`. The executor
`REVIEWED_OPERATIONAL_POLICY` in
`scripts/integration-program/consolidated_package_executor.py` still pins that
3.10.0 file's exact bytes. This file has **no signature** and is **not** pinned
as `operational_packet_sha256`.

## Decision

Do not publish yet. Do not retarget
`.github/workflows/publish-consolidated-original-candidate.yml`. Do not promote
the staged 3.10.0 candidate (run `40347931480`, artifact `12664519771`, source
`5cff67228e550b353b3483eb3b37dfdb73a41c74`) to Feedz or nuget.org. After Sipke
signs the exact SHA-256 of this file, a later reviewed PR may introduce a
**distinct** 3.10.1 executor policy. Until then this packet is identity only.

| Candidate field | Value |
| --- | --- |
| Version | `3.10.1` |
| Source commit | `ba2b801a932646e2771167a8e02a93e2f569a104` (`release/3.10.0`; #9193; not #9198) |
| Producer workflow | `.github/workflows/prepare-consolidated-3.10.1-release-candidate.yml` |
| Producer run / attempt | `42458011137` / `1` (`workflow_dispatch`, conclusion `success`) |
| Artifact | `13609452227` |
| Artifact name | `consolidated-candidate-ba2b801a932646e2771167a8e02a93e2f569a104-42458011137-1` |
| Original envelope SHA-256 | `72a463f8a49b6fddfdd668779f45ab5d098a6fa665e1682fd646780652e57bc8` |
| Archive SHA-256 | `76a79c05967f8d5f74cb29e551518d1a6d4a4e74e1fcca924044e252ffee7d29` |
| Archive size | `96234443` |
| Pre-upload manifest SHA-256 | `f2d1a2bdf45aad88a835d827af8d4432df51db4edcc23201f9ebf57520d2444d` |
| Candidate expires | `2026-12-08T08:00:48Z` (`2026-12-08 09:00:48 Europe/Amsterdam`) |
| Retrieved at (producer retrieve job) | `2026-11-08T08:01:07Z` |
| Stage set | 231 `.nupkg` + 231 `.snupkg`, all version `3.10.1` |
| Assembly receipt | 688 entries, assembly version `3.10.1.0` |
| nuget.org promote (later) | 230 IDs; exclude only `Elsa.Platform.Integration`. `Elsa.Testing.Shared.Integration` and `Elsa.Studio.PlatformIntegration` stay in the 230. |
| Target feed (later) | `https://f.feedz.io/elsa-workflows/elsa-3/nuget` (symbols: `.../elsa-3/symbols`) |

#9198 is open on `release/3.10.0` and is **not** in this candidate. Do not
substitute that source.

## Roles

| Role | Identity |
| --- | --- |
| Dispatcher | `elsa-workflows-bot` (GitHub App) |
| Sole approver | `sfmskywalker` (user id `938393`) |
| Accountable owner | `sfmskywalker` |
| Hash signoff | **pending** `sfmskywalker`; no signature is recorded in this file |

The environment prevents self-review, so the approver cannot approve a run they
dispatched. The dispatcher is therefore a different identity from the approver.

## Environment identity

Independently re-read through the GitHub REST API at `2026-11-08T08:09:58Z`
(`repos/elsa-workflows/elsa-core/environments/feedz-publish` and its
`deployment-branch-policies`). Same environment as the 3.10.0 packet; a change
to the environment still fail-closes until a new reviewed PR pins the new values.

| Field | Value |
| --- | --- |
| Environment | `feedz-publish` (id `25739609238`) |
| Administrator bypass | off (`can_admins_bypass: false`) |
| Required reviewers rule | id `70613239`, reviewers `[938393]`, `prevent_self_review: true` |
| Branch policy rule | id `70613240`, custom branch policies only |
| Deployment branch policies | `[{"id": 64850643, "type": "branch", "name": "main"}]` |
| Allowed ref | `refs/heads/main` |

## Feedz upload path (design; not implemented here)

Owner direction retains Feedz and nuget.org, with all QA gates. A 3.10.1 Feedz
publish cannot reuse the 3.10.0 executor identity. It also cannot silently reuse
PR #9192 or raise `MAX_SECONDS = 1800`. That bound is inspect plus all HTTP,
including sequential PackageBaseAddress GET readback.

Cold-feed sequential PUT-then-GET of 231 packages does not fit the remaining
slice after inspect. The reviewable design that **meets** this bound, without
weakening it, is a later distinct 3.10.1 executor/workflow that keeps
`MAX_SECONDS = 1800` and the 60-minute job timeout, uses bounded parallel HTTP
under that same deadline and `MAX_REQUESTS`, and refuses PUT unless remaining
time covers post-upload GET of those uploads. Pre-upload GET is not post-upload
proof. Uploaded bytes still need an after-GET. No new executor is implemented
until that design is reviewed.

## Status

Unsigned. Pending `sfmskywalker` hash signoff of this file's exact bytes. Not
release GO. QA independent audit is started and is not completed by this packet.
This packet takes effect only after that signoff and a later reviewed PR that
pins the resulting SHA-256 in a distinct 3.10.1 policy. Until then, selection
rejects nuget.org for `3.10.0` and `3.10.1`, and the 3.10.0 Feedz publisher stays
bound to artifact `12664519771`.

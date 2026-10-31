# Feedz publish operational packet

This packet records the decision that `REVIEWED_OPERATIONAL_POLICY` in
`scripts/integration-program/consolidated_package_executor.py` pins. The executor
pins the SHA-256 of this file's exact bytes as `operational_packet_sha256`, so any
edit to this file needs a matching change to that hash in a reviewed PR.

## Decision

Publish the pinned 3.10.0 consolidated candidate to the Feedz `elsa-3` feed through
the `feedz-publish` environment, using
`.github/workflows/publish-consolidated-original-candidate.yml` from `refs/heads/main`.

| Candidate field | Value |
| --- | --- |
| Version | `3.10.0` |
| Source commit | `5cff67228e550b353b3483eb3b37dfdb73a41c74` |
| Producer run / attempt | `40347931480` / `1` |
| Artifact | `12664519771` |
| Original envelope SHA-256 | `93fdcbd271fe712ab9c96d0ee868528c608e6bd13517c17d319eecd2c2f0be48` |
| Archive SHA-256 | `2dd253e1f2946ddcb2b8e75ef397596001757ddbcb0a91f717e9505bbf28b4f6` |
| Pre-upload manifest SHA-256 | `b6a1750f1f796f68a441bef93f67386b4a862dbca94d498ecf64536b102c2994` |
| Candidate expires | `2026-11-25T16:05:50Z` |
| Target feed | `https://f.feedz.io/elsa-workflows/elsa-3/nuget` (symbols: `.../elsa-3/symbols`) |

## Roles

| Role | Identity |
| --- | --- |
| Dispatcher | `elsa-workflows-bot` (GitHub App) |
| Sole approver | `sfmskywalker` (user id `938393`) |
| Accountable owner | `sfmskywalker` |

The environment prevents self-review, so the approver cannot approve a run they
dispatched. The dispatcher is therefore a different identity from the approver.

## Environment identity

Observed read-only through the GitHub REST API at `2026-10-30T02:12:00Z`
(`repos/elsa-workflows/elsa-core/environments/feedz-publish` and its
`deployment-branch-policies`).

| Field | Value |
| --- | --- |
| Environment | `feedz-publish` (id `25739609238`) |
| Administrator bypass | off (`can_admins_bypass: false`) |
| Required reviewers rule | id `70613239`, reviewers `[938393]`, `prevent_self_review: true` |
| Branch policy rule | id `70613240`, custom branch policies only |
| Deployment branch policies | `[{"id": 64850643, "type": "branch", "name": "main"}]` |
| Allowed ref | `refs/heads/main` |

Any change to the environment, including deleting and recreating it, changes these
ids. Admission then fails closed until a new reviewed PR pins the new values.

## Status

Pending approval. This packet takes effect only after `sfmskywalker` signs it off
and the PR that pins it is merged.

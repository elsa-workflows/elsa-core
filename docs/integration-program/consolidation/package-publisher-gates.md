# Package and coverage publication gates

The Packages workflow now builds and tests normally on pushes and release events, but those events cannot publish packages or deploy coverage. The four publication jobs are default-off `workflow_dispatch` options:

- `publish_preview_feedz` publishes the run's packages to Feedz. Dispatch from a branch for a preview build (`<base_version>-preview.<run number>`), or from a 3.10.x prerelease tag. Stable 3.10.x Feedz is owned only by `publish-consolidated-original-candidate.yml`.
- `publish_nuget` publishes a release-tag package to NuGet.org. Dispatch from a release-version tag that is not a stable 3.10.x tag. Stable `3.10.0` nuget.org is rejected: the staged candidate (run `40347931480`, artifact `12664519771`, source `5cff67228e550b353b3483eb3b37dfdb73a41c74`) is immutable historical evidence and must not be promoted. Stable `3.10.1` stays rejected until Sipke signs the distinct 3.10.1 packet and a later reviewed PR wires that pin; the 3.10.0 candidate must not be reused. Other tags push this run's pack. An optional `exclude_ids` dispatch input lists package IDs to skip on a `publish_nuget` dispatch for a stable 3.10 tag; empty keeps the full set. A non-empty list is rejected on any other path, and a stable 3.10.x nuget.org dispatch is still rejected while that later pin is unsigned/unwired. Remaining packages must not depend on an excluded ID.
- `publish_npm` publishes `@elsa-workflows/elsa-studio-wasm` and `@elsa-workflows/elsa-studio-wasm-react` to Feedz. `build_npm` builds the pair without credentials. Dispatch from a release-version tag. Feedz npm is live for 3.10.x prerelease tags. The npmjs.com upload is not in this job until trusted publishing is confirmed for `elsa-workflows/elsa-core` (#8725). Stable 3.10.x is rejected in the selection check and also fail-closed in the job. The pair builder accepts 3.8–3.10 versions only, so a 3.11+ tag fails that check.
- `deploy_coverage` deploys the coverage report to GitHub Pages. Dispatch from `main` or `release/3.6.1`, the refs whose coverage run uploads a Pages artifact.

A `3.10.0` tag `release` event (and a non-promote dispatch from that tag) packs through `prepare_consolidated_release_candidate.py` instead of Core-only `Compile+Pack`, then runs the 231-ID guard. The inventory baseline is per branch: `consolidated-candidate-inventory-baseline.json` on `main` is main's evaluation. `release/3.10.0` must regenerate it from that branch; the two inventories are not the same. A dry-run of the Core-only path on `release/3.10.0` @ `ca7ac4ff` produced 109 IDs.

A tag dispatch accepts only release-version tags matching `^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z]+(\.[0-9A-Za-z]+)*)?$`, such as `3.8.4`, `3.8.0-rc2` or `3.10.0`. Tags such as `v3.10.0` or `test-1` are rejected whatever the options, and so is a 3.10 release spelled without the literal `3.10.` prefix (for example `03.10.0`). The tagged commit must also be contained in `main` or a `release/*` branch; the build checks this before packing. A stable 3.10.x nuget.org dispatch is rejected by the selection check: `3.10.0` must not be promoted, and `3.10.1` cannot reach nuget.org or Feedz until the distinct pin is signed and wired. The retrieve/stage steps in `publish_nuget` remain pinned to the 3.10.0 candidate identity so a loosened selection cannot silently publish those bytes as a later tag.

Each option defaults to `false`. An authorized operator should set only the option approved for that run; the workflow run records the selected ref and inputs. Package publication and coverage deployment remain separate decisions. Builds and uploaded package artifacts remain available without opting in when their event already uploads them; a manual dispatch uploads package artifacts only when a publisher option is selected.

## Trigger to publication matrix

| Trigger and ref | Selected options | Package build | Feedz | NuGet.org | Coverage to Pages |
|---|---|---|---|---|---|
| Push to `main` or `release/*` | none (inputs exist only on dispatch) | yes; artifact uploaded | no | no | no |
| Release published | none | yes, at the tag version (`3.10.0` uses the 231-ID candidate pack); artifact uploaded | no | no | no |
| Dispatch from any branch or release tag | none | yes; no artifact | no | no | no |
| Dispatch from a branch | `publish_preview_feedz` | yes | preview version | no | no |
| Dispatch from a release tag | `publish_nuget` | yes, at the tag version | no | tag version | no |
| Dispatch from a 3.10.x prerelease tag | `publish_preview_feedz` | yes, at the tag version | tag version | no | no |
| Dispatch from a stable 3.10.x tag | `publish_preview_feedz` | rejected | no | no | no |
| Dispatch from a stable 3.10.0 tag | `publish_nuget` | rejected | no | no (must not promote staged 3.10.0) | no |
| Dispatch from a later stable 3.10.x tag (`3.10.1`, …) | `publish_nuget` | rejected | no | no (blocked until the distinct pin is signed and wired) | no |
| Dispatch from `main` or `release/3.6.1` | `deploy_coverage` | yes | no | no | deployed |
| Dispatch from a 3.10.x prerelease tag | `publish_npm` | yes, at the tag version | Feedz npm | no | no |
| Dispatch from a stable 3.10.x tag | `publish_npm` | rejected | no | no | no |
| Dispatch from a 3.11+ tag | `publish_npm` | pair version check fails | no | no | no |
| Dispatch from a branch | `publish_nuget` | rejected | no | no | no |
| Dispatch from a release tag other than 3.10.x | `publish_preview_feedz` | rejected | no | no | no |
| Dispatch from any other branch, or any tag | `deploy_coverage` | rejected | no | no | no |
| Dispatch from a tag that is not a release version | any, including none | rejected | no | no | no |

Compatible options can be combined in one run. The `Validate publication selection` job runs the selection check (`scripts/validate_packages_publication_selection.py`) on every manual dispatch. If any selected option does not fit the ref, the check fails and the whole run is rejected: the package build and every publisher and deployment job are skipped, including the options that would have been valid on their own. Tests still run. Check the selected job's result, not just the build result, before reporting publication or deployment complete.

## Activation and rollback

After the relevant release or deployment approval, use **Actions → Packages → Run workflow**. The **Use workflow from** picker lists both branches and tags. Select the approved branch or release tag and enable only the matching option. The workflow-dispatch CLI or REST API works too, with the approved branch or tag as `ref`, for example `gh workflow run packages.yml --ref 3.8.5 -f publish_nuget=true`. Review the run ref, inputs, produced package artifacts, and job result before treating the operation as complete.

To cancel an opt-in before the job starts, cancel that workflow run. Once packages are pushed or Pages is deployed, this gate cannot undo that external effect: follow the feed's package correction policy or redeploy the previous accepted coverage revision. To restore the former automatic behavior in code, revert the gate changes as a separately reviewed change; doing so restores publication/deployment on matching events and should occur only after the publisher/deployment boundary is intentionally reopened.

## Local validation

The workflow validator parses the workflow with PyYAML (`python3 -m pip install PyYAML`). Run:

```sh
actionlint -shellcheck '' .github/workflows/packages.yml .github/workflows/validate-packages-gates.yml
python3 scripts/validate_packages_workflow_gates.py
python3 -m unittest discover -s scripts -p 'test_validate_packages_*.py'
```

The validator parses `packages.yml` and fails in these cases:

- Any job other than the four gated jobs references a Feedz or NuGet secret, the whole `secrets` context, `NuGet/login`, `actions/deploy-pages`, `dotnet nuget push`, the `push-nuget-packages` action, `npm publish`, `id-token: write`, an `environment`, or reusable-workflow secrets. The same applies to workflow-level settings.
- A gated job does not need `validate_publication_selection`.
- A gated job's `if:` does not require both a `workflow_dispatch` event and its own input as top-level conditions.
- A gated job's `if:` calls a status function that would let it run after a failed selection check.
- The coverage allowlist differs from the selection check, or includes a ref for which no Pages artifact is uploaded.
- A boolean input does not default to `false`.
- The selection job does not run the selection check with the dispatched ref and inputs, or can continue on error.
- The selection check accepts a tag that is not a release version.
- `publish_preview_feedz` accepts a stable `3.10.x` tag, or the selection check accepts Feedz for `3.10.0` / `3.10.1`.
- The selection check accepts nuget.org for `3.10.0` or `3.10.1`.
- `publish_nuget` does not retrieve the pinned candidate, stage it, and run the ID-set guard before NuGet login, those steps can continue on error, or the stage+guard `run:` is not exactly those two commands on separate lines.
- `publish-consolidated-original-candidate.yml` does not run the same ID-set guard before its Feedz publish, or the guard step's `run:` is not exactly `set -euo pipefail`, stage, and the ID-set command on separate lines. A guard `shell:` or `defaults.run.shell` is rejected.
- `Compile+Pack` still runs on a stable 3.10.x nuget.org dispatch or a `3.10.0` tag pack, or `packages.yml` packs `ConsolidatedReleaseCandidate` / builds Studio ClientLibs instead of calling `prepare_consolidated_release_candidate.py`.
- `publish_npm` is missing, builds the pair itself, is not fail-closed on a stable 3.10.x tag, publishes to npmjs.com, or `validate_packages_id_set.txt` is not exactly 231 IDs.
- `build_npm` is missing, does not pin `setup-dotnet` to `10.0.300` at the `studio-npm-pair.yml` SHA, or `publish_npm` Feedz publish has no dist-tag / already-published handling.

The tests mutate copies of the real workflow to confirm each of these cases is rejected. They also check every accepted and rejected dispatch selection.
The nonpublishing `Validate package publication gates` workflow installs a hash-pinned PyYAML and runs the validator and tests on relevant PRs and main-branch changes.

## Corrected 3.10.1 candidate (unsigned identity; not publication)

The 3.10.0 producer (`prepare-consolidated-release-candidate.yml` `--version 3.10.0`), envelope, pin inventory, operational packet, and `publish-consolidated-original-candidate.yml` stay the immutable 3.10.0 historical path. Do not retarget them.

A distinct unpublished 3.10.1 identity is recorded in
`scripts/integration-program/consolidated_3101_candidate_input.py`,
`scripts/integration-program/persisted-workflow-upgrade/3101-original-envelope.json`,
and `docs/integration-program/feedz-publish-3.10.1-operational-packet.md`:

1. Source `ba2b801a932646e2771167a8e02a93e2f569a104` (not `5cff67228e550b353b3483eb3b37dfdb73a41c74`; #9198 is not in this candidate).
2. Producer `.github/workflows/prepare-consolidated-3.10.1-release-candidate.yml` run `42458011137` attempt `1`, artifact `13609452227`.
3. Archive SHA-256 `76a79c05967f8d5f74cb29e551518d1a6d4a4e74e1fcca924044e252ffee7d29`, size `96234443`, expiry `2026-12-08T08:00:48Z`, pre-upload manifest SHA-256 `f2d1a2bdf45aad88a835d827af8d4432df51db4edcc23201f9ebf57520d2444d`.
4. 231 `.nupkg` + 231 `.snupkg` to stage; nuget.org promote later excludes only `Elsa.Platform.Integration` (230). `Elsa.Testing.Shared.Integration` stays in the 230.
5. Packet is **unsigned** and pending Sipke hash signoff. `REVIEWED_OPERATIONAL_POLICY` still hashes the 3.10.0 packet. Stage `PINNED_TAG` stays `refs/tags/3.10.0`.

A 3.10.1 Feedz upload is **not** implemented here. A later distinct 3.10.1 executor is proposed; fit against `MAX_SECONDS = 1800` requires measurement and is not claimed here. Do not reuse PR #9192 and do not silently raise that bound.

Operator inputs that stay rejected until the 3.10.1 packet is signed and a later PR wires it:

```text
gh workflow run packages.yml --ref 3.10.0 -f publish_nuget=true
gh workflow run packages.yml --ref 3.10.1 -f publish_nuget=true
gh workflow run publish-consolidated-original-candidate.yml -f operation=publish
```

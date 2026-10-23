# Package and coverage publication gates

The Packages workflow now builds and tests normally on pushes and release events, but those events cannot publish packages or deploy coverage. The four publication jobs are default-off `workflow_dispatch` options:

- `publish_preview_feedz` publishes the run's packages to Feedz. Dispatch from a branch for a preview build (`<base_version>-preview.<run number>`), or from a 3.10.x prerelease tag. Stable 3.10.x Feedz is owned only by `publish-consolidated-original-candidate.yml`.
- `publish_nuget` publishes a release-tag package to NuGet.org. Dispatch from a release-version tag. Stable 3.10.x nuget.org publishing is blocked until re-pin ([#9016](https://github.com/elsa-workflows/elsa-core/issues/9016)): the job downloads the current pin (run/artifact `11412848210`), verifies each `.nupkg` sha256 against the candidate seal, and runs the 231-ID list and Studio content guards on that exact set before any push. The current 225-package set fails that guard. Other tags push this run's pack.
- `publish_npm` publishes `@elsa-workflows/elsa-studio-wasm` and `@elsa-workflows/elsa-studio-wasm-react`. Dispatch from a release-version tag. Feedz npm is live for prerelease tags. The npmjs.com upload is fail-closed on every ref until trusted publishing is confirmed for `elsa-workflows/elsa-core` (#8725). Stable 3.10.x also stays fail-closed before Feedz until the 231-ID nuget.org promote guard can pass.
- `deploy_coverage` deploys the coverage report to GitHub Pages. Dispatch from `main` or `release/3.6.1`, the refs whose coverage run uploads a Pages artifact.

A `3.10.0` tag `release` event (and a non-promote dispatch from that tag) packs the 231-ID set through `prepare_consolidated_release_candidate.py` instead of Core-only `Compile+Pack`. A dry-run of that Core-only path on `release/3.10.0` @ `ca7ac4ff` produced 109 IDs.

A tag dispatch accepts only release-version tags matching `^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z]+(\.[0-9A-Za-z]+)*)?$`, such as `3.8.4`, `3.8.0-rc2` or `3.10.0`. Tags such as `v3.10.0` or `test-1` are rejected whatever the options, and so is a 3.10 release spelled without the literal `3.10.` prefix (for example `03.10.0`). The tagged commit must also be contained in `main` or a `release/*` branch; the build checks this before packing. A stable 3.10.x nuget.org dispatch still runs this check but skips pack; publication of those bytes is blocked until re-pin ([#9016](https://github.com/elsa-workflows/elsa-core/issues/9016)). `3.10.1` and later cannot reach nuget.org or Feedz until a new pin exists.

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
| Dispatch from a stable 3.10.0 tag | `publish_nuget` | no pack; retrieve + 231-ID guard (blocked until re-pin (#9016)) | no | blocked until re-pin (#9016) | no |
| Dispatch from a later stable 3.10.x tag (`3.10.1`, …) | `publish_nuget` | no pack; pin is 3.10.0 only | no | blocked until a new pin | no |
| Dispatch from `main` or `release/3.6.1` | `deploy_coverage` | yes | no | no | deployed |
| Dispatch from a release tag | `publish_npm` | yes, at the tag version | Feedz npm; npmjs fail-closed | no | no |
| Dispatch from a 3.10.x prerelease tag | `publish_npm` | yes, at the tag version | Feedz npm; npmjs fail-closed | no | no |
| Dispatch from a stable 3.10.x tag | `publish_npm` | 3.10.0 candidate pack; npm fail-closed | no | no | no |
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
- `publish_nuget` does not retrieve the pinned candidate, stage it, and run the ID-set guard before NuGet login, those steps can continue on error, or the stage+guard `run:` is not exactly those two commands on separate lines.
- `publish-consolidated-original-candidate.yml` does not run the same ID-set guard before its Feedz publish, or the guard step's `run:` is not exactly `set -euo pipefail`, stage, and the ID-set command on separate lines. A guard `shell:` or `defaults.run.shell` is rejected.
- `Compile+Pack` still runs on a stable 3.10.x nuget.org dispatch or a `3.10.0` tag pack, or `packages.yml` packs `ConsolidatedReleaseCandidate` / builds Studio ClientLibs instead of calling `prepare_consolidated_release_candidate.py`.
- `publish_npm` is missing, is not fail-closed on a stable 3.10.x tag, is not fail-closed on npmjs until trusted publishing is confirmed, or `validate_packages_id_set.txt` is not exactly 231 IDs.

The tests mutate copies of the real workflow to confirm each of these cases is rejected. They also check every accepted and rejected dispatch selection.
The nonpublishing `Validate package publication gates` workflow installs a hash-pinned PyYAML and runs the validator and tests on relevant PRs and main-branch changes.

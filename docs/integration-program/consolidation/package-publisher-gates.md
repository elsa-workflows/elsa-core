# Package and coverage publication gates

The Packages workflow now builds and tests normally on pushes and release events, but those events cannot publish packages or deploy coverage. The three publication jobs are default-off `workflow_dispatch` options:

- `publish_preview_feedz` publishes the run's packages to Feedz. Dispatch from a branch for a preview build (`<base_version>-preview.<run number>`), or from a 3.10.x release tag, which publishes at the tag's release version. 3.10.x is Feedz-only.
- `publish_nuget` publishes the release-tag build to NuGet.org. Dispatch from a release tag that is not 3.10.x.
- `deploy_coverage` deploys the coverage report to GitHub Pages. Dispatch from `main` or `release/3.6.1`, the refs whose coverage run uploads a Pages artifact.

A tag dispatch accepts only release-version tags matching `^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z]+(\.[0-9A-Za-z]+)*)?$`, such as `3.8.4`, `3.8.0-rc2` or `3.10.0`. Tags such as `v3.10.0` or `test-1` are rejected whatever the options, and so is a 3.10 release spelled without the literal `3.10.` prefix (for example `03.10.0`). The tagged commit must also be contained in `main` or a `release/*` branch; the build checks this before packing.

Each option defaults to `false`. An authorized operator should set only the option approved for that run; the workflow run records the selected ref and inputs. Package publication and coverage deployment remain separate decisions. Builds and uploaded package artifacts remain available without opting in when their event already uploads them; a manual dispatch uploads package artifacts only when a publisher option is selected.

## Trigger to publication matrix

| Trigger and ref | Selected options | Package build | Feedz | NuGet.org | Coverage to Pages |
|---|---|---|---|---|---|
| Push to a configured branch | none (inputs exist only on dispatch) | yes; artifact uploaded | no | no | no |
| Release published | none | yes, at the tag version; artifact uploaded | no | no | no |
| Dispatch from any branch or release tag | none | yes; no artifact | no | no | no |
| Dispatch from a branch | `publish_preview_feedz` | yes | preview version | no | no |
| Dispatch from a release tag other than 3.10.x | `publish_nuget` | yes, at the tag version | no | tag version | no |
| Dispatch from a 3.10.x release tag | `publish_preview_feedz` | yes, at the tag version | tag version | no | no |
| Dispatch from `main` or `release/3.6.1` | `deploy_coverage` | yes | no | no | deployed |
| Dispatch from a branch | `publish_nuget` | rejected | no | no | no |
| Dispatch from a release tag other than 3.10.x | `publish_preview_feedz` | rejected | no | no | no |
| Dispatch from a 3.10.x release tag | `publish_nuget` | rejected | no | no | no |
| Dispatch from any other branch, or any tag | `deploy_coverage` | rejected | no | no | no |
| Dispatch from a tag that is not a release version | any, including none | rejected | no | no | no |

Compatible options can be combined in one run. The `Validate publication selection` job runs the selection check (`scripts/validate_packages_publication_selection.py`) on every manual dispatch. If any selected option does not fit the ref, the check fails and the whole run is rejected: the package build and every publisher and deployment job are skipped, including the options that would have been valid on their own. Tests still run. The `publish_nuget` job also excludes `3.10.` tags in its own condition, so a 3.10.x tag cannot reach NuGet.org even if the selection check were bypassed. Check the selected job's result, not just the build result, before reporting publication or deployment complete.

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

- Any job other than the three gated jobs references a Feedz or NuGet secret, the whole `secrets` context, `NuGet/login`, `actions/deploy-pages`, `dotnet nuget push`, the `push-nuget-packages` action, `id-token: write`, an `environment`, or reusable-workflow secrets. The same applies to workflow-level settings.
- A gated job does not need `validate_publication_selection`.
- A gated job's `if:` does not require both a `workflow_dispatch` event and its own input as top-level conditions.
- A gated job's `if:` calls a status function that would let it run after a failed selection check.
- `publish_nuget` does not exclude `3.10.` tags.
- The coverage allowlist differs from the selection check, or includes a ref for which no Pages artifact is uploaded.
- A boolean input does not default to `false`.
- The selection job does not run the selection check with the dispatched ref and inputs, or can continue on error.
- The selection check accepts NuGet.org publication of a 3.10.x tag, or accepts a tag that is not a release version.

The tests mutate copies of the real workflow to confirm each of these cases is rejected. They also check every accepted and rejected dispatch selection.
The nonpublishing `Validate package publication gates` workflow installs a hash-pinned PyYAML and runs the validator and tests on relevant PRs and main-branch changes.

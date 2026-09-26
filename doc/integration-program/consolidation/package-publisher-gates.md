# Package and coverage publication gates

The Packages workflow now builds and tests normally on pushes and release events, but those events cannot publish packages or deploy coverage. The three publication jobs are default-off `workflow_dispatch` options:

- `publish_preview_feedz` publishes the branch build to Feedz. Dispatch from a branch.
- `publish_nuget` publishes the release-tag build to NuGet.org. Dispatch from a tag whose commit is contained in `main` or a `release/*` branch. The Actions UI picker selects branches; use the workflow-dispatch CLI or REST API with the approved tag as `ref` for this option.
- `deploy_coverage` deploys the coverage report to GitHub Pages. Dispatch from `main`, `develop/3.6.1`, or `release/3.6.1`.

Each option defaults to `false`. An authorized operator should set only the option approved for that run; the workflow run records the selected ref and inputs. Package publication and coverage deployment remain separate decisions. Builds and uploaded package artifacts remain available without opting in when their event already uploads them; a manual dispatch uploads package artifacts only when a publisher option is selected.

## Activation and rollback

After the relevant release or deployment approval, use **Actions → Packages → Run workflow** for an approved branch, or the workflow-dispatch CLI/API for an approved tag, and enable only the matching option. For a NuGet release, dispatch the approved release tag. Review the run ref, inputs, produced package artifacts, and job result before treating the operation as complete.

To cancel an opt-in before the job starts, cancel that workflow run. Once packages are pushed or Pages is deployed, this gate cannot undo that external effect: follow the feed's package correction policy or redeploy the previous accepted coverage revision. To restore the former automatic behavior in code, revert the gate changes as a separately reviewed change; doing so restores publication/deployment on matching events and should occur only after the publisher/deployment boundary is intentionally reopened.

## Local validation

Run:

```sh
actionlint -shellcheck '' .github/workflows/packages.yml
python3 scripts/validate_packages_workflow_gates.py
```

The validator checks that all three inputs remain explicitly default-off, that their jobs require the matching manual opt-in and eligible ref type, and that normal main pushes cannot publish or deploy while each deliberate opt-in case can.

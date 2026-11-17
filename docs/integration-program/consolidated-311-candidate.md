# Consolidated Elsa 3.11 NuGet candidate

Stable Elsa 3.11 uses the consolidated candidate workflow. The lockstep inventory includes 251 NuGet package IDs and 251 matching symbol packages. It includes the 231 stable 3.10 IDs, 18 React Studio packages labeled Preview, and two additional Blazor BPMN packages. The version-specific project baseline records every evaluated package and exclusion. The ID guard also checks the React NuGet archives for the Preview tag and the root `SOURCE-NOTICE.md` and `README.md` files. Those checks verify package labeling and notice inclusion. Distribution clearance is tracked separately.

## Private dry-run

With Python 3.12, Node.js 22, and the source's .NET SDK installed, use an independent clean local checkout of the proposed source. Set its local branch to `release/3.11.0` and configure the canonical Git origin URL for build provenance. Run from the reviewed tooling checkout with an unused output path outside the source directory.

```bash
export PYTHONDONTWRITEBYTECODE=1
version=3.11.0
source_checkout=/absolute/path/to/local/source
candidate_output=/absolute/path/to/private/output
source_commit=$(git -C "$source_checkout" rev-parse HEAD)
python3 scripts/integration-program/prepare_consolidated_release_candidate.py \
  check-source --version "$version" --source "$source_commit" --source-root "$source_checkout"
python3 scripts/integration-program/prepare_consolidated_release_candidate.py \
  --version "$version" --source-root "$source_checkout" --output "$candidate_output"
PACKAGES_DIR="$candidate_output/artifacts" VERSION="$version" python3 scripts/validate_packages_id_set.py
python3 scripts/integration-program/prepare_consolidated_release_candidate.py \
  check-nupkgs --version "$version" --packages-dir "$candidate_output/artifacts"
```

Keep the resulting inventory, package and symbol verification, source-provenance report, receipt, and pre-upload manifest private. Adding `--inventory-only` to the preparation command evaluates the complete source and compares it to the baseline. Its result covers project evaluation. Full preparation also builds and inspects the packages. Local preparation uses the checkout's Git objects for source verification. The producer workflow verifies the pushed source separately and runs representative consumers from the recovered archive.

## Candidate handoff and promotion

After the release owner clears external handling of the candidate, dispatch `prepare-consolidated-release-candidate.yml` from `main` with the exact source SHA on `release/3.11.0` and the stable version. Record the source and tooling SHAs, successful producer run, first attempt, artifact ID, original archive SHA-256, manifest SHA-256, and representative consumer receipts. The producer retains an immutable archive for 30 days. The promotion verifier binds the selected version to that archive, verifies the production source ancestry and tooling SHA, and checks every sealed file and package/symbol pair.

With release-owner approval, add the reviewed version entry to `.github/release-pins/consolidated.json` on `main`. For the full lockstep inventory both feed counts are 251 and `exclude_ids` is empty. A deliberate NuGet.org exclusion requires a reviewed pin, an adjusted count, and a successful dependency-closure check. Use the protected `packages.yml` promotion on `main` with `promote_candidate_version` set, the approved `promote_target`, and the other publication selections off. The candidate is independently verified before either feed credential is acquired. Ordinary NuGet tag publication refuses the 3.11 line.

Before promotion, record the confirmed React donor grant and approved distribution terms from the release owner. Run React browser acceptance on the selected source and record the exact candidate evidence. Also verify the live `feedz-publish` and `nuget-org` environment branch/reviewer controls, Feedz authentication, and NuGet.org trusted publishing for this workflow and package ownership. The reviewed workflow and a private package build provide no account-configuration, approval, live-feed, or browser-acceptance evidence. Studio npm, container images, GitHub releases and tags, and Templates have separate release steps.

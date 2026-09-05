# Elsa release runbook

Read this once at the start of a release. Codex operates the procedure; the user supplies a version and optional exceptions. Scripts handle repeatable checks and state, while Codex performs source review, follows repository build instructions, and uses connected social tools. No script assumes it can author release notes or call a connector available only to the agent.

## 1. Resolve the plan and preflight all repositories

Helpers require Python 3.10+ on macOS/Linux, Git, GitHub CLI, and the SDKs required by the source repositories. They use the Python standard library.

Use [elsa-profile.json](elsa-profile.json). This is the maintained default for repository names, dependency declarations, expected jobs, feeds, fixed-version exceptions, post-release website/documentation targets, and announcement destinations. Read [post-release site guidance](post-release-sites.md) for the content audit and receipt workflow. A custom `--profile` can change these for another environment; do not put credentials in it. Discover repository paths from the workspace/saved projects and verify each GitHub remote. Do not assume the user's saved checkouts are up to date.

Default release branches are `release/<base-version>`. Fetch and inspect all required repositories before publishing Core. An explicit source such as `core=3.9.0-rc1` overrides only that source. An absent branch or conflicting version needs a concrete source decision; the helper must not fall back to `HEAD` or manufacture a branch from an unrelated release line.

Check:

- GitHub authentication, repository access, branch policies, current tags/releases, workflow configuration, supported SDKs, feeds in `NuGet.Config`, and the configured announcement tools/accounts. Report missing credentials without printing them. GitHub publishing uses its existing OIDC/secrets; local NuGet publishing credentials are not required.
- Named prerequisite PRs are merged and included in the intended source. Waiting on a specified PR means monitoring that PR; implementing or merging it requires authorization for that work. No other open PR becomes a gate automatically.
- All three workflow `base_version` values match the new release line before branch CI. Version tags drive named release artifacts, but the branch preview version must also be correct. Make this routine preparation change on the intended release branch when needed.
- The workflow subscribes once to `release: types: [published]`. GitHub `published` covers both stable and prereleases. Use `release.prerelease` or parsed package versions for distinctions, never `event.action == published` as a stable-only test. [GitHub reference](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#release).
- The profile intentionally publishes named stable, RC, and preview GitHub releases to NuGet and Feedz. Automated branch previews are separate, Feedz-only builds. Studio named prereleases use npm `next`; stable uses `latest`. If the source workflow disagrees with this policy, resolve the mismatch before release; do not weaken verification to match missing output.
- Identify advisories and build/test prerequisites early. Assess actual usage and document disposition; a known warning from a prior release is evidence, not permanent acceptance. Keep unrelated upgrades out of the release. New material unresolved risks or failures require an explicit resolution before irreversible publication.

Use a persistent directory outside the repositories, for example `~/.codex/releases/elsa/3.9.0`. It holds source bindings, notes, artifact manifests/downloads, reports, and post receipts. Keep one release owner; the checkpoint uses an OS lock for concurrent updates. Do not run multiple publishing agents for the same version.

Initialize the plan (local state only):

```bash
python3 <skill>/scripts/release_train.py --state <run>/state.json init \
  --version 3.9.0 --repos-root <parent-of-the-three-repositories>
```

Optional arguments: `--kind stable|rc|preview`, `--repositories core studio`, `--source core=3.9.0-rc1`, repeated `--pr <URL>`, `--no-announcements`, `--no-post-refresh`, and `--profile <JSON>`. The two flags are independent. For “draft announcements”, use `--no-announcements` for publication tracking and retain the explicit draft requirement in the task notes. A named subset includes its upstream repositories as verification-only dependencies; do not publish those implicitly. The post-release receipt scope contains only the selected repositories.

Repeating `init` with identical inputs preserves progress. Conflicting inputs fail rather than overwrite an in-flight plan.

## 2. Follow the next phase

```bash
python3 <skill>/scripts/release_train.py --state <run>/state.json status
```

The helper inspects current GitHub releases, resolved tag SHAs, exact release-event workflow runs, and successful required jobs. Verified package receipts are bound to manifest/report hashes and the immutable source. Stored `running` text is never evidence of a live job. Recheck actual package feeds on a long-delayed resume or any provenance concern.

`adopt-existing` → reconstruct the binding at the returned immutable tag SHA, recover its published notes, generate the source inventory, download that release run’s artifacts and verify them; do not create another release. `prepare` → create an isolated worktree from the selected source, prepare and validate it. `publish` → run the reviewed release helper. `wait-for-run` → observe the returned run ID. `repair-pipeline` → diagnose that run. `verify-packages` → verify the downloaded artifacts. `wait-for-upstream` → complete the indicated dependency. `sites` → follow [post-release site guidance](post-release-sites.md), update the enabled website/documentation targets, and record live production receipts. `announcements` → follow the announcement skill. `adopt-post-refresh` → explicitly upgrade a legacy checkpoint; use `--targets website` for a website-only follow-up on an already completed release and preserve its existing announcement receipts. `missing-upstream-release` for a verification-only dependency requires the missing upstream release to exist, or new authorization to expand scope.

Poll live jobs/feeds at a bounded interval (typically 30–60 seconds, then back off). A timeout is not failure and must not start a replacement run. Announce meaningful changes rather than narrating identical polls. Use the environment's persistent goal or a single supported heartbeat if waiting beyond the active turn; preserve state and stop the monitor at completion.

## 3. Prepare and validate one repository

Preserve the saved checkout. Create an isolated `codex/release-<version>` worktree from the intended source. Validate Core first. Only after verified upstream publication may a downstream worktree's package references change:

```bash
python3 <skill>/scripts/release_train.py --state <run>/state.json align \
  --repo studio --repo-path <studio-worktree>
# Review the declared files, then repeat with --execute.
```

The helper updates only the configured declarations and checks upstream evidence live. Studio uses `Directory.Packages.props` → `Elsa.Api.Client`. Extensions uses `Directory.Build.props` → `ElsaVersion` and `ElsaStudioVersion`. It rejects missing/duplicate declarations and dirty tracked worktrees. Repeated alignment of already-correct values is harmless. Do not update dependency caches or source npm placeholder versions as substitutes for the configured package references.

Use the source repository's current AGENTS/build/workflow instructions. As of this profile:

- Core: relevant regression coverage plus the branch workflow's unit/integration/component and package build checks; real host/API smoke tests when the prerequisite touches endpoint discovery or hosting.
- Studio: build the JavaScript assets as its workflow does; fresh solution restore, Release solution build and tests for the release version.
- Extensions: fresh solution restore, Release solution build and tests for the release version.

Use `--force --no-cache` restores for changed upstream packages and inspect `project.assets.json` for exact resolved versions and package provenance. Verify all supported target frameworks via the repository's build. Record commands, exit results and intentional service-dependent test skips against the exact commit. Resolve failures; do not replace broad required checks with a convenient small passing subset.

Review and commit only intended changes. Integrate into the intended release branch according to actual branch protection: ordinary fast-forward where permitted, otherwise PR plus required checks/review and authorized merge. The explicit full-release instruction includes routine dependency/version integration. It does not authorize bypassing branch protection, unrelated fixes, or changing existing release tags. Wait for branch CI build/test/pack success before tagging. An independent preview feed upload need not delay a stable tag once that validation passed.

## 4. Freeze notes, package inventory, and source

Generate and curate notes using [release-note guidance](release-notes.md). `release_notes.py` refuses overwriting an existing file unless explicitly requested. Remove its scaffold marker after review, retain the version metadata, and include every commit/PR in the selected range. Use the tested SHA for gathering commits and the destination version in the displayed comparison. Notes must describe the final source, including stable dependency alignment.

After validation and integration, generate the expected inventory by evaluating the solution's project properties, independently of the downloaded artifact set:

```bash
python3 <skill>/scripts/package_manifest.py --state <run>/state.json \
  --repo core --repo-path <core-worktree> --output <run>/core-manifest.json
python3 <skill>/scripts/release_train.py --state <run>/state.json bind \
  --repo core --repo-path <core-worktree> --commit <tested-SHA> \
  --manifest <run>/core-manifest.json --notes-file <run>/core-notes.md
```

Repeat for Studio and Extensions at their proper turn. The manifest records expected NuGet IDs/versions, source SHA, feeds, upstream dependency versions, and npm IDs/dist-tags. The Core sample has its own fixed source version; its explicitly documented artifact-only exception must remain anchored to that source declaration. Do not silently discard unexpected artifacts or change the expected package count to match an incomplete download.

Binding checks the worktree, source, notes and policy. A prepublication correction can use `bind --replace` after review, fresh validation and integration; it refuses replacement once the remote tag or release exists. After tagging, repair only publication/infrastructure without changing source. A source fix needs a new release version and a concrete user decision.

## 5. Publish and verify before proceeding

Show the resolved repository, SHA, tag, kind and notes in a progress update. Run the helper dry-run, inspect it, then execute under the user's existing release authorization:

```bash
python3 <skill>/scripts/release.py --repo-path <worktree> \
  --source-ref <bound-SHA> --tag <version> --release-kind <kind> \
  --notes-file <bound-notes-file>
# Same arguments plus --execute after reviewing the dry-run.
```

Matching existing tags/releases are reused. A different SHA, version/kind, or draft status fails. Do not force-push or recreate a tag. Re-run `status`; retain the returned release run ID. It must be a release-event run at the exact tag and SHA, with all configured publishing jobs successful.

Download every artifact configured by that source workflow into a dedicated `<run>/<repo>-artifacts/` directory using `gh run download <run-id> --repo <owner/repo> --name <artifact-name> --dir <directory>`. Download NuGet and Studio's two npm archives in separate subdirectories of that artifact root. Verify:

```bash
python3 <skill>/scripts/release_train.py --state <run>/state.json verify \
  --repo core --artifacts <run>/core-artifacts
```

`verify_packages.py` compares the explicit manifest with local artifacts and actual published feed content, handles NuGet repository signing, and checks npm integrity/dist-tags. Each attempt writes a report even when indexing is incomplete. Retry missing/not-yet-indexed packages with backoff; diagnose provenance mismatches rather than calling them propagation delays. Never accept a queued/uploaded state as package availability. Once Core verifies, prepare Studio; once Studio verifies, prepare Extensions.

NuGet verification is not a release-wide artifact verdict. Verify Docker images, templates, samples, and other configured release artifacts independently, including the exact source/version or image tag and the public pull/template URL. Record those checks in the site audit or release completion record before using the artifact in current guidance.

## 6. Refresh sites, announce, recover, and finish

After package verification, follow [post-release site guidance](post-release-sites.md). Update and live-verify the configured Elsa Hub website and Elsa GitBook documentation targets in the selected scope. Review pre-existing Lovable unpublished changes before publishing, include only release-related changes, and preserve unrelated work. A merged docs PR, a Lovable queue acknowledgement, or a preview URL without production verification is not completion. Record each verified target:

```bash
python3 <skill>/scripts/release_train.py --state <run>/state.json record-site \
  --target website --receipt <run>/website-receipt.json
python3 <skill>/scripts/release_train.py --state <run>/state.json record-site \
  --target documentation --receipt <run>/documentation-receipt.json
```

Only after the enabled site targets are verified, invoke [Elsa Release Announcements](../../elsa-release-announcements/SKILL.md) with the verified releases and notes. Use configured Discord, LinkedIn, and X destinations; discover and verify live accounts. The release request includes these standard posts unless explicitly overridden. Draft and review factual copy as part of the task, then publish now. Retain per-channel intent/message IDs before retrying, and verify actual sent state, exact text, public URLs and Discord crossposting.

The connector's verified result can be recorded as a small JSON receipt containing `id`, `url`, `text`, `status: sent` (or `published`), `error: null`, and for Discord `crossposted: true`. Record it:

```bash
python3 <skill>/scripts/release_train.py --state <run>/state.json record-announcement \
  --platform linkedin --receipt <run>/linkedin-receipt.json \
  --message-file <run>/linkedin.txt
```

The checkpoint validates receipt content and hashes, but does not replace the connector's live `get_post` verification. On an interrupted social publish, reconcile the recorded intent and current channel posts before sending anything again. Verify Discord with the bot API and LinkedIn/X with their connector. Queue status is only completion when the user requested scheduling. A legacy checkpoint reports `adopt-post-refresh`; adopt it explicitly, and use `--targets website` when only the website needs to be refreshed on an already completed release. Do not create a new publication or repost existing announcements for that follow-up.

Finally rerun `status`, audit the exact release/version/source/workflow and feed evidence, verify all requested social results, save a concise completion record, stop any heartbeat, and report release/post URLs plus any material limitation. Complete a persistent goal only after this audit.

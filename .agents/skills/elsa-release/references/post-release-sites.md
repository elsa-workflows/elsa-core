# Post-release website and documentation refresh

The release train has two default post-release content targets. They run after every selected repository has a verified package receipt and before social announcements. The phase is checkpointed separately from announcements so a content or deployment outage does not cause a release to be announced prematurely.

## Targets and audit matrix

| Target | Exact target | Update scope | Required update evidence | Required production evidence |
| --- | --- | --- | --- | --- |
| Official website | Lovable project `Elsa Hub`, project `42142296-d1c7-49c5-aeb5-ced6054f340b`, workspace `Skywalker Digital`; production `https://www.elsaworkflows.io`; preview `https://preview--elsa-hub.lovable.app` | The selected repository subset only. Preserve unrelated project work. | Reviewed pending changes, the release-labelled content update, a non-empty changed URL list, and a deployment or commit identifier. | A live check of `https://www.elsaworkflows.io` reporting the release version, with an evidence timestamp. |
| Documentation | `elsa-workflows/elsa-gitbook` `main`; production `https://docs.elsaworkflows.io` | The selected repository subset only. Integrate a normal reviewed docs PR into `main` according to repository policy. | PR/commit identifier, source-ground technical snippets, a non-empty changed URL list, and the merged commit. | A live check of `https://docs.elsaworkflows.io` reporting the release version, with an evidence timestamp. |

The receipt is accepted only when it contains all of the following: `id`, `target`, `status` (`completed`, `published`, or `verified`), exact `version`, sorted `scope`, `changed_urls`, `deployment_or_commit`, timezone-bearing `evidence_at`, and `production_verification` with `verified: true`, the configured production URL, the exact version, and the same evidence timestamp. Website receipts also carry the configured Lovable `project_id` and `workspace_name`; documentation receipts carry `repository` and `branch`.

Stable releases update the current stable guidance and use `content_label: stable`, `updates_current_stable: true`, and `replaces_latest_stable: true` when they are the newest stable release. A release on an older maintenance line may preserve a newer stable recommendation instead: use `updates_current_stable: false`, `replaces_latest_stable: false`, `latest_stable_version` greater than the release version, and verified `latest_stable_verification` evidence for that newer version. A preview or RC is additive and visibly labelled with its channel. It must preserve the latest stable guidance with `updates_current_stable: false` and `replaces_latest_stable: false`; it never moves a stable pointer or makes a prerelease the default recommendation.

Keep historical references such as Elsa 3.7 examples separate from current recommendations. A historical example may remain when it is explicitly labelled as historical, but do not present an older Elsa runtime as the new stable release in a current install path. A separately versioned template or container may remain the latest published artifact; label its actual embedded runtime version and give a verified upgrade or source-build path instead of inventing a matching version. Verify Docker images, templates, samples, and other release artifacts independently of NuGet package verification; a green NuGet feed check does not prove those artifacts were published or that the website points to them.

## Update workflow

1. Read the verified release notes, package manifest, source commit, and selected repository scope. Extract technical claims from the tagged source and release workflow. Do not copy an unverified issue description or invent an API, package, Docker tag, template version, migration, or timeline.
2. For the Lovable target, open the exact Elsa Hub project and inspect the existing unpublished changes before preparing the release update. Identify which pending changes belong to this release, preserve unrelated work, and review the complete proposed content before publishing. Never blindly promote all existing drafts. The Lovable connector may report `USER_NOT_LOGGED_IN`; use the signed-in browser UI when available and record the exact project and public target in the receipt.
3. For documentation, fetch `elsa-workflows/elsa-gitbook` and use an isolated worktree from its current `main`, preserving any saved-checkout edits. Use Git and `gh` for source changes and PR integration; use GitBook publication status and the live site for deployment verification. Create or update the normal docs PR, review the diff, and integrate it through the repository's ordinary checks and branch policy. A PR being merged is an update identifier, not production verification.
4. Check current stable pages and install paths, release-labelled pages, Docker/template/sample references, and links. For a prerelease, add or update only the relevant labelled page or section and leave the stable navigation and recommendation unchanged.
5. Audit source-backed roadmap release claims too. Use the sibling `elsa-roadmap-refresh` skill for bounded release-status corrections to Core `ROADMAP.md`, retaining unfinished productization work. Integrate the reviewed changes normally, mirror the final roadmap to Core issue #3232, and refresh the website roadmap snapshot through its existing admin sync. Verify that the next sync will not restore stale claims. Do not close unrelated issues or treat every feature on `main` or an earlier RC branch as part of the selected stable tag.
6. Compile changed runnable C# examples against the exact published packages, and test startup/authentication where the change affects setup. Check Markdown/navigation links and build the website; a TypeScript build does not validate C# snippets stored in strings. Read back affected production pages, metadata and code blocks after publishing. Historical links/slugs may remain when changing them would break links.
7. Publish or integrate the reviewed update, then verify the live production URL. Capture the version shown by the deployed page, every changed public URL on the configured production origin, the deployment ID or commit, and a past UTC evidence timestamp. Save the receipt before doing anything else.
8. Resume from the saved receipt and operation/message/project IDs. If an update response is uncertain, reconcile the exact target and existing operation before retrying; do not submit the same prompt or PR again merely because the connector timed out. An uncertain create is resolved by lookup, never by a blind duplicate.

The helper records each target with:

```bash
python3 <skill>/scripts/release_train.py --state <run>/state.json record-site \
  --target website --receipt <run>/website-receipt.json
python3 <skill>/scripts/release_train.py --state <run>/state.json record-site \
  --target documentation --receipt <run>/documentation-receipt.json
```

`status` remains in `sites` until every target in the checkpoint scope has a valid receipt. `--no-post-refresh` is independent of `--no-announcements`; use it only when the user explicitly excludes the content phase. A legacy checkpoint that predates this phase reports `adopt-post-refresh` rather than silently becoming complete. For a completed historical release that needs only the website refresh, explicitly adopt the existing checkpoint with `--targets website`; this preserves its verified package and announcement receipts and does not reopen publication or repost announcements:

```bash
python3 <skill>/scripts/release_train.py --state <run>/state.json adopt-post-refresh \
  --targets website
```

Adoption does not claim that the website was updated. It only creates the missing checkpoint; the live website receipt is still required before completion.

If a saved receipt is missing, tampered, or replaced by fresh live evidence, status remains in `sites` and the normal record command refuses to overwrite the existing binding. After reviewing the new target/version/production evidence, use `--replace` (or `--replace-site-receipt`) explicitly; the version, scope, origin, and live checks still apply.

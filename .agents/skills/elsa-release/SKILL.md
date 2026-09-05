---
name: elsa-release
description: Release Elsa Core, Studio, Extensions, and Templates in dependency order, including validation, package verification, post-release website/documentation refresh, recovery, and community announcements. Use for requests such as "release Elsa 3.9.0 stable", a named preview or RC, or resuming an interrupted Elsa release.
---

# Elsa Release

“Release Elsa <version>” means complete the release across Core, Studio, Extensions, and Templates, including the configured post-release website/documentation refresh and announcements. Read [the runbook](references/runbook.md) and [post-release site guidance](references/post-release-sites.md) before execution. They contain the commands, repository profile, prerequisites, content audit, validation and recovery procedure. Read [release-note guidance](references/release-notes.md) when preparing notes.

## Interpret the request

- A plain version such as `3.9.0` means stable. `3.9.0-rc1` means RC; `3.9.0-preview.1` means preview. RC and preview are both GitHub prereleases; an unspecified “prerelease” channel means preview. Reject contradictory version/kind combinations. If a prerelease number is omitted, inspect existing tags and use the next unused number for the named series; record the exact choice before mutations.
- Default to all four repositories in the order Core → Studio → Extensions → Templates. Honor a named subset; verify any upstream packages it needs without publishing repositories outside the requested scope.
- Default source is the freshly fetched `origin/release/<base-version>` for Core, Studio, and Extensions. Templates follows its documented repository policy: stable releases use freshly fetched `origin/main`, while RC/preview releases use `origin/release/<base-version>` or an explicit source override. Never use the caller's checkout HEAD as an accidental source or silently publish a preview from Templates `main`.
- Default to curated notes against the previous stable version for stable releases; for RC/preview use the previous release in that version's series, or the previous stable if this is the first. Stable promotion still requires rebuilding downstream repositories with stable dependency references.
- The default post-release scope is both the official Elsa Hub website and the Elsa GitBook documentation. A repository subset limits the content claims and receipt scope to the selected repositories; it does not silently publish an unselected package. Use `--no-post-refresh` only when the user explicitly excludes this phase. For an existing completed checkpoint, use explicit `adopt-post-refresh --targets website` for a website-only follow-up.
- A specific prerequisite such as “after PR #123 merges” is part of this release plan. Check that it is merged and included in the selected source. Do not infer that every open PR is a prerequisite or merge unrelated PRs. “Wait for” does not by itself request implementation of the prerequisite.

## Authorization and defaults

An explicit end-to-end release instruction authorizes the defined release operations: required dependency edits, validation and integration into the intended release branches, immutable tags, GitHub publication, routine content updates and deployment or normal documentation PR integration, and the standard release announcements. It does not authorize an unrelated redesign, promotion of unrelated pre-existing Lovable drafts, or a protection bypass. Present the concrete source/version/notes and dry-run result as a progress update; do not ask again for authorization already supplied by that instruction. Never interpret a request to assess, plan, or dry-run as authorization to publish.

Honor overrides such as “Core only”, “without announcements”, “without the website/docs refresh”, “draft announcements”, or a different source/channel. `--no-post-refresh` and `--no-announcements` are independent. For a standalone announcements request, use the authorization rules in the announcement skill. Ask only for missing intent that materially affects the release, a genuine new scope decision, or access that prevents completion. Do not weaken checks to avoid a question.

## Execution invariant

**Core release → verify its configured feeds → align Studio → validate, release and verify Studio → align Extensions → validate, release and verify Extensions → align and validate Templates → install and smoke-test the published Templates package → refresh and live-verify the website and documentation → announce and verify posts.**

Use the checkpoint helper in the runbook. It reports the next phase from GitHub state and package evidence; it does not publish automatically. Codex performs the indicated step, using `release.py` for publication and connectors for social posts. Keep only one owner for mutations. Preparation and independent review can run in parallel; dependency publication cannot.

## Completion and recovery

- A green upload job is insufficient: verify the expected package inventory, versions, source commits, dependencies, and actual feed content. Verify Studio npm artifacts and `latest`/`next` according to release kind.
- Bind exact source commits and reviewed manifests/notes before publication. Preserve existing tags; matching releases are reusable, conflicting releases require investigation.
- Resume by inspecting live GitHub state and recorded package/post evidence. Do not recreate tags or repost after an uncertain result. Retain run and message IDs; wait on concrete jobs with bounded polling and backoff.
- Assess advisory findings during preflight. Record severity, relevant usage, and disposition. Existing warnings are not automatically blockers or automatically accepted forever. Never insert an unvalidated dependency upgrade into a release to suppress a warning; a new material unresolved risk requires a concrete scope decision.
- After package verification, invoke [Elsa Release Announcements](../elsa-release-announcements/SKILL.md). Default to publishing now on Discord, LinkedIn, and X. Draft-only is an explicit override, not completion of a request to announce.
- Before announcements, complete the post-release site phase using receipts that identify the exact target, version, selected scope, changed public URLs, deployment/commit, and live production evidence with a timestamp. A queued Lovable operation or merged docs PR alone is not completion. Stable updates must preserve current stable guidance; prereleases must remain visibly labelled and never replace the latest stable recommendation.
- Complete only after every selected release and required feed, every enabled post-release target, and the requested announcements are verified sent (and Discord crossposted). State limitations accurately. Stop any follow-up monitor when finished.

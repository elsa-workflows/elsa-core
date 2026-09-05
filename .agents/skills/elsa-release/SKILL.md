---
name: elsa-release
description: Release Elsa Core, Studio, and Extensions in dependency order, including validation, package verification, recovery, and community announcements. Use for requests such as "release Elsa 3.9.0 stable", a named preview or RC, or resuming an interrupted Elsa release.
---

# Elsa Release

“Release Elsa <version>” means complete the release across Core, Studio, and Extensions, including the configured announcements. Read [the runbook](references/runbook.md) before execution. It contains the commands, repository profile, prerequisites, validation and recovery procedure. Read [release-note guidance](references/release-notes.md) when preparing notes.

## Interpret the request

- A plain version such as `3.9.0` means stable. `3.9.0-rc1` means RC; `3.9.0-preview.1` means preview. RC and preview are both GitHub prereleases; an unspecified “prerelease” channel means preview. Reject contradictory version/kind combinations. If a prerelease number is omitted, inspect existing tags and use the next unused number for the named series; record the exact choice before mutations.
- Default to all three repositories. Honor a named subset; verify any upstream packages it needs without publishing repositories outside the requested scope.
- Default source is the freshly fetched `origin/release/<base-version>` in each repository. Never use the caller's checkout HEAD as an accidental source. A missing or ambiguous release branch requires resolving the source before publication. Preserve explicit source choices, including promotion from a specified RC.
- Default to curated notes against the previous stable version for stable releases; for RC/preview use the previous release in that version's series, or the previous stable if this is the first. Stable promotion still requires rebuilding downstream repositories with stable dependency references.
- A specific prerequisite such as “after PR #123 merges” is part of this release plan. Check that it is merged and included in the selected source. Do not infer that every open PR is a prerequisite or merge unrelated PRs. “Wait for” does not by itself request implementation of the prerequisite.

## Authorization and defaults

An explicit end-to-end release instruction authorizes the defined release operations: required dependency edits, validation and integration into the intended release branches, immutable tags, GitHub publication, and the standard release announcements. Present the concrete source/version/notes and dry-run result as a progress update; do not ask again for authorization already supplied by that instruction. Never interpret a request to assess, plan, or dry-run as authorization to publish.

Honor overrides such as “Core only”, “without announcements”, “draft announcements”, or a different source/channel. For a standalone announcements request, use the authorization rules in the announcement skill. Ask only for missing intent that materially affects the release, a genuine new scope decision, or access that prevents completion. Do not weaken checks to avoid a question.

## Execution invariant

**Core release → verify its configured feeds → align Studio → validate, release and verify Studio → align Extensions → validate, release and verify Extensions → announce and verify posts.**

Use the checkpoint helper in the runbook. It reports the next phase from GitHub state and package evidence; it does not publish automatically. Codex performs the indicated step, using `release.py` for publication and connectors for social posts. Keep only one owner for mutations. Preparation and independent review can run in parallel; dependency publication cannot.

## Completion and recovery

- A green upload job is insufficient: verify the expected package inventory, versions, source commits, dependencies, and actual feed content. Verify Studio npm artifacts and `latest`/`next` according to release kind.
- Bind exact source commits and reviewed manifests/notes before publication. Preserve existing tags; matching releases are reusable, conflicting releases require investigation.
- Resume by inspecting live GitHub state and recorded package/post evidence. Do not recreate tags or repost after an uncertain result. Retain run and message IDs; wait on concrete jobs with bounded polling and backoff.
- Assess advisory findings during preflight. Record severity, relevant usage, and disposition. Existing warnings are not automatically blockers or automatically accepted forever. Never insert an unvalidated dependency upgrade into a release to suppress a warning; a new material unresolved risk requires a concrete scope decision.
- After package verification, invoke [Elsa Release Announcements](../elsa-release-announcements/SKILL.md). Default to publishing now on Discord, LinkedIn, and X. Draft-only is an explicit override, not completion of a request to announce.
- Complete only after every selected release and required feed is verified and the requested announcements are verified sent (and Discord crossposted). State limitations accurately. Stop any follow-up monitor when finished.

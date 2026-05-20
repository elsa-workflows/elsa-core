---
name: elsa-release
description: Release Elsa repositories from GitHub tags. Use when Codex needs to create a preview or stable GitHub release for Elsa Core, Elsa Studio, Elsa Extensions, or any similarly configured Elsa repository where releases are driven by Git tags and GitHub release events; supports curated release notes, retagging an existing RC tag as a stable tag, publishing prereleases without NuGet, publishing stable releases that trigger NuGet, and sequencing downstream Elsa repository releases after packages are available.
---

# Elsa Release

## Overview

Use this skill to release an Elsa repository by preparing curated release notes, creating or reusing a Git tag, creating the matching GitHub release, and letting the repository's GitHub Actions pipeline build and publish packages. Stable releases are normal GitHub releases; preview releases are GitHub prereleases.

The bundled helper `scripts/release.py` performs the repeatable release checks and prints the exact Git/GitHub commands before execution. It defaults to dry-run mode. The bundled helper `scripts/release_notes.py` collects commits into a categorized Markdown scaffold for curated release notes.

## Inputs

Collect or infer:

- Repository path or GitHub repo, e.g. `elsa-workflows/elsa-core`.
- Desired release tag, e.g. `3.7.0`.
- Release kind: `stable` or `preview`.
- Source ref, if the desired tag should point at a specific existing ref. For stable-from-RC releases, use the RC tag, e.g. `3.7.0-rc1`.
- Release notes range: previous release tag/ref to desired release tag/ref. For stable releases, compare against the previous stable tag unless the user requests another range.
- Release notes strategy: curated notes by default; generated GitHub notes only when the user explicitly wants the quick path or there is not enough time/context.

If the user asks for "stable 3.7 from RC1", interpret that as:

```bash
python3 .agents/skills/elsa-release/scripts/release.py \
  --repo-path /path/to/repo \
  --source-ref 3.7.0-rc1 \
  --tag 3.7.0 \
  --release-kind stable
```

## Release Workflow

1. Inspect the repository's release workflow before acting.
   - Confirm it has a `release` trigger.
   - Confirm stable vs preview behavior. In Elsa Core, `.github/workflows/packages.yml` publishes to feedz.io for release events and publishes to nuget.org only when `github.event.action == 'published'`.
   - Treat draft releases carefully because draft publication can change release event behavior.

2. Prepare the dry run.
   - Run the helper without `--execute`.
   - Confirm the source commit, destination tag, remote repository, containing remote branches, and release command.
   - For Elsa-style pipelines, the source commit should be reachable from `origin/main` or an `origin/release/*` branch unless the user explicitly accepts the risk.

3. Prepare curated release notes.
   - Generate a scaffold with `scripts/release_notes.py`.
   - Rewrite the scaffold into polished, developer-facing release notes before publishing.
   - Store Elsa Core release notes under `doc/changelogs/<version>.md` when working inside this repository.
   - Pass the curated notes to `scripts/release.py` with `--notes-file`.

4. Ask for explicit confirmation before live operations.
   - Pushing a tag and publishing a GitHub release are production release actions.
   - Show the exact source ref, resolved commit, desired tag, release kind, and target GitHub repository.
   - Show the release notes file path and compare range.
   - Do not use `--execute` until the user confirms.

5. Execute the release.
   - Re-run the same command with `--execute`.
   - The helper creates an annotated tag if needed, pushes it, then runs `gh release create`.
   - Stable releases are created without `--prerelease` and with `--latest`.
   - Preview releases are created with `--prerelease` and `--latest=false`.

6. Verify GitHub.
   - Check `gh release view <tag> --repo <owner/repo>`.
   - Check the repository's Actions tab or `gh run list --repo <owner/repo> --workflow <workflow> --limit 5`.
   - Confirm the pipeline started from the `release` event and is using the expected version tag.

7. Sequence Elsa repositories.
   - Release Elsa Core first.
   - Wait until packages are available in NuGet and/or feedz.io according to the release kind.
   - Update Elsa Studio and Elsa Extensions to consume the newly published Elsa Core package versions using their repository-specific dependency update process.
   - Release Elsa Studio and Elsa Extensions with the same skill when they use the same tag-and-GitHub-release pattern.

## Curated Release Notes

Recommendation: use curated release notes for stable releases and meaningful previews. GitHub generated notes are useful raw input, but the final release should explain why changes matter to developers consuming Elsa packages.

Use this structure:

```markdown
Compare: <from-ref>...<to-ref>

---

## 🌟 Highlights

---

## ⚠️ Breaking changes / upgrade notes

---

## ✨ New features

### Component or theme

---

## 🔧 Improvements

---

## 🐛 Fixes

---

## 🔒 Security

---

## 🧩 Developer-facing changes

---

## 🧪 Tests

---

## 🔁 CI / Build

---

## 📦 Dependencies

---

## 📦 Full changelog (short)
```

Omit empty sections. Put the highest-signal user-facing changes in `Highlights` first, limited to 3-6 bullets. Keep `Full changelog` comprehensive so every commit or PR in the range is represented somewhere.

Writing rules:

- Prefer PR titles and labels when available; otherwise use commit subjects.
- Do not paste a flat generated changelog as the final result.
- Group related changes under component-oriented subsection headings when that improves scanning, e.g. `#### Workflows`, `#### Shells`, `#### HTTP`, `#### Persistence`.
- Follow the Elsa `3.7.0-rc1` style: compare line first, `---` separators, `##` category headings with small icons, component prefix before the colon, and a short full changelog at the end.
- For breaking changes, include who is affected and what to do.
- For fixes, explain the observable problem that was corrected, not only the implementation detail.
- For dependency/package changes, include package names and versions when available.
- End bullets with a PR number or short SHA when available, e.g. `(#7400)` or `(b88af1e02)`.
- Never invent PR numbers, affected components, migration steps, or known issues.

Generate a scaffold:

```bash
python3 .agents/skills/elsa-release/scripts/release_notes.py \
  --repo-path . \
  --from-ref 3.6.2 \
  --to-ref 3.7.0 \
  --version 3.7.0 \
  --output doc/changelogs/3.7.0.md
```

Then edit the scaffold into polished notes and release with:

```bash
python3 .agents/skills/elsa-release/scripts/release.py \
  --repo-path . \
  --source-ref origin/release/3.7.0 \
  --tag 3.7.0 \
  --release-kind stable \
  --notes-file doc/changelogs/3.7.0.md
```

If the GitHub release already exists and only the notes need improvement, update it with:

```bash
gh release edit 3.7.0 \
  --repo elsa-workflows/elsa-core \
  --notes-file doc/changelogs/3.7.0.md
```

## Helper Usage

Dry run a stable release by retagging an RC:

```bash
python3 .agents/skills/elsa-release/scripts/release.py \
  --repo-path . \
  --source-ref 3.7.0-rc1 \
  --tag 3.7.0 \
  --release-kind stable
```

Execute after confirmation:

```bash
python3 .agents/skills/elsa-release/scripts/release.py \
  --repo-path . \
  --source-ref 3.7.0-rc1 \
  --tag 3.7.0 \
  --release-kind stable \
  --execute
```

Dry run a preview release from the current commit:

```bash
python3 .agents/skills/elsa-release/scripts/release.py \
  --repo-path . \
  --tag 3.8.0-preview2 \
  --release-kind preview
```

Use `--github-repo owner/name` when the local remote is ambiguous. Use `--notes-file path/to/notes.md` to publish supplied release notes instead of generated notes. Use `--notes-start-tag <tag>` to control GitHub's generated-notes comparison range.

## Guardrails

- Never move, delete, or force-update an existing release tag unless the user explicitly requests that exact destructive operation.
- Never publish a stable release when the user asked for preview.
- Do not create a draft release for the normal automated pipeline unless the repository workflow has been reviewed and the user explicitly wants a draft.
- Keep release notes and release titles factual. Prefer the exact tag as the GitHub release title unless the repository has a different convention.
- Do not publish curated notes without reviewing the compare range and confirming all included commits belong in the release.
- If `gh` is unauthenticated, stop and ask the user to authenticate; do not try to handle credentials.
- If the pipeline semantics are unclear, inspect the workflow or ask before publishing.

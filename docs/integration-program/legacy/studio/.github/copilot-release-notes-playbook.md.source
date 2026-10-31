# Copilot Playbook: Elsa Studio Release Notes (GitHub Release + PR/commit links)

## Goal
Generate consistent GitHub Release notes for `elsa-workflows/elsa-studio` between two tags/refs, including:
- a short **Highlights** section up top
- standard grouped sections (Breaking/Features/Improvements/Fixes/Tests/CI)
- **GitHub-style** PR/commit references at the end of bullets: `(#1234)` or `(abcd123)`
- a short **Full changelog** section (one line per commit/PR)
- optional **Known issues** section when applicable

## Inputs to provide (required)
- Repository: `elsa-workflows/elsa-studio`
- From tag/ref: `<FROM>`
- To tag/ref: `<TO>`

Also provide ONE of:
- `git log --oneline <FROM>..<TO>` output (preferred), OR
- compare URL: `https://github.com/elsa-workflows/elsa-studio/compare/<FROM>...<TO>`

Optionally provide:
- whether merges are squash (often include `(#NNNN)` in subject) vs merge commits
- any release-specific callouts you want forced into Highlights / Known issues

## Output format (required)
- Return a single Markdown file using a four-backtick fenced block:
  - filename: `release-notes-<TO>.md` (or `release-notes-<version>.md`)
- Use this section order and headings exactly:

1. Title and compare range
2. `### 🌟 Highlights`
3. `### ⚠️ Breaking changes / upgrade notes`
4. `### ✨ New features`
5. `### 🔧 Improvements`
6. `### 🐛 Fixes`
7. `### 🧩 Developer-facing changes` (omit if none)
8. `### 🧪 Tests` (omit if none)
9. `### 🔁 CI / Build` (omit if none)
10. `### 🧭 Known issues` (omit if none provided/known)
11. `### 📦 Full changelog (short)`

## Link style rules
- Prefer PR number if available: end bullet with `(#NNNN)`
- If no PR association: end bullet with `(abcd123)` short SHA (7–12 chars)
- If multiple relevant PRs/commits: include multiple suffixes, e.g. `(#7172) (abcd123)`

Never invent PR numbers. Only use what is present in commit subjects, merge commits, or explicitly provided.

## PR title vs commit subject convention (new)
- If a PR number is available, prefer **PR title wording** over commit subject wording **when you have the PR title**.
- If PR titles are not available (e.g., only `git log` output), use commit subjects as-is.
- If you *do* have PR titles and they differ significantly from commit subjects:
  - use the PR title in the release note bullet
  - keep “Full changelog (short)” as the raw commit subjects (or PR titles if you have them consistently)

(If you want PR-title-first release notes, provide a list of PR URLs/titles or enable an API-derived PR list in the session.)

## Highlights convention (new)
- 3–6 bullets max
- Must be user-facing and high-signal:
  - new capabilities
  - resilience / reliability improvements
  - important defaults/behavior changes
- Each highlight bullet should still end with `(#NNNN)` / `(sha)`.

## Known issues convention (new)
Only include if the user provides known issues or you can clearly infer them from the provided material (avoid guessing).
Each entry should include:
- symptom (what breaks)
- workaround (if known)
- reference suffix `(#NNNN)` / `(sha)` if applicable

If none are provided, omit the section entirely.

## How to build the content (process)
1. Parse the provided commit list into an ordered changelog.
2. Extract PR numbers from subjects:
   - `(#7174)` at end of subject
   - `Merge pull request #7157 ...`
3. Group changes into sections:
   - Breaking: API changes, package swaps, SDK/toolchain, serialization model changes
   - Features: new APIs/modules/capabilities
   - Improvements: performance, resilience, refactors that improve behavior
   - Fixes: correctness, bugs
   - Developer-facing: attributes, extension points, new hooks/contracts
   - Tests: new test projects, new coverage, determinism fixes
   - CI/Build: workflow changes, branch triggers, packaging/versioning
4. Produce concise bullets:
   - Start with an action verb (“Added”, “Introduced”, “Fixed”, “Improved”, “Updated”, “Removed”)
   - One idea per bullet
   - Add details as sub-bullets only when necessary (max 2 levels)
   - Append PR/commit suffix at end
5. Generate **Highlights** by selecting the top 3–6 bullets across all sections (no duplicates).
6. Add **Full changelog (short)**:
   - include every commit line from the provided input range, in the same order
   - each line should include PR number if present; otherwise SHA

## Consistency checks before finalizing
- Compare range line present: `Compare: <FROM>...<TO>`
- Highlights present (3–6 bullets)
- Every bullet has suffix `(#NNNN)` or `(sha)` where available
- No PR numbers are guessed
- Full changelog includes all commits provided
- Optional sections (Developer-facing / Tests / CI / Known issues) are omitted if empty

## Example prompt for a new session
“Generate GitHub Release notes for `elsa-workflows/elsa-studio` from `<FROM>` to `<TO>`. Use the template with **Highlights** and **Known issues** (only if I provide them). Prefer PR titles when available, otherwise use commit subjects. Use GitHub-style `(#NNNN)` references and include a short Full changelog. Here is `git log --oneline <FROM>..<TO>`: …”
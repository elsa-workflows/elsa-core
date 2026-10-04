# Advisory PR reviewers

Live list of automated reviewers for this repository (elsa-extensions). Agents read it before opening a PR.

Last verified: 2026-10-04

| Reviewer | Status | How to request | Notes |
| --- | --- | --- | --- |
| GitHub Copilot code review (`copilot-pull-request-reviewer[bot]`) | live | Add reviewer `@copilot`: `gh pr edit <n> --add-reviewer @copilot` (GitHub CLI 2.88 or later) | Advisory. A `@copilot review` comment does not trigger it. Bot-authored PRs need the org policy that lets Copilot review them. |
| Greptile (`greptile-apps[bot]`) | not live | Comment `@greptileai` (once enabled here) | Advisory; not part of the merge gate (see Rules). Installed, but does not review PRs here. |
| CodeRabbit (`coderabbitai[bot]`) | not live | Comment `@coderabbitai review` (once enabled here) | Not enabled for this repository. |
| Cursor Bugbot | not live | Top-level PR comment `cursor review` (once enabled) | Must first be enabled in the Cursor dashboard. `cursor[bot]` comments on PRs come from Cursor cloud agents, not Bugbot. |
| GitHub Code Quality (`github-code-quality[bot]`) | informational | Automatic; cannot be requested | CodeQL code-quality comments. Not an advisory pick and not part of the merge gate. |

## Rules

- Greptile does not review PRs on this repository. Do not wait for or request a Greptile score.
- Pick exactly one live advisory reviewer from the table (currently Copilot) and request it once the PR is open. Do not request a second reviewer.
- If the picked reviewer declines or skips the PR, request another live reviewer if there is one; otherwise continue without one.
- The PR author (human or agent) never reviews or approves its own PR.
- Merge gate: the only merge gate is an Elsa 3 Code Review on the PR whose body starts with the line `Elsa 3 Code Review: APPROVE + HIGH @ <head sha>` (the full 40-character SHA of the PR's current head commit), plus green CI on that head. The Code Review is posted as `sfmskywalker` with review event COMMENT, so its GitHub review state is COMMENTED, not APPROVED. A `REQUEST_CHANGES` verdict, a confidence below `HIGH`, or a SHA other than the current head does not pass. The Code Review is a separate reviewer from the PR author, even when both post from the same account.
- Pin the merge to the approved SHA: `gh pr merge <n> --match-head-commit <sha>`, or the merge API (`PUT /repos/{owner}/{repo}/pulls/{n}/merge`) with `sha=<sha>`. Any push after the approval voids it; Code Review must re-confirm with `Elsa 3 Code Review: APPROVE + HIGH @ <new head sha>` before merging.
- Greptile, CodeRabbit, Copilot and Bugbot are advisory only. Their findings feed into the Code Review, but none of them is required for merge: there is no Greptile score gate (no 5/5 requirement) and no waive process.
- Update this file whenever a reviewer is added, removed, or changes how it is requested.

# Advisory PR reviewers

Live list of automated reviewers for this repository (elsa-studio). Agents read it before opening a PR.

Last verified: 2026-10-04

| Reviewer | Status | How to request | Notes |
| --- | --- | --- | --- |
| Greptile (`greptile-apps[bot]`) | live | Automatic when a PR is opened; comment `@greptileai` to review a new head after a push | Advisory; not part of the merge gate (see Rules). Posts a "Confidence Score: N/5" summary and a "Greptile Review" check. Reviews only allow-listed authors; others get "PR author is not in the allowed authors list". |
| CodeRabbit (`coderabbitai[bot]`) | live | Automatic on PRs into `main`; comment `@coderabbitai review` for other base branches or a re-review after a push | Advisory. Skips PRs into other base branches unless requested. Reviews draw on an hourly allowance, so do not request it while its automatic review is pending. |
| GitHub Copilot code review (`copilot-pull-request-reviewer[bot]`) | live | Add reviewer `@copilot`: `gh pr edit <n> --add-reviewer @copilot` (GitHub CLI 2.88 or later) | Advisory. A `@copilot review` comment does not trigger it. Bot-authored PRs need the org policy that lets Copilot review them. |
| Cursor Bugbot | not live | Top-level PR comment `cursor review` (once enabled) | Must first be enabled in the Cursor dashboard. `cursor[bot]` comments on PRs come from Cursor cloud agents, not Bugbot. |

## Rules

- Greptile reviews automatically when a PR is opened. It is advisory: after a push you may comment `@greptileai` for a fresh review of the new head, but nobody waits for a Greptile score.
- Besides Greptile, request at most one additional advisory reviewer (CodeRabbit or Copilot) once the PR is open. If your pick reviews automatically on this PR (CodeRabbit does on PRs into `main`), wait for that run instead of requesting it; request it manually only if no run appears, the PR targets another base branch, or you need a re-review after a push.
- If the additional reviewer declines or skips the PR, you may request the other one instead.
- The PR author (human or agent) never reviews or approves its own PR.
- Merge gate: the only merge gate is an Elsa 3 Code Review on the PR whose body starts with the line `Elsa 3 Code Review: APPROVE + HIGH @ <head sha>` (the full 40-character SHA of the PR's current head commit), plus green CI on that head. The Code Review is posted as `sfmskywalker` with review event COMMENT, so its GitHub review state is COMMENTED, not APPROVED. A `REQUEST_CHANGES` verdict, a confidence below `HIGH`, or a SHA other than the current head does not pass. The Code Review is a separate reviewer from the PR author, even when both post from the same account.
- Pin the merge to the approved SHA: `gh pr merge <n> --match-head-commit <sha>`, or the merge API (`PUT /repos/{owner}/{repo}/pulls/{n}/merge`) with `sha=<sha>`. Any push after the approval voids it; Code Review must re-confirm with `Elsa 3 Code Review: APPROVE + HIGH @ <new head sha>` before merging.
- Greptile, CodeRabbit, Copilot and Bugbot are advisory only. Their findings feed into the Code Review, but none of them is required for merge: there is no Greptile score gate (no 5/5 requirement) and no waive process.
- Update this file whenever a reviewer is added, removed, or changes how it is requested.

# Advisory PR reviewers

Live list of automated reviewers for this repository (elsa-core). Agents read it before opening a PR.

Last verified: 2026-10-04

| Reviewer | Status | How to request | Notes |
| --- | --- | --- | --- |
| Greptile (`greptile-apps[bot]`) | live | Automatic when a PR is opened; comment `@greptileai` to review a new head after a push | Advisory; not part of the merge gate (see Rules). Posts a "Confidence Score: N/5" summary and a "Greptile Review" check. Reviews only allow-listed authors; others get "PR author is not in the allowed authors list". |
| CodeRabbit (`coderabbitai[bot]`) | live | Automatic on PRs into `main`; comment `@coderabbitai review` for other base branches or a re-review after a push | Advisory. Skips PRs into other base branches unless requested. Reviews draw on an hourly allowance, so do not request it while its automatic review is pending. |
| GitHub Copilot code review (`copilot-pull-request-reviewer[bot]`) | live | Add reviewer `@copilot`: `gh pr edit <n> --add-reviewer @copilot` (GitHub CLI 2.88 or later) | Advisory. A `@copilot review` comment does not trigger it. Bot-authored PRs need the org policy that lets Copilot review them. |
| Cursor Bugbot (`cursor[bot]`) | live (elsa-core only) | Mention-only: top-level PR comment `cursor review` (also `@cursor review` or `bugbot run`); comment again after a push to review the new head | Advisory. Enabled on elsa-core only, not on elsa-studio or elsa-extensions. Posts a review whose body starts with `<!-- BUGBOT_REVIEW -->` and names the reviewed commit; first verified on #8598 (review landed about 3 minutes after the comment). Cloud agents also comment as `cursor[bot]`, so identify Bugbot by that review marker. |
| GitHub Code Quality (`github-code-quality[bot]`) | informational | Automatic; cannot be requested | CodeQL code-quality comments. Not an advisory pick and not part of the merge gate. |

## Rules

- Greptile reviews automatically when a PR is opened. It is advisory: after a push you may comment `@greptileai` for a fresh review of the new head, but nobody waits for a Greptile score.
- Besides Greptile, request at most one additional advisory reviewer (CodeRabbit, Copilot, or Bugbot on elsa-core) once the PR is open. If your pick reviews automatically on this PR (CodeRabbit does on PRs into `main`), wait for that run instead of requesting it; request it manually only if no run appears, the PR targets another base branch, or you need a re-review after a push.
- If the additional reviewer declines or skips the PR, you may request another eligible reviewer instead.
- The PR author (human or agent) never reviews or approves its own PR.
- Merge gate: the only merge gate is an Elsa 3 Code Review on the PR whose body starts with the line `Elsa 3 Code Review: APPROVE + HIGH @ <head sha>` (the full 40-character SHA of the PR's current head commit), plus green CI on that head. The Code Review is posted as `sfmskywalker` with review event COMMENT, so its GitHub review state is COMMENTED, not APPROVED. A `REQUEST_CHANGES` verdict, a confidence below `HIGH`, or a SHA other than the current head does not pass. The Code Review is a separate reviewer from the PR author, even when both post from the same account.
- Pin the merge to the approved SHA: `gh pr merge <n> --match-head-commit <sha>`, or the merge API (`PUT /repos/{owner}/{repo}/pulls/{n}/merge`) with `sha=<sha>`. Any push after the approval voids it; Code Review must re-confirm with `Elsa 3 Code Review: APPROVE + HIGH @ <new head sha>` before merging.
- Greptile, CodeRabbit, Copilot and Bugbot are advisory only. Their findings feed into the Code Review, but none of them is required for merge: there is no Greptile score gate (no 5/5 requirement) and no waive process.
- Update this file whenever a reviewer is added, removed, or changes how it is requested.

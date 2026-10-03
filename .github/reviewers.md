# Advisory PR reviewers

Live list of automated reviewers for this repository (elsa-studio). Agents read it before opening a PR.

Last verified: 2026-10-03

| Reviewer | Status | How to request | Notes |
| --- | --- | --- | --- |
| Greptile (`greptile-apps[bot]`) | live | Automatic when a PR is opened; comment `@greptileai` after every push | **Required for merge** on this repository (see Rules). Posts a "Confidence Score: N/5" summary and a "Greptile Review" check. Reviews only allow-listed authors; others get "PR author is not in the allowed authors list". |
| CodeRabbit (`coderabbitai[bot]`) | live | Automatic on PRs into `main`; comment `@coderabbitai review` for other base branches or a re-review after a push | Advisory. Skips PRs into other base branches unless requested. Reviews draw on an hourly allowance, so do not request it while its automatic review is pending. |
| GitHub Copilot code review (`copilot-pull-request-reviewer[bot]`) | live | Add reviewer `@copilot`: `gh pr edit <n> --add-reviewer @copilot` (GitHub CLI 2.88 or later) | Advisory. A `@copilot review` comment does not trigger it. Bot-authored PRs need the org policy that lets Copilot review them. |
| Cursor Bugbot | not live | Top-level PR comment `cursor review` (once enabled) | Must first be enabled in the Cursor dashboard. `cursor[bot]` comments on PRs come from Cursor cloud agents, not Bugbot. |

## Rules

- Greptile is a required reviewer on this repository. It reviews automatically when a PR is opened; after every push, comment `@greptileai` so it reviews the new head.
- Besides Greptile, request at most one additional advisory reviewer (CodeRabbit or Copilot) once the PR is open. If your pick reviews automatically on this PR (CodeRabbit does on PRs into `main`), wait for that run instead of requesting it; request it manually only if no run appears, the PR targets another base branch, or you need a re-review after a push.
- If the additional reviewer declines or skips the PR, you may request the other one instead.
- The PR author (human or agent) never reviews or approves its own PR.
- Merge gate: an Elsa 3 Code Review `APPROVE + HIGH @ <head sha>` (the full 40-character SHA of the PR's current head commit), green CI, and Greptile 5/5 on that same head. Any push after the approval needs a re-confirm on the new head.
- Exception: if Greptile cannot review the head (unavailable, skipped, declined, or out of credits), `APPROVE + HIGH @ <head sha>` plus green CI is enough, and the Code Review must state that Greptile was required and unavailable.
- CodeRabbit and Copilot reviews are advisory and never replace the merge gate.
- Update this file whenever a reviewer is added, removed, or changes how it is requested.

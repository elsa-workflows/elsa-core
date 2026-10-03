# Advisory PR reviewers

Live list of automated reviewers for this repository (elsa-studio). Agents read it before opening a PR.

Last verified: 2026-10-03

| Reviewer | Status | How to request | Notes |
| --- | --- | --- | --- |
| Greptile (`greptile-apps[bot]`) | live | Automatic when a PR is opened; comment `@greptileai` to re-request after new pushes | Posts a "Confidence Score: N/5" summary and a "Greptile Review" check (studio#1103, 2026-09-30). `@greptileai` re-reviews worked on studio#1071 and studio#1073 (2026-09-28). No out-of-credits messages. |
| GitHub Copilot code review (`copilot-pull-request-reviewer[bot]`) | live | Add reviewer `@copilot`: `gh pr edit <n> --add-reviewer @copilot` (gh 2.88+) | Last review studio#1041 (2026-09-14); every earlier request got a review. Bot-authored PRs need the org policy that lets Copilot review them (core#7907, 2026-08-03: "no eligible user to bill"). |
| CodeRabbit (`coderabbitai[bot]`) | live | Comment `@coderabbitai review` | Installed 2026-10-03. Auto-reviews PRs into `main` (first seen on studio#1104, 2026-10-03). Plan allowance: 10 included reviews per hour. With Greptile, PRs into `main` currently get two automatic reviews. |
| Cursor Bugbot | not live | Top-level PR comment `cursor review` (once enabled) | Must first be enabled in the Cursor dashboard. `cursor[bot]` comments on PRs come from Cursor cloud agents, not Bugbot. |

## Rules

- Pick exactly one live reviewer before opening the PR, and request it right after the PR is open, using the command in the table. Do not request a second reviewer.
- Automatic reviews (see Notes) run on their own and do not count against that limit. If the reviewer you picked already reviewed the current head automatically, that counts as the request; do not trigger it again.
- If the picked reviewer declines or skips the PR (for example "PR author is not in the allowed authors list" or "Review skipped"), request a different live reviewer instead.
- The PR author (human or agent) never reviews or approves its own PR.
- This review is advisory. It does not replace the merge gate: an Elsa 3 Code Review `APPROVE @ <head sha>` on the PR's current head commit, plus green CI. Any push after the approval needs a re-confirm.
- Update this file whenever a reviewer is added, removed, or runs out of credits.

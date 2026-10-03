# Advisory PR reviewers

Live list of automated reviewers for this repository (elsa-extensions). Agents read it before opening a PR.

Last verified: 2026-10-03

| Reviewer | Status | How to request | Notes |
| --- | --- | --- | --- |
| GitHub Copilot code review (`copilot-pull-request-reviewer[bot]`) | live | Add reviewer `@copilot`: `gh pr edit <n> --add-reviewer @copilot` (gh 2.88+) | Last reviews ext#194 and ext#193 (2026-09-13); every earlier request got a review; reviewed again on ext#272 (2026-10-03). Bot-authored PRs need the org policy that lets Copilot review them (core#7907, 2026-08-03: "no eligible user to bill"). |
| Greptile (`greptile-apps[bot]`) | not live | Comment `@greptileai` (once enabled here) | Installed but not reviewing: no review and no "Greptile Review" check on ext#258, #259, #266, #267 (2026-09-28) or ext#271 (2026-10-02). Other authors get "PR author is not in the allowed authors list" (ext#199, 2026-09-14). Last real review ext#123 (2026-02-11). |
| CodeRabbit (`coderabbitai[bot]`) | not live | Comment `@coderabbitai review` (once enabled here) | No CodeRabbit activity on this repository, including ext#272 (2026-10-03). Its org installation (2026-10-03) covers selected repositories: elsa-core and elsa-studio. |
| Cursor Bugbot | not live | Top-level PR comment `cursor review` (once enabled) | Must first be enabled in the Cursor dashboard. `cursor[bot]` comments on PRs come from Cursor cloud agents, not Bugbot. |
| GitHub Code Quality (`github-code-quality[bot]`) | informational | Automatic; cannot be requested | CodeQL code-quality comments (ext#267, 2026-09-28). Does not count as the advisory reviewer. |

## Rules

- Pick exactly one live reviewer before opening the PR, and request it right after the PR is open, using the command in the table. Do not request a second reviewer.
- Automatic reviews (see Notes) run on their own and do not count against that limit. If the reviewer you picked already reviewed the current head automatically, that counts as the request; do not trigger it again.
- If the picked reviewer declines or skips the PR (for example "PR author is not in the allowed authors list" or "Review skipped"), request a different live reviewer instead.
- The PR author (human or agent) never reviews or approves its own PR.
- This review is advisory. It does not replace the merge gate: an Elsa 3 Code Review `APPROVE @ <head sha>` on the PR's current head commit, plus green CI. Any push after the approval needs a re-confirm.
- Update this file whenever a reviewer is added, removed, or runs out of credits.

# Advisory PR reviewers

Live list of automated reviewers for this repository (elsa-extensions). Agents read it before opening a PR.

Last verified: 2026-10-03

| Reviewer | Status | How to request | Notes |
| --- | --- | --- | --- |
| GitHub Copilot code review (`copilot-pull-request-reviewer[bot]`) | live | Add reviewer `@copilot`: `gh pr edit <n> --add-reviewer @copilot` (gh 2.88+) | Last reviews ext#194 and ext#193 (2026-09-13); every earlier request got a review. Cannot review bot-authored PRs. |
| Greptile (`greptile-apps[bot]`) | not live | Comment `@greptileai` (once enabled here) | Installed but not reviewing: no review and no "Greptile Review" check on ext#258, #259, #266, #267 (2026-09-28) or ext#271 (2026-10-02). Other authors get "PR author is not in the allowed authors list" (ext#199, 2026-09-14). Last real review ext#123 (2026-02-11). |
| CodeRabbit (`coderabbitai[bot]`) | not live | Comment `@coderabbitai review` (once enabled here) | No CodeRabbit activity on this repository. Its org installation (2026-10-03) covers selected repositories, and it has only been seen on elsa-core. |
| Cursor Bugbot | not live | Top-level PR comment `cursor review` (once enabled) | Must first be enabled in the Cursor dashboard. `cursor[bot]` comments on PRs come from Cursor cloud agents, not Bugbot. |
| GitHub Code Quality (`github-code-quality[bot]`) | informational | Automatic; cannot be requested | CodeQL code-quality comments (ext#267, 2026-09-28). Does not count as the advisory reviewer. |

## Rules

- Pick exactly one live reviewer per PR and request it with the command in the table. A reviewer that already reviewed the PR automatically when it was opened counts as that one request; do not add a second reviewer.
- The PR author (human or agent) never reviews or approves its own PR.
- This review is advisory. It does not replace the merge gate: an Elsa 3 Code Review `APPROVE @ <head sha>` on the PR's current head commit, plus green CI. Any push after the approval needs a re-confirm.
- Update this file whenever a reviewer is added, removed, or runs out of credits.

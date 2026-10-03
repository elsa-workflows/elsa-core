# Advisory PR reviewers

Live list of automated reviewers for this repository (elsa-studio). Agents read it before opening a PR.

Last verified: 2026-10-03

| Reviewer | Status | How to request | Notes |
| --- | --- | --- | --- |
| Greptile (`greptile-apps[bot]`) | live | Automatic when a PR is opened; comment `@greptileai` to re-request after new pushes | Posts a "Confidence Score: N/5" summary and a "Greptile Review" check (studio#1103, 2026-09-30). `@greptileai` re-reviews worked on studio#1071 and studio#1073 (2026-09-28). No out-of-credits messages. |
| GitHub Copilot code review (`copilot-pull-request-reviewer[bot]`) | live | Add reviewer `@copilot`: `gh pr edit <n> --add-reviewer @copilot` (gh 2.88+) | Last review studio#1041 (2026-09-14); every earlier request got a review. Cannot review bot-authored PRs. |
| CodeRabbit (`coderabbitai[bot]`) | not live | Comment `@coderabbitai review` (once enabled here) | No CodeRabbit activity on this repository. Its org installation (2026-10-03) covers selected repositories, and it has only been seen on elsa-core. |
| Cursor Bugbot | not live | Top-level PR comment `cursor review` (once enabled) | Must first be enabled in the Cursor dashboard. `cursor[bot]` comments on PRs come from Cursor cloud agents, not Bugbot. |

## Rules

- Pick exactly one live reviewer per PR and request it with the command in the table. A reviewer that already reviewed the PR automatically when it was opened counts as that one request; do not add a second reviewer.
- The PR author (human or agent) never reviews or approves its own PR.
- This review is advisory. It does not replace the merge gate: an Elsa 3 Code Review `APPROVE @ <head sha>` on the PR's current head commit, plus green CI. Any push after the approval needs a re-confirm.
- Update this file whenever a reviewer is added, removed, or runs out of credits.

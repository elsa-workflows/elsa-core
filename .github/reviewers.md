# Advisory PR reviewers

Live list of automated reviewers for this repository (elsa-extensions). Agents read it before opening a PR.

Last verified: 2026-10-03

| Reviewer | Status | How to request | Notes |
| --- | --- | --- | --- |
| GitHub Copilot code review (`copilot-pull-request-reviewer[bot]`) | live | Add reviewer `@copilot`: `gh pr edit <n> --add-reviewer @copilot` (GitHub CLI 2.88 or later) | Advisory. A `@copilot review` comment does not trigger it. Bot-authored PRs need the org policy that lets Copilot review them. |
| Greptile (`greptile-apps[bot]`) | not live (waived) | Comment `@greptileai` (once enabled here) | **Waived on this repository**: not required for merge, so nobody waits for a Greptile score. Installed, but does not review PRs here. |
| CodeRabbit (`coderabbitai[bot]`) | not live | Comment `@coderabbitai review` (once enabled here) | Not enabled for this repository. |
| Cursor Bugbot | not live | Top-level PR comment `cursor review` (once enabled) | Must first be enabled in the Cursor dashboard. `cursor[bot]` comments on PRs come from Cursor cloud agents, not Bugbot. |
| GitHub Code Quality (`github-code-quality[bot]`) | informational | Automatic; cannot be requested | CodeQL code-quality comments. Not an advisory pick and not part of the merge gate. |

## Rules

- Greptile is waived on this repository. Do not wait for or request a Greptile score.
- Pick exactly one live advisory reviewer from the table (currently Copilot) and request it once the PR is open. Do not request a second reviewer.
- If the picked reviewer declines or skips the PR, request another live reviewer if there is one; otherwise continue without one.
- The PR author (human or agent) never reviews or approves its own PR.
- Merge gate: an Elsa 3 Code Review `APPROVE + HIGH @ <head sha>` (the full 40-character SHA of the PR's current head commit), plus green CI. Any push after the approval needs a re-confirm on the new head.
- Advisory reviews never replace the merge gate.
- Update this file whenever a reviewer is added, removed, or changes how it is requested.

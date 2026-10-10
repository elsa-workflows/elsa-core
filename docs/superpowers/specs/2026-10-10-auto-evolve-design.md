# Elsa auto-evolve — design

Status: draft for review · 2026-10-10 · owner: Sipke Schoorstra

## 1. Goal

A set of Claude cloud routines that keep Elsa 3 moving without a human in the loop:
1. Triage every open issue.
2. Assign each issue to a release milestone (`3.10`, then `3.11`, …) or close it with evidence.
3. Implement agent-ready issues.
4. Decide when a milestone is ready to ship.
5. Cut and publish the release: packages and container images.

You keep a veto at every release step. Nothing ships without your explicit approval.

**Success criteria:**
- Every open issue in elsa-core has a milestone or is closed with an evidence comment within 4 weeks of Triage going live.
- 3.10.0 ships in shadow mode: the routines propose, you cut. Every later release (3.10.x, 3.11.0, …) ships through the routines. No step needs you unless you use the veto.
- No release goes out with an open `release-blocker` or `severity:critical/high` bug in its milestone.
- No two routines ever write the same concern at the same time.

## 2. Decisions

| # | Decision | Rationale |
|---|---|---|
| D1 | **Migrate elsa-apps into elsa-core** as `apps/`, replacing `docker/`. The elsa-apps repo stays for ≤3.9 image maintenance, then is archived. | See §3. |
| D2 | **Transfer open Studio and Extensions issues to elsa-core** (`gh issue transfer`), except 3.8/3.9-only maintenance. | One repo to triage. Matches the "Contribution routing after consolidation" section in `CONTRIBUTING.md`. |
| D3 | **Claude cloud routines; GitHub is the only state store.** | Routines run while the Mac sleeps. Cloud runs have no local disk, so ledgers in `~/.claude/issue-runs` or Codex `state.json` are not usable. |
| D4 | **Fully autonomous release, protected by machine gates and a veto window.** | Replaces today's human approvals (`.github/reviewers.md`, environment required reviewers). |
| D5 | **Hybrid cadence for minors:** no sooner than 3 weeks after the previous minor, no later than 8 weeks. Inside that window, cut when the value score passes its threshold and the hard gates hold. | Avoids both trickle releases and stalled trains. |
| D6 | **Patch releases only for the latest minor.** | Keeps backport cost bounded. 3.8/3.9 lines stay manual in their source repos. |
| D7 | **Backlog pruning closes only with evidence.** Anything ambiguous goes to `Backlog`. | 766 issues have no milestone and about 350 predate 2024. Silent closure would damage community trust. |
| D8 | **3.10.0 is a shadow release.** | It is also the publisher cutover, which the lockstep ADR calls a manual maintainer step. |

## 3. elsa-apps migration (D1)

**Why:**
- **Lockstep collapses its version axes.** From 3.10, the three independent version knobs in elsa-apps (`ElsaVersion`, `ElsaStudioVersion`, `ElsaExtensionsVersion`) describe a single version.
- **One release run replaces a cross-repo chain.** Today that chain is: Dependabot PR against Feedz → manual `workflow_dispatch` with three version inputs → a receipt read back by `elsa-release`.
- **Images are built from the exact nupkgs being released,** through a local feed, not from whatever the feed resolves.
- **The hosts become PR-time integration tests** of the public package surface.
- **Duplication goes away.** `docker/` has four Dockerfiles that are never pushed, and the README references stale images.

**Shape:**
- `apps/src/*` holds the six hosts plus `Shared` and `Elsa.Server.Shared`. They use ProjectReferences through the existing `UseProjectReferences` switch.
- `scripts/containers/container_release.py` and its tests are moved verbatim.
- `.github/workflows/container-images.yml` runs in two places:
  - **On PRs:** only when `apps/**` or a referenced path changes, built for amd64 only.
  - **On release:** called by the release workflow with the packed `elsa-nuget-packages` artifact as a local feed, built multi-arch.
- Docker Hub credentials live in a `docker-hub` environment.
- The verified-correction workflow is ported, so an image-only fix can be re-promoted from `release/<minor>` without a package bump. Immutable `<version>-sha-<commit>` tags are preserved.
- `docker-ca.yml`'s TLS/CA smoke and the Datadog variant are ported. After that, `docker/` is deleted.
- The following are updated: `scripts/solution/solution-groups.json`, README image references, and the `container_release` target in `.agents/skills/elsa-release/references/elsa-profile.json`.
- An ADR records the decision next to `docs/adr/2026-10-08-product-directory-boundaries.md`.

**Precondition:** the open elsa-apps branches from 2026-10-08 (dashboard hosts, verified correction, API smoke compatibility) merge into elsa-apps first.

## 4. Routines

| Routine | Trigger | May write | Must not touch |
|---|---|---|---|
| **Triage** | every 6h, ≤30 issues per run, oldest untriaged first | labels, milestones, one triage comment per issue, closures with evidence | code, branches, tags, workflows |
| **Deliver** | every 2h, while no claim is active; one issue per run | branch, PR, merge behind the machine merge gate (§7) | milestone scope, releases |
| **Readiness** | daily | control-issue report, release proposals, honoring `/hold` | code, issue labels |
| **Release-train** | an accepted proposal whose veto window has expired | tags, `release/<minor>` branches, the version-bump PR, workflow dispatches, the GitHub release body | issue and PR bodies, except through the isolated notes step (§8) |

**Control issue.** One pinned issue titled "Auto-evolve control" acts as ledger, lock and veto channel.

**Locking.**
- A routine claims a concern by editing a fenced JSON block in the control issue's body. The block holds `{concern, routine, runId, expiresAt}`.
- After editing, the routine re-reads the body. If its claim is not the one present, it backs off.
- A claim expires after 3h.
- Every routine reads the control issue before any write.

**Reuse:**
- **Deliver** wraps the deliver-issue pipeline and the board-* agents.
- **Release-train** wraps `.agents/skills/elsa-release` (`runbook.md` and `release_train.py`) and the release-notes skill.

**Identity.** All routines act as a dedicated GitHub App installation with the narrowest permissions per routine. They never use your personal access token.

**Budget.**
- Each routine has a per-run token cap and a monthly ceiling.
- When a routine hits its ceiling, it pauses and posts to the control issue instead of failing silently.

**Paused Codex automations.** Retire `triage-elsa-3-9-milestone`, `deliver-elsa-core-3-9-milestone` and `deliver-elsa-3-9-across-core-studio-and-extensions` before any routine goes live.

## 5. Issue state model

**Milestones:**
- No milestone = untriaged. This is Triage's work queue.
- `3.10`, `3.11` = committed. At most two minor milestones are open: the current one and the next.
- `3.10.x` (for example `3.10.1`) = patch, latest minor only.
- `Backlog` = accepted but unscheduled.

**Labels.** Create the following and reuse equivalents where they already exist:
- `severity:critical|high|medium|low|unknown|n/a`. Exactly one per issue. It measures impact, not priority.
- `area:core|studio|extensions|apps|docs|ci`.
- `agent-ready`, `needs-info`, `needs-decision`, `release-blocker`, `backport:<minor>`, `breaking`. The existing `schema change` label is kept.
- `triaged` is removed after a one-off reconciliation pass.

**Assignment rules:**
- Compatible, well-scoped work goes to the current minor while it has fewer than 60 open issues. Overflow goes to the next minor.
- A `breaking` change goes to a minor and needs migration notes in the PR before merge.
- A `severity:critical/high` bug that exists on the latest release line also gets `backport:<latest>`.
- An unverified security report gets no public detail. Triage moves it to a private advisory and notifies you.

**Closure (D7).** Triage closes an issue only for one of these reasons, and always with a comment citing the evidence:
- fixed by a linked merged PR or commit;
- duplicate of a linked open issue;
- Elsa 2-only;
- `needs-info` with no reporter response for 6 months or more.

## 6. Readiness rubric

**Hard gates.** All must hold:
1. No open `release-blocker` or `severity:critical/high` bug in the milestone.
2. The last 3 `packages.yml` runs on `main` are green. Quarantined flaky tests are excluded, but each needs a linked issue.
3. The public API diff against the previous release has been computed, and every break is covered by a merged `breaking` PR with migration notes.
4. Every closed `schema change` issue has migrations for each persistence provider.
5. Images built from the candidate pass the container smoke suite on amd64 and arm64.
6. Templates and samples compile against the candidate.
7. No Deliver claim is active on an issue in the milestone.

**Window.** At least 3 weeks since the last minor. At 8 weeks, Readiness proposes a cut regardless of the value score. Unfinished issues move to the next minor.

**Value score.** A sum over the milestone's closed issues:

| Closed issue | Points |
|---|---|
| feature/enhancement | 3 |
| bug, severity high or above | 3 |
| bug, severity medium | 2 |
| bug, severity low | 1 |
| chore, docs, maintenance | 0.5 |

The initial threshold is 40. Phase 4 recalibrates it by backtesting against 3.8.0 and 3.9.0.

**Flow:**
1. **Proposal.** Readiness posts the proposal to the control issue (for example, "Cutting 3.11.0-rc.1 at <UTC time>") and sends you a notification.
2. **Veto window, 48h.** A `/hold` comment from you pauses the release until you comment `/resume`. Release-train also runs behind a GitHub environment with a 48h wait timer. That timer is the native backstop: cancelling the run is the veto.
3. **RC soak, 7 days.** If no new `severity:high+` issue is filed against the RC, Readiness proposes the stable release, with another 48h window.
4. **Patch releases.** A merged fix labeled `backport:<latest>` is cherry-picked to `release/<latest>`. Gates 1, 2 and 5 apply, with a 24h window and no RC.

**Rollback.** A scripted runbook does four things:
1. Unlists the affected NuGet versions.
2. Re-points image version tags to the last good `-sha-` tag.
3. Opens a `release-blocker` issue.
4. Starts a forward patch.

Autonomy is not enabled until this script has been exercised once against Feedz.

## 7. Machine merge gate (replaces `.github/reviewers.md` human approval)

A PR from Deliver merges only when all of the following hold:
- CI is green on its head.
- Two fresh reviewer subagents, one per axis (Standards and Spec), report no must-fix findings on that head.
- Greptile scores it 5/5 on that head.
- A PR labeled `breaking` or `schema change` also carries migration notes.

PRs from outside contributors keep the existing human gate. Deliver never merges them.

## 8. Prompt-injection boundary

Issue bodies, comments and PR text are untrusted. The boundary has four rules:
- **Triage** reads them, but its App permissions are limited to issues: no contents or workflow access.
- **Release-train** decides only from structured state: milestone membership, labels, check conclusions and tags.
- **Release notes** are generated in a separate step with no tools and no write access. Its output is plain text that Release-train inserts into the release body. It is never executed.
- **Secrets** are used only inside GitHub workflows through OIDC or environments. They never enter agent context.

## 9. Rollout

| Phase | Work | Exit criterion |
|---|---|---|
| 0. Bootstrap (with you) | See list below. | Gates script passes. Rollback exercised on Feedz. |
| 1. Issues | Transfer Studio/Extensions issues; create labels and `Backlog`; Triage in dry-run for 1 week (writes only to the control issue), then live. | ≥95% precision on a 20-issue spot check of proposed closures. |
| 2. elsa-apps | §3 migration PR plus ADR. | Images from the monorepo artifact pass smoke on both architectures. |
| 3. Deliver | Cloud routine over the 3.10 milestone. | 5 consecutive issues merged with no human intervention and no reverts. |
| 4. Release shadow | Readiness and Release-train run in verify mode for 3.10.0; you cut. Backtest the rubric. | Proposals and receipts match the manual cut. |
| 5. Autonomous | Live from 3.10.1 / 3.11.0. Downstream bump PRs (gitbook, templates, samples). Announcement copy is drafted; posting stays manual. | First autonomous release completes with no veto needed. |

**Phase 0 bootstrap work:**
- Create the GitHub App.
- Unify the tag format to `X.Y.Z-rc.N`.
- Derive `base_version` in `packages.yml` from a single version source.
- Lift the 3.10.x Feedz-only gate at the cutover.
- Fix the stale pins in `extensions/src/Directory.Build.props`.
- Fix #8502 and #8609.
- Swap environment required reviewers for wait timers.
- Implement the §7 merge gate.
- Script the rollback runbook.

## 10. Out of scope

- 3.8/3.9 maintenance releases. These stay with the source repos and are manual.
- Elsa 4.
- npm publishing of Studio packages. Ownership is undecided (#8216).
- Posting announcements.
- Discussions triage.

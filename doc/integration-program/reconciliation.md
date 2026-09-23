# Initial backlog reconciliation

Program: [#8194](https://github.com/elsa-workflows/elsa-core/issues/8194). Checked: 2026-09-23.

The existing backlog was read and retained: 10 epics, 40 features, 6 stories, and 12 tasks, plus the program. No issues were recreated, transferred, closed, or assigned. Title prefixes retain the semantic levels without introducing organization-wide issue types or labels.

## Relationships

The `Parent:` fields and parent child lists agree for all 68 children. The intended hierarchy is Program → Epic → Feature → Story → Task. Parents were queried before any mutation; no unrelated native parent was replaced. GitHub REST supports both sub-issues and blocking dependencies with the current authentication.

The 68 native parent relationships were added through `POST /repos/elsa-workflows/elsa-core/issues/{parent}/sub_issues` using the child's numeric issue ID, then individually read back through `GET .../issues/{child}/parent`. The `replace_parent` option was not used.

The 26 explicit dependencies in the existing `Blocking dependencies` and `Dependencies (separate from hierarchy)` sections were recorded through `POST .../issues/{blocked}/dependencies/blocked_by`, using the blocker issue ID, after querying existing dependencies. Each was read back. These include epic and story evidence gates as well as task dependencies. The original qualification “prerequisite evidence; discovery may overlap” still applies; a dependency is not a ban on parallel discovery. No dependency was inferred merely from containment.

[The machine-readable snapshot](hierarchy.json) records each issue, native parent edge and explicit blocking edge separately. Both directed graphs are acyclic. The snapshot records open issue state at the audit, not a guarantee about later execution.

## Reproduce verification

From the repository root:

```sh
python3 doc/integration-program/verify-hierarchy.py
python3 doc/integration-program/verify-hierarchy.py --live
```

The first command validates counts, levels, uniqueness, coverage and cycles offline. The second uses authenticated `gh` read-only calls to check every recorded native relationship. It deliberately does not remove additional relationships or reparent work. Run it again before subsequent execution if the backlog may have changed. GitHub is the current state; this file is dated evidence.

## Reporting and decisions

- **In progress:** source inspection or research has started; findings are not yet accepted.
- **Proposed / in review:** evidence is committed in a linked PR; its issue remains open and acceptance is not implied.
- **Delivered:** the reviewed deliverable is integrated and its acceptance evidence is verified. A proposed PR alone is not delivery.
- **Blocked:** record the precise missing dependency, account capability or decision, without concealing independent work that can continue.

Use existing issue acceptance criteria and append scoped execution notes. Keep source commits, validation commands, limitations, and PR links with each deliverable. Do not check program implementation outcomes for this audit phase. Architecture choices remain proposed until review; package publishing and migration have separate approval and validation gates.

## Scope preserved

The audit worktrees start from Core `610790ec57ae9d5c334181d50c1e65f99613fd86`. The supplied detached checkout at `be5bdae501997401bd248f7642eeabacd1994d8a` was left untouched. Open PRs and local worktrees were inspected before starting, including existing Secrets validation, tenant runtime, and NuGet publishing work. The inventory PR contains the detailed cross-repository active-work ledger and source pins.

No wholesale import, NuGet publication, repository archival, PR merge, or production change is authorized by this audit. Elsa 3 remains the target. Co-location, package release, host deployment, and workflow schema compatibility remain separate decisions.

## Publishing safeguard discovered during the audit

Core's pinned `.github/workflows/packages.yml` matches `codex/*` pushes and can publish the full preview package set to Feedz after tests/build. The initial hierarchy branch triggered [run 35863141778](https://github.com/elsa-workflows/elsa-core/actions/runs/35863141778); it was cancelled during tests. The build and both publishing jobs have no executed steps. No package publication occurred. Remaining audit branches use `audit/*`, outside the current push filters. Further commits on the existing hierarchy branch use `[skip ci]` to prevent the push workflow. This deliberately leaves that branch's Actions evidence skipped/cancelled; local document checks and GitHub relationship verification are reported separately. Do not rerun its Packages workflow.

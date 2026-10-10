# Quality loop

Two scheduled agent jobs keep Elsa simple, consistent and pleasant to build on. The roadmap and run log live in [#8700](https://github.com/elsa-workflows/elsa-core/issues/8700).

| Source | Purpose |
|---|---|
| [PRINCIPLES.md](PRINCIPLES.md) | The standard the review job audits against and the fixer's reviewers enforce. |
| [`CONTEXT.md`](../../CONTEXT.md) | The canonical domain vocabulary. |
| [`docs/adr/`](../../docs/adr/) and issues closed as *not planned* | Decisions already made, so proposals are not raised again. |
| [metrics.sh](metrics.sh) | Size metrics posted with every review run. Run it from the repository root. |

## Labels

| Label | Meaning |
|---|---|
| `audit:proposed` | Filed by an audit. Waiting for maintainer triage. |
| `audit:approved` | Approved. The fixer job may deliver it, including merging the PR once the merge gate passes. |
| `audit:auto` | Approved by the review job itself under the low-risk rules below. Remove `audit:approved` to veto. |
| `audit:tracking` | The roadmap and run-log issue. |
| `area:*`, `priority:P1`–`P3`, `breaking` | Classification, set when the issue is filed. |

To decline a proposal, close it as *not planned*, with a one-line reason when the reasoning should outlive the issue.

## Areas and focus

Each review run covers one **area × focus** cell, plus everything merged since the previous run.

| Area | Scope |
|---|---|
| `foundation` | Projects in `Elsa.Foundation.slnf` |
| `runtime`, `persistence`, `scripting`, `security`, `integrations`, `ai`, `operations` | Projects in the matching `Elsa.<Area>.slnf` that are not in `Elsa.Foundation.slnf` |
| `studio` | `studio/` |
| `extensions` | `extensions/` |
| `repo` | Root files, `.github/`, `build/`, `scripts/`, `docker/`, `docs/`, solution and solution filters, agent instructions |

The focuses are: `api` (public types, signatures, entry points, developer experience), `language` (domain terms against `CONTEXT.md`), `simplify` (dead code, needless abstractions, duplication), `architecture` (boundaries, dependencies, directory and solution structure), `tests` (redundancy, filler, gaps), `docs`, `ci` (workflows and build) and `product` (a new user's path, core versus niche features).

The cell for a run is focus `w % 8` and area `w % 11`, using the orders listed above. Here `w` is a continuous week number, `$(( $(date +%s) / 604800 ))` (weeks since the Unix epoch), not the ISO week, which resets every year. Because 8 and 11 have no common factor, every cell is visited once every 88 weeks.

## Throttle

- At most 5 new issues per run.
- No new `audit:proposed` issues while 12 or more are waiting for triage.
- At most 3 auto-approved issues per run.
- Candidates held back by the throttle are listed in the run log, so nothing is lost.

## Auto-approval

The review job adds `audit:approved` and `audit:auto` only when all of these hold:

- The change is not breaking and does not change the public surface of any shipped package.
- It is one of: repository hygiene (stray or duplicate files), a CI workflow removal or fix, test consolidation that keeps behavior coverage, removal of unreferenced non-public code (verified by search across all products), or a docs correction.
- The effort is small or medium.

Everything else waits for the maintainer.

## Fixer

Each run delivers one `audit:approved` issue end to end: branch, implement, two-axis review, PR, CI, Greptile and squash-merge, following the merge gate in [`.github/reviewers.md`](../reviewers.md). It picks the earliest approved issue in the order in #8700 whose prerequisites are closed. It never delivers issues labeled `breaking`: public API changes follow principle 6 (deprecate, don't break).

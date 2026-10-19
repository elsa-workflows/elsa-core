# Native compensation and saga recovery delivery program

[Program #8884](https://github.com/elsa-workflows/elsa-core/issues/8884) is the live control record; [PRD #8886](https://github.com/elsa-workflows/elsa-core/issues/8886) is the single canonical product requirements record. This entrypoint covers **planned Elsa 3 capability**, not released functionality. Sipke authorized program establishment on 19 October 2026; this setup does not start production implementation or authorize publication or deployment.

The outcome is a native workflow authored, published, executed, inspected and recovered in Elsa Studio, with recovery surviving backend process restarts and uncertain external outcomes remaining explicit. Studio participates from semantic design and the first durable slice.

## Ownership and evidence

Current Elsa 3 development targets `elsa-workflows/elsa-core` main. Core owns `core/src/` and `core/test/`; Studio owns `studio/src/` and `studio/test/` plus colocated tests; Extensions owns optional integrations. Shared decisions live in `docs/adr/`. Source co-location does not change package identities, public 3.x compatibility or retained maintenance/publisher responsibilities. See [discovery](discovery.md), [decisions](decisions.md), [the proposed boundary ADR](../../../docs/adr/2026-10-19-native-compensation-recovery-boundaries.md), [contribution routing](../../../CONTRIBUTING.md) and [Studio guidance](../../../studio/src/AGENTS.md).

The original [Compensation #4015](https://github.com/elsa-workflows/elsa-core/issues/4015) is reused as an Epic, preserving its author and request. [Saga question #304](https://github.com/elsa-workflows/elsa-core/issues/304) remains demand context. The [Elsa 4 Program](https://github.com/elsa-workflows/elsa-foundation/issues/3035) and [PRD](https://github.com/elsa-workflows/elsa-foundation/issues/3036) inform outcomes and staging only. No Elsa 4 contract, runtime mode or repository layout is assumed, and Elsa 3 delivery does not wait for Elsa 4.

BPMN implementation reuse, adapters, shared compensation abstractions and dependencies are completely excluded. Do not extend or extract the existing BPMN compensation implementation for this program.

## Progressive delivery

| Stage | Work | Exit proof |
|---|---|---|
| M0 | [Semantics and Studio design](https://github.com/elsa-workflows/elsa-core/issues/8890) | Reviewed state, identity, version and persistence-boundary contract together with authoring, capture mapping, validation and inspection interactions. |
| M1 | [First durable visual journey](https://github.com/elsa-workflows/elsa-core/issues/8891) | Real Studio publish/run/inspect, durable store and independently persisted participant; kill and restart backend during recovery. |
| M2 | [Uncertainty and reconciliation](https://github.com/elsa-workflows/elsa-core/issues/8892) | Lost response, crash before/after effect and acknowledgement, duplicate/stale execution, idempotency and explicit intervention. |
| M3 | [Composition and lifecycle](https://github.com/elsa-workflows/elsa-core/issues/8893) | Ordering, nesting, repeated occurrences, confirmation, retries, cancellation/late results, shutdown/resumption and pinned versions. |
| M4 | [Operator controls](https://github.com/elsa-workflows/elsa-core/issues/8894) | Accessible occurrence-to-attempt views and authorized, tenant-scoped, audited retry/reconciliation with stale-action protection. |
| M5 | [Integrated qualification](https://github.com/elsa-workflows/elsa-core/issues/8895) | Accepted provider/runtime/host matrix, real restart proof, upgrade/retention tests, sample, runbook and release-readiness evidence. |

The four native Epics are [#4015](https://github.com/elsa-workflows/elsa-core/issues/4015), [#8887](https://github.com/elsa-workflows/elsa-core/issues/8887), [#8888](https://github.com/elsa-workflows/elsa-core/issues/8888) and [#8889](https://github.com/elsa-workflows/elsa-core/issues/8889). Native parentage and blocking relationships are separate: later milestones are blocked by preceding accepted outcomes, not merely listed below them. Only initial setup/design tasks are elaborated; feature outlines do not grant implementation readiness.

## Next work and control surface

At establishment, [setup #8885](https://github.com/elsa-workflows/elsa-core/issues/8885) owns the documentation PR. **The next work item is [design task #8896](https://github.com/elsa-workflows/elsa-core/issues/8896)**. It is prepared, blocked on acceptance of setup documentation, unclaimed and not labelled `ready-for-agent`. The lead refreshes current source, comments, claims, open PRs and dependencies before promoting it. This session does not dispatch it. Only that design task's accepted M0 output can make a production leaf ready.

Product authority is Sipke Schoorstra. The program lead owns scope, decisions, readiness and integration; Core, persistence, Studio and security reviewers accept their boundaries. Individual delivery ownership requires the org App-authored claim protocol, not assignment of a bot or the owner. Role ownership is not a staffing or deployment commitment. Keep one active leaf and one integration lane within this program; do not take over unrelated consolidation/release work.

Project creation as `elsa-workflows-codex[bot]` was attempted on 19 October 2026 and returned `FORBIDDEN`: the app does not have permission to create organization Projects. **No board was created.** Organization issue-type enumeration also returned HTTP 403. Types are therefore expressed in issue titles/bodies and existing labels; native parent/sub-issue and blocking relations are used independently. No personal identity was substituted.

Until permission is available, the Program and its issues are the live control surface. The lead owns a board follow-up after App Projects permission is granted: add these same records, Status, Type, Milestone, Area, Priority, Agent State and Verification; create Roadmap, Execution, Critical Path, Agent Queue, Workstreams and Blocked views. No duplicate backlog. All product milestone verification starts Pending. Reconcile issue/PR state and readiness after material events; no scheduler or recurring automation is established by this setup.

## Evidence required for delivery

The first journey uses two controlled reservations with a later rejection. An author configures scope/handlers and captured data in a real Studio host, receives server-backed publication validation, publishes and runs it. The backend writes recovery intent before effects; a separate participant persists its own ledger. Kill the backend mid-recovery, restart against the same durable store, reconnect Studio, and inspect the original occurrences and recovery attempts through supported APIs. Include a deliberate pending/failed case and baseline tenant/data-access negatives.

For each milestone retain exact source head, declared host/provider/runtime configuration, workflow definition/version and occurrence/operation IDs, participant ledger, crash point, persisted result and sanitized browser/accessibility evidence. Compare participant effects with the UI and stored recovery state. A process reload, mocked client, in-memory suite or closed issue is insufficient. Later milestones add uncertain outcomes, concurrent/late work, nested/loop semantics, safe operations and upgrade/retention cases. Failed proof keeps the milestone open.

No durability or product milestone test was run by this planning change. Its validation is documentation/source review plus live issue graph readback. Delivery tests belong to the gated implementation tasks.

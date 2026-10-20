# Native compensation and saga recovery delivery program

[Program #8884](https://github.com/elsa-workflows/elsa-core/issues/8884) is the live control record; [PRD #8886](https://github.com/elsa-workflows/elsa-core/issues/8886) remains the single canonical product requirements record. [Project #60](https://github.com/orgs/elsa-workflows/projects/60) is the program board. This entrypoint covers **planned Elsa 3 capability**, not released functionality. The owner's [21 October standing authorization](https://github.com/elsa-workflows/elsa-core/issues/8884#issuecomment-6303225186) supersedes earlier delivery holds and authorizes the program lead to proceed through actual readiness and review gates. It does not itself satisfy those gates or change release and deployment policy.

The outcome is a native workflow authored, published, executed, inspected and recovered in Elsa Studio, with recovery surviving backend process restarts and uncertain external outcomes remaining explicit. Studio participates from semantic design and the first durable slice.

## Ownership and evidence

Elsa 3 development targets `elsa-workflows/elsa-core` main. Core owns `core/src/` and `core/test/`; Studio owns `studio/src/` and `studio/test/` plus colocated tests; Extensions owns optional integrations. Shared decisions live in `docs/adr/`. Source co-location does not change package identities, public 3.x compatibility or retained maintenance/publisher responsibilities. See [discovery](discovery.md), [decisions](decisions.md), the [proposed scope-boundary ADR](../../../docs/adr/2026-10-19-native-compensation-recovery-boundaries.md), the [proposed instance-journal ADR](../../../docs/adr/2026-10-20-native-compensation-instance-journal.md), [contribution routing](../../../CONTRIBUTING.md) and [Studio guidance](../../../studio/src/AGENTS.md).

The original [Compensation #4015](https://github.com/elsa-workflows/elsa-core/issues/4015) is reused as an Epic, preserving its author and request. [Saga question #304](https://github.com/elsa-workflows/elsa-core/issues/304) remains demand context. The [Elsa 4 Program](https://github.com/elsa-workflows/elsa-foundation/issues/3035) and [PRD](https://github.com/elsa-workflows/elsa-foundation/issues/3036) inform outcomes and staging only. No Elsa 4 contract, runtime mode or repository layout is assumed, and Elsa 3 delivery does not wait for Elsa 4.

BPMN implementation reuse, adapters, shared compensation abstractions and dependencies are completely excluded. Do not extend or extract the existing BPMN compensation implementation for this program.

## Progressive delivery

| Stage | Work | Exit proof |
|---|---|---|
| M0 | [Elsa 3 adaptation design](https://github.com/elsa-workflows/elsa-core/issues/8896) | Reviewed design packet with owner policies O1–O3 accepted, Elsa 3 source adaptation and Studio interactions recorded, and E1–E3 technical gates kept explicit. |
| M1 | [First durable visual journey](https://github.com/elsa-workflows/elsa-core/issues/8891) | Real Studio publish/run/inspect, durable store and independently persisted participant; kill and restart backend during recovery. |
| M2 | [Uncertainty and reconciliation](https://github.com/elsa-workflows/elsa-core/issues/8892) | Lost response, crash before/after effect and acknowledgement, duplicate/stale execution, idempotency and explicit intervention. |
| M3 | [Composition and lifecycle](https://github.com/elsa-workflows/elsa-core/issues/8893) | Ordering, nesting, repeated occurrences, confirmation, retries, cancellation/late results, shutdown/resumption and pinned versions. |
| M4 | [Operator controls](https://github.com/elsa-workflows/elsa-core/issues/8894) | Accessible occurrence-to-attempt views and authorized, tenant-scoped, audited retry/reconciliation with stale-action protection. |
| M5 | [Integrated qualification](https://github.com/elsa-workflows/elsa-core/issues/8895) | Accepted provider/runtime/host matrix, real restart proof, upgrade/retention tests, sample, runbook and release-readiness evidence. |

The four native Epics are [#4015](https://github.com/elsa-workflows/elsa-core/issues/4015), [#8887](https://github.com/elsa-workflows/elsa-core/issues/8887), [#8888](https://github.com/elsa-workflows/elsa-core/issues/8888) and [#8889](https://github.com/elsa-workflows/elsa-core/issues/8889). Native parentage and blocking relationships are separate: later milestones are blocked by preceding accepted outcomes, not merely listed below them. Only initial setup/design tasks are elaborated; feature outlines do not grant implementation readiness.

## Current status and next work

The owner has accepted the O1 explicit-confirmation, O2 compensation-aware-cancellation and O3 `Compensated`-versus-original-fault policies. Their detailed rules and the proposed technical design live in the [adaptation packet](../specs/native-compensation/README.md); the [canonical PRD](https://github.com/elsa-workflows/elsa-core/issues/8886) remains the product-requirements source. The [instance-journal ADR](../../../docs/adr/2026-10-20-native-compensation-instance-journal.md) is still proposed. Policy acceptance is not physical architecture acceptance or runtime evidence.

The packet's E1–E3 technical gates remain to be proved before M1 [#8891](https://github.com/elsa-workflows/elsa-core/issues/8891) can become ready. The bounded E1 evidence task is [#8930](https://github.com/elsa-workflows/elsa-core/issues/8930); consult its live record for execution status. Product authority is Sipke Schoorstra. The program lead owns scope, decisions, readiness and integration; Core, persistence, Studio and security reviewers own their technical reviews. Individual delivery ownership follows the org App-authored claim protocol. Keep one active leaf and one integration lane within this program; do not take over unrelated consolidation or release work.

Use [Program #8884](https://github.com/elsa-workflows/elsa-core/issues/8884), [PRD #8886](https://github.com/elsa-workflows/elsa-core/issues/8886) and [Project #60](https://github.com/orgs/elsa-workflows/projects/60) as the live control surfaces. Keep the board aligned with those canonical records and refresh issue, PR, claim and dependency state before making readiness claims. The standing authorization allows progress through actual gates; a claim, issue closure, board field or documentation merge does not substitute for required technical evidence.

## Evidence required for delivery

The first journey uses two controlled reservations with a later rejection. An author configures scope/handlers and captured data in a real Studio host, receives server-backed publication validation, publishes and runs it. The backend writes recovery intent before effects; a separate participant persists its own ledger. Kill the backend mid-recovery, restart against the same durable store, reconnect Studio, and inspect the original occurrences and recovery attempts through supported APIs. Include a deliberate pending/failed case and baseline tenant/data-access negatives.

For each milestone retain exact source head, declared host/provider/runtime configuration, workflow definition/version and occurrence/operation IDs, participant ledger, crash point, persisted result and sanitized browser/accessibility evidence. Compare participant effects with the UI and stored recovery state. A process reload, mocked client, in-memory suite or closed issue is insufficient. Later milestones add uncertain outcomes, concurrent/late work, nested/loop semantics, safe operations and upgrade/retention cases. Failed proof keeps the milestone open.

No durability or product milestone test was run by this documentation reconciliation. Its checks cover links, generated ADR navigation and document consistency; they provide no runtime, browser, participant or restart proof. Delivery qualification belongs to its gated implementation work.

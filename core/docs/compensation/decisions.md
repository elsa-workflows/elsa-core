# Program decisions and requirements gate

The [PRD](https://github.com/elsa-workflows/elsa-core/issues/8886) owns product requirements. This register records program decisions and deferred architecture questions; the [boundary ADR](../../../docs/adr/2026-10-19-native-compensation-recovery-boundaries.md) records the proposed architecture constraints. Design task [#8896](https://github.com/elsa-workflows/elsa-core/issues/8896) owns resolution before implementation readiness.

## 2026-10-19 — Establish Elsa 3 independently

**Decision:** Establish the native Elsa 3 program now, with Studio authoring, inspection and recovery in scope from M0/M1. Reuse #4015 as the original capability Epic. Coordinate language and observable behavior with Elsa 4 without dependency edges to its program.

**Authority:** Direct owner request. Elsa 4's earlier plan to assess Elsa 3 after its delivery is historical context for that program, not a gate on this new authorization.

**Consequence:** No production implementation or release in setup. The two programs can compare scope, occurrence, operation, attempt, confirmation and uncertain-outcome vocabulary while choosing independent runtime/storage/API contracts. New public Elsa 3 terminology belongs in `CONTEXT.md` with its eventual reviewed API, not as prematurely finalized names here.

**Revisit:** M0 semantic review and any demonstrated observable behavior conflict; program lead resolves source questions and escalates material product choices to Sipke.

## 2026-10-19 — Preserve native and product boundaries

**Decision:** Completely exclude BPMN implementation reuse, adapters, shared compensation abstractions and dependencies. Use current Elsa 3 main's confirmed product layout and native runtime seams; preserve public package/activity identities. Prefer existing mechanisms where they meet the required guarantee; do not presume they do.

**Consequence:** This additive planning packet is needed because no matching native delivery program exists. It reuses the demand issue and creates no duplicate scheduler, outbox, specification system or production code. There is no obsolete product code to remove. Retained 3.8/3.9 maintenance and publishing are not moved by this program.

**Revisit:** No planned relaxation of the BPMN boundary. Refresh source ownership on each implementation handoff.

## 2026-10-19 — Gate delivery on real integrated evidence

**Decision:** Deliver semantics plus Studio interactions first, then a thin durable visual journey, uncertainty handling, composition, operator controls and integrated qualification. Baseline authorization, tenant isolation and sensitive-data protection apply to the first slice. Later operator features deepen these guarantees.

**Consequence:** Each milestone needs real Studio/backend/persistence evidence, including process restart and independent participant effects. Scheduling, issue closure or an in-memory pass cannot prove successful compensation. The initial ordering recommendation is sequential reverse durable completion order; M0 must resolve ties, targeting and lifecycle races before implementation.

**Revisit:** Program lead/QA at every milestone; failed proof reopens the owning work and keeps the milestone incomplete.

## Deferred decisions with owners and gates

| ID | Decision | Accountable owner | Revisit / evidence | Why safe to defer now |
|---|---|---|---|---|
| A1 | Recovery schema, pre-effect commit, atomic result updates, CAS/fencing and dispatch/resumption hooks | Program lead + Core/persistence reviewers | M0; inspect configured commit strategy and store participation; accept a bounded spike if evidence is insufficient. Before M1 tasks are ready. | Setup creates no public contract or production state. |
| A2 | Scope/handler notation, captured mapping, publication errors and accessible occurrence/attempt UI | Program lead + Studio reviewer | M0 paired walkthrough for sequential, loop and nested examples, including failure/uncertainty. | No activity API is frozen ahead of UX. |
| A3 | Confirmation, nested obligations, cancellation/late result and ordering policy | Program lead + runtime/Studio reviewers; Sipke for unresolved product policy | M0 transition table; full supported composition matrix before M3 refinement. | First milestone is design; no worker has to invent the policy. |
| A4 | Exact provider/runtime/host support and 3.x target release | Program lead + QA/release reviewers | M0 selects first durable host/provider, proposed SQLite local configuration; M5 proves the full advertised matrix. | Source support is not advertised as released support. No commitment to every provider. |
| A5 | Permission names, safe summary/detail model, audit retention, pinned-version retention and purge behavior | Program lead + security/operations reviewers | Baseline before M1; full action model before M4; upgrade/retention proof at M5. | Existing tenant, data and secret policies are fixed requirements. Destructive cleanup and broad new privileges are not authorized. |
| A6 | Named implementation/release maintainers and runbook acceptance | Program lead | Before M1 dispatch for implementation; before M5 exit for release readiness. | Role accountability is recorded; no unconfirmed person is assigned. |
| A7 | GitHub Project and org issue types | Program lead; organization administrator supplies permission | Retry only after App permission change or explicit identity authorization. | Native issue hierarchy and blockers work now; board absence cannot hide or block the design queue. |

## Requirements-interrogation result

The gate passes for **establishment and the bounded M0 design task only**. Goal, actors, journeys, native/BPMN boundary, Studio inclusion, current repository ownership, security invariants, critical path and evidence standard are resolved by the owner request and source inspection. A1–A6 are design deliverables with owners and revisit gates; they cannot invalidate the establishment work. A7 is an exact administrative limitation, not product ambiguity.

No production leaf is Agent Ready. Next [#8896](https://github.com/elsa-workflows/elsa-core/issues/8896) is prepared but blocked on acceptance of the setup documentation. The program lead must refresh live claims/PRs and accept that design-only readiness gate before labelling it ready. After M0, only a dependency-satisfied, explicitly reviewed M1 task may be refined/promoted; unresolved material M1 choices must be settled or escalated first. Do not auto-dispatch follow-on work from this session.

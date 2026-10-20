# Program decisions and requirements gate

The [PRD](https://github.com/elsa-workflows/elsa-core/issues/8886) remains the single product-requirements record. This page preserves dated program decisions and current status; the [adaptation packet](../specs/native-compensation/README.md) owns the detailed proposed design and technical gates. The [scope-boundary ADR](../../../docs/adr/2026-10-19-native-compensation-recovery-boundaries.md) and [instance-journal ADR](../../../docs/adr/2026-10-20-native-compensation-instance-journal.md) both remain proposed.

**Status note (21 October 2026):** The owner's [standing authorization](https://github.com/elsa-workflows/elsa-core/issues/8884#issuecomment-6303225186) to lift all current and future delivery holds supersedes earlier pause instructions recorded below. Those dated quotations remain historical. O1–O3 are accepted product policies; the physical architecture and E1–E3 evidence remain unaccepted or unproved as detailed in the adaptation packet.

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

## 2026-10-21 — Standing delivery authorization and current gates

The owner authorized the program lead to lift all current and future delivery holds. This supersedes the earlier owner-directed pause of production work and spikes. The lead may proceed with setup, bounded evidence work and production delivery as each real dependency, review and verification gate permits. The authorization itself is not evidence, does not make a production task Ready, and does not change separate release or deployment policy.

O1, O2 and O3 are accepted product policy. O1 requires explicit confirmation to end a scope occurrence's compensation eligibility. O2 requires persisted, compensation-aware business cancellation that preserves uncertain and in-flight work. O3 records successful recovery as `Compensated` and selects either that outcome or explicit propagation of the original fault. The [adaptation packet's decision register](../specs/native-compensation/decisions.md) owns the complete rules; this page does not duplicate the canonical PRD or freeze public API names.

E1–E3 remain technical evidence gates. E1 is the bounded runtime/persistence proof tracked by [#8930](https://github.com/elsa-workflows/elsa-core/issues/8930); E2 covers security, protection, disclosure and authorization; E3 covers Studio/runtime composition and integration. Their scope and exit evidence are defined in the [design packet](../specs/native-compensation/decisions.md). The proposed instance-journal ADR describes a direction to test, not an accepted physical architecture. M1 [#8891](https://github.com/elsa-workflows/elsa-core/issues/8891) cannot become Ready until the required gates pass review.

## Requirements-interrogation result

The setup requirements and M0 design packet are recorded. Owner policies O1–O3 are accepted, while E1–E3 and the physical architecture remain open for technical evidence and review. [#8930](https://github.com/elsa-workflows/elsa-core/issues/8930) is the bounded E1 work item; consult its live record for execution status. Keep M1 [#8891](https://github.com/elsa-workflows/elsa-core/issues/8891) Not Ready until its dependencies and evidence pass. The [Program #8884](https://github.com/elsa-workflows/elsa-core/issues/8884), [PRD #8886](https://github.com/elsa-workflows/elsa-core/issues/8886) and [Project #60](https://github.com/orgs/elsa-workflows/projects/60) remain the live program controls. No real runtime, browser, independent-participant or restart proof is claimed by these planning records.

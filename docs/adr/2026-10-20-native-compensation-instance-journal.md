# Retain native compensation obligations in the workflow instance

- Status: Proposed
- Date: 2026-10-20
- Owners: Elsa 3 program lead with runtime, persistence, Studio and security reviewers
- Related: [Program #8884](https://github.com/elsa-workflows/elsa-core/issues/8884), [design #8896](https://github.com/elsa-workflows/elsa-core/issues/8896), [packet](../../core/docs/specs/native-compensation/README.md)

## Context

The owner permits one adaptation design task while production remains paused. Elsa 4 provides useful native compensation semantics and Studio interactions, but Elsa 3 lacks its checkpoint/causal-result/structure contracts. Elsa 3 removes completed child contexts and maintains a transient output register. A child's properties or generic execution history cannot be the only retained recovery truth.

## Options

1. Child activity state plus ordinary output references: small, but cannot preserve occurrence capture and eligibility after context cleanup.
2. Reconstruct obligations from execution logs: avoids a journal, but prunable/partial history cannot prove armed intent, pending obligations or exact immutable recovery values.
3. A separate compensation database/table and scheduler: may eventually be justified, but creates a second state/coordination mechanism before its need is proven.
4. A versioned journal rooted in existing workflow-instance state, committed with native scheduled work: selected proposed direction. The state snapshot already contains properties, active contexts, callbacks and scheduled work; physical commit/ownership guarantees still need evidence.

## Decision

Retain scope/operation/attempt identities, exact pins, protected occurrence captures, recovery reservations and unresolved evidence in a namespaced root instance journal. Use native scheduling/callback/fault/resumption/authorization infrastructure. Keep logical operation and undo keys stable across attempts; commit intent before effects and commit recovery reservation with specific handler work. Use a safe read model and authorized audited revision-checked commands. Completed scope eligibility outlives child context cleanup; explicit confirmation/root finalization is accepted owner policy O1; business cancellation and outcome/propagation choices O2/O3 remain open. This policy acceptance does not accept or qualify the proposed physical architecture.

Completely exclude BPMN implementation reuse, adapters, shared compensation abstractions and dependencies. Do not import Elsa 4 runtime APIs, persistence modes or React slot contracts, or create a universal scheduler/retry engine. Preserve existing 3.x APIs/data and product package ownership. Additive public contracts require concrete caller scenarios and compatibility review.

## Consequences and evidence gate

One snapshot boundary minimizes new state representations, but is not proof that every provider atomically saves required stores or rejects stale owners. Existing conditional activity commits, no-op default transaction and opt-in EF wrapper do not establish the selected protocol. E1 must prove the candidate serial local/SQLite profile or select a revised provider/seam before M1 readiness. E2 covers protection/disclosure/retention; E3 covers native child/identity/Studio integration. Unknown remote outcome remains explicit even when local transactions are perfect.

The [evidence map](../../core/docs/specs/native-compensation/evidence.md) pins actual source; [persistence](../../core/docs/specs/native-compensation/persistence.md) specifies the protocol and [verification](../../core/docs/specs/native-compensation/verification.md) supplies crash/security controls. No implementation, provider qualification, restart evidence or public API freeze is claimed by this ADR.

## Revisit

Accept or revise after design review and owner policy decisions. If journal size, lifecycle, supported provider atomicity or protection cannot satisfy the contract, revise this ADR with evidence before adding a separate retained store. Production work needs a separate owner authorization and readiness gate even if this design PR merges. Retain held setup PR #8897 as its own record; this ADR does not accept it.

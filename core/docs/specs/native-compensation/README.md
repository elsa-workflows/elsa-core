# Elsa 3 native compensation adaptation design

Status: proposed design packet for [#8896](https://github.com/elsa-workflows/elsa-core/issues/8896), 20 October 2026. [Program #8884](https://github.com/elsa-workflows/elsa-core/issues/8884), [canonical PRD #8886](https://github.com/elsa-workflows/elsa-core/issues/8886), [Project #60](https://github.com/orgs/elsa-workflows/projects/60).

On 21 October 2026 the owner granted [standing authorization to lift current and future delivery holds](https://github.com/elsa-workflows/elsa-core/issues/8884#issuecomment-6303225186), superseding the earlier #8896-only exception. The program lead now owns end-to-end delivery through the actual readiness/review/qualification gates. E1–E3 still gate M1 readiness; authority is not evidence. This design remains independent of setup PR #8897 and does not accept or close #8885. Its former blocking edge was waived as this design's start prerequisite.

## Design result

Adopt Elsa 4's product vocabulary and paired-step interaction, implemented through Elsa 3's own activities, callbacks, fault signals, state snapshot, persistence and Blazor extension points. Keep forward operation, scope occurrence, compensation eligibility and individual attempts distinct. Use a versioned journal rooted in workflow-instance state, beyond completed child contexts; persist operation intent before effects, retain immutable occurrence captures and strictly pinned executable references, and stop rather than inventing an outcome when evidence is missing.

No BPMN implementation reuse, adapters, shared compensation abstractions or dependencies are permitted. No Elsa 4 structural activity, checkpoint API, persistence mode or React slot contract is imported. Existing Elsa 3 scheduling, protection, authorization, tenancy and retry/resumption infrastructure remains the integration surface; this design creates no second universal scheduler or retry engine.

## Packet and ownership

| Document | Owns |
|---|---|
| [Source evidence and adaptation](evidence.md) | Verified mechanisms versus design changes, pinned source references and Elsa 4 differences. |
| [Semantics](semantics.md) | Scope/operation transitions, ordering, confirmation, nesting, fault/cancellation/shutdown and late results. |
| [Data and persistence](persistence.md) | Identities, retained/protected captures, strict versions, root snapshot protocol, atomicity and recovery gates. |
| [Guarded native snapshots](guarded-snapshot.md) | Proposed storage-owned marker, receipt, tenant/revision/epoch fence, fail-closed outer hydration, mutation-path enforcement and bounded E1 transaction proof. |
| [Recovery API and security](recovery-api.md) | Safe read model, commands, authorization, audit, concurrency and disclosure. |
| [Native Core and Studio caller contract (E3)](e3-studio-contract.md) | Source-grounded owned-child model/traversal, strict pins, bindings, actual publish diagnostics, E2-safe Studio read path, and first tracer/proof handoff. |
| [Studio interaction contract](../../../../studio/docs/specs/native-compensation/plan.md) | Authoring, captures, publication diagnostics, execution navigation, accessible states and host integration. |
| [Verification plan](verification.md) | Real browser/backend/store/participant restart demonstrations and negative controls; no pass is implied. |
| [Review record](review.md) | Independent findings, corrections and the limits of design review. |
| [Decisions and delivery gates](decisions.md) | Accepted owner policies, selected design proposals, remaining technical gates and progressive delivery. |
| [Shared proposed ADR](../../../../docs/adr/2026-10-20-native-compensation-instance-journal.md) | Root instance journal and generation-specific recovery boundary. |
| [Guarded-snapshot ADR](../../../../docs/adr/2026-10-21-native-compensation-guarded-snapshots.md) | Proposed persistence-owned admission and write-fence contract; no provider/API or qualification claim. |
| [Owned-child and caller ADR](../../../../docs/adr/2026-10-21-native-compensation-owned-children.md) | Proposed native owned-child and paired Studio extension points; no global serializer change or API spelling freeze. |

The program lead integrates the packet. Runtime/persistence reviewers own the commit, scheduling and ownership proofs; Studio reviewers own the interactions/client mapping; security reviewers own command/disclosure/protection; QA owns reproducible qualification. These are accountable roles, not claims that named maintainers have accepted future implementation work.

## Requirements traceability

| PRD requirement | Design / acceptance |
|---|---|
| FR1 explicit scopes, handlers, compensation and confirmation | Semantics; Studio authoring; V1/V6. |
| FR2 stable operation/occurrence/attempt identity | Persistence; V2/V3/V6. |
| FR3 captured context and pinned versions | Persistence; V3/V4. |
| FR4 durable pre-effect intent and progress | Persistence protocol; V2/V3; evidence gate E1. |
| FR5 uncertainty, reconciliation, idempotency and late results | Semantics; recovery API; V2/V5. |
| FR6 ordering, nesting, cancellation and shutdown | Semantics; V2/V6; owner decisions O1–O3. |
| FR7 failed recovery and original cause | Semantics; recovery API; V5. |
| FR8 visual mapping and publication validation | Studio contract; V1/V4. |
| FR9 occurrence/attempt visualization and accessibility | Studio contract; V1/V5/V7. |
| FR10 tenant authorization, audit and sensitive data | Recovery API; V4/V5/V7; evidence gate E2. |
| FR11 capability, environment and host behavior | Studio contract; V7. |

## Acceptance boundary

This packet can be complete as a reviewed design while M1 remains Not Ready: #8896 explicitly permits a bounded evidence-spike proposal when existing source cannot establish a consequential guarantee. E1–E3 are those evidence gates. The standing authorization permits the lead to schedule the bounded E1 experiment and later production leaves once each passes its readiness gate. O1 explicit confirmation, O2 compensation-aware cancellation and O3 Compensated/default-versus-original-fault behavior are accepted by the owner. E1–E3 remain unproved, and policy acceptance alone grants no implementation readiness.

Existing setup records are reused, not copied into a second product PRD. This additive technical packet is necessary because current main has no native compensation contract. No product code is made obsolete or removed. Feature plans remain working documents during delivery; after qualification, move lasting contracts to product documentation/ADRs and retire redundant plan text under the owning program.

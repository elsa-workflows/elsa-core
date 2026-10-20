# Elsa 3 native compensation adaptation design

Status: proposed design packet for [#8896](https://github.com/elsa-workflows/elsa-core/issues/8896), 20 October 2026. [Program #8884](https://github.com/elsa-workflows/elsa-core/issues/8884), [canonical PRD #8886](https://github.com/elsa-workflows/elsa-core/issues/8886), [Project #60](https://github.com/orgs/elsa-workflows/projects/60).

The owner lifted the pause for **#8896 only**: deliver this reviewed design and its remaining decisions. Production implementation, M1 readiness, other leaves and establishment PR #8897 remain paused. This design is independent of that held PR and does not accept or close setup #8885. Its former blocking edge was waived only as this design's start prerequisite, with the owner instruction recorded on #8896.

## Design result

Adopt Elsa 4's product vocabulary and paired-step interaction, implemented through Elsa 3's own activities, callbacks, fault signals, state snapshot, persistence and Blazor extension points. Keep forward operation, scope occurrence, compensation eligibility and individual attempts distinct. Use a versioned journal rooted in workflow-instance state, beyond completed child contexts; persist operation intent before effects, retain immutable occurrence captures and strictly pinned executable references, and stop rather than inventing an outcome when evidence is missing.

No BPMN implementation reuse, adapters, shared compensation abstractions or dependencies are permitted. No Elsa 4 structural activity, checkpoint API, persistence mode or React slot contract is imported. Existing Elsa 3 scheduling, protection, authorization, tenancy and retry/resumption infrastructure remains the integration surface; this design creates no second universal scheduler or retry engine.

## Packet and ownership

| Document | Owns |
|---|---|
| [Source evidence and adaptation](evidence.md) | Verified mechanisms versus design changes, pinned source references and Elsa 4 differences. |
| [Semantics](semantics.md) | Scope/operation transitions, ordering, confirmation, nesting, fault/cancellation/shutdown and late results. |
| [Data and persistence](persistence.md) | Identities, retained/protected captures, strict versions, root snapshot protocol, atomicity and recovery gates. |
| [Recovery API and security](recovery-api.md) | Safe read model, commands, authorization, audit, concurrency and disclosure. |
| [Studio interaction contract](../../../../studio/docs/specs/native-compensation/plan.md) | Authoring, captures, publication diagnostics, execution navigation, accessible states and host integration. |
| [Verification plan](verification.md) | Real browser/backend/store/participant restart demonstrations and negative controls; no pass is implied. |
| [Review record](review.md) | Independent findings, corrections and the limits of design review. |
| [Decisions and delivery gates](decisions.md) | Selected design proposals, remaining owner decisions, technical evidence gates and progressive delivery. |
| [Shared proposed ADR](../../../../docs/adr/2026-10-20-native-compensation-instance-journal.md) | Root instance journal and generation-specific recovery boundary. |

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

This packet can be complete as a reviewed design while M1 remains Not Ready: #8896 explicitly permits a bounded evidence-spike proposal when existing source cannot establish a consequential guarantee. E1–E3 are those evidence gates. No production task or spike execution is authorized here. O1 explicit confirmation is accepted by the owner; O2/O3 still require acceptance before affected work. E1–E3 remain unproved, and O1 acceptance alone grants no implementation readiness or production authorization.

Existing setup records are reused, not copied into a second product PRD. This additive technical packet is necessary because current main has no native compensation contract. No product code is made obsolete or removed. Feature plans remain working documents during delivery; after qualification, move lasting contracts to product documentation/ADRs and retire redundant plan text under the owning program.

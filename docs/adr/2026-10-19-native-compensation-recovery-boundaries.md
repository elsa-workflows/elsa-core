# Keep native compensation recovery within Elsa 3 runtime and Studio boundaries

- Status: Proposed; owner scope constraints and O1–O3 policy are binding, while the physical design and technical guarantees remain unproved.
- Date: 2026-10-19
- Owners: Elsa 3 program lead, with Core, persistence, Studio and security review
- Related: [Program #8884](https://github.com/elsa-workflows/elsa-core/issues/8884), [PRD #8886](https://github.com/elsa-workflows/elsa-core/issues/8886), [design task #8896](https://github.com/elsa-workflows/elsa-core/issues/8896)

## Context

Elsa 3 needs visually authored native compensation with durable recovery, explicit uncertain outcomes and authorized operations. The owner accepted O1 explicit confirmation, O2 compensation-aware cancellation and O3 distinct `Compensated`/original-fault behavior; the [decision register](../../core/docs/specs/native-compensation/decisions.md) records their exact policy. The 21 October [standing authorization](https://github.com/elsa-workflows/elsa-core/issues/8884#issuecomment-6303225186) supersedes earlier delivery holds but does not prove technical readiness. Historical Elsa 2 scopes establish useful concepts, but their activity-ID scheduling and scheduling-time `Compensated` marker do not meet occurrence-aware recovery requirements. The source mechanisms summarized in [the discovery snapshot](../../core/docs/compensation/discovery.md) are candidate extension points; none alone proves safe compensation of arbitrary external effects.

Current main co-locates Core and Studio under `core/` and `studio/` in `elsa-core`. This is independently verified Elsa 3 ownership. Elsa 4 reference outcomes do not supply Elsa 3 contracts, repository paths or persistence modes. See the [source baseline](../../core/docs/compensation/discovery.md).

## Options considered

1. Directly port Elsa 2 activity/service contracts. Familiar semantics, but insufficient operation identity, uncertain-outcome handling, retained context and truthful success evidence.
2. Share or adapt BPMN compensation infrastructure. Explicitly excluded by the owner, including shared abstractions and dependencies; not a permissible implementation option.
3. Import Elsa 4 contracts or wait for its delivery. Couples Elsa 3 to a different runtime and violates independent delivery.
4. Design native recovery using Elsa 3 extension points, with paired Studio and persistence acceptance. The [adaptation packet](../../core/docs/specs/native-compensation/README.md) now proposes a versioned workflow-instance journal and related mechanisms; the [instance-journal ADR](2026-10-20-native-compensation-instance-journal.md) remains Proposed, and E1–E3 must establish the guarantees before M1 readiness.

## Proposed decision

Keep compensation a native Elsa 3 capability. Review runtime/activity/persistence/API and Studio interactions as one vertical product. Use existing registration, scheduling, state, bookmark, validation, client and visualization extension points where their guarantees fit. Do not introduce a second universal execution model or casually widen the dispatch outbox into a generic effect engine.

Separate definition activity identity, concrete forward occurrence, logical external operation and recovery attempt. Retain immutable captured recovery context and resolvable pinned workflow/handler versions beyond normal activity-context cleanup. Persist recovery intent and operation identity before external effects; make result persistence, retries and concurrent/stale work explicit. Participant cooperation and reconciliation remain necessary: at-least-once delivery is not exactly-once effects. Scheduling recovery never counts as successful compensation.

Model business cancellation separately from process shutdown. Pending, failed or uncertain recovery remains an unresolved obligation until supported evidence settles it; original fault and recovery failure remain distinct. Confirmation cannot silently discard required recovery or audit history. Exact state schema, ordering/nesting policies, retention and storage/concurrency mechanism are deliberately not selected by this ADR.

Studio authoring, captured-data mapping and server-authoritative publication validation belong in M0/M1. Execution views must relate forward occurrences to attempts, use text/icons and accessible navigation, and respect backend capability and environment changes. Recovery commands require backend authorization, tenant scope, concurrency and deduplication checks, durable audit and existing sensitive-data protections. Do not put unrestricted recovery payloads in generic state, logs or live events.

## Consequences and validation

A focused first slice costs more than porting a few activity classes, but tests the actual product guarantee. The M1 exit is a real Studio-authored reservation workflow with a durable store and independent participant, backend process death during recovery, restart/resumption and accurate Studio inspection. M2–M5 expand uncertainty, composition, operations and compatibility qualification. No runtime implementation or proof is claimed by this ADR.

Core and Studio remain separate product/package responsibilities within one source repository. Preserve 3.x public APIs and persisted data; no maintenance/publisher cutover or release is authorized. Existing matching issues and mechanisms retain their owner, and no BPMN code/adapters/shared abstractions/dependencies are introduced.

## Revisit

[Design task #8896](https://github.com/elsa-workflows/elsa-core/issues/8896) records the reviewed adaptation proposal and accepted owner policies; it does not accept the physical architecture by itself. Core, persistence, Studio and security reviewers must complete the packet's E1–E3 gates before M1 tasks become ready. Revisit the proposal when source evidence invalidates an assumption; failed restart or disclosure evidence prevents milestone acceptance. Elsa 4 terminology coordination is advisory and never blocks Elsa 3 delivery.

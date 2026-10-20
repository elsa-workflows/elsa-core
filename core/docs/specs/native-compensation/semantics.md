# Proposed native compensation semantics

These are reviewable design choices, not implemented behavior or an accepted public API. O1 in [decisions](decisions.md) is accepted product policy; O2/O3 remain owner gates. The physical realization is still proposed and unproved. Identity/storage/commit terms are defined in [persistence](persistence.md); the canonical product requirements remain PRD #8886.

## Scope and operation model

A scope owns an ordered collection of steps. A step has stable authored StepId, mutable label, one forward child, optional handler and typed capture mappings. ScopeOccurrenceId identifies one activation. Each repeated step activation gets a distinct OccurrenceId and stable OperationKey. Forward and undo attempts are separately tracked; renaming/reordering a step or retrying it never changes an existing operation's identity.

A handler may be omitted only for a declared no-effect/pure step in the first profile (for example the definite rejection step). Publication and execution reject a recoverable external-effect step without a qualified handler/participant contract. NoHandler is a truthful non-obligation classification, never synthetic successful undo of an applied external effect. A missing required handler discovered after publication is intervention, not NoHandler. Wider no-compensation-required policies need explicit capability/owner acceptance before support.

Recommended adopted behavior: successful recovery emits outcome `Compensated`. An explicit `faultAfterCompensation` setting can propagate the original fault afterward. Propagation does not delete recovery evidence. A successfully completed forward scope remains eligible until confirmed; body completion and the release of its recovery obligation are different events.

## Durable states and transitions

Scope phases: Forward, CompletedEligible, Recovering, AwaitingIntervention, Compensated, Confirmed. Entry states: Armed, Applied, NoEffect, NoHandler, Uncertain, Compensating, CompensationFailed, Compensated, Confirmed. These are proposed domain terms, not additions to global WorkflowStatus/WorkflowSubStatus enums.

| Event / condition | Durable change | Scheduling and safety rule |
|---|---|---|
| Activate scope | Allocate ScopeOccurrenceId, pin definition, initialize journal revision and sequence | M1 has one serial child lane. |
| Prepare forward operation | Create Armed entry with opaque OperationKey, immutable resolved inputs, capture policy and exact handler pins | Persist entry and forward work identity; require acknowledged pre-effect boundary before invoking participant. |
| Known forward success | Append immutable receipt/result capture; Armed -> Applied, allocate CompletionOrdinal exactly once | Persist completion/capture and next work in one accepted transition. Only then advance forward cursor. |
| Contract proves no effect | Armed -> NoEffect; retain original failure | No recovery for that occurrence; recover prior Applied entries if policy triggers. A timeout is not this proof. |
| Result lost, crash at uncertain boundary or unclassified external fault | Armed -> Uncertain (or retain Armed pending classification) | No assumption that absence of a completion means absence of effect. Stop forward progress. M1 blocks; M2 resolves through supported participant contract. |
| Scope body completes | Forward -> CompletedEligible; retain entries/captures | Emit Done to parent without confirming or deleting obligations. O1 governs root finalization. |
| Owned fault (M1); explicit compensation targeting (M3) | Scope -> Recovering; record original cause, target and command identity | Stop new forward scheduling. Own the descendant fault through FaultSignal; do not duplicate middleware fault-count recovery or terminalize before durable handoff. |
| Select next known eligible entry | Applied -> Compensating and create one handler attempt/work identity | Persist reservation and handler schedule together; invoke with the stable UndoKey. |
| Handler acknowledged success | Compensating -> Compensated; retain receipt and attempt | Advance exactly once; scheduling alone never means success. |
| Handler fails or outcome is unknown | Record separate recovery error; CompensationFailed or unresolved Compensating/Uncertain | Stop the serial unwind. Keep original cause separate. Await retry/reconciliation/intervention; no parent success. |
| Safe retry | Append a fresh AttemptId under the same OccurrenceId, OperationKey/UndoKey | Legal only under declared idempotency/no-effect/reconciliation contract; revision and owner checks apply. |
| Last required handler settled | Recovering -> Compensated | Emit Compensated, then optionally re-raise original fault. Any unresolved entry prevents this transition. |
| Confirm target (minimal authored activity in M1; wider targeting/cascade in M3) | Applied/eligible scope -> Confirmed; record actor/cause | Idempotent, closes eligibility but retains audit. Authored confirmation records workflow actor kind, pinned definition/instance and trusted initiator provenance; it never fabricates a human operator. Refuse while recovery, uncertainty, failed compensation or in-flight effect exists. |
| Duplicate/stale result | No repeated phase/cursor advancement | Match occurrence, effect direction, attempt and revision; preserve stale evidence safely, reject stale writes. |
| Shutdown / lost execution ownership | No business confirmation or compensation transition | Stop dispatch; preserve committed work. Resume through accepted native runtime route using the same operation keys. |
| Business cancellation | Stop forward scheduling; preserve obligations and in-flight/unknown state | O2 defines the compensation-aware completion policy. Never interpret host cancellation as a request to undo business effects. |

M1 may display AwaitingIntervention without shipping operator mutation controls: no unsafe replay or synthetic success is permitted. Its effective wait/resumption mechanism must be accepted under E1; a blocking incident alone is not proof that the chosen incident strategy will keep recovery nonterminal.

## Ordering and targeting

Recovery is sequential in descending **durable completion-registration ordinal**, allocated by the scope on first known successful registration. It is not timestamp order, activity definition order or ExecutionCount. Duplicate completion cannot allocate a second ordinal. The scope also records arm sequence for in-flight operations; unresolved uncertain work blocks unwind until reconciliation, or until a proven compensate-if-present contract settles that occurrence. Never silently omit the most recently armed external operation.

The first profile is fully serial. Any later support for parallel forward effects must explicitly define serialized completion-registration order and its proof; automatic parallel compensation remains excluded. Explicit targeting uses ScopeOccurrenceId/OccurrenceId resolved inside the tenant and instance, never merely a label or “latest execution of node”. Authored nearest-scope targeting is resolved to a concrete retained scope occurrence before recording the request.

## Nested and repeated work (M3 contract)

Each loop iteration creates an occurrence with iteration ancestry and its own capture; retries of that iteration preserve its operation key. A parent treats a completed nested scope as one retained obligation referencing the inner journal. It unwinds inner entries newest first, waits until all inner obligations settle, then runs an optional explicitly authored outer handler. An uncertain/failed inner recovery blocks that outer handler. Ownership is recorded once; no flattening or double compensation.

Accepted O1 policy: confirming an outer scope recursively confirms only its owned, eligible descendant obligations in one accepted transition; refuse if any descendant is unresolved or in flight. Audit each affected scope identity. The owner accepted this cascade and normal workflow-end policy under O1; E1/E3 must still prove their realization before supported production work. A missing nested schema or unsupported composite is a publication/runtime error, not an empty child list.

Elsa 3's ancestor fault signals can cross ordinary containers; do not copy Elsa 4's Sequence patch. M3 must prove actual descendant ownership, callback withdrawal, active sibling behavior and nested fault counts for each accepted composite. Until then, unsupported compositions fail publication and execution capability validation.

## Confirmation, cancellation and terminal workflows

Accepted O1 policy: never implicitly confirm on scope completion. For the initial supported design, a workflow with unresolved or unconfirmed recoverable obligations reaches a durable nonterminal wait rather than silently losing eligibility at root finalization. Terminalization follows explicit confirmation or completed recovery. The exact root guard/bookmark/incident integration is E1. M1 must include a minimal authored Confirm activity after a successfully completed serial scope, resolving its authored reference to the exact retained ScopeOccurrenceId. This is not an operator ConfirmScope API or support for arbitrary targeting. The no-fault walkthrough is scope Done -> explicit Confirm -> root completion. For the bounded M1 serial profile, server publication and pre-effect capability validation reject a reachable successful exit without that Confirm (unsupported dynamic shapes are rejected). If an already-armed instance is altered/imported into an invalid shape, preserve a visible blocked wait; M1 has no supported operator release path. It requires a separately authorized repair or later accepted control, and a new instance does not settle the original obligation. Normal completion and confirmation cascade policy are accepted under O1. Implementation feasibility remains unproved under E1/E3; production work is still paused and no corresponding task becomes ready through this decision.

O2 recommendation: compensation-aware business cancellation stops forward work, settles or identifies in-flight uncertainty, then recovers eligible obligations before reporting Cancelled. Existing workflow behavior outside opted-in compensation scopes stays unchanged. A future privileged force-stop/abandon path must retain unresolved obligations and audit rather than claiming Compensated. Its product policy is deferred to M4; no force-success operation is designed.

A terminal legacy/imported workflow with retained unresolved recovery data is inspectable but must not be silently reopened through ordinary resume. Recovery of already-terminal instances requires a separately accepted execution/lifetime policy before it can be advertised; it is outside M1. This does not weaken the requirement that a completed scope inside a still-live workflow retains its obligation.

## Participant and late-result rules

External delivery is at least once. Forward/undo APIs need stable keys and durable participant-side deduplication or a stronger explicitly documented contract. A lookup returning “absent” is insufficient if an old in-flight forward call can still commit afterward. The reference participant records a closed/tombstoned operation atomically with undo and refuses a delayed forward call for that operation. Otherwise uncertainty requires reconciliation/intervention.

Late success appends evidence to the same occurrence; it never restarts the forward path. If undo already succeeded, the participant's closed-key rule prevents resurrection. Unexpected effect after confirmation or a contradictory receipt is a visible intervention case, not an automatic rewrite of history. Reconciliation requires evidence appropriate to the participant; an operator assertion is not an external-effect proof.

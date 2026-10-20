# Elsa 3 Studio compensation interaction design

Status: proposed design for [#8896](https://github.com/elsa-workflows/elsa-core/issues/8896), 20 October 2026. Backend source, semantics, persistence, security and remaining decisions live in the [Core packet](../../../../core/docs/specs/native-compensation/README.md). Only this design task is active; production remains paused. Follow [Studio guidance](../../../src/AGENTS.md) and the [design system](../../design/system.md).

## Elsa 3 implementation boundary

Use Blazor components in focused Studio modules and existing activity descriptor/input/output editors. The StateMachine custom designer, embedded-activity resolver and validation are the precedent for owned forward/handler children; X6 activity-port providers map diagram connections, not paired composite slots. Extend the owning Workflows module through public contracts and notifications; do not reach into unrelated module internals. Preserve existing package/activity/serialization identities and supported Server/WebAssembly boundaries.

The Elsa 4 paired-step semantics are reused, not its React/Vite components, structure facets, `enterSlotScope` APIs or runtime projections. E3 in the Core decision register must verify native child traversal/serialization and exact extension placement before code is ready.

## Authoring — sequential

```text
Order fulfilment > Compensation scope: Reserve and recover
Steps run top to bottom. Recovery undoes eligible occurrences newest first.

#  Operation              Forward             Compensation          Capture
1  Reserve inventory      [Reserve inventory] [Release inventory]   [reservationId -> targetId]
2  Reserve delivery       [Reserve delivery]  [Cancel delivery]     [deliveryId -> targetId]
3  Reject order           [Reject order]      [Add handler]         —

[Add step]     On recovery: [Finish Compensated | Propagate original fault]
```

Each row carries stable opaque StepId, separate editable label and stable MappingIds. Renaming/reordering never changes existing runtime identity. Delete/recreate creates a new StepId. Node/activity IDs identify child locations, not logical external operations; one StepId can produce many occurrences and OperationKeys. Draft JSON contains native child activity definitions plus stable IDs/mappings; exact serialized field/public names are not frozen here.

Row actions: rename, named Move up/Move down controls for reorder, open forward activity, open/edit/remove handler, open capture mapping and remove step after confirmation. Breadcrumbs identify scope, step label and role. Do not use ordinary flow lines to imply that a handler executes on the successful forward path.

The M1 no-fault flow also has a minimal authored Confirm activity after scope Done, with an authored scope reference resolved to the exact retained occurrence. It closes eligible obligations before root completion under accepted O1 policy; runtime/Studio realization remains unproved. The bounded M1 validator rejects a reachable successful exit that omits it, at publication and again before effects. An already-armed invalid/altered instance remains visibly blocked with no supported M1 operator release path; it needs a separately authorized repair or later control. It cannot silently report workflow success. Wider targeting/cascade and operator confirmation remain later milestones.

## Captured-data mapping

```text
Release inventory — required input targetId
Source: [Captured output of THIS Reserve inventory occurrence: reservationId]
Type: String   Capture schema: 1   Target: targetId
[Validate mapping]
```

Show paired descriptor-backed outputs first. A mapping binds stable source-output/target-input descriptor identities, type, schema and any explicit supported converter identity/settings. Capture runtime values under OccurrenceId when that forward result commits; snapshot required resolved request values before the effect. These are separate panels: Request capture is frozen at Armed; Result capture is appended with Applied and completion ordinal. After a lost response, request/keys/pins remain available but reservationId/result displays Unknown, not null or a guessed value. A required handler output binding stays blocked until supported lookup/deduplicated replay recovers and commits it. “Captured” must never mean reading current workflow variables or latest output by node at handler execution time. M1 accepts captures/literals for required recovery targets. Any later recovery-time expression is visibly labelled and cannot replace required identity/target captures.

Output/receipt sensitivity is fail-closed under the recovery policy; today's output descriptors alone cannot classify it. Source suppression/redaction must also protect generic execution/log records (E2). Missing/unavailable/redacted values are distinct from null. Show capture policy and safe previews only to authorized users; secrets are not rendered in the ordinary editor/run data panel. Publication cannot promise a captured output that the participant contract does not produce, or a converter whose version/type is unavailable.

## Nested and loop examples (M3 notation, not M1 support)

```text
2  Delivery [Nested scope: Delivery]   Undo inner obligations, then optional outer handler
1  For each item: Reserve item         3 occurrences; each has its own captured values
```

Expand a nested scope's actual occurrence/owned entries. Unresolved inner recovery blocks the outer handler. Loop execution groups the authored row but lists concrete occurrences and iteration ancestry, each with separate operation key, capture and attempt history. Recovery order is the persisted completion ordinal; do not sort by label or browser clock. Selecting one iteration must not change the displayed capture to the most recent iteration. Unsupported nesting/loop shapes remain preserved in drafts and blocked at publication/execution until the backend advertises support.

## Publication and runtime validation

Core validation is authoritative; Studio maps structured server diagnostics back to stable targets. Current publish responses flatten errors to messages and Studio ValidationError is message-only. Propose an additive per-diagnostic wire entry with `code`, `message`, stable `target` and `relatedIds`, alongside existing message compatibility; precise DTO naming is E2/E3. Never correlate one top-level extension to many errors by array index. An older server shows the message in an accessible summary without inventing a target; capability absence disables unsupported publication/actions and preserves the draft. Proposed target paths are `/scopes/{scopeNodeId}/steps/{stepId}/forward`, `/handler` and `/captures/{mappingId}`, with error code and related IDs. Row indexes are display hints only.

Block publication for duplicate/missing IDs, missing forward children, effectful steps without qualified handlers, a reachable M1 successful exit without exact-scope confirmation, invalid handler pins, unavailable types/schema/converters, invalid capture types/required inputs, unsupported effects/profiles/compositions and nonresolvable targets. Incomplete drafts can be saved and round-tripped without dropping unknown children. Unsupported draft schema remains readable/preserved with editing limitations; it is not published as an empty scope. Server execution rechecks mandatory safety capability even if validation was globally disabled or publication bypassed Studio.

Selecting a diagnostic focuses the correct step/control. Examples: “Reserve inventory needs a forward activity”; “Captured reservationId is unavailable for targetId”; “This backend has not qualified recovery for the selected persistence profile.” Do not hide a pre-effect safety requirement as an optional warning deferred to release polish.

## Execution inspection

```text
Scope Reserve and recover / occurrence sc-7 — Awaiting intervention
Original cause: Reject order failed [Show incident]

Operation / occurrence        Forward       Recovery          Attempts / current state
Reserve inventory / occ-1     Applied       Release inventory 1 / Pending
Reserve delivery / occ-2      Applied       Cancel delivery   2 / Failed — needs action
[Open occurrence] [Open attempt history] [Recovery actions, if authorized]
```

The server recovery projection, not diagram color/latest execution per node, supplies obligation state and stable forward-to-handler links. Existing execution record IDs identify protected/redacted drill-down projections annotated with safe scope/occurrence/attempt role and causal links. Do not enable ordinary generic details until server-side authorization/redaction satisfies the same policy. Display original failure separately from every recovery error. Show pinned handler versions and capture schema, with unavailable data explicit. Never equate queued/started work with Compensated.

The projection has separate dimensions: `scopePhase` (Forward/Recovering/AwaitingIntervention/etc.), `forwardOutcome` (Pending/Running/Applied/NoEffect/Failed/Uncertain), `obligationState` (Eligible/NoHandler/Confirmed/Compensating/Compensated/Unresolved), `attemptOutcome` (NotRun/Pending/Running/Succeeded/Failed/Uncertain), and `dataAvailability` (Available/NotLoaded/Redacted/MissingVersion/UnknownSchema). These are proposed wire concepts, not new global runtime enums. Compose text/icons from them, for example “Forward applied; recovery failed; intervention required”; distinguish forward failure from undo failure and data unavailable from an uncertain external effect. Paged history uses a bounded server cursor; “more not loaded” is not “no more attempts”. Finished-scope inspection works from the retained journal even if its runtime child context or ordinary logs were pruned.

## Recovery action interaction

An action dialog identifies instance, exact scope/occurrence/attempt, current state/revision, consequences and a required reason. The server returns allowed actions based on capability, permission and state; it independently checks all authority on submission. Proposed commands and audit rules are in the [recovery API contract](../../../../core/docs/specs/native-compensation/recovery-api.md). Use backend API clients through the existing provider, not a generic anonymous bookmark link or a browser-owned mutation of workflow state.

A stale revision produces an explicit conflict and refetch; it must not silently retry against a newly selected occurrence. Retrying the same submitted action retains CommandId. Pending action state, reconnect and asynchronous completion are visible. Unknown schema/version/capability or missing protection key disables mutation and offers diagnostic guidance without leaking payloads. Manual force-success is absent; any future resolution/abandonment requires its own policy/evidence.

Accepted O2 adds an explicit Cancel and compensate journey at the planned cancellation/operator milestones. Distinguish request accepted, recovery in progress, uncertain/needs intervention, and completed cancellation using text/icons; request acceptance is not Cancelled. Preserve the cancellation reason separately from handler failure. Repeated submission retains command identity and joins existing progress. Confirmed effects stay in place; a deadline never silently abandons remaining obligations. Offer the action only with the required cancellation/recovery authority; ordinary cancel permission is insufficient. Unqualified backends must not expose it as working native cancellation. Host shutdown/reconnect resumes committed state without creating cancellation intent. This policy acceptance does not ship components or advance M1 readiness.

## Accessibility, capabilities and environments

Use semantic tables/lists and text/icons, never color alone. Accessible row/control names identify label, occurrence and action: “Open compensation for Reserve inventory, occurrence 2”. Arrow keys navigate rows; reordering uses named Move up/Move down controls and announces the new position; Tab traverses forward/handler/capture controls; Enter opens; Delete confirms. Restore focus after dialogs/navigation; announce validation and material status changes without flooding live regions. Cover narrow screens, long labels and large histories.

Probe optional recovery capability/version and supported profile/composition matrix. Unknown or unavailable capability cannot enable actions. Distinguish loading, empty, unauthorized, disconnected, stale and unsupported states. On backend/tenant/environment changes, cancel in-flight requests, dispose subscriptions, clear selected occurrence/actions/cached protected details and refetch capabilities; cache keys include backend and trusted tenant identity. A response for the old environment cannot populate the new view. Supported Server and WebAssembly hosts must both use the same authorized contract.

## Verification responsibilities

Reuse existing client/component tests at the lowest useful level and add paired browser coverage where the infrastructure supports it. V1/V4/V5a–c/V7 in the [qualification plan](../../../../core/docs/specs/native-compensation/verification.md) define the actual integrated journey, permission/disclosure negatives, action races and accessibility states. Wireframes are review artifacts; no browser or backend capability is demonstrated by this document. Studio plus backend review is required before a first production leaf is Ready.

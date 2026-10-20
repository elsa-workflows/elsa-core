# Qualification plan and design validation

This is a plan, not a test report. No implementation, .NET/browser run or restart proof is delivered by #8896. Candidate provider/host support remains unproved. The design can pass source/scenario/document review while production readiness remains blocked by E1–E3 and remaining owner decisions O2/O3. O1 policy is accepted, with its runtime behavior still to be proved.

## Common real-journey fixture

Use a real supported Studio host/browser and the candidate local Elsa runtime against a persistent database. A separate participant owns a separate durable ledger, with keyed Reserve, Undo and Lookup operations. Its ledger records unique effect counts, receipts, delayed-request refusal and operation closure. No transaction or in-memory fixture is shared with Elsa's state. Use deterministic injection barriers at actual pre/post commit/effect boundaries, never sleeps as correctness synchronization.

Record exact source head, host/provider/commit configuration, workflow and handler versions, tenant, scope/occurrence/operation/attempt IDs, process IDs and kill points, persisted Elsa revisions, participant receipts, UI/API state and sanitized accessibility evidence. Restart a new backend process against the same database and persisted protection keys, then reconnect Studio. A same-process reload or mocked client does not satisfy the gate.

## Scenarios and negative controls

| ID | Demonstration | Required result / negative control |
|---|---|---|
| V1 | Author two reservation steps and paired handlers, map captured IDs, add later definite rejection, publish/run/inspect through Studio without JSON | Reverse known completion order; correct original cause; occurrence-to-attempt links; both participant reservations released once. Invalid mapping navigates to its stable step/mapping target and server rejects publication. |
| V2a | Kill before the Armed commit acknowledges | No external call is allowed. Unknown local commit outcome reloads/reconciles with the same operation identity. |
| V2b | Kill after Armed commit before forward body | Restored work uses the same OperationKey; exactly one logical participant effect. |
| V2c | Participant commits, response is lost / kill before result commit | Preserve uncertainty; keyed retry/lookup cannot duplicate effect or create a second obligation. M1 blocks if participant cannot safely settle it. |
| V2d | Kill after Compensating + specific handler work commit, before undo | Same UndoKey/target/capture and exactly one reserved handler progression. |
| V2e | Participant undo commits, kill before local receipt commit; also kill between handlers | Replay deduplicates by UndoKey; no leaked/skipped obligation and no duplicate successful undo. |
| V3 | Redeliver forward/undo completion, stale attempt result and command; race two owners | Cursor/ordinal advances once, keys remain stable, stale-owner commit is rejected. Remove/disable the protection in a controlled negative fixture and observe the expected risk; do not count a no-op fixture as proof. |
| V4 | Republish workflow/handler while recovery is interrupted; remove pinned version/capture, handler work node or callback owner/child, change schema or migrate the definition; use another tenant/unauthorized identity | Old instance uses exact pins/capture or blocks visibly; never falls back to latest/null. No cross-tenant listing/detail/mutation leakage; validation and execution checks work without Studio. |
| V5a (M1) | Handler fails or an effect result is unknown | Original cause and recovery failure remain separate; scope stays nonterminal/unresolved; safe inspection distinguishes failure/uncertainty. No operator mutation controls, synthetic success or unsafe replay; unauthorized reads are denied. |
| V5b (M2) | Supported retry/reconciliation, unknown result and later forward response | Attempts keep the same keys/captures; closed participant key rejects resurrection. Removing participant dedupe exposes the duplicate-effect control case. |
| V5c (M4) | Operator action, repeated request, stale revision and denied authority | Capability/permission/revision/state are server checked; CommandId deduplication and audit survive restart; no cross-tenant action/disclosure. |
| V6 | M3 loop, nested completed scope, confirmation cascade, cancellation and shutdown | Distinct repeated occurrences/captures; inner-before-outer ordering; no double recovery; no confirmation of unresolved work; O1/O2 outcomes; shutdown resumes instead of cancelling business work. Include scheduler queued-sibling/interrupted-context cases before widening serial support. |
| V7 | Keyboard/screen reader; empty, long, large/paged, redacted, disconnected, unauthorized, stale and unsupported backend; tenant/environment switch | Text/icon state meaning, focus recovery and live announcements; NotLoaded differs from Unknown; stale state clears; no unauthorized action/disclosure or unbounded history loading. |
| V8a (M1) | Current-profile codec/store/restart round trip; delete/import/alteration attempts on active obligations; unknown schema; finished child/log cleanup | Keys/pins/captures/work/callbacks survive new-process decode. Destructive mutation is refused, not allowed to drop obligations; unknown schema/missing refs preserve state and block before omission/effects. Finished-scope inspection still works. No production schema migration/upcast is supported yet. |
| V8b (M5) | Advertised schema upgrade/upcaster, retention/purge and provider matrix | Every advertised upgrade preserves/reconciles identities and pending obligations; accepted retention never purges eligible/unresolved state; completed purge is audited. Qualify actual supported versions/providers rather than extrapolating from V8a. |

V3 command-specific redelivery, V4 mutation negatives and V7 operator-action checks apply when their M2–M4 entrypoints are delivered. M1 exercises native state/attempt deduplication, denied inspection and publication/execution validation; it does not need later operator APIs. V8a is the exact M1 baseline; V8b remains M5 upgrade/retention qualification.

For provider or mode support, repeat the material crash/security matrix for the advertised configuration. Do not infer every provider or Blazor host is qualified from one successful run. A failed demonstration keeps its milestone open and identifies the corrective work; issue closure and compilation are not proof.

## M0 design review walkthroughs

- Sequential: arm inventory, acknowledge/capture it, arm delivery, acknowledge/capture it, definite rejection, reserve delivery undo then inventory undo; each P0–P6 boundary has an explicit stored/participant state.
- No fault (M1, accepted O1; O3 still proposed): scope Done retains obligations; minimal authored Confirm resolves the exact serial scope occurrence, records confirmation and permits root completion. Without Confirm, publication and pre-effect execution reject the invalid shape. For an already-armed altered/imported invalid instance, preserve a blocked wait and explain the lack of an M1 operator release path. Authored confirmation audits the workflow actor and trusted initiator provenance. Test all paths before M1 support.
- Unknown: lose delivery response; do not omit delivery because no Completed event exists. Same-key lookup/closed-key participant rule settles it, otherwise stop without reporting Compensated.
- Repeated/nested: two iterations of the same authored StepId retain distinct OperationKeys/captures. A completed inner scope's journal stays addressable from the parent even after its context is pruned; confirm/recover affects exact ownership.
- Safety: missing handler pin cannot use ordinary Published/Latest fallback; invalid profile cannot reach effect code; an operator request without correct tenant/permission/revision/reason cannot reserve work.

Review against PRD FR1–FR11, evidence links and the physical proof gates. Record reviewer findings at a specific document/line, fix coherent findings, and repeat on the final candidate. Runtime/persistence, Studio/accessibility, security and program acceptance are separate review lenses. Proposed product policies must remain marked Proposed after technical review until the owner accepts them.

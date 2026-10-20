# Decisions, remaining gates and progressive delivery

## Settled task authority and proposed architecture

The owner granted [standing authorization to lift all current and future delivery holds](https://github.com/elsa-workflows/elsa-core/issues/8884#issuecomment-6303225186) on 21 October 2026. Root lead owns end-to-end integration/review and may schedule experiments, setup and production delivery through actual readiness gates without another hold-lifting request. No M1 task is Ready before E1–E3 evidence. The canonical PRD stays in #8886. Elsa 4 review is available, so this task adapts its intent rather than repeating an independent product exploration. Delivery does not depend on Elsa 4 M5.

Selected technical direction for this design review: versioned root instance journal; serial native scope first; explicit stable operation/undo keys; immutable protected occurrence captures; strict pinned versions; mandatory acknowledged pre-effect intent; atomic reservation with serialized handler work; safe projection and authorized audited commands over native execution services. [ADR](../../../../docs/adr/2026-10-20-native-compensation-instance-journal.md) remains Proposed. These choices do not claim current code already supplies the guarantee.

## Accepted owner decision O1 — explicit confirmation

**Status: Accepted by Sipke on 20 October 2026.** [Program decision record](https://github.com/elsa-workflows/elsa-core/issues/8884#issuecomment-6301154596). Owner instruction: “Adopt option C with those rules for O1. Record the decision in the design packet and program, keep production implementation paused, and walk me through O2 next.”

1. Successful scope completion preserves compensation eligibility. Done and Confirmed are different events; neither scope completion nor successful workflow completion implicitly confirms.
2. Confirm resolves one exact ScopeOccurrenceId, including for repeated scope activations. It ends eligibility without undoing effects, deleting captures/history, or implicitly calling a participant's business-confirm API.
3. Confirming an outer scope includes only its owned eligible descendants in one accepted transition. Reject the entire request if any affected obligation is uncertain, in-flight, recovering or has failed compensation; no partial confirmation. Settled descendant history is preserved.
4. Confirmation is durable and idempotent, preserves audit/provenance and remains subject to the ownership/revision/protection protocol.
5. The root cannot terminalize with outstanding eligible or unresolved obligations. Explicit confirmation or completed recovery settles them; otherwise retain a visible durable nonterminal wait.

Option C was chosen over confirming at scope end (closes the recovery window too early), implicitly confirming at workflow end (uses technical completion as business commitment), and terminal completion with later recovery eligibility (requires a separate lifetime/execution policy, outside M1).

M1 includes minimal authored exact-scope Confirm and bounded publication/pre-effect validation. Already-armed invalid/altered instances remain blocked and have no supported M1 operator release path; a separately authorized repair or later accepted control is required. Wider targeting/cascade remains M3. Runtime/persistence and Studio reviewers own technical verification under E1/E3; O1 policy acceptance does not prove those mechanisms or authorize their implementation. Recovery of already-terminal legacy instances remains outside M1.

## Accepted owner decision O2 — compensation-aware cancellation

**Status: Accepted by Sipke on 20 October 2026.** [Program decision record](https://github.com/elsa-workflows/elsa-core/issues/8884#issuecomment-6301313484). Owner instruction: “Adopt option C with those rules for O2. Record the decision in the design packet and program, keep production implementation paused, and walk me through O3 next.”

1. Persist cancellation intent and stop scheduling new forward operations. Accepting the request is distinct from completing cancellation; do not erase armed/in-flight work or recovery progress.
2. Settle in-flight external effects using the supported participant contract. Provable no-effect, known success and uncertain outcomes remain distinct; a timeout is not no-effect proof. Unknown outcomes wait for safe reconciliation before ordered recovery proceeds.
3. Recover remaining eligible obligations in the defined order, with Cancelled reported only after required obligations settle. Cancellation never implicitly confirms; already-confirmed effects remain in place.
4. Compensation failures preserve the cancellation reason and separate recovery errors. Apply bounded retry policy, then visible intervention. No elapsed deadline or exhausted retry budget silently abandons obligations or manufactures Cancelled.
5. Duplicate cancellation requests do not start another unwind. Durable cancellation intent/progress and stable operation/undo keys survive interruption and restart.
6. Host shutdown only interrupts/resumes committed work; it does not request business cancellation. Preserve existing behavior outside opted-in compensation workflows.
7. Studio clearly presents Cancel and compensate with action-specific authority. Ordinary cancel permission alone cannot silently grant compensation authority. Exact routes/permission mapping remain E2 work, without weakening this rule.
8. Privileged force-stop/abandon is a separate later policy; no force-success or automatic deadline escape is accepted.

Option C was chosen over rejecting cancellation until explicit settlement, and stopping forward work while leaving settlement to a separate decision. The trade-off is that cancellation may remain pending for reconciliation or intervention. Cancellation composition remains M3 and operator UI M4; acceptance does not pull them into M1 or grant implementation readiness. E1–E3 must prove cancellation ordering, preservation of recovery work, restart, disclosure and authority before support. The first candidate cannot advertise cancel-triggered recovery before those gates pass.

## Accepted owner decision O3 — explicit recovery outcome and fault propagation

**Status: Accepted by Sipke on 20 October 2026.** [Program decision record](https://github.com/elsa-workflows/elsa-core/issues/8884#issuecomment-6302344070). Owner instruction: “Adopt option C with those rules for O3. Record the decision in the design packet and program, keep production implementation paused, and walk me through the remaining technical gates next.”

1. Persist successful settlement as Compensated before outward completion/fault signalling. Forward failure and successful recovery remain distinct facts.
2. Default: emit the distinct Compensated outcome. A per-scope explicit opt-in instead propagates the original fault into the native parent/fault-handling path, retaining Compensated recovery history.
3. Select exactly one outward path: the Compensated continuation or original-fault propagation. Never execute the continuation and then also raise the fault. The parent's/native incident policy determines the eventual workflow state when a fault propagates.
4. Preserve the original cause separately from recovery errors in both modes. Failed, pending or uncertain recovery stays unresolved; neither mode can prematurely report successful compensation.
5. O2 cancellation remains Cancelled after required settlement; do not manufacture an original fault for cancellation. The propagation setting applies to fault-triggered recovery: explicit non-cancellation compensation without an original fault uses the Compensated path, even when that setting is enabled; cancellation follows O2. No fault is invented.
6. Pin the chosen behavior with the published workflow version; republishing cannot change an interrupted instance. Public setting/DTO names remain unfrozen.
7. Studio presents After successful recovery: Continue with Compensated (default) | Propagate the original fault, alongside the paired forward/handler authoring. Inspection keeps forward and recovery results separate, including successful recovery followed by original-fault propagation.

Option C was chosen over always propagating the fault (inflexible for expected business rejection) and always emitting Compensated (requires separately authored propagation where callers need the original error). The trade-off is a visible setting and qualification of both behaviors. E1/E3 must prove durable handoff selection, restart and native fault/count/callback integration without duplicate continuation/propagation; O3 acceptance does not establish those guarantees.

## Owner policy resolution and delivery authority

O1, O2 and O3 are accepted product policy; no remaining owner-policy choice is identified for the currently defined packet. E1–E3 below are unproved technical gates, not implicit implementation authorization. The physical architecture ADR remains Proposed. The 21 October standing authorization supersedes previous owner pauses for production, spikes, #8891–#8895 and setup #8885/PR #8897. Technical dependencies, required review and verification still control their execution. No commercial, release or deployment policy is changed. New evidence that conflicts with accepted policy must return to the owner rather than silently weakening it.

## Technical evidence gates (bounded proposals, no new ready tasks)

| ID | Question and required output | Owner / revisit / consequence |
|---|---|---|
| E1 | Prove candidate local-runtime/EF SQLite serial profile: root journal plus specific scheduled work, mandatory commit after resolved inputs and before effect, required store participation, durable acknowledgement/unknown-commit recovery, effective ownership + commit-time stale-writer fence, abrupt restart route, map-codec round trip, restore-reference admission and nonterminal intervention/finalization and the single selected post-recovery handoff. Prove fail-closed outer snapshot hydration before any decode fallback as well as inner journal admission; preserve original bytes. Inspect actual hooks and run bounded controlled-participant spike #8930 under the standing authorization after design acceptance. If unsupported, recommend a different provider or additive native persistence/invocation seam with a revised ADR. | Runtime + persistence reviewers, program lead accountable. Required before any M1 task is Ready. A named candidate is not a supported provider claim. |
| E2 | Map protected inline capture and safe summary/detail projection to existing host protection, key retention, tenant authorization, sensitive-output/log source suppression, command dedup/audit and deletion/import/alteration guards. Freeze minimum caller-required public DTO/permission names and additive compatibility after review. | Security + Core/Studio reviewers, before M1 baseline and before later operator tasks. No generic state/diagnostic bypass. |
| E3 | Prove native authored-child traversal/serialization and stable step/mapping resolution for paired Blazor authoring; settle exact callbacks/invocation identity propagation, strict version resolver, both O3 outcome/fault paths, per-error publish diagnostics and supported composite matrix. Verify keyboard/component/browser test placement and supported hosts. | Studio + runtime reviewers, before M1 refinement. Full loop/nesting tests precede M3 readiness. |

No unresolved gate is delegated as product intent for a production worker to guess. #8896 may finish with these precise source/evidence questions because its scope expressly allows proposing a bounded spike instead of claiming an unproved guarantee. The next executable evidence leaf is spike #8930 after design acceptance. Existing feature #8891 stays dependency-blocked / Not Ready until E1–E3 pass; the lead then refines only the next coherent production leaf. Escalate a new material product/security/support choice, rather than asking again to lift a delivery hold.

## Progressive contract

| Stage | Minimum integrated outcome | Support boundary / acceptance |
|---|---|---|
| M0 (#8896) | Reviewed Elsa 3 design with proposed policies, source adaptation, paired Studio scenarios and gates | This packet; O1–O3 accepted; E1–E3 stay explicit. No source/schema/API implementation. |
| M1 (#8891) | Small visually authored serial reservation/rejection/recovery, minimal authored confirmation for the no-fault path, immutable capture/pins and accurate safe inspection | One proven host/provider/participant profile; V1/V1b, V2a–e, V3, V4 and V5a; baseline V7 inspection/accessibility controls and V8a preservation/destructive-mutation guards. O1–O3 policy is accepted; technical gates, both O3 handoff modes and a no-fault confirmation walkthrough remain required. Unknown/failed recovery blocks visibly even before full reconciliation/operator UX. No arbitrary activities, parallel/loop/nested/background siblings or broad provider claim. |
| M2 (#8892) | Explicit uncertainty, safe keyed reconciliation/retry and intervention | Same keys across automatic/operator retries; participant closed-key late-result rule. Durable wait and resumption, original/recovery causes distinct. |
| M3 (#8893) | Wider explicit compensation/confirmation targeting and late completed-scope compensation, nested/loop identity and cancellation composition | Retention model is chosen now; execute wider behavior under accepted O1–O3 after technical gates and V6. Unsupported combinations rejected. |
| M4 (#8894) | Full accessible operator control and hardened recovery history | Server action/tenant/revision/dedup/audit/disclosure proof; no force-success toggle. Baseline protection already applies at M1. |
| M5 (#8895) | Qualified supported matrix, upgrade/retention, samples/runbook and release readiness | V1–V8 as applicable, exact-candidate review/checks and independent participant evidence. Release/deploy requires separate authority. |

## Requirements gate result

Design readiness: satisfied for #8896 by the owner's bounded authorization, available Program/PRD/reference and this task's explicit acceptance. Production readiness: **not satisfied**. Evidence gates, owner policies and the final review/acceptance prevent premature claims. After PR review, reconcile Program/Project from actual head and issue state; do not mark a product milestone Passed or infer downstream readiness because a design document merged.

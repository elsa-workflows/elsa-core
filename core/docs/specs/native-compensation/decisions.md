# Decisions, remaining gates and progressive delivery

## Settled task authority and proposed architecture

The owner authorizes #8896 only. Root lead owns integration/review; no implementation or spike execution starts, no M1 task is promoted, and #8897 remains held. The canonical PRD stays in #8886. Elsa 4 review is available, so this task adapts its intent rather than repeating an independent product exploration. Delivery does not depend on Elsa 4 M5.

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

## Remaining owner decisions

| ID | Concrete recommendation and trade-off | Accountable owner / revisit |
|---|---|---|
| O2 | A compensation-aware business cancel stops forward work and reaches Cancelled only after required recovery settles; unknown/in-flight work waits. Host shutdown only interrupts/resumes. This preserves obligations but may delay a requested cancel; a privileged force-stop/abandon policy is a later separate choice. | Sipke, before any cancellation-enabled production task. Until accepted/proved, first candidate cannot advertise cancel-triggered recovery. |
| O3 | Successful recovery emits Compensated; an explicit option can propagate the original fault afterward while retaining the recovery evidence. Paired forward/handler authoring is the recommended interaction. This makes recovery completion distinct from normal forward success, with a deliberate failure-propagation choice. | Sipke, before M1 readiness and public activity/outcome contracts. Elsa 4 policy acceptance is not inherited. |

O2/O3 remain recommendations for owner acceptance. O1 alone is accepted; the physical architecture ADR remains Proposed and E1–E3 remain unproved. Production implementation, spikes, #8891–#8895 and setup #8885/PR #8897 remain paused. No commercial, release or deployment policy is changed.

## Technical evidence gates (bounded proposals, no new ready tasks)

| ID | Question and required output | Owner / revisit / consequence |
|---|---|---|
| E1 | Prove candidate local-runtime/EF SQLite serial profile: root journal plus specific scheduled work, mandatory commit after resolved inputs and before effect, required store participation, durable acknowledgement/unknown-commit recovery, effective ownership + commit-time stale-writer fence, abrupt restart route, map-codec round trip, restore-reference admission and nonterminal intervention/finalization. Inspect actual hooks and run a bounded controlled-participant spike only after separate authorization. If unsupported, recommend a different provider or additive native persistence/invocation seam with a revised ADR. | Runtime + persistence reviewers, program lead accountable. Required before any M1 task is Ready. A named candidate is not a supported provider claim. |
| E2 | Map protected inline capture and safe summary/detail projection to existing host protection, key retention, tenant authorization, sensitive-output/log source suppression, command dedup/audit and deletion/import/alteration guards. Freeze minimum caller-required public DTO/permission names and additive compatibility after review. | Security + Core/Studio reviewers, before M1 baseline and before later operator tasks. No generic state/diagnostic bypass. |
| E3 | Prove native authored-child traversal/serialization and stable step/mapping resolution for paired Blazor authoring; settle exact callbacks/invocation identity propagation, strict version resolver, per-error publish diagnostics and supported composite matrix. Verify keyboard/component/browser test placement and supported hosts. | Studio + runtime reviewers, before M1 refinement. Full loop/nesting tests precede M3 readiness. |

No unresolved gate is delegated as product intent for a production worker to guess. #8896 may finish with these precise source/evidence questions because its scope expressly allows proposing a bounded spike instead of claiming an unproved guarantee. The next implementation-ready leaf does not yet exist; return to the owner after design acceptance to authorize the next bounded evidence/implementation scope. Existing feature #8891 stays Paused / Not Ready.

## Progressive contract

| Stage | Minimum integrated outcome | Support boundary / acceptance |
|---|---|---|
| M0 (#8896) | Reviewed Elsa 3 design with proposed policies, source adaptation, paired Studio scenarios and gates | This packet; O1 accepted; O2/O3 and E1–E3 stay explicit. No source/schema/API implementation. |
| M1 (#8891) | Small visually authored serial reservation/rejection/recovery, minimal authored confirmation for the no-fault path, immutable capture/pins and accurate safe inspection | One proven host/provider/participant profile; V1, V2a–e, V3, V4 and V5a; baseline V7 inspection/accessibility controls and V8a preservation/destructive-mutation guards. O1 is accepted; O3 acceptance and a no-fault confirmation walkthrough remain required. Unknown/failed recovery blocks visibly even before full reconciliation/operator UX. No arbitrary activities, parallel/loop/nested/background siblings or broad provider claim. |
| M2 (#8892) | Explicit uncertainty, safe keyed reconciliation/retry and intervention | Same keys across automatic/operator retries; participant closed-key late-result rule. Durable wait and resumption, original/recovery causes distinct. |
| M3 (#8893) | Wider explicit compensation/confirmation targeting and late completed-scope compensation, nested/loop identity and cancellation composition | Retention model is chosen now; execute wider behavior under accepted O1, after O2/O3 acceptance and V6. Unsupported combinations rejected. |
| M4 (#8894) | Full accessible operator control and hardened recovery history | Server action/tenant/revision/dedup/audit/disclosure proof; no force-success toggle. Baseline protection already applies at M1. |
| M5 (#8895) | Qualified supported matrix, upgrade/retention, samples/runbook and release readiness | V1–V8 as applicable, exact-candidate review/checks and independent participant evidence. Release/deploy requires separate authority. |

## Requirements gate result

Design readiness: satisfied for #8896 by the owner's bounded authorization, available Program/PRD/reference and this task's explicit acceptance. Production readiness: **not satisfied**. Evidence gates, owner policies and the final review/acceptance prevent premature claims. After PR review, reconcile Program/Project from actual head and issue state; do not mark a product milestone Passed or remove downstream pauses because a design document merged.

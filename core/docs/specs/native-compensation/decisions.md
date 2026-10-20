# Proposed decisions, remaining gates and progressive delivery

## Settled task authority and proposed architecture

The owner authorizes #8896 only. Root lead owns integration/review; no implementation or spike execution starts, no M1 task is promoted, and #8897 remains held. The canonical PRD stays in #8886. Elsa 4 review is available, so this task adapts its intent rather than repeating an independent product exploration. Delivery does not depend on Elsa 4 M5.

Selected technical direction for this design review: versioned root instance journal; serial native scope first; explicit stable operation/undo keys; immutable protected occurrence captures; strict pinned versions; mandatory acknowledged pre-effect intent; atomic reservation with serialized handler work; safe projection and authorized audited commands over native execution services. [ADR](../../../../docs/adr/2026-10-20-native-compensation-instance-journal.md) remains Proposed. These choices do not claim current code already supplies the guarantee.

## Remaining owner decisions

| ID | Concrete recommendation and trade-off | Accountable owner / revisit |
|---|---|---|
| O1 | No implicit confirmation at scope completion. Confirming an outer scope recursively closes only its owned eligible descendants and is refused for unresolved work. A workflow cannot silently terminalize with required/unconfirmed obligations: retain a nonterminal wait. This preserves eligibility, at the cost of explicit confirmation/lifetime handling. Recovery of an already-terminal legacy instance is outside M1. | Sipke, before M1 readiness for minimal explicit confirmation/root finalization, then M3 cascade/composition. Technical feasibility belongs to E1. |
| O2 | A compensation-aware business cancel stops forward work and reaches Cancelled only after required recovery settles; unknown/in-flight work waits. Host shutdown only interrupts/resumes. This is safer for obligations but may delay a requested cancel; a privileged force-stop/abandon policy is a later separate choice. | Sipke, before any cancellation-enabled production task. Until accepted/proved, first candidate cannot advertise cancel-triggered recovery. |
| O3 | Successful recovery emits Compensated; an explicit option can propagate the original fault afterward while retaining the recovery evidence. Paired forward/handler authoring is the recommended interaction. This makes recovery completion distinct from normal forward success, with a deliberate failure-propagation choice. | Sipke, before M1 readiness and public activity/outcome contracts. Elsa 4 policy acceptance is not inherited. |

O1–O3 are recommendations for owner acceptance, not settled Elsa 3 product policy. No commercial, release or deployment policy is changed.

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
| M0 (#8896) | Reviewed Elsa 3 design with proposed policies, source adaptation, paired Studio scenarios and gates | This packet; E1–E3 and O1–O3 stay explicit. No source/schema/API implementation. |
| M1 (#8891) | Small visually authored serial reservation/rejection/recovery, minimal authored confirmation for the no-fault path, immutable capture/pins and accurate safe inspection | One proven host/provider/participant profile; V1, V2a–e, V3, V4 and V5a; baseline V7 inspection/accessibility controls and V8a preservation/destructive-mutation guards. O1/O3 acceptance and a no-fault confirmation walkthrough are required. Unknown/failed recovery blocks visibly even before full reconciliation/operator UX. No arbitrary activities, parallel/loop/nested/background siblings or broad provider claim. |
| M2 (#8892) | Explicit uncertainty, safe keyed reconciliation/retry and intervention | Same keys across automatic/operator retries; participant closed-key late-result rule. Durable wait and resumption, original/recovery causes distinct. |
| M3 (#8893) | Wider explicit compensation/confirmation targeting, completed-scope eligibility, nested/loop identity and cancellation composition | Retention model is chosen now; execute wider behavior only after accepted O1–O3 and V6. Unsupported combinations rejected. |
| M4 (#8894) | Full accessible operator control and hardened recovery history | Server action/tenant/revision/dedup/audit/disclosure proof; no force-success toggle. Baseline protection already applies at M1. |
| M5 (#8895) | Qualified supported matrix, upgrade/retention, samples/runbook and release readiness | V1–V8 as applicable, exact-candidate review/checks and independent participant evidence. Release/deploy requires separate authority. |

## Requirements gate result

Design readiness: satisfied for #8896 by the owner's bounded authorization, available Program/PRD/reference and this task's explicit acceptance. Production readiness: **not satisfied**. Evidence gates, owner policies and the final review/acceptance prevent premature claims. After PR review, reconcile Program/Project from actual head and issue state; do not mark a product milestone Passed or remove downstream pauses because a design document merged.

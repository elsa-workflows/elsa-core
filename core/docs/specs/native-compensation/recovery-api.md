# Proposed recovery projection, commands and security

A recovery-specific service boundary owns validation and state transitions. Native retry, alteration, bookmark/resumption and dispatch services remain execution mechanisms underneath it. Do not copy Elsa 4's `RetryFaultedActivity/1` contract or build a competing universal retry protocol. Public route/permission spellings below are proposals for review, not frozen APIs or implemented endpoints.

## Safe projection

For protected instances, the [canonical E2 contract](recovery-data.md) owns the summary allowlist, per-resource baseline/detail authorization, capture classification, and source suppression. This section adds only recovery-history paging and correlation behavior.

Proposed reads: a dedicated instance recovery summary; paged scopes/occurrences; paged attempts for one occurrence; and separately authorized capture/evidence detail. Correlate an occurrence to its attempts, concrete execution drill-down, and pinned definition/mapping provenance using the opaque server-generated identifiers allowed by the [E2 summary contract](recovery-data.md#read-record-and-mutation-boundaries). A concrete execution ID is a drill-down target, not the logical recovery identity. Recovery links open only the reviewed guarded projection; never route a sensitive occurrence directly to today's unrestricted record view.

The canonical root journal is authoritative for obligations and status only when its protected bytes and schema are verifiable; the [E2 contract](recovery-data.md#protection-and-preservation) owns data-unavailable and business-outcome distinctions. Enrich journal history with existing execution/incident details only through authorized joins. Do not reconstruct recovery state from latest-by-node execution records, inferred diagram colors or prunable logs. Show NotLoaded, Redacted, MissingVersion, Unknown and NotRun distinctly. An incomplete history page cannot be presented as a complete successful recovery.

## Proposed commands

| Command | Planned availability | Legal use / effect |
|---|---|---|
| CompensateScope | M3 backend; M4 operator UI | Resolve target occurrence; reserve recovery of eligible obligations under semantics; do not merely invoke a handler by node ID. |
| RetryRecovery | M2 supported participant/runtime retry; M4 operator UI | Failed/intervention attempt with safe participant retry contract; append attempt, reuse UndoKey and capture; preserve original cause. |
| ReconcileOperation | M2 supported reconciliation; M4 operator UI | Obtain/validate participant evidence for an uncertain forward or undo outcome; may schedule safe recovery only after the accepted transition. |
| ConfirmScope | M3 wider command; M4 operator UI | End eligibility under O1; refuse in-flight, failed, uncertain or recovering obligations; preserve audit. |
| CompensateAndCancel | Accepted O2 policy; M3 backend, M4 operator UI | Persist intent; stop forward scheduling; settle in-flight/uncertain work and ordered eligible recovery before Cancelled. Never conflate host drain with business cancellation or implicitly confirm. |
| ManualResolution / Abandon | M4 policy only | M4 policy only; no force-success toggle. A future permitted action must distinguish claimed resolution, participant evidence and unresolved abandonment. |

Accepted O2 requires explicit Cancel and compensate semantics with action-specific authority; existing cancel permission alone cannot authorize compensation. Duplicate requests join the current cancellation/recovery operation rather than creating a second unwind. The exact route/permission mapping remains an unproved E2 readiness dependency under the standing delivery authorization.

M1 supplies only the fault-triggered path and minimal authored Confirm activity following accepted O1–O3 policy, with technical gates still open. It does not expose these operator commands. An authored Confirm can target only its owned instance/scope through the pinned published workflow; it records workflow actor kind, definition/instance identity and trusted initiator provenance, not an invented operator grant. Operator action permissions cannot be bypassed through an arbitrary cross-instance activity target. All later entrypoints follow this one transition boundary.

Each mutation includes opaque CommandId, instance/scope/occurrence/attempt target as applicable, ExpectedRevision and required reason. The tenant comes from the trusted request scope. Resolve every target through tenant-scoped stores and verify cross-target ownership server-side; a matching activity ID or signed bookmark URL does not supply operator authority.

The backend checks authentication, action permission, target ownership, revision/state, pinned versions, schema/capture availability, execution ownership and participant capability. Persist command acceptance/audit intent before any effect it starts; receipt/effect transitions share the accepted durable protocol. Same CommandId + same normalized payload returns the existing operation/result. Reusing an ID for different payload is a conflict. Concurrent commands are serialized or rejected by revision/CAS; checking a revision in memory before an unconditional save is insufficient.

Proposed response classes: Accepted with action/operation ID for asynchronous work, Forbidden (or nondisclosing NotFound under current policy), Conflict for stale state/ID mismatch, and Unavailable/validation error for unproved capability/pin/schema. Safe errors must not reveal another tenant's target existence or capture contents. UI refresh/retry does not mint new business operations.

## Authorization and audit

Command/action verbs for compensate, retry, reconcile and confirm are distinct from the instance/execution read baselines and protected-detail gate in the [E2 authorization contract](recovery-data.md#authorization-and-tenant-check); none grants another resource's read. Privileged intervention remains a separate M4 decision. Existing cancellation, runtime-control or alteration execution permissions are not automatically grants for every recovery action. Exact command names, roles and public DTOs require security review. UI gates visibility; backend checks remain authoritative, including embedded/custom-element hosts and direct API calls.

Audit records bind actor, trusted tenant, CommandId/action, reason, target IDs, prior/new revision/status, participant/evidence references, execution/incident causality and result. Authorization denials/conflicts follow the existing safe audit policy. Do not put raw credentials, capture values or external response bodies in reasons, logs, audit summaries, error messages or invalidation events. Audit persistence and command deduplication must survive restart; if needed inside the root snapshot, audit records must be protected from user-editable workflow-state/import paths.

All capture classification and protected storage, full-state and execution/incident disclosure, exception source suppression, Secrets boundaries, authenticated tenant binding, and Studio cache-clearing proof are defined by the [canonical E2 contract](recovery-data.md#protection-and-preservation), its [read/write boundaries](recovery-data.md#read-record-and-mutation-boundaries), its [authorization section](recovery-data.md#authorization-and-tenant-check), and its [proof matrix](recovery-data.md#executable-proof-matrix). This API sketch adds no parallel capture or disclosure policy.

## Baseline versus later delivery

M1 requires authorized safe inspection, deny-before-effect capability checks, tenant isolation, protected capture and a visible unresolved handler/unknown state. Full operator mutation UI arrives later, but the command identity/revision/audit rules are designed now so it cannot create a second recovery state machine. Future live notifications carry identifiers/invalidation only; clients refetch through authorized reads. Reconnect and large history handling are bounded and cancellation-aware.

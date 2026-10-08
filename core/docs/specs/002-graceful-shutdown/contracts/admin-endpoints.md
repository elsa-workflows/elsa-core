# Contract: Admin Endpoints (Pause / Resume / Status / Force)

**Module**: `Elsa.Workflows.Runtime/Endpoints/Admin`
**Pattern**: `ElsaEndpoint<TRequest, TResponse>` (FastEndpoints), one class per endpoint (Constitution III).
**Auth**: `ConfigurePermissions(PermissionNames.ManageWorkflowRuntime)` — new permission, R9. All endpoints honour `EndpointSecurityOptions.SecurityIsEnabled` (if false → `AllowAnonymous()`, matching the rest of the codebase).

All endpoints are **idempotent** (FR-033) and **return the post-request state**, not a transient acknowledgement.

---

## `POST /admin/workflow-runtime/pause`

**Request**

```json
{
  "reason": "string, optional, human-readable"
}
```

**Response (200)**

```json
{
  "state": {
    "reason": "AdministrativePause",          // QuiescenceReason flags as string
    "pausedAt": "2026-04-24T09:12:43Z",
    "drainStartedAt": null,
    "pauseReasonText": "maintenance for queue migration",
    "pauseRequestedBy": "ops@example.com"
  },
  "sources": [
    { "name": "http.trigger", "state": "Paused", "lastError": null, "lastTransitionAt": "..." },
    { "name": "scheduling.cron", "state": "Paused", "lastError": null, "lastTransitionAt": "..." },
    { "name": "internal.bookmark-queue-worker", "state": "Paused", "lastError": null, "lastTransitionAt": "..." }
  ],
  "activeExecutionCycleCount": 3
}
```

**Semantics**

- Idempotent: calling this while already paused returns the current state without writing a new audit event (SC-007).
- Partial failures: if any source failed to pause, its entry shows `state: "PauseFailed"` with `lastError`. The endpoint still returns 200 — the runtime is reporting its state faithfully, not an HTTP-level failure.
- Audit event: written via mediator notification `RuntimePauseRequested { RequestedBy, Reason, Timestamp }` (FR-034).

---

## `POST /admin/workflow-runtime/resume`

**Request**

```json
{}
```

**Response (200)**

Identical shape to the pause response, but with `state.reason == "None"` when successful.

**Semantics**

- Idempotent: calling this while not paused returns the current state unchanged.
- **Rejected during drain**: if the quiescence state currently has `Drain` set, the response returns 409 Conflict with a body `{ "code": "runtime-draining", "state": {...} }`. Callers interpret this per the edge case "Resume is requested while drain is in progress".
- Audit event: `RuntimeResumeRequested { RequestedBy, Timestamp }`.

---

## `GET /admin/workflow-runtime/status`

**Request**: none.

**Response (200)**

Identical shape to the pause response. The endpoint is always readable (subject to auth), even while drain is in progress.

---

## `POST /admin/workflow-runtime/force-drain`

**Request**

```json
{
  "reason": "string, optional"
}
```

**Response (200)**

```json
{
  "outcome": {
    "overallResult": "Forced",
    "startedAt": "...",
    "completedAt": "...",
    "pausePhaseDuration": "00:00:00.1234",
    "waitPhaseDuration": "00:00:00.0001",
    "sources": [ ... ],
    "executionCyclesForceCancelledCount": 2,
    "forceCancelledInstanceIds": [ "..." ]
  }
}
```

**Semantics**

- Triggers `IDrainOrchestrator.DrainAsync(DrainTrigger.OperatorForce, ct)` with a deadline of zero — all active execution cycles are immediately cancelled and marked `Interrupted`. This is the operator-escalation path referenced in FR-017.
- Calling this ends normal operation. The runtime is in `Drain` state afterwards; the host process is NOT exited (that is only done by the host-stop path) — but no new work will be accepted until the host is restarted or the shell is reactivated.
- Returns 409 Conflict if a drain is already in progress via a different trigger.
- Audit event: `RuntimeForceRequested { RequestedBy, Reason, Timestamp, Outcome }`.

---

## Error shape

Validation errors use FastEndpoints' standard validation response.
Runtime errors follow the existing `ElsaEndpoint` error convention; no new envelope is introduced.

## Authorisation

- New permission name: `"ManageWorkflowRuntime"` added to `Elsa.Api.Common` `PermissionNames`.
- All four endpoints call `ConfigurePermissions(PermissionNames.ManageWorkflowRuntime)` in their `Configure()` override.
- When `EndpointSecurityOptions.SecurityIsEnabled == false` (dev convenience), the permission check is skipped — matching the codebase convention.

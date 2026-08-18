# REST API Contract

Base path: `/user-tasks`. Authenticated endpoints use Elsa endpoint permission metadata plus `IUserTaskAccessPolicy`. JSON uses camel case. IDs and cursors are opaque. Enum-valued fields (`status`, `healthSeverity`) are emitted as strings so a client never depends on ordinal values.

## Capabilities

`GET /user-tasks/capabilities` returns the tenant- and actor-scoped feature descriptor:

```json
{
  "enabled": true, "canList": true, "canRead": true, "canReadAll": false,
  "canClaim": true, "canComplete": true, "canRelease": true, "canAssign": false,
  "canUpdate": false, "canCancel": false, "canCreateGuestLinks": false,
  "canViewProtected": true, "participantPicker": false,
  "realtime": true, "pollingIntervalSeconds": 30
}
```

The descriptor is advisory: it decides what a client renders, never what the server allows. It is gated on `read:user-tasks`, so an actor without read access receives `403` and clients treat that as "hide the feature".

`GET /user-tasks/{id}/capabilities` returns the per-task projection: `taskId`, `revision`, `allowedActions`, `canReadProtected`, and `canManage`.

## Query

`GET /user-tasks?scope=assigned|available|history|all|needsAttention&cursor=&limit=&sort=&direction=&status=&priorityFrom=&priorityTo=&due=&from=&to=&workflowDefinitionId=&workflowInstanceId=&reference=&taskType=&search=&includeTotalCount=`

Returns `{ items, nextCursor, totalCount? }`. Default/max limit: 50/200. Stable sorts: created, due, priority, title, updated, each with an ID tiebreaker.

- `scope` is part of the authorization predicate, not a display filter. `all` and `needsAttention` require manager scope and answer `403` — not an empty page — for anyone else.
- `status` is repeatable. Unknown values are dropped rather than rejected, so a stale bookmark still loads.
- `due` is a derived filter (`overdue`, `today`, `thisWeek`, `noDueDate`); `from`/`to` carry an explicit range.

Items contain only the safe summary and the caller's capabilities: id, title, summary, reference, tags, task type, status, priority, assignee display, a count-only `candidateSummary`, due/overdue, created/updated/assigned/completed times, workflow definition name and version, a safe workflow instance reference, health (managers only), `allowedActions`, and `revision`.

`GET /user-tasks/{id}` returns the flat summary plus protected fields and a `disclosure` block when authorized. `GET /user-tasks/{id}/events?cursor=&limit=` returns safe append-only audit entries (`id`, `kind`, `summary`, `occurredAt`, `actorDisplayName`); it returns an empty page for callers without protected access rather than revealing who acted.

`POST /user-tasks/{id}/reveal` with `{ fieldKey }` discloses one masked form field. The field must be marked `masked` and `canReveal` by its form provider, the caller must hold protected access, and the reveal is written to the audit trail. Masked values are never included in the ordinary detail response.

`GET /user-task-participants?search=&type=&cursor=&limit=` requires `lookup:user-task-participants`; absence of a directory returns an empty page, never an identity-module error.

## Commands

Every command accepts an `operationId`. Clients mint one per user submission and reuse it across retries, so a retry is recognised as the same command instead of accepted as a second one.

- `POST /user-tasks/{id}/claim`: `{ expectedRevision, operationId? }` → `200` `{ operationId, status, revision, task }`.
- `POST /user-tasks/{id}/release`: `{ expectedRevision, operationId?, reason? }` → `200`.
- `POST /user-tasks/{id}/assign`: `{ expectedRevision, assignee, operationId?, reason? }` → `200`; managers may assign a non-candidate and it is audited. The assignee's tenant is taken from the caller's own scope and never from the body, so a cross-tenant reference cannot be constructed over the wire.
- `PATCH /user-tasks/{id}`: `{ expectedRevision, priority?, dueAt?, operationId? }` → `200`; no other materialized fields are mutable.
- `POST /user-tasks/{id}/complete`: `{ expectedRevision, operationId, actionKey, data? }` → `202`.
- `POST /user-tasks/{id}/cancel`: `{ expectedRevision, operationId, reason }` → `202` only when enabled.
- `POST /user-tasks/{id}/retry-resolution`: `{ expectedRevision, operationId? }` → `202`; retries the original form or snapshot input.
- `POST /user-tasks/{id}/invitations`: invitation definition → `201` metadata; the secret is delivered through the dispatcher, never returned to API clients.
- `GET /user-tasks/{id}/invitations` → `{ items }` and `DELETE /user-tasks/{id}/invitations/{invitationId}` → `204` require the invite relationship.

## Guest API

- `GET /user-task-invitations/{token}` → a generic challenge descriptor `{ challengeType, prompt, requiresCode }`.
- `POST /user-task-invitations/{token}/verify` → a generic failure or `{ sessionCredential, taskId, expiresAt }`.
- `GET /user-task-sessions/current` → the guest-visible task projection.
- `POST /user-task-sessions/current/complete` → `202`, with the same operation and idempotency semantics as the authenticated command.

The session credential is presented as `Authorization: UserTaskSession <credential>`.

**Documented adjustment.** An earlier draft had guests read and complete through the ordinary `/user-tasks/{id}` resource. The shipped contract addresses the guest surface through the session instead, for two reasons. First, Elsa's endpoint security applies permission metadata at the transport layer through `ConfigurePermissions`; mixing an anonymous session scheme into those routes would have required weakening the gate on the two most sensitive endpoints. Second, the session already identifies exactly one task, so keeping the task ID out of the route removes a task-existence oracle instead of relying on `404` concealment. The guest's authorization semantics are unchanged: `IUserTaskAccessPolicy` still scopes the actor to its own task and intersects completion with the invitation's action allowlist.

## Errors and idempotency

Failures return `{ code, message }` with a stable code and display-safe copy. Messages never carry exception text, payload fragments, or infrastructure detail.

- `401`: no valid authenticated or guest principal.
- `403`: the module permission itself is missing, including a list scope the actor may not use. This is decided at the transport layer before any task is loaded, so it discloses nothing about a specific task.
- `404`: task absent, or present but denied by the relationship check. Both answer with the same status and the same copy, so an ID-guessing caller cannot use a command to prove that a task exists.
- `409`: revision race (`revision-conflict`), invalid lifecycle transition (`terminal`, `transition-in-progress`, `not-claimable`), or an operation ID reused with a different canonical payload (`idempotency-conflict`).
- `422`: invalid action, form validation, payload size, cancellation configuration, or other semantic input.
- `429`: anonymous invitation traffic throttled, partitioned by caller rather than by token.

Repeated terminal commands with the same task, operation ID, and canonical payload return the existing operation response. Accepted terminal operations always return `202`; clients observe the final state by requery or invalidation.

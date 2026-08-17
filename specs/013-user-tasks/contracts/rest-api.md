# REST API Contract

Base path: `/user-tasks`. Authenticated endpoints use Elsa endpoint permission metadata plus `IUserTaskAccessPolicy`. JSON uses camel case. IDs and cursors are opaque.

## Query

`GET /user-tasks?scope=assigned|available|history|all|needsAttention&cursor=&limit=&sort=&direction=&status=&priorityFrom=&priorityTo=&dueFrom=&dueTo=&workflowDefinitionId=&workflowInstanceId=&reference=&taskType=&search=&includeTotalCount=`

Returns `{ items, nextCursor, totalCount? }`. Default/max limit: 50/200. Stable sorts: created, due, priority, title, each with ID tiebreaker. Items contain only safe summary and caller capabilities. `all` and `needsAttention` require manager scope.

`GET /user-tasks/{id}` returns safe summary plus protected fields only when authorized. `GET /user-tasks/{id}/events?cursor=&limit=` returns safe append-only audit entries. `GET /user-tasks/{id}/capabilities` returns current capability booleans and revision.

`GET /user-task-participants?search=&type=&cursor=&limit=` requires `lookup:user-task-participants`; absence of a directory returns an empty capability response, never an identity-module error.

## Commands

- `POST /user-tasks/{id}/claim`: `{ expectedRevision }` → `200` updated summary.
- `POST /user-tasks/{id}/release`: `{ expectedRevision, reason? }` → `200`.
- `POST /user-tasks/{id}/assign`: `{ expectedRevision, assignee, reason? }` → `200`; managers may assign a non-candidate and audit it.
- `PATCH /user-tasks/{id}`: `{ expectedRevision, priority?, dueAt? }` → `200`; no other materialized fields are mutable.
- `POST /user-tasks/{id}/complete`: `{ expectedRevision, operationId, actionKey, data? }` → `202` `{ operationId, status, revision }`.
- `POST /user-tasks/{id}/cancel`: `{ expectedRevision, operationId, reason }` → `202` only when enabled.
- `POST /user-tasks/{id}/retry-resolution`: `{ expectedRevision }` → `202` and retries the original form or snapshot input.
- `POST /user-tasks/{id}/invitations`: invitation definition → `201` metadata; secret is delivered through dispatcher, not returned to ordinary API clients.
- `GET /user-tasks/{id}/invitations` and `DELETE /user-tasks/{id}/invitations/{invitationId}` require invite/manage relationship.

## Guest API

- `GET /user-task-invitations/{token}` → generic challenge descriptor only.
- `POST /user-task-invitations/{token}/verify` → generic failure or task-scoped session credential.
- Guest detail and completion use the ordinary task resource with the issued session, preserving operation semantics.

## Errors and idempotency

- `401`: no valid authenticated or guest principal.
- `403`: permission exists but operation is denied; APIs may use `404` where existence would leak.
- `404`: task absent or intentionally concealed.
- `409`: revision race, invalid lifecycle transition, or operation ID reused with different canonical payload.
- `422`: invalid action, form validation, payload size, cancellation configuration, or semantic input.
- `429`: anonymous verification throttled.

Repeated terminal commands with the same task, operation ID, and canonical payload return the existing operation response. Accepted terminal operations always return `202`; clients observe final state by requery or invalidation.

# User Tasks

`Elsa.UserTasks` adds durable, identity-neutral, workflow-bound human work to Elsa. A workflow suspends at the `UserTask` activity, authorized workers discover and act on tasks through the `/user-tasks` REST API, and the workflow resumes with a typed action and validated form result.

Start in [src/modules/Elsa.UserTasks](../../src/modules/Elsa.UserTasks).

## Design Principles

- **Identity neutral.** The module stores opaque, tenant-scoped participant references. It has no required dependency on `Elsa.Identity` and no foreign key into any user table.
- **Workflow state is authoritative.** Task records are projected from committed workflow bookmarks (see [ADR 0027](../adr/0027-project-user-tasks-from-committed-bookmarks.md)). The bookmark survives persistence and resume; the task record is a queryable projection of it.
- **Fail closed.** Every operation requires both a module permission and a task-relationship check. A denied command returns `404` rather than `403` so it cannot confirm a task exists.

## Identity Integration

Authentication mapping, live group membership, directory lookup, and task authorization are replaceable host contracts:

| Contract | Purpose |
| --- | --- |
| `IUserTaskIdentityResolver` | Maps `ClaimsPrincipal` to participant references. |
| `IUserTaskAccessPolicy` | Authorizes list visibility and commands per task. |
| `IUserTaskParticipantDirectory` | Directory lookup for display, snapshot groups, and enrichment. |

The built-in adapter maps namespaced claims from `ClaimsPrincipal`. Directory resolution failure does not invalidate an exact live participant claim. See [ADR 0026](../adr/0026-identity-neutral-user-task-participants.md) for the rationale.

A participant reference stores:

```json
{
  "tenantId": "acme",
  "provider": "entra",
  "type": "User",
  "id": "external-subject-id"
}
```

## Lifecycle

```
Available ──claim──► Assigned ──complete/cancel──► Completing ──► Completed / Cancelled
    │                   │                                                   │
    └─── timeout ───────┴─────────────────────────────────────► TimedOut ──┘
                                                                (all terminal)
```

- **Direct assignments** start as `Assigned`; **candidate or invitation tasks** start as `Available`.
- Tasks with no route to a worker start as manager-only `Unassigned` with a blocking health issue.
- Terminal commands (complete, cancel) transition through `Completing` while the workflow bookmark resumes, then finalize once the bookmark clears.
- Claims and terminal actions use optimistic concurrency. A `revisionConflict` result signals a concurrent edit.

## Feature Wiring

[UserTasksFeature](../../src/modules/Elsa.UserTasks/Features/UserTasksFeature.cs) registers:

- FastEndpoints assembly
- user task services and repositories (via `AddUserTasksServices`)
- hosted projector, reconciler, and delivery workers

Production hosts should select one of the persistence packages rather than relying on the default in-memory store.

## Persistence

| Package | Backend |
| --- | --- |
| `Elsa.UserTasks.Persistence.EFCore.Sqlite` | SQLite |
| `Elsa.UserTasks.Persistence.EFCore.SqlServer` | SQL Server |
| `Elsa.UserTasks.Persistence.EFCore.PostgreSql` | PostgreSQL |
| `Elsa.UserTasks.Persistence.EFCore.MySql` | MySQL |
| `Elsa.UserTasks.Persistence.EFCore.Oracle` | Oracle |
| `Elsa.UserTasks.Persistence.VNext` | VNext provider-neutral store |

`UserTaskRevisionConflictException` translates store-specific concurrency failures across all providers so concurrent edits always surface as the documented `409 revisionConflict` result.

## Forms And Actions

- Action keys are stable literals; labels may be expressions.
- `Timeout` and `Cancelled` are reserved action keys.
- An optional `IUserTaskFormProvider` resolves and pins a provider-neutral form reference when the task activates, then validates and normalizes the completion payload.
- An unresolved form creates a blocking manager health issue.
- Tasks without a form accept an action only.
- Protected task, form, and completion payloads are limited to 256 KiB by default.

## Guest Invitations

Guest invitations let a workflow reach external participants who are not Elsa users.

- Core hashes the one-time token; retry material lives only in a Data Protection-encrypted transient outbox.
- The first successful verification claims the task, revokes sibling invitations, and issues a bounded guest session (`Authorization: UserTaskSession <credential>`).
- Anonymous errors are generic and rate-limited; guests cannot release or reassign work.
- A manager may revoke an invitation after it has been consumed, which revokes the live guest session.
- Invitations require an `IDataProtectionProvider`; non-web hosts must call `AddDataProtection()` explicitly.

## HTTP Surface

Clients interact through:

- `GET /user-tasks/capabilities` — capability descriptor; read before rendering navigation.
- `GET /user-tasks` — paged task queue (Assigned to me, Available, All, History, Needs Attention views).
- `GET /user-tasks/{id}` — single task detail.
- `POST /user-tasks/{id}/claim` — claim an available task.
- `POST /user-tasks/{id}/release` — release a claimed task.
- `POST /user-tasks/{id}/complete` — complete with action and optional form data.
- `POST /user-tasks/{id}/cancel` — cancel (manager only).
- `POST /user-tasks/{id}/reveal` — disclose a masked form field (audited).
- `POST /user-tasks/{id}/invitations` — issue a guest invitation.
- `DELETE /user-tasks/{id}/invitations/{invitationId}` — revoke a guest invitation.

Every mutable command carries a client-minted `operationId`. Same-payload retries are idempotent; divergent reuse is rejected.

**Status codes:** `202` for terminal commands (the workflow resumes out of band), `409` for revision conflicts, `422` for semantic input errors, `429` for rate-limited anonymous invitation traffic. Failure bodies are `{ code, message }` — never exception text or payload fragments.

## Authorization

User Tasks permissions live in [UserTasksPermissions](../../src/modules/Elsa.UserTasks/Permissions/UserTasksPermissions.cs). The module uses the legacy `verb:resource` format and has not yet migrated to the new catalog model introduced in [ADR 0025](../adr/0025-two-axis-authorization-model.md). Scope (assignee relationship or manager role) is an additional predicate layered on top of the permission check.

## Tests

- `test/component`: persistence conformance suite with fault injection (`Elsa.UserTasks.ComponentTests`)
- `test/integration`: task lifecycle, invitation flow, and REST surface tests

## Further Reading

- [specs/013-user-tasks/spec.md](../../specs/013-user-tasks/spec.md) — full requirements and acceptance scenarios
- [specs/013-user-tasks/contracts/rest-api.md](../../specs/013-user-tasks/contracts/rest-api.md) — REST contract
- [specs/013-user-tasks/contracts/runtime-contract.md](../../specs/013-user-tasks/contracts/runtime-contract.md) — activity and bookmark contract
- [specs/013-user-tasks/quickstart.md](../../specs/013-user-tasks/quickstart.md) — end-to-end validation guide
- [doc/user-tasks.md](../user-tasks.md) — operator-facing design summary
- [ADR 0026](../adr/0026-identity-neutral-user-task-participants.md) — identity-neutrality decision
- [ADR 0027](../adr/0027-project-user-tasks-from-committed-bookmarks.md) — bookmark-projection decision

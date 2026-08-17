# User Tasks

User Tasks add durable, workflow-bound human work to Elsa. A workflow suspends at the `UserTask` activity, authorized workers discover and act on the task through `/user-tasks`, and the workflow resumes with a typed action and validated form result.

## Identity integration

The module does not require `Elsa.Identity`. It stores opaque, tenant-scoped participant references:

```json
{
  "tenantId": "acme",
  "provider": "entra",
  "type": "User",
  "id": "external-subject-id"
}
```

The default adapter maps namespaced claims from `ClaimsPrincipal`. Embedded hosts can replace `IUserTaskIdentityResolver`, `IUserTaskAccessPolicy`, and `IUserTaskParticipantDirectory` to integrate their own authentication, authorization, users, and groups. Directory lookup enriches display and snapshot groups; it is never required for an exact live claim match.

Every operation requires both its module permission and a relationship to the task. Candidate workers can read safe queue metadata and claim. Only the assignee and tenant-scoped managers can read protected instructions, task data, and form content. Releasing a task revokes that access immediately.

## Lifecycle

Direct assignments start as `Assigned`; candidate or invitation work starts as `Available`; a task with no route to a worker starts as manager-only `Unassigned`. Completion, timeout, and cancellation first enter a transitional state while the workflow bookmark is resumed, then finalize as `Completed`, `TimedOut`, or `Cancelled`.

Claims and terminal actions use optimistic concurrency. Completion and cancellation require a client operation ID, making same-payload retries idempotent while rejecting divergent reuse. A post-commit bookmark projector and paged reconciler repair interrupted projection and resume delivery without duplicating tasks.

## Forms and actions

Action keys are stable literals; their labels may be expressions. `Timeout` and `Cancelled` are reserved. An optional `IUserTaskFormProvider` resolves and pins a provider-neutral form reference when the task activates, then validates and normalizes the completion payload. An unresolved form creates a blocking manager health issue. Tasks without a form accept an action only.

Protected task, form, and completion payloads are limited to 256 KiB by default. Put files in an external object/document provider and submit references rather than file bytes.

## Persistence

The Core feature includes an in-memory repository for development and tests. Production hosts can select the EF Core User Tasks package for SQLite, SQL Server, PostgreSQL, MySQL, or Oracle, or use the VNext persistence adapter. The provider-neutral API and activity contract do not change with the store.

Terminal tasks are retained indefinitely by default. Configurable cleanup may purge terminal aggregates and their protected data, but never open tasks. Audit events remain append-only and contain safe metadata only.

## Guest invitations

Guest invitations are task-scoped and do not create Elsa users. Core hashes the one-time token, retains retry material only in a Data Protection-encrypted transient outbox, and delegates delivery and challenge verification to host services. The first successful verification claims the task, revokes sibling invitations, and issues a bounded guest session. Anonymous errors are generic and rate-limited; guests cannot release or reassign work.

## Studio and custom applications

Elsa Studio provides Workflows → User Tasks with Assigned to me, Available, History, and manager-only All and Needs Attention views. Studio is a reference workbench, not a required runtime dependency. Custom applications use the same REST APIs and server-computed capability projections. Realtime messages are invalidations only; clients always requery authorized data.

See [the feature specification](../specs/013-user-tasks/spec.md), [REST contract](../specs/013-user-tasks/contracts/rest-api.md), and [quickstart](../specs/013-user-tasks/quickstart.md) for the complete design and examples.

# Identity and Authorization Contract

## Participant value

`ParticipantReference` is an opaque value with required `tenantId`, `provider`, `type` (`User` or `Group`), and `id`; `displayName` is an optional non-authoritative snapshot. Equality uses the four required fields with host-configurable normalization. A configured default provider may expand a designer-entered bare ID. Core never verifies existence during task activation and never persists an Elsa Identity foreign key.

## Host interfaces

- `IUserTaskIdentityResolver`: maps the current authenticated principal to one or more exact participant references and tenant context.
- `IUserTaskAccessPolicy`: evaluates an operation against module permission, task relationship, exclusions, management scope, and optional guest capabilities.
- `IUserTaskParticipantDirectory`: searches display entries, resolves display snapshots, and optionally enumerates group membership for snapshot mode.

The built-in identity resolver maps namespaced `ClaimsPrincipal` claims. All interfaces are replaceable. Directory failure affects display or task health; it does not make participant values invalid.

## Assignment membership

- `Live` (default): candidate group checks use current principal references/claims. An exact reference match remains authoritative even if the directory cannot resolve it.
- `Snapshot`: groups are expanded at activation and stored as snapshot members. Failure to enumerate creates a blocking manager-only health issue.
- Explicit user exclusions deny assignment and protected access. Manager override is disabled by default; when enabled it requires `user-tasks:supervise` and a reason.
- `Requester` is context only and grants no capability.

## Operation permissions

`user-tasks:view`, `:claim`, `:complete`, `:assign`, `:update`, `:cancel`, `:invite`, `:supervise`, and `user-tasks/participants:view` are independent permissions; no verb implies another. Permission alone is insufficient: ordinary callers also need the corresponding task relationship. Managers are tenant-scoped. Guest sessions carry an allowlist of capabilities for one task.

## Disclosure

Safe summary: title, summary, reference, tags, task type, workflow context, status/health, assignee display, priority, due/created times, and caller capabilities. Protected detail: instructions, task data, pinned form, and completion data. Candidates receive summary before claim; assignees receive protected detail; release revokes it immediately. After termination, only the completer and managers retain protected detail. Prior assignees see safe summary and audit history.

## Default-deny rules

Cross-tenant references never match. Unknown relationship, missing tenant, malformed reference, failed policy integration, revoked guest session, and stale capability decisions deny access. List authorization must be applied in the query path so unauthorized rows and totals cannot leak.

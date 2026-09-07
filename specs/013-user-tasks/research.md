# Research: User Tasks

**Status**: Approved  
**Last reviewed**: 2026-08-17

This research captures the product and integration decisions behind `Elsa.UserTasks`. The feature is deliberately identity-neutral: Elsa stores references to participants supplied by the host application, but does not create or require Elsa users.

## Decision: Make User Tasks a workflow-bound inbox projection

**Rationale**: A User Task must block workflow execution and must also be queryable as durable work from Elsa Studio or a custom application. The activity therefore creates a bookmark, while the User Tasks module projects the committed bookmark into a task record. Completion records the requested result and resumes the bookmark asynchronously. This gives workflow execution and inbox queries separate persistence boundaries without allowing a task row to become an independent workflow execution primitive.

**Alternatives considered**:

- Reuse the existing generic `RunTask` activity and `/tasks/{id}/complete`: rejected because it has a different contract and does not provide assignment, participant, form, audit, or queue semantics.
- Create standalone tasks in v1: rejected because the first release must have an unambiguous workflow resume target; standalone work can follow once its lifecycle and retention rules are separately designed.
- Make the task table the source of workflow state: rejected because a database write could claim or complete a task without atomically coordinating the workflow bookmark.

## Decision: Store opaque, tenant-scoped participant references

**Rationale**: Camunda models `assignee`, `candidateUsers`, and `candidateGroups` as identifiers resolved by its surrounding identity system. Its current documentation also notes that identifiers are case-sensitive and that group IDs are preferred. Flowable is even more explicit: its runtime does not verify that an assigned user exists, which allows an embedded engine to use an existing identity service. Elsa should adopt this integration boundary directly.

Elsa persists a participant as `{ provider, type, id }`, where `type` is `user` or `group`. The value is an external reference, not an `Elsa.Identity` foreign key. Claims, directory lookups, display names, and group membership are replaceable host integrations. A display name is a convenience snapshot and never participates in authorization.

**Sources**: [Camunda user task assignments](https://docs.camunda.io/docs/components/modeler/bpmn/user-tasks/), [Flowable Task API](https://www.flowable.com/open-source/docs/bpmn/ch04-API/), [Flowable user-task assignment](https://www.flowable.com/open-source/docs/userguide-5/).

**Alternatives considered**:

- Foreign keys to `Elsa.Identity` users or groups: rejected because integrations may use Entra ID, LDAP, an application database, a SaaS identity provider, or no Elsa user store at all.
- Free-form strings without a provider namespace: rejected because the same subject identifier can exist in multiple issuers or tenants.
- A mandatory directory provider: rejected because authorization must work with claims alone and directory services are often unavailable or eventually consistent.

## Decision: Separate assignment from task state

**Rationale**: Camunda Tasklist treats assignment as a distinct concern: a task can be available to candidates, claimed by one worker, released, reassigned, and then completed. Flowable uses the same model: claiming makes one user the assignee and removes the task from the other candidates' personal queues. Elsa therefore exposes dedicated claim, release, assign, and complete operations, rather than treating an assignee field update as completion or as a generic patch.

The public lifecycle is `Unassigned`, `Available`, `Assigned`, transitional `Completing`, `TimingOut`, and `Cancelling`, and terminal `Completed`, `TimedOut`, and `Cancelled`. A task with no usable assignment is manager-only and raises a designer/runtime warning; it never becomes visible to every authenticated user merely because no candidate was configured.

**Sources**: [Camunda Tasklist overview](https://docs.camunda.io/docs/components/tasklist/userguide/using-tasklist/), [Camunda user-task lifecycle](https://docs.camunda.io/docs/apis-tools/frontend-development/task-applications/user-task-lifecycle/), [Flowable task lists and claiming](https://www.flowable.com/open-source/docs/bpmn/ch07a-BPMN-Introduction/).

**Alternatives considered**:

- Let every candidate complete directly: rejected because it creates race conditions and makes accountability unclear.
- Treat `InProgress` as a required engine state: rejected for v1; hosts can add application-level progress outside the workflow lifecycle. `Assigned` is the accountable state.
- Automatically reassign when a worker disconnects: rejected because leases and absence detection are host-specific and can surprise users.

## Decision: Give candidates a safe queue projection and protect task content

**Rationale**: Camunda's Tasklist combines a queue with a selected-task detail view and shows task name, process, assignee, priority, creation date, due date, and follow-up date. Elsa keeps the useful queue/detail pattern but makes the security boundary explicit: an available candidate receives only summary-safe fields and can claim the task; protected instructions, task data, and form metadata are returned only after claim or to an authorized manager. This is a deliberate Elsa security improvement, not a claim that all Camunda deployments behave this way.

The API returns `dataAccess` and server-computed `allowedActions` so Studio does not infer authorization from the current user interface state. An inaccessible task returns `404`, avoiding existence leaks.

**Source**: [Camunda Tasklist queue and task details](https://docs.camunda.io/docs/components/tasklist/userguide/using-tasklist/).

## Decision: Keep priority and due dates with an explicit timeout outcome

**Rationale**: Camunda supports expression-capable due dates, follow-up dates, and a 0–100 priority with a default of 50; Tasklist can sort by those fields. Elsa keeps an expression-capable `DueAt` and priority because approvals, exception queues, and SLA work need these signals. Every overdue task publishes one idempotent notification. By default it remains actionable; when the designer explicitly enables timeout, a cluster-safe due operation races through `TimingOut` and resumes the workflow with reserved action `Timeout`. Follow-up dates and first-party escalation delivery remain deferred.

**Sources**: [Camunda user-task scheduling and priority](https://docs.camunda.io/docs/components/modeler/bpmn/user-tasks/), [Camunda priority labels](https://docs.camunda.io/docs/components/tasklist/userguide/defining-task-priorities/).

**Alternatives considered**:

- Automatically cancel at the due date: rejected because business processes differ on whether a missed deadline should escalate, remain actionable, or take a rejection path.
- Use an unbounded numeric priority: rejected because a bounded range is easier to validate, render, and sort consistently.
- Include follow-up or first-party escalation delivery in the first domain contract: rejected because it couples task persistence to a delivery system that Elsa does not otherwise provide. The module publishes lifecycle notifications for host subscribers.

## Decision: Use provider-neutral, version-pinned forms

**Rationale**: Camunda links forms to user tasks as separately deployed, versioned resources. It supports binding to the latest, a deployment, or a version tag so a running process can pin the form version it expects. Elsa has no first-party form builder to assume, so `FormReference` contains a provider name, key, and version/binding. On activation, the module resolves and pins the concrete version. The provider validates and normalizes submitted data; Core does not merge arbitrary fields into workflow variables. Studio renders only an installed trusted provider renderer and never arbitrary server-provided HTML.

If the form cannot be resolved or validated, the task records a blocking health issue and is manager-only until configuration is repaired. A manager may retry the original reference; they cannot silently replace the live form on an active task.

**Sources**: [Camunda form linking](https://docs.camunda.io/docs/components/modeler/forms/utilizing-forms/), [Camunda resource binding](https://docs.camunda.io/docs/components/best-practices/modeling/choosing-the-resource-binding-type/), [Camunda Tasklist form behavior](https://docs.camunda.io/docs/components/tasklist/userguide/using-tasklist/).

**Alternatives considered**:

- Build a form designer into Core: rejected because it would make Elsa responsible for a large UI and schema ecosystem before the provider contract is proven.
- Store only an unversioned form key: rejected because a form change could invalidate an active task.
- Render arbitrary HTML returned by a provider: rejected because it creates an unnecessary XSS and trust-boundary risk.
- Persist completion data as arbitrary workflow variables: rejected because user-submitted data needs provider validation and an explicit typed `UserTaskResult` boundary.

## Decision: Make completion optimistic-concurrency-safe and idempotent

**Rationale**: A queue is inherently concurrent: two workers may select the same candidate task, or a client may retry after a network timeout. Claim, release, assign, and complete requests require `expectedRevision`. Completion additionally requires a client-generated `operationId`; repeating the same operation is safe, while reusing it with a different payload returns `409`. Completion returns `202 Accepted` while the workflow resume is pending and transitions the task through `Completing`.

This combines Camunda's explicit assignment/completion operations with Elsa's bookmark-resume boundary. The first committed terminal transition wins; later completion, cancellation, or workflow-cancel attempts receive a conflict or are recorded as an already-finalized no-op according to the operation contract.

**Sources**: [Camunda lifecycle operations and events](https://docs.camunda.io/docs/apis-tools/frontend-development/task-applications/user-task-lifecycle/), [Camunda task API](https://docs.camunda.io/docs/apis-tools/tasklist-api-rest/controllers/tasklist-api-rest-task-controller/).

## Decision: Use a rich, authorized queue API and a capability-driven Studio

**Rationale**: Camunda's task search API supports status, assignment, candidate users/groups, process references, date ranges, priority, variable predicates, include-variable selection, sorting, and cursor pagination. Elsa adopts those high-value query dimensions with opaque cursor pagination and a stable ID tie-breaker. List rows contain bounded metadata; details load protected content only when authorized.

Studio adds **Assigned to me**, **Available**, **History**, and manager-only **All** and **Needs Attention** views at `/workflows/user-tasks`. The list uses server-side filtering, URL-persisted filters, sorting, and cursor paging. The detail view shows workflow context, protected form/data when allowed, action buttons from `allowedActions`, and a safe audit timeline. A participant picker is optional and capability-driven; raw opaque IDs and expressions remain available when no directory exists.

**Sources**: [Camunda task search](https://docs.camunda.io/docs/apis-tools/tasklist-api-rest/specifications/search-tasks/), [Camunda Tasklist overview](https://docs.camunda.io/docs/apis-tools/tasklist-api-rest/tasklist-api-rest-overview/).

**Alternatives considered**:

- A client-side list loaded from workflow instances: rejected because it cannot enforce task-level scope efficiently or support custom task stores.
- A mandatory user/group picker: rejected because host identity systems may not expose a searchable directory.
- A single page showing every task to every authenticated user: rejected because it leaks business data and makes a manager scope meaningless.

## Decision: Mirror the Elsa.Secrets persistence shape

**Rationale**: The module follows the repository's established split: a Core package owns contracts, services, default repository, activity, endpoints, permissions, and an in-memory implementation; persistence packages add provider-specific repositories, shell features, migrations, and mappings. This keeps the default useful for development and tests while allowing durable EF Core providers and future persistence-vNext adapters without changing the REST or activity contract.

Persist `UserTasks`, normalized `UserTaskCandidates`, and append-only `UserTaskEvents`. Index tenant, status, assignee provider/ID, priority, due date, workflow definition/instance, activity instance, created/completed time, and revision. Store bounded protected task data, completion data, and form references as JSON. Unique keys cover task ID, bookmark ID, and the tenant/workflow-instance/activity-instance materialization key.

The module has no secret-like cleartext protection requirement; protected task data is governed by task authorization and provider storage guarantees. Terminal retention is indefinite by default, with opt-in purge of terminal tasks and events only.

## Decision: Keep guest invitations separate from ordinary identity

**Rationale**: External customer or partner approvals are high-value, but a guest link must not become a general Elsa credential. Guest invitations use the same task aggregate and authorization model, with a single-task, scoped, one-time token and an optional bearer-only permission. The invitation token is hashed at rest; the host owns dispatch and verification. Authenticated workers never need to use invitation APIs.

This is staged after the authenticated lifecycle so the core task contract can be validated first. It follows the same task queue and completion rules, including revision and operation-idempotency checks.

**Alternatives considered**:

- Treat every guest as an Elsa user: rejected because it recreates the identity coupling the module is intended to avoid.
- Put all task data in a bearer URL: rejected because links are routinely copied, logged, or forwarded.
- Make invitation delivery part of Core: rejected because email/SMS/portal delivery is host-specific.

## What Elsa should and should not copy

| Copy | Adapt | Do not copy as a v1 dependency |
| --- | --- | --- |
| Camunda's assignment vocabulary, queue/detail layout, priority/due fields, explicit actions, form references, and lifecycle events. | Camunda's candidate visibility: use Elsa's summary-safe projection and host authorization policy rather than coupling to Elsa.Identity. | Camunda Tasklist V1/V2 visibility behavior; its docs describe different candidate semantics across API versions. |
| Flowable's standalone identity boundary and candidate-group claim semantics. | Flowable's rich query vocabulary, adding tenant and workflow bookmark constraints. | Flowable's assumption that the engine's identity component can resolve group membership; Elsa must support claims-only hosts. |
| Temporal's durable human approval pattern and immutable execution history. | Expose mediator notifications and webhooks/events for host integrations. | A signal-only implementation with no durable inbox, task assignment, or Studio list. |

The Camunda visibility split is documented in [Tasklist access restrictions](https://docs.camunda.io/docs/8.8/components/tasklist/user-task-access-restrictions/). The Temporal comparison comes from its official [human-in-the-loop reference architecture](https://go.temporal.io/platform-hub/ai-engineering/ai-reference-architecture/), which uses Signals/Updates and durable waits rather than a built-in task inbox.

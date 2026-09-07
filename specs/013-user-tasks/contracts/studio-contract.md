# Studio Contract: User Tasks

The Core/server User Tasks feature is paired with an optional `elsa-studio` module. Studio owns the authenticated task workspace, workflow-designer activity editing, and the guest completion page. The server remains authoritative for task visibility, participant identity, disclosure, capabilities, validation, claims, and completion.

Studio must not reference `Elsa.Identity` user entities. Participant identifiers are opaque, tenant-scoped references supplied by the host application. A host may provide a participant picker, but the task module remains fully usable when no picker is installed.

## Information Architecture

The authenticated module contributes a **User Tasks** entry under **Workflows**:

```text
Workflows
├── Definitions
├── Instances
└── User Tasks
```

Routes:

- `/workflows/user-tasks` — task workspace and queue.
- `/workflows/user-tasks/{taskId}` — authenticated task detail and response page.
- `/tasks/guest?t={opaqueTaskToken}` — guest task page; it is not linked from the authenticated menu.

The task list must not offer a generic **Create task** action. Runtime tasks are created by executing a published workflow containing the User Task activity. Designers create task definitions from the workflow designer.

The menu is displayed only when the remote User Tasks feature is enabled and the current actor has list/read capability. The menu never grants authorization.

## Task Workspace

The default route opens the **Assigned to me** tab. Tabs are represented in the URL and preserve browser back/forward behavior:

| Tab | Query value | Purpose | Required capability |
| --- | --- | --- | --- |
| Assigned to me | `tab=assigned` | Tasks assigned to the current actor, including claimed tasks | `user-tasks:view` |
| Available | `tab=available` | Unassigned tasks for which the actor is an eligible candidate | `user-tasks:view` |
| History | `tab=history` | Terminal tasks completed by the actor and safe history for tasks they acted on | `user-tasks:view` |
| All | `tab=all` | Tenant-visible tasks for operations and support | `user-tasks:supervise` |
| Needs Attention | `tab=needs-attention` | Unassigned, blocked-health, overdue, and stale-operation tasks | `user-tasks:supervise` |

Tabs that are not allowed by the capability response are hidden. A deep link to a hidden tab displays the permitted default tab rather than an authorization error from the UI. The API remains authoritative.

### Queue/list behavior

The list is server-paged and server-filtered. It follows the existing Studio `MudTable.ServerData` pattern used by Secrets and Workflow Definitions.

The table displays safe summary fields only:

- task title and task type/key;
- status and priority;
- assignee or candidate summary, subject to disclosure policy;
- due date and overdue indicator;
- workflow definition name/version and a safe workflow instance reference;
- created/updated timestamps;
- an action menu derived from the row's `allowedActions`.

The row action menu may contain **Open**, **Claim**, **Release**, **Assign**, **Complete**, **Cancel**, and **Copy link**, but only when both the server capability response and the row's `allowedActions` allow the action. A row click opens the detail route. The list never performs a client-only mutation or assumes that a visible button is authorized.

The toolbar provides:

- Search over the server-approved task title, task key/type, and workflow summary fields.
- Status filter using the canonical lifecycle states; overdue is a derived filter.
- Priority filter.
- Due-date filter (`overdue`, `today`, `thisWeek`, `noDueDate`, or explicit range).
- Workflow definition/instance filters when the actor has the corresponding workflow visibility.
- A reset-filters action.

Search and filters are debounced, cancellable, and applied before paging. Empty states distinguish **No tasks**, **No tasks match these filters**, and **Tasks are unavailable**. Errors preserve the current filters and offer retry.

### URL state

The list serializes only non-sensitive view state:

```text
/workflows/user-tasks?tab=assigned&status=Assigned&priorityFrom=75&due=overdue&search=approval&cursor=opaque&pageSize=25&sort=due&direction=asc
```

Supported query keys are `tab`, repeated `status`, `priorityFrom`, `priorityTo`, `due`, `from`, `to`, `search`, `workflowDefinitionId`, `workflowInstanceId`, `cursor`, `pageSize`, `sort`, and `direction`. Unknown or invalid values are ignored and replaced by safe defaults. Tenant, actor identity, form data, access tokens, task payloads, and protected fields must never appear in the URL.

Changing a filter resets the page to the first page. Reloading or sharing a URL restores the same safe list state, subject to the current actor's capabilities. The server's canonical sort and page-size limits win over client values.

## Task Detail

The detail page is a responsive, single-task workspace. It loads a safe task projection and a server-provided capability/action projection; it does not load the raw workflow instance state.

The desktop layout contains a task header, primary response surface, and contextual summary. On narrow screens it becomes a single column with a sticky bottom action bar. All actions remain keyboard accessible and have accessible names that include the task title where useful.

Detail tabs are:

1. **Task** — title, description, due/priority/status, participant summary, and the form or outcome controls.
2. **Workflow** — safe definition and instance context, shown only when the response authorizes it.
3. **History** — claims, assignments, releases, and completion audit entries, shown only when the response authorizes it.

The Task tab is always the landing tab. Tabs that are not disclosed are omitted rather than rendered disabled. A task that has become completed, cancelled, expired, or stale is rendered read-only with its final status and outcome where disclosure permits.

### Claim and completion

- **Claim** is an atomic server operation. On success Studio refreshes the task projection and opens the response surface.
- **Release** returns a claimed task to its eligible queue when allowed.
- **Complete** validates the response both client-side and server-side. The server may require a current claim, an expected task revision, or a participant authorization check.
- Stale task responses produce a conflict state with **Reload task** and a clear explanation; Studio must not overwrite another actor's claim or completion.
- Completion success shows the outcome and workflow continuation state when safe, then returns to the originating list while preserving its URL filters.
- Repeated completion of the same task is treated as an idempotent success only when the submitted response matches the already recorded completion; otherwise Studio displays the server conflict.

## Protected Disclosure

User Tasks can contain business data and workflow-derived values. The Studio task module must treat all task projections and form responses as protected data.

- Render only fields explicitly returned by the server's disclosure projection.
- Never expose bookmark IDs/hashes, workflow input dictionaries, variables, activity execution state, bearer/SAS tokens, internal storage payloads, or provider credentials.
- Do not put form data, participant claims, or protected description text in URLs, browser titles, telemetry, exception messages, clipboard content, or client-side logs.
- Sensitive form fields are masked by default and are not copied into diagnostics or analytics. A server-provided `canReveal`/`disclosure` decision is required before any reveal control is rendered.
- Workflow and history tabs use safe summaries and server-rendered audit values only; Studio must not reconstruct them from generic workflow APIs.
- If the server marks a task or field as `guestVisible: false`, it is absent from the guest page even if it is visible to an authenticated operator.

## Guest Task Page

The guest page is a narrow completion surface for a signed, expiring, single-purpose task token. It is not a task inbox and must not require or create an Elsa Identity account.

The token is supplied as an opaque query value (`t`). Studio passes it to the guest-task API without decoding, persisting, logging, or displaying it. The server resolves tenant, task, actor constraints, expiry, disclosure, and allowed operations.

Guest behavior:

- Show only the task title, explicitly guest-visible description/form fields, due/status information, and server-approved outcomes.
- Do not show workflow definition/instance identifiers, participant IDs, claims, internal links, task history, or protected fields.
- Do not provide list navigation, menu navigation, claim/reassign/cancel actions, or arbitrary workflow links.
- Require explicit submit confirmation when completion is irreversible.
- On success, show a confirmation/status page and invalidate or consume the token according to the server response.
- On expired, revoked, already completed, or invalid token, show a generic safe error with no task existence oracle.
- Do not silently retry a state-changing request. Retry is allowed only for an idempotent read.

## Capabilities and Action Projection

Studio obtains a User Tasks capability descriptor before displaying navigation or choosing controls. The descriptor is server-generated for the current actor and tenant and may include:

```json
{
  "enabled": true,
  "canList": true,
  "canRead": true,
  "canReadAll": false,
  "canClaim": true,
  "canComplete": true,
  "canRelease": true,
  "canAssign": false,
  "canCancel": false,
  "canCreateGuestLinks": false,
  "canViewProtected": false,
  "participantPicker": false,
  "realtime": true,
  "pollingIntervalSeconds": 30
}
```

The exact capability transport may be part of the User Tasks REST capability endpoint, but the Studio client must not infer permissions from the presence of an endpoint or an Elsa Identity role. Each task row/detail response also supplies `allowedActions`, `disclosure`, and `revision` so that per-task authorization and concurrency are explicit.

When capabilities are unavailable, Studio fails closed for mutations and hides the User Tasks menu unless the remote feature's safe default explicitly permits read-only discovery. A `403` from any API remains an authorization result, not a client error to suppress.

## Realtime Invalidation and Polling

The module may subscribe to a User Tasks SignalR hub when the capability descriptor advertises `realtime`. The hub sends tenant- and actor-authorized invalidation envelopes, not task data:

```json
{
  "kind": "task.updated",
  "taskId": "task-123",
  "revision": 7,
  "occurredAt": "2026-01-01T12:00:00Z"
}
```

Supported invalidation kinds are `task.created`, `task.updated`, `task.claimed`, `task.released`, `task.assigned`, `task.completed`, `task.cancelled`, and `task.expired`. The server enforces tenant and actor visibility before publishing an envelope. The envelope must not contain title, description, participant IDs, form values, workflow input, or protected audit data.

Studio behavior:

- Debounce bursts of invalidations into one list reload.
- Reload the visible list when an invalidation can affect its filters; preserve the selected page where possible and reset only when the server indicates that the current page is no longer valid.
- Reload the open detail when its `taskId` is invalidated. Do not apply partial event data to local state.
- If SignalR is unavailable or disconnected, fall back to visibility-aware polling at the server-advertised interval (30 seconds by default), stopping while the document is hidden and when the component is disposed.
- Reconnect with bounded backoff, then perform a full list/detail reload after reconnect to close gaps.
- Guest pages do not subscribe to a task inbox or realtime stream.

## Workflow Designer Activity

The User Task activity is contributed to the activity catalog under a human-task category and uses the normal activity descriptor/input/output editor. The activity is blocking: execution creates a task and suspends until an authorized completion resumes it.

Required designer inputs:

| Field | Behavior |
| --- | --- |
| Title | Required, expression-capable string; safe display text for the task list/detail. |
| Summary and instructions | Optional expression-capable safe summary and separately protected instructions. |
| Reference, tags, and task type | Optional safe values used for filtering, reporting, and host form/participant policies. |
| Requester | Optional informational participant reference that grants no access. |
| Assignee | Optional expression-capable opaque participant reference. It is not an Elsa Identity user ID. |
| Candidate users and groups | Optional collections of opaque participant references using the canonical user/group kinds. |
| Membership and exclusions | Live or snapshot membership, excluded users, and optional reason-required manager override. |
| Due date | Optional expression-capable UTC timestamp; Studio displays the configured timezone explicitly. |
| Priority | Numeric value from 0 through 100 with default 50. |
| Form/actions | Optional provider-neutral form reference and literal immutable action keys with expression-capable labels. |
| Timeout/cancellation | Explicit enablement for the reserved `Timeout` and `Cancelled` outcomes. |
| Invitations | Optional guest definitions including challenge provider, bounded expiry, allowed actions, and explicit bearer-only opt-in. |

Outputs:

- `TaskId` — the durable task identifier.
- `Outcome` — the selected completion outcome.
- `Response` or `FormData` — the validated completion payload according to the activity contract.
- `CompletedBy` and `CompletedAt` — safe completion metadata when configured by the activity.

All inputs remain expression-capable unless the activity descriptor marks a field as non-expression metadata. The generic Studio editor must preserve expressions and bindings when a participant picker or form editor is not installed. The editor must not fetch or persist resolved participant names as workflow definition values.

The activity editor should show a clear note that a task is created only when the workflow runs, and that changing the definition does not mutate already-created tasks.

## Optional Participant Picker

The User Tasks Studio module may consume a host-provided participant picker. The picker is an optional contribution, not a dependency on Elsa Identity.

Suggested contribution shape:

```csharp
public interface IUserTaskParticipantPicker
{
    ValueTask<ParticipantPickerResult?> PickAsync(
        ParticipantPickerContext context,
        CancellationToken cancellationToken = default);
}
```

The context includes the current opaque values, allowed participant kinds, task key, tenant scope, and whether multiple selection is allowed. Results contain opaque `kind`/`id` references and optional display labels. Labels are presentation-only; the workflow definition persists the references. If no picker is registered, Studio renders a safe text/reference editor and preserves unknown participant kinds.

The picker must obey the server's participant lookup authorization, avoid returning hidden users/groups, and never be used to authorize claim or completion actions.

## Client Contracts and Verification

The paired Studio module should provide a Refit client whose DTOs mirror the User Tasks REST contract, plus:

- menu/feature registration following the existing Secrets module pattern;
- list/detail/guest routes above;
- server-paged list loading with cancellation and URL-state restoration;
- task-form validation and conflict handling;
- capability/action/disclosure rendering;
- optional participant-picker registration;
- SignalR invalidation subscription with polling fallback.

Studio verification must cover:

- menu gating and hidden tabs for missing capabilities;
- every URL filter, invalid-value normalization, page reset, and browser navigation;
- safe row/detail rendering and absence of protected data in URLs/logs/telemetry;
- claim, release, complete, conflict, already-completed, and server-error states;
- responsive detail and guest-page behavior;
- fallback text participant editing with no picker;
- SignalR debounce/reconnect, polling start/stop, and post-reconnect reload;
- preservation of expressions/bindings and disclosure defaults in the designer.

# Quickstart: Elsa User Tasks

**Status**: Approved  
**Last reviewed**: 2026-08-17

This walkthrough creates a workflow-bound invoice approval, executes it, lists the candidate task, claims it as an external user, and completes it with a typed action and form payload. The examples use the planned `Elsa.UserTasks` REST contract and do not require `Elsa.Identity`.

## Prerequisites

- An Elsa Server with workflow management, workflow runtime, and the User Tasks module enabled.
- A host identity provider that issues a bearer token with a stable subject and optional group claims.
- The `user-tasks:view`, `user-tasks:claim`, and `user-tasks:complete` permissions for the worker token. A manager token additionally needs `user-tasks:assign` and `user-tasks:supervise`.
- An installed form provider if the task uses `formReference`.

The Core module defaults to an in-memory repository for development and tests. A durable host adds the User Tasks EF Core persistence package and its provider-specific shell feature, following the same package split used by `Elsa.Secrets`:

```csharp
services.AddElsa(elsa =>
{
    elsa.UseUserTasks();
});

// Enable the provider-specific User Tasks persistence feature in the host.
// The feature registers the User Task repository, DbContext, mappings, and migrations.
// Use the SQLite, SQL Server, PostgreSQL, MySQL, or Oracle package selected by the host.
```

Do not add an `Elsa.Identity` reference merely to run this walkthrough. The default claims resolver reads the configured subject/provider/group claims; a host may replace it with `IUserTaskIdentityResolver` and `IUserTaskAccessPolicy`.

## Create the workflow

In Elsa Studio, create a workflow definition named `invoice-review` with a sequence containing one `UserTask` activity:

```csharp
new UserTask
{
    Id = "ReviewInvoice",
    Title = "Review invoice",
    Summary = "Approve or reject invoice INV-1042.",
    Instructions = "Check the supplier, amount, and purchase order before deciding.",
    Reference = "INV-1042",
    TaskType = "invoice-approval",
    CandidateGroups =
    [
        new ParticipantReference(
            TenantId: "acme",
            Provider: "acme-directory",
            Type: UserTaskParticipantType.Group,
            Id: "accounts-payable")
    ],
    Priority = 75,
    DueAt = "= now() + duration(\"P2D\")",
    FormReference = new UserTaskFormReference
    {
        ProviderName = "acme-forms",
        Key = "invoice-review",
        Binding = "version:3"
    },
    Actions =
    [
        new UserTaskActionDefinition { Key = "approved", Label = "Approve" },
        new UserTaskActionDefinition { Key = "rejected", Label = "Reject" }
    ],
    TaskData = new
    {
        invoiceNumber = "INV-1042",
        amount = 1250.00,
        currency = "EUR",
        supplier = "Northwind Supplies"
    }
};
```

The C# above describes the activity shape used by the workflow builder. Studio serializes the same values into the workflow definition. `CandidateGroups` stores an opaque external reference; the string `accounts-payable` is not an Elsa group ID. The activity blocks on its User Task bookmark and exposes the selected action as the workflow outcome after completion.

If a form provider is not installed, omit `FormReference` and complete with an action only. Without a form, arbitrary business data is not accepted by the Core completion endpoint.

## Execute the workflow

After publishing the definition, execute it through the existing workflow runtime endpoint. Set `DEFINITION_ID` to the published definition ID returned by the workflow-definition API:

```bash
export ELSA_BASE_URL="https://localhost:5001"
export DEFINITION_ID="invoice-review"
export WORKER_TOKEN="<worker-access-token>"

curl --fail-with-body -i \
  -X POST "$ELSA_BASE_URL/workflow-definitions/$DEFINITION_ID/execute" \
  -H "Authorization: Bearer $WORKER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "correlationId": "order-1042",
    "input": {
      "invoiceNumber": "INV-1042"
    }
  }'
```

The response includes the normal workflow state and the `x-elsa-workflow-instance-id` response header. Save that value for the task detail and workflow deep link.

## List the candidate queue

The accounts-payable worker supplies a token whose external group claims include `acme-directory/accounts-payable`. The User Tasks API evaluates the host identity policy and returns summary-safe rows:

```bash
curl --fail-with-body \
  "$ELSA_BASE_URL/user-tasks?scope=available&taskType=invoice-approval&limit=20&includeTotalCount=true" \
  -H "Authorization: Bearer $WORKER_TOKEN" \
  -H "Accept: application/json"
```

An available row has this shape (the exact response may include links and tenant metadata):

```json
{
  "items": [
    {
      "id": "01JUSER-TASK-1042",
      "title": "Review invoice",
      "summary": "Approve or reject invoice INV-1042.",
      "reference": "INV-1042",
      "taskType": "invoice-approval",
      "status": "Available",
      "assignee": null,
      "priority": 75,
      "dueAt": "2026-08-19T10:30:00Z",
      "isOverdue": false,
      "allowedActions": ["claim"],
      "dataAccess": "summary",
      "revision": 1
    }
  ],
  "totalCount": 1
}
```

The row does not expose `Instructions`, `TaskData`, or the form payload. A worker can search by reference, status, priority, due-date range, workflow definition/instance, and stable cursor/sort parameters. Studio uses the same endpoint for its **Available** tab.

## Claim the task

Claiming is a separate, concurrency-safe operation. The `expectedRevision` prevents a stale list row from claiming a task that another worker already took:

```bash
export TASK_ID="01JUSER-TASK-1042"

curl --fail-with-body \
  -X POST "$ELSA_BASE_URL/user-tasks/$TASK_ID/claim" \
  -H "Authorization: Bearer $WORKER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"expectedRevision":1}'
```

A successful claim changes the task to `Assigned`, sets the assignee to the external subject from the token, increments `revision`, and returns `allowedActions` containing `release` and `complete`.

## Read protected details

After claiming, the worker can fetch the full detail. The same request made with a different candidate token returns only the safe projection or `404`, depending on the host access policy:

```bash
curl --fail-with-body \
  "$ELSA_BASE_URL/user-tasks/$TASK_ID" \
  -H "Authorization: Bearer $WORKER_TOKEN" \
  -H "Accept: application/json"
```

The assigned response includes the pinned form reference, protected instructions, task data, workflow definition/instance links, action definitions, revision, and a safe lifecycle timeline. It still omits secrets that the host form provider does not authorize.

## Complete with a form action

The form provider validates and normalizes `data`. The `actionKey` must match a configured action; it is also emitted as the User Task activity outcome. The `operationId` makes a network retry idempotent:

```bash
export OPERATION_ID="6d1cf9d1-22e4-4f4d-a13e-0f9a50b754d8"

curl --fail-with-body -i \
  -X POST "$ELSA_BASE_URL/user-tasks/$TASK_ID/complete" \
  -H "Authorization: Bearer $WORKER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "expectedRevision": 2,
    "operationId": "'"$OPERATION_ID"'",
    "actionKey": "approved",
    "data": {
      "purchaseOrder": "PO-1042",
      "approvedAmount": 1250.00
    }
  }'
```

The endpoint returns `202 Accepted` while the task is `Completing`. Poll the task resource until `Completed`:

```bash
until curl --fail-with-body -s \
  "$ELSA_BASE_URL/user-tasks/$TASK_ID" \
  -H "Authorization: Bearer $WORKER_TOKEN" \
  | rg -q '"status"[[:space:]]*:[[:space:]]*"Completed"'; do
  sleep 1
done
```

When the bookmark resumes, the workflow receives `UserTaskResult` with `actionKey = "approved"`, the provider-normalized data, the external completing participant, and the completion timestamp. A workflow branch can use the `approved` or `rejected` outcome to continue execution.

Submitting the same `operationId` and payload again returns the original completion result or an equivalent idempotent response. Reusing it with a different action or data returns `409 Conflict`.

## Release or reassign

If the worker cannot finish, release clears the assignee and returns the task to the candidate queue:

```bash
curl --fail-with-body \
  -X POST "$ELSA_BASE_URL/user-tasks/$TASK_ID/release" \
  -H "Authorization: Bearer $WORKER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"expectedRevision":2}'
```

A manager can assign the task to a specific external user. Managers cannot complete on another user's behalf; they must assign it to an accountable user first:

```bash
export MANAGER_TOKEN="<manager-access-token>"

curl --fail-with-body \
  -X POST "$ELSA_BASE_URL/user-tasks/$TASK_ID/assign" \
  -H "Authorization: Bearer $MANAGER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "expectedRevision": 3,
    "assignee": {
      "provider": "acme-directory",
      "type": "user",
      "id": "u-2048"
    },
    "reason": "Accounts-payable coverage for the month-end queue."
  }'
```

## Inspect the manager queue and audit history

Managers with `user-tasks:supervise` can query all task scopes and see the safe event timeline:

```bash
curl --fail-with-body \
  "$ELSA_BASE_URL/user-tasks?scope=all&status=Assigned&priorityFrom=50&limit=50" \
  -H "Authorization: Bearer $MANAGER_TOKEN" \
  -H "Accept: application/json"

curl --fail-with-body \
  "$ELSA_BASE_URL/user-tasks/$TASK_ID/events" \
  -H "Authorization: Bearer $MANAGER_TOKEN" \
  -H "Accept: application/json"
```

Events include created, claimed, released, assigned, reassigned, completion requested, completed, and canceled transitions with timestamps, external participant references, revisions, and safe reasons. They do not include protected task or form payloads.

## Validate the integration

Run these scenarios against an in-memory host first, then repeat with the selected EF Core provider:

1. Two candidate workers claim the same task; one succeeds and the other receives `409`.
2. A candidate can list a summary but cannot fetch protected data before claiming.
3. A claimed worker can release and another candidate can claim the task.
4. A completion retry with the same `operationId` does not resume the bookmark twice.
5. A stale `expectedRevision` cannot complete or reassign a task.
6. A task with no assignment is manager-only and produces a design/runtime warning.
7. A missing form provider produces a blocking health issue and never renders arbitrary HTML.
8. The workflow receives the configured action outcome and typed result after `Completing` reaches `Completed`.
9. A canceled workflow removes the bookmark and records the User Task as canceled.
10. No request or persistence path requires an `Elsa.Identity` user or group.

The implementation should turn each scenario into a Core integration or component test and keep this document synchronized with the REST and runtime contracts.

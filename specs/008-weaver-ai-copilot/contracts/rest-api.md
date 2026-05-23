# REST and Streaming API Contract: Weaver AI Copilot Platform

## POST `/ai/chat`

Starts or continues a Weaver chat turn.

**Request**

```json
{
  "conversationId": "optional-existing-conversation-id",
  "message": "Explain why this workflow failed",
  "attachments": [
    {
      "kind": "WorkflowInstance",
      "referenceId": "instance-123",
      "timeRange": {
        "from": "2026-05-20T08:00:00Z",
        "to": "2026-05-20T10:00:00Z"
      }
    }
  ],
  "agent": "instance-diagnostics"
}
```

**Response**

Streams `AIStreamEvent` records as Server-Sent Events or an equivalent server-owned streaming transport.

```json
{
  "type": "assistant.delta",
  "conversationId": "conversation-123",
  "sequence": 12,
  "timestamp": "2026-05-20T10:15:00Z",
  "data": {
    "messageId": "message-456",
    "content": "The failure happened in..."
  }
}
```

**Stream Event Types**

- `conversation.started`
- `assistant.delta`
- `assistant.completed`
- `tool.started`
- `tool.progress`
- `tool.completed`
- `tool.failed`
- `proposal.created`
- `proposal.updated`
- `conversation.completed`
- `conversation.error`

## GET `/ai/tools`

Returns available tools for the current user, tenant, and optional agent scope.

**Query**: `agent`, `mutability`, `dangerLevel`

**Response**

```json
[
  {
    "name": "workflow.getDefinition",
    "displayName": "Get workflow definition",
    "mutability": "ReadOnly",
    "dangerLevel": "Low",
    "permissions": ["read:workflows"],
    "tenantBehavior": "TenantScoped",
    "auditBehavior": "RecordInvocation",
    "schema": {}
  }
]
```

## GET `/ai/capabilities`

Returns provider-neutral Weaver capabilities for Studio.

**Response**

```json
{
  "streaming": true,
  "conversationPersistence": true,
  "proposalReview": true,
  "supportedAttachmentKinds": [
    "WorkflowDefinition",
    "WorkflowInstance",
    "ActivitySelection",
    "DiagnosticsScope",
    "TimeRange"
  ],
  "agents": [
    {
      "name": "workflow-author",
      "displayName": "Workflow author",
      "description": "Creates safe workflow proposals"
    }
  ]
}
```

## GET `/ai/proposals/{id}`

Returns proposal details when the current user can view the referenced workflow or target area.

## POST `/ai/proposals/{id}/approve`

Marks a proposal as approved by an authorized user. Approval does not apply the proposal. In MVP, the same authorized user may request, approve, and apply the proposal.

## POST `/ai/proposals/{id}/reject`

Rejects a proposal and records the reason.

## POST `/ai/proposals/{id}/apply`

Validates and applies an approved proposal.

**Apply Rules**

- User must have apply permission.
- Proposal must be approved.
- Proposal baseline must match current workflow revision when applicable.
- Validation must pass.
- Apply emits audit records and returns the created or updated workflow reference.

## Streaming Reconnect

If the streaming client disconnects during a turn, the server continues the turn for a configurable grace window. Authorized clients may reconnect with the conversation ID and recover durable outputs produced during the grace window.

## Runtime Trend Scope

Runtime trend tools inspect attached references plus an explicit user-selected time range and diagnostics scope by default. Tenant-wide runtime analysis requires a future explicit scope expansion.

## Error Behavior

- `400`: Invalid request, unsupported attachment kind, invalid proposal transition.
- `401`: Unauthenticated.
- `403`: Missing permission, tenant mismatch, or denied tool/proposal access.
- `404`: Context reference, conversation, or proposal not found.
- `409`: Stale proposal baseline or invalid proposal state.
- `422`: Proposal validation failed.
- `503`: Provider runtime unavailable.

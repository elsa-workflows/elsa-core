# REST API Contract: Weaver Grounding Tools

This feature extends existing Weaver APIs without exposing provider SDK types.

## GET `/ai/capabilities`

Add grounding capability descriptors to the existing response.

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
  "grounding": [
    {
      "name": "activities",
      "displayName": "Activity catalog",
      "enabled": true,
      "toolNames": [
        "activities.search",
        "activities.getDescriptor"
      ],
      "supportedAttachmentKinds": [
        "ActivitySelection"
      ]
    }
  ]
}
```

## GET `/ai/tools`

Returns the grounding tools available for the current actor, tenant, and optional agent scope. Existing `AIToolDefinition` shape remains the contract.

## POST `/ai/chat`

Existing chat request shape is retained. Grounding uses attachments and available tools.

```json
{
  "conversationId": "conversation-123",
  "message": "Create a workflow that starts on HTTP POST and sends an email",
  "agent": "workflow-author",
  "attachments": [
    {
      "kind": "ActivitySelection",
      "referenceId": "activities:http,email"
    }
  ]
}
```

## Stream Events

Existing stream event shape is retained. Grounding tools should map to current tool lifecycle events:

- `tool.started`
- `tool.result`
- `proposal.created`
- `conversation.error`
- `conversation.completed`

Tool result data should include `toolName`, `toolCallId`, `status`, `summary`, and optional redacted result data.

## Error Behavior

- `400`: Invalid search filters, unsupported attachment kind, invalid draft payload.
- `403`: Missing permission, tenant mismatch, denied tool access.
- `404`: Activity, workflow, instance, incident, or proposal not found.
- `409`: Stale workflow baseline.
- `422`: Draft validation failed.
- `503`: Provider runtime unavailable; grounding capability endpoints may still work.

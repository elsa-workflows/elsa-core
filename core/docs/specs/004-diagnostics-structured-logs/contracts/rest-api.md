# REST API Contract: Diagnostics Structured Logs

All endpoints use the Elsa API route prefix and require `read:diagnostics:structured-logs`.

## Get Recent Structured Logs

`POST /diagnostics/structured-logs/recent`

Request body is `StructuredLogFilter`.

Response body is `RecentStructuredLogsResult`.

```json
{
  "items": [
    {
      "id": "01h...",
      "sequence": 42,
      "timestamp": "2026-05-10T12:00:00Z",
      "receivedAt": "2026-05-10T12:00:00Z",
      "level": "Information",
      "category": "Elsa.Workflows.Runtime",
      "eventId": 1001,
      "eventName": "WorkflowStarted",
      "message": "Workflow order-123 started",
      "messageTemplate": "Workflow {WorkflowInstanceId} started",
      "exception": null,
      "scopes": {
        "TenantId": "tenant-a"
      },
      "properties": {
        "WorkflowInstanceId": "order-123"
      },
      "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
      "spanId": "00f067aa0ba902b7",
      "correlationId": "corr-123",
      "tenantId": "tenant-a",
      "workflowDefinitionId": "orders",
      "workflowInstanceId": "order-123",
      "sourceId": "local"
    }
  ],
  "droppedCount": 0
}
```

## List Structured Log Sources

`GET /diagnostics/structured-logs/sources`

Response body is a collection of `StructuredLogSource`.

```json
[
  {
    "id": "local",
    "name": "elsa-server",
    "machineName": "dev-machine",
    "processId": 12345,
    "processName": "Elsa.Server.Web",
    "podName": null,
    "namespace": null,
    "containerName": null,
    "nodeName": null,
    "startedAt": "2026-05-10T11:59:00Z",
    "lastSeen": "2026-05-10T12:00:00Z",
    "status": "Healthy"
  }
]
```

## Compatibility Boundary

The previous `/server-logs/*` route names are not part of this unpublished feature's final contract. This module does not capture direct stdout/stderr writes.

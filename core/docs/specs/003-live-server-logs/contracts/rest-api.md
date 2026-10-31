# Contract: Server Logs REST API

All endpoints use the Elsa API route prefix and require `read:server-logs`.

## List Recent Logs

`GET /server-logs/recent`

Query parameters:

- `minimumLevel`
- `level`
- `categoryPrefix`
- `text`
- `tenantId`
- `workflowDefinitionId`
- `workflowInstanceId`
- `traceId`
- `correlationId`
- `sourceId`
- `from`
- `to`
- `take`

Response:

```json
{
  "items": [
    {
      "id": "evt-1",
      "timestamp": "2026-05-06T10:00:00Z",
      "level": "Information",
      "category": "Elsa.Workflows.Runtime",
      "message": "Workflow instance started",
      "sourceId": "elsa-server-1"
    }
  ],
  "droppedEvents": 0
}
```

## List Sources

`GET /server-logs/sources`

Response:

```json
{
  "items": [
    {
      "id": "elsa-server-1",
      "displayName": "elsa-server-7fd9c8b9c4-a2k1",
      "serviceName": "elsa-server",
      "podName": "elsa-server-7fd9c8b9c4-a2k1",
      "containerName": "elsa-server",
      "namespace": "production",
      "status": "Connected",
      "lastSeen": "2026-05-06T10:00:02Z"
    }
  ]
}
```

## Validation

- `take` above the configured maximum is clamped or rejected consistently.
- Unknown levels return validation errors.
- Date filters are UTC-normalized.

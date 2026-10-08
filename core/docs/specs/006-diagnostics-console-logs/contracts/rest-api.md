# REST API Contract: Diagnostics Console Logs

All endpoints use the Elsa API route prefix and require `read:diagnostics:console-logs`.

## Get Recent Console Logs

`POST /diagnostics/console-logs/recent`

Request body is `ConsoleLogFilter`.

```json
{
  "sourceId": "local",
  "stream": "stdout",
  "query": "workflow",
  "from": "2026-05-18T10:00:00Z",
  "to": "2026-05-18T10:05:00Z",
  "limit": 100
}
```

Response body is `RecentConsoleLogsResult`.

```json
{
  "items": [
    {
      "id": "01j...",
      "timestamp": "2026-05-18T10:00:01Z",
      "receivedAt": "2026-05-18T10:00:01Z",
      "sequence": 42,
      "stream": "stdout",
      "text": "Workflow order-123 started",
      "source": {
        "id": "local",
        "displayName": "elsa-server",
        "serviceName": "Elsa.Server.Web",
        "processId": 12345,
        "machineName": "dev-machine",
        "podName": null,
        "containerName": null,
        "namespace": null,
        "nodeName": null,
        "lastSeen": "2026-05-18T10:00:01Z",
        "health": "connected"
      },
      "truncated": false,
      "dropped": null
    }
  ],
  "dropped": []
}
```

Rules:

- The server clamps `limit` to `ConsoleLogsOptions.MaxRecentQuerySize`.
- Returned text and source metadata are redacted.
- ANSI escape sequences are stripped by default unless the host preserves them.
- Results are ordered deterministically by received order with a stable source-aware tiebreaker.

## List Console Log Sources

`GET /diagnostics/console-logs/sources`

Response body is a collection of `ConsoleLogSource`.

```json
[
  {
    "id": "local",
    "displayName": "elsa-server",
    "serviceName": "Elsa.Server.Web",
    "processId": 12345,
    "machineName": "dev-machine",
    "podName": null,
    "containerName": null,
    "namespace": null,
    "nodeName": null,
    "lastSeen": "2026-05-18T10:00:01Z",
    "health": "connected"
  }
]
```

Rules:

- Source listing requires the same `read:diagnostics:console-logs` permission as line access.
- Sensitive source metadata is redacted before providers store or return it.
- Stale and disconnected sources remain listable while their recent history is still retained.

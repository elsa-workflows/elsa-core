# Contract: Backend API

Base path:

```text
/diagnostics/opentelemetry
```

All endpoints require the OpenTelemetry diagnostics view permission.

Core owns these normalized response DTOs. Studio mirrors them for client models and rendering, but does not parse OTLP protobuf payloads or expose raw OTLP transport types in UI code. Additive response fields are allowed; removals or semantic changes require coordinated Core and Studio updates.

## Resources

```text
POST /diagnostics/opentelemetry/resources/search
```

Request:

```json
{
  "serviceName": "elsa-api",
  "status": "active",
  "text": "worker"
}
```

Response:

```json
{
  "items": [
    {
      "resourceKey": "elsa-api/instance-1",
      "serviceName": "elsa-api",
      "serviceInstanceId": "instance-1",
      "serviceVersion": "3.6.0",
      "deploymentEnvironment": "development",
      "status": "active",
      "firstSeen": "2026-05-25T09:00:00Z",
      "lastSeen": "2026-05-25T09:03:00Z",
      "attributes": {}
    }
  ],
  "totalCount": 1
}
```

## Trace Search

```text
POST /diagnostics/opentelemetry/traces/search
```

Request:

```json
{
  "resourceKey": "elsa-api/instance-1",
  "serviceName": "elsa-api",
  "traceId": null,
  "workflowInstanceId": null,
  "workflowDefinitionId": null,
  "status": "error",
  "text": "SendHttpRequest",
  "from": "2026-05-25T09:00:00Z",
  "to": "2026-05-25T10:00:00Z",
  "take": 100
}
```

Response:

```json
{
  "items": [
    {
      "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
      "rootSpanId": "00f067aa0ba902b7",
      "name": "workflow.execute",
      "startTime": "2026-05-25T09:01:00Z",
      "endTime": "2026-05-25T09:01:03Z",
      "durationMs": 3000,
      "status": "error",
      "spanCount": 12,
      "errorCount": 1,
      "workflowInstanceId": "wf-1",
      "workflowDefinitionId": "order-workflow",
      "resourceKeys": ["elsa-api/instance-1"]
    }
  ],
  "totalCount": 1,
  "droppedCount": 0
}
```

## Trace Detail

```text
GET /diagnostics/opentelemetry/traces/{traceId}
```

Response includes trace summary, ordered spans, OTLP log records for the trace when available, and related resource records.

## Metrics

```text
POST /diagnostics/opentelemetry/metrics/search
```

Request filters by resource, instrument name, attribute text, and time range.

Response includes instruments and bounded recent series points.

## OTLP Logs

```text
POST /diagnostics/opentelemetry/logs/search
```

Request filters by resource, trace ID, span ID, severity, text, and time range.

Response returns OTLP log records only. Elsa first-party structured logs remain under `/diagnostics/structured-logs`.

## Collector Configuration

```text
GET /diagnostics/opentelemetry/collector-configuration
```

Response:

```json
{
  "httpEndpoint": "http://localhost:5000/elsa/otlp",
  "grpcEndpoint": null,
  "grpcEnabled": false,
  "grpcDisabledReason": "gRPC ingestion is not enabled for this host.",
  "requiredHeaders": ["x-otlp-api-key"],
  "isLoopbackOnly": true,
  "recommendedEnvironment": {
    "OTEL_EXPORTER_OTLP_ENDPOINT": "http://localhost:5000/elsa/otlp",
    "OTEL_EXPORTER_OTLP_PROTOCOL": "http/protobuf",
    "OTEL_SERVICE_NAME": "<service-name>",
    "OTEL_RESOURCE_ATTRIBUTES": "service.instance.id=<instance-id>"
  }
}
```

## Storage Diagnostics

```text
GET /diagnostics/opentelemetry/storage
```

Returns dropped counts and current resource/trace/series counts.

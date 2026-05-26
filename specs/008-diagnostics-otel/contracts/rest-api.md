# Contract: REST API

Base path:

```text
/diagnostics/opentelemetry
```

All endpoints require the OpenTelemetry diagnostics view permission.

Search endpoints accept a caller-provided limit capped by server options. Search results are ordered by newest receive time first unless a more specific sort is later added. Trace detail spans are returned in parent/child order with chronological ordering inside each sibling set.

## Resources

```text
POST /diagnostics/opentelemetry/resources/search
```

Filters by service name, status, and text.

## Trace Search

```text
POST /diagnostics/opentelemetry/traces/search
```

Filters by resource key, service name, trace ID, workflow instance ID, workflow definition ID, status, text, and time range.

## Trace Detail

```text
GET /diagnostics/opentelemetry/traces/{traceId}
```

Returns trace summary, ordered spans, related resources, and OTLP logs for the trace when available.

## Metrics

```text
POST /diagnostics/opentelemetry/metrics/search
```

Filters by resource, instrument name, attribute text, and time range.

## OTLP Logs

```text
POST /diagnostics/opentelemetry/logs/search
```

Filters by resource, trace ID, span ID, severity, text, and time range.

## Collector Configuration

```text
GET /diagnostics/opentelemetry/collector-configuration
```

Returns HTTP endpoint metadata, nullable/disabled gRPC metadata, required header names, loopback status, and recommended non-secret OTEL environment variables.

## Storage Diagnostics

```text
GET /diagnostics/opentelemetry/storage
```

Returns capacity, dropped counts, and current resource/trace/series counts.

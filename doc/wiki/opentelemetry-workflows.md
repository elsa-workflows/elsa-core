# OpenTelemetry Workflow Instrumentation

Elsa emits first-party OpenTelemetry-compatible workflow and activity instrumentation through `System.Diagnostics`.

To collect workflow traces, configure OpenTelemetry to listen to the `Elsa.Workflows` activity source. To collect workflow metrics, configure it to listen to the `Elsa.Workflows` meter.

```csharp
services.AddOpenTelemetry()
    .WithTracing(builder => builder.AddSource("Elsa.Workflows"))
    .WithMetrics(builder => builder.AddMeter("Elsa.Workflows"));
```

If you previously enabled workflow tracing through the `Elsa.OpenTelemetry` extension package, avoid enabling both the extension tracing middleware and the first-party workflow spans for the same host unless duplicate workflow and activity spans are acceptable. Both integrations publish to the `Elsa.Workflows` activity source so existing collectors can keep the same source configuration.

## Traces

Elsa creates spans around workflow execution cycles and activity execution. The spans include workflow and activity identifiers, definition metadata, status, tenant ID when available, and fault status. Workflow input, activity input, output payloads, headers, and variable values are not added as span attributes.

Faulted workflow and activity spans use `ActivityStatusCode.Error` and record the exception type as `exception.type` when an exception is available. Exception messages and stack traces are not added to spans or exception events by Elsa workflow instrumentation.

Outbound `SendHttpRequest` and `FlowSendHttpRequest` calls inject the current W3C trace context headers when an active workflow/activity span exists, so downstream services can continue the same trace without Elsa-specific middleware. Enable standard .NET HTTP client instrumentation in your OpenTelemetry setup when you want outbound HTTP spans for these calls.

## Metrics

The `Elsa.Workflows` meter emits:

- `elsa.workflow.started`
- `elsa.workflow.completed`
- `elsa.workflow.faulted`
- `elsa.activity.duration` in seconds

Metric tags use the same low-cardinality workflow and activity metadata as the spans where practical. The started counter omits execution status tags because it records the pre-execution boundary; completed and faulted counters include terminal workflow status tags.

## Elsa Diagnostics Collector

The `Elsa.Diagnostics.OpenTelemetry` module can collect OTLP HTTP/protobuf telemetry for local diagnostics and Studio visualization. The default ingestion routes are:

- `/elsa/otlp/v1/traces`
- `/elsa/otlp/v1/metrics`
- `/elsa/otlp/v1/logs`

Use the collector for development and focused troubleshooting. Production deployments should usually export to an external OpenTelemetry Collector or observability backend unless Elsa collector capacity, retention, and ingress security are deliberately configured for that environment.

### .NET Setup

Configure the .NET OpenTelemetry SDK to collect Elsa workflow instrumentation and export via OTLP:

```csharp
services.AddOpenTelemetry()
    .WithTracing(builder => builder.AddSource("Elsa.Workflows"))
    .WithMetrics(builder => builder.AddMeter("Elsa.Workflows"));
```

Then configure standard OTEL environment variables for the process:

```bash
OTEL_SERVICE_NAME=elsa-server
OTEL_RESOURCE_ATTRIBUTES=service.instance.id=local-dev
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:5000/elsa/otlp/v1
OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
OTEL_BSP_SCHEDULE_DELAY=1000
OTEL_METRIC_EXPORT_INTERVAL=1000
```

If the collector is exposed beyond loopback, also configure the required API key header. The header name is returned by the collector configuration API; do not expose the secret value through Studio or documentation.

### Polyglot Setup

Non-.NET services can use the same standard OTEL variables:

```bash
OTEL_SERVICE_NAME=worker
OTEL_RESOURCE_ATTRIBUTES=service.instance.id=worker-1
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:5000/elsa/otlp/v1
OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
```

Use each language SDK's OTLP exporter package. Studio identifies resources by `service.name` and `service.instance.id` when those attributes are present.

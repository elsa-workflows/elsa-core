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

Outbound `SendHttpRequest` and `FlowSendHttpRequest` calls use the configured `HttpClient`, so standard .NET HTTP client instrumentation can create child HTTP spans and propagate W3C trace context without Elsa-specific middleware. Enable HTTP client instrumentation in your OpenTelemetry setup when you want outbound HTTP spans and downstream context propagation.

## Metrics

The `Elsa.Workflows` meter emits:

- `elsa.workflow.started`
- `elsa.workflow.completed`
- `elsa.workflow.faulted`
- `elsa.activity.duration` in seconds

Metric tags use the same low-cardinality workflow and activity metadata as the spans where practical. The started counter omits execution status tags because it records the pre-execution boundary; completed and faulted counters include terminal workflow status tags.

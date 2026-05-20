# OpenTelemetry Workflow Instrumentation

Elsa emits first-party OpenTelemetry-compatible workflow and activity instrumentation through `System.Diagnostics`.

To collect workflow traces, configure OpenTelemetry to listen to the `Elsa.Workflows` activity source. To collect workflow metrics, configure it to listen to the `Elsa.Workflows` meter.

```csharp
services.AddOpenTelemetry()
    .WithTracing(builder => builder.AddSource("Elsa.Workflows"))
    .WithMetrics(builder => builder.AddMeter("Elsa.Workflows"));
```

## Traces

Elsa creates spans around workflow execution cycles and activity execution. The spans include workflow and activity identifiers, definition metadata, status, tenant ID when available, and fault status. Workflow input, activity input, output payloads, headers, and variable values are not added as span attributes.

Outbound `SendHttpRequest` and `FlowSendHttpRequest` calls inject the current W3C trace context into the request when an `Activity.Current` exists, allowing downstream HTTP spans to stay connected to the Elsa workflow trace.

## Metrics

The `Elsa.Workflows` meter emits:

- `elsa.workflow.started`
- `elsa.workflow.completed`
- `elsa.workflow.faulted`
- `elsa.activity.duration` in seconds

Metric tags use the same low-cardinality workflow and activity metadata as the spans where practical.

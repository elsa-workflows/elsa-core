# Quickstart: Diagnostics OpenTelemetry

## Enable Core Diagnostics

```csharp
services.AddElsa(elsa =>
{
    elsa.UseOpenTelemetryDiagnostics(options =>
    {
        options.HttpEndpointPath = "/elsa/otlp/v1";
        options.ApiKey = builder.Configuration["Diagnostics:OpenTelemetry:ApiKey"];
        options.TraceCapacity = 2_000;
        options.SpanCapacity = 10_000;
        options.MetricPointCapacity = 20_000;
        options.LogRecordCapacity = 10_000;
        options.ResourceCapacity = 500;
        options.SubscriberChannelCapacity = 1_000;
        options.MaxHttpRequestBodySize = 10 * 1024 * 1024;
    });
});
```

## Configure Elsa Workflow Telemetry Export

```csharp
services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddSource("Elsa.Workflows")
        .AddOtlpExporter())
    .WithMetrics(builder => builder
        .AddMeter("Elsa.Workflows")
        .AddOtlpExporter());
```

Development HTTP/protobuf environment:

```text
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:5000/elsa/otlp/v1
OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
OTEL_SERVICE_NAME=elsa-api
OTEL_RESOURCE_ATTRIBUTES=service.instance.id=local-elsa-api,deployment.environment=development
OTEL_BSP_SCHEDULE_DELAY=1000
OTEL_BLRP_SCHEDULE_DELAY=1000
OTEL_METRIC_EXPORT_INTERVAL=1000
```

When the collector is reachable from non-loopback addresses, configure the server-side API key and send only the configured header name/value through OTLP exporter headers. Collector configuration must advertise required header names but never secret values.

gRPC senders should only use gRPC when collector configuration advertises a gRPC endpoint:

```text
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
```

## Verify

```bash
dotnet test test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Elsa.Diagnostics.OpenTelemetry.UnitTests.csproj
dotnet test test/integration/Elsa.Diagnostics.OpenTelemetry.IntegrationTests/Elsa.Diagnostics.OpenTelemetry.IntegrationTests.csproj
```

Manual checks:

- Run a workflow and confirm a workflow trace appears through `/diagnostics/opentelemetry/traces/search` within 2 seconds.
- Query `/diagnostics/opentelemetry/traces/{traceId}` and confirm workflow/activity spans are parented and ordered.
- Query collector configuration and confirm HTTP metadata, nullable/disabled gRPC metadata, and required header names.
- Attempt non-loopback ingestion without the configured API key and confirm rejection.

## Scope Boundaries

This feature does not add durable OTEL persistence, vendor exporters, Studio UI, alerting, Kubernetes/Docker APIs, or historical `Elsa.OpenTelemetry` producer middleware. Production deployments should usually export to a standard OpenTelemetry Collector or observability platform unless Elsa collector capacity and security are deliberately configured.

# Quickstart: Diagnostics OpenTelemetry

## Core Setup

Enable the backend diagnostics module:

```csharp
services.AddElsa(elsa =>
{
    elsa.UseOpenTelemetryDiagnostics(options =>
    {
        options.HttpCollectorPath = "/elsa/otlp";
        options.RequireApiKeyForNonLoopback = true;
    });
});
```

Configure the Elsa server's own OpenTelemetry SDK to export workflow telemetry:

```csharp
services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddSource("Elsa.Workflows")
        .AddOtlpExporter())
    .WithMetrics(builder => builder
        .AddMeter("Elsa.Workflows")
        .AddOtlpExporter());
```

Development environment variables for HTTP/protobuf:

```text
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:5000/elsa/otlp
OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
OTEL_SERVICE_NAME=elsa-api
OTEL_RESOURCE_ATTRIBUTES=service.instance.id=local-elsa-api,deployment.environment=development
OTEL_BSP_SCHEDULE_DELAY=1000
OTEL_BLRP_SCHEDULE_DELAY=1000
OTEL_METRIC_EXPORT_INTERVAL=1000
```

For gRPC senders when the backend advertises a gRPC endpoint:

```text
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
```

## Studio Setup

Register the Studio module in the Studio bundle or host:

```csharp
services.AddOpenTelemetryDiagnosticsModule(backendApiConfig);
```

Open:

```text
/diagnostics/opentelemetry
```

Expected views:

- Resources: active services and instances sending telemetry.
- Traces: recent trace summaries with filters.
- Trace detail: span waterfall, span attributes/events, related OTLP logs, and Structured Logs links.
- Metrics: instruments and bounded recent points.
- Logs: OTLP log records received through the collector.
- Setup: active collector endpoints and environment variable guidance.

## Targeted Verification

Core:

```bash
dotnet test test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Elsa.Diagnostics.OpenTelemetry.UnitTests.csproj
dotnet test test/integration/Elsa.Diagnostics.OpenTelemetry.IntegrationTests/Elsa.Diagnostics.OpenTelemetry.IntegrationTests.csproj
```

Studio:

```bash
dotnet test src/modules/Elsa.Studio.Diagnostics.OpenTelemetry.Tests/Elsa.Studio.Diagnostics.OpenTelemetry.Tests.csproj
```

Manual checks:

- Run a workflow and confirm a workflow trace appears within 2 seconds.
- Open a trace and confirm workflow/activity spans are parented and timed correctly.
- Open Structured Logs from a span and confirm trace/span filters are applied.
- Send telemetry from a second service name and confirm it appears as a separate resource.
- Disable the remote feature or permission and confirm Studio shows hidden navigation or direct-route unavailable/unauthorized states.

## Scope Boundaries

This feature does not add durable OTEL persistence, vendor exporters, alerting, Kubernetes/Docker log APIs, or an Aspire-style launcher. Production environments should usually export to their standard OpenTelemetry Collector or observability platform; Elsa's collector is intended for local development and focused diagnostics unless capacity and security are configured deliberately.

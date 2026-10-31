# Implementation Plan: Diagnostics OpenTelemetry

**Branch**: `008-diagnostics-otel` | **Date**: 2026-05-25 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/008-diagnostics-otel/spec.md`

## Summary

Add a paired diagnostics OpenTelemetry feature across `elsa-core` and `elsa-studio`. Core owns the opt-in `Elsa.Diagnostics.OpenTelemetry` module: OTLP ingestion, redaction, bounded storage, authenticated REST APIs, live updates, and collector configuration metadata. Studio owns the `Elsa.Studio.Diagnostics.OpenTelemetry` module: feature-gated Diagnostics navigation, trace waterfall, metrics, OTLP logs, setup, and links into Structured Logs and Console Logs.

The design borrows Aspire's clean separation between collector, in-memory telemetry repository, API, and Blazor UI, but adapts it to Elsa's architecture: Studio does not launch resources, Core is the collector/API boundary, and existing `Elsa.Workflows` `ActivitySource`/`Meter` instrumentation remains the producer.

## Technical Context

**Language/Version**: C# latest, nullable reference types enabled, implicit usings enabled in both repositories.  
**Primary Dependencies**: `elsa-core`: OpenTelemetry SDK/proto contracts, ASP.NET Core endpoints, optional gRPC services, SignalR, Elsa module/shell feature infrastructure, existing workflow instrumentation, structured/console diagnostics correlation. `elsa-studio`: Blazor module framework, `IBackendApiClientProvider`, `IRemoteFeatureProvider`, `IHttpConnectionOptionsConfigurator`, Refit-style clients, SignalR client, existing diagnostics menu conventions.  
**Storage**: Core bounded in-memory telemetry repository for traces, spans, resources, metrics, and OTLP log records. Durable OTEL persistence is out of scope for v1. Studio stores only visible state, URL filters, selected rows, and bounded live buffers.  
**Testing**: Core xUnit unit tests for OTLP normalization, redaction, filtering, capacity/drop accounting, resource identity, and metric aggregation; Core integration tests for HTTP OTLP, gRPC OTLP if implemented, REST permissions, and SignalR updates. Studio xUnit tests for API/filter/url mapping and component helpers; manual/sample-host verification for gated UI, trace waterfall, metrics, live updates, and responsive states.  
**Target Platform**: ASP.NET Core Elsa Server in `elsa-core`; Blazor Server and Blazor WebAssembly Studio hosts in `elsa-studio`.  
**Project Type**: Two modular .NET feature packages plus tests and documentation.  
**Performance Goals**: Local telemetry appears in Studio within 2 seconds; trace detail remains responsive with 100 spans; metrics view remains bounded with 20 instruments and 1,000 recent points; backend stores and subscriber queues remain bounded under overload.  
**Constraints**: Keep Structured Logs, Console Logs, and OpenTelemetry separate. Redact before storage/streaming. Do not add durable OTEL storage, vendor exporters, Kubernetes/Docker log APIs, or an Aspire-style app launcher in this slice. Non-loopback OTLP ingestion requires explicit protection.  
**Scale/Scope**: `elsa-core` collector/API module and tests, `elsa-studio` viewer module and tests, README/wiki quickstarts, bundle/sample host registration, and correlation links from existing diagnostics pages where practical.

## Constitution Check

### Elsa Studio Constitution v1.0.0

| Principle | Verdict | Evidence |
|-----------|---------|----------|
| I. Modular Studio Features | PASS | Studio work is a focused `Elsa.Studio.Diagnostics.OpenTelemetry` module with route, registration, menu, client, services, UI, and tests. |
| II. Backend Capability Awareness | PASS | Navigation and direct route gate on `Elsa.Diagnostics.OpenTelemetry`; REST uses `IBackendApiClientProvider`; live updates use `IHttpConnectionOptionsConfigurator`. |
| III. UX Consistency and Density | PASS | The UI is an operational diagnostics tool with tabs, tables, waterfalls, filters, drawers, and explicit states. |
| IV. Async, Disposal, and Real-Time Discipline | PASS | Live subscriptions, reloads, backend switching, and client buffers are explicit lifecycle concerns. |
| V. Testing and Verification | PASS | Plan includes mapper/service tests and sample-host verification across empty, large, error, disconnected, and unauthorized states. |
| VI. Focused Change Sets | PASS | Studio changes stay in one diagnostics module plus optional public cross-links from existing diagnostics modules. |
| VII. Simplicity, DRY, and Maintainability | PASS | Reuses existing diagnostics patterns; shared helpers are limited to filter/url mapping and telemetry row formatting where duplication is structural. |

### Elsa Core Constitution v1.1.0

| Principle | Verdict | Evidence |
|-----------|---------|----------|
| I. Modular Architecture | PASS | Core work is a focused `Elsa.Diagnostics.OpenTelemetry` module with contracts, services, endpoints, real-time hub, options, permissions, and shell feature. |
| II. Composition & Extensibility | PASS | Ingestion, redaction, store, source registry, and live feed are explicit contracts so later persistence or external collectors can be added without changing Studio contracts. |
| III. Convention-Driven Design | PASS | Names, endpoint layout, shell feature, permissions, and tests follow diagnostics module conventions and American English. |
| IV. Async & Pipeline Execution | PASS | Provider queries, hub streams, endpoint handlers, and optional exporter configuration are async and cancellation-aware. |
| V. Testing Discipline | PASS | Unit and integration coverage is called out for OTLP ingestion, security, storage, and API/live contracts. |
| VI. Trunk-Based Development | PASS | Core and Studio can land as separate PRs after contracts stabilize. |
| VII. Simplicity, SRP, DRY & KISS | PASS | V1 uses bounded in-memory storage and avoids durable storage, vendor-specific backends, and launcher responsibilities. |

## Project Structure

### Documentation (this feature)

```text
specs/008-diagnostics-otel/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── backend-api.md
│   ├── otlp-ingest.md
│   ├── signalr-client.md
│   └── url-state.md
└── checklists/
    └── requirements.md
```

### Source Code: elsa-core

```text
/Users/sipke/Projects/Elsa/elsa-core/
├── src/modules/Elsa.Diagnostics.OpenTelemetry/
│   ├── Contracts/
│   │   ├── IOpenTelemetryIngestor.cs
│   │   ├── IOpenTelemetryProvider.cs
│   │   ├── IOpenTelemetryRedactor.cs
│   │   ├── IOpenTelemetrySourceRegistry.cs
│   │   ├── IOpenTelemetryStore.cs
│   │   └── IOpenTelemetryLiveFeed.cs
│   ├── Endpoints/OpenTelemetry/
│   │   ├── CollectorConfiguration/Endpoint.cs
│   │   ├── Logs/Endpoint.cs
│   │   ├── Metrics/Endpoint.cs
│   │   ├── Resources/Endpoint.cs
│   │   ├── Storage/Endpoint.cs
│   │   ├── Trace/Endpoint.cs
│   │   └── Traces/Endpoint.cs
│   ├── Extensions/
│   ├── Features/OpenTelemetryFeature.cs
│   ├── Ingestion/
│   │   ├── Grpc/
│   │   └── HttpProtobuf/
│   ├── Models/
│   ├── Options/OpenTelemetryDiagnosticsOptions.cs
│   ├── Permissions/OpenTelemetryPermissions.cs
│   ├── Providers/InMemory/
│   ├── RealTime/OpenTelemetryHub.cs
│   ├── Services/
│   └── ShellFeatures/OpenTelemetryFeature.cs
├── test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/
└── test/integration/Elsa.Diagnostics.OpenTelemetry.IntegrationTests/
```

### Source Code: elsa-studio

```text
/Users/sipke/Projects/Elsa/elsa-studio/
├── src/modules/Elsa.Studio.Diagnostics.OpenTelemetry/
│   ├── Client/IOpenTelemetryApi.cs
│   ├── Contracts/
│   │   ├── IOpenTelemetryObserver.cs
│   │   └── IOpenTelemetryService.cs
│   ├── Extensions/ServiceCollectionExtensions.cs
│   ├── Feature.cs
│   ├── Menu/OpenTelemetryMenu.cs
│   ├── Models/
│   ├── Services/
│   │   ├── OpenTelemetryFilterMapper.cs
│   │   ├── OpenTelemetryUrlStateMapper.cs
│   │   ├── RemoteOpenTelemetryService.cs
│   │   └── SignalROpenTelemetryObserver.cs
│   ├── UI/Pages/
│   │   ├── OpenTelemetry.razor
│   │   ├── OpenTelemetry.razor.cs
│   │   └── OpenTelemetry.razor.css
│   ├── UI/Components/
│   │   ├── MetricSeriesTable.razor
│   │   ├── ResourcePicker.razor
│   │   ├── SpanDetailsDrawer.razor
│   │   └── TraceWaterfall.razor
│   └── wwwroot/openTelemetry.js
├── src/modules/Elsa.Studio.Diagnostics.OpenTelemetry.Tests/
└── src/bundles/Elsa.Studio/
```

**Structure Decision**: Add two paired diagnostics modules rather than extending Structured Logs or Console Logs. Existing diagnostics modules may add public route links into OpenTelemetry, but no module reaches into another module's internals.

## Phase 0 Output

See [research.md](./research.md).

Resolved decisions:

- Keep workflow/activity instrumentation in `Elsa.Workflows.Core`; `Elsa.Diagnostics.OpenTelemetry` collects and serves telemetry.
- Do not port the historical `Elsa.OpenTelemetry` module from `elsa-extensions` into this feature; it is producer-side tracing middleware and overlaps current Core instrumentation.
- Support standard OTLP ingestion in Core so Studio remains a viewer, not a collector.
- Use bounded in-memory storage for v1.
- Normalize OTLP protobuf payloads into Elsa diagnostics read models.
- Use resource attributes as source identity.
- Treat OTLP logs as correlation data, not a replacement for `Elsa.Diagnostics.StructuredLogs`.
- Provide collector configuration metadata because Studio cannot inject environment variables into arbitrary processes.
- Require explicit protection for non-loopback OTLP ingestion.

## Phase 1 Output

- [data-model.md](./data-model.md)
- [contracts/backend-api.md](./contracts/backend-api.md)
- [contracts/otlp-ingest.md](./contracts/otlp-ingest.md)
- [contracts/signalr-client.md](./contracts/signalr-client.md)
- [contracts/url-state.md](./contracts/url-state.md)
- [quickstart.md](./quickstart.md)

## Post-Design Constitution Re-Check

| Area | Verdict | Post-design evidence |
|------|---------|----------------------|
| Studio constitution | PASS | Contracts, data model, and quickstart keep Studio modular, feature-gated, URL-aware, async/disposable, and bounded. |
| Core constitution | PASS | Contracts keep ingestion, storage, redaction, live feed, and permissions in a single module with explicit extension points. |

## Phase 2 Handoff

Suggested implementation order:

1. Finalize Core/Studio DTO contracts in this spec and agree on route names.
2. Implement Core module skeleton, options, permission, shell feature, endpoint mapping, and storage contracts.
3. Implement OTLP HTTP/protobuf ingestion and normalization for traces, metrics, and logs; add gRPC ingestion through the same `IOpenTelemetryIngestor` when dependencies are settled.
4. Add bounded in-memory store, redactor, source registry, provider facade, diagnostics APIs, SignalR hub, and Core tests.
5. Implement Studio module skeleton, feature/menu/route gating, API client, observer, filters, URL state, and tests.
6. Build Studio views: resources, traces list, trace detail waterfall, metrics, OTLP logs, setup, and cross-links to Structured Logs/Console Logs.
7. Add README/wiki quickstarts in both repositories and run targeted builds/tests.

Deferred producer-side enhancements:

- Revisit custom workflow/activity error-span handlers after the collector and Studio views are stable.
- Revisit trace-boundary policy only if current `Elsa.Workflows.Core` instrumentation cannot express required parent/link behavior without duplicate spans.

## Complexity Tracking

No constitution violations identified.

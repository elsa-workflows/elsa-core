# Implementation Plan: Diagnostics OpenTelemetry

**Branch**: `008-diagnostics-otel` | **Date**: 2026-05-25 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/008-diagnostics-otel/spec.md`

## Summary

Add an opt-in `Elsa.Diagnostics.OpenTelemetry` Core diagnostics module that receives OTLP telemetry, normalizes it into Elsa diagnostics read models, redacts sensitive values before provider boundaries, stores recent telemetry in bounded memory, exposes authenticated REST APIs and SignalR live updates for Studio, and publishes collector configuration metadata for standard OTEL senders.

The module is a collector/API backend, not producer-side workflow tracing middleware. Existing `Elsa.Workflows.Core` `ActivitySource` and `Meter` instrumentation remains the workflow telemetry producer. The historical `Elsa.OpenTelemetry` module from `elsa-extensions` is not ported in v1.

## Technical Context

**Language/Version**: C# latest, nullable reference types enabled, implicit usings enabled.  
**Primary Dependencies**: OpenTelemetry SDK/proto contracts, ASP.NET Core endpoint patterns, optional gRPC services, SignalR, Elsa feature/module infrastructure, FastEndpoints through Elsa API endpoint patterns, Elsa shell feature infrastructure, existing Elsa identity/authorization patterns, current `Elsa.Workflows.Core` instrumentation.  
**Storage**: Bounded in-memory telemetry repository for resources, traces, spans, metric instruments/points, OTLP log records, live subscribers, and dropped-count diagnostics. Defaults are configurable, drop oldest per signal on overflow, and start with at least 500 resources, 2,000 traces, 10,000 spans, 20,000 metric points, 10,000 OTLP log records, and 1,000 queued live updates per subscriber. No durable database schema in v1.  
**Testing**: xUnit unit tests for normalization, redaction, resource identity, filtering, bounded storage, dropped counts, and collector configuration; integration tests for HTTP/protobuf OTLP ingestion, optional gRPC ingestion when enabled, permissions, SignalR, API contracts, and workflow export-to-collector timing.  
**Target Platform**: ASP.NET Core Elsa Server on supported repository target frameworks.  
**Project Type**: Modular .NET library inside the existing Elsa solution.  
**Performance Goals**: Local workflow telemetry appears through diagnostics APIs within 2 seconds; trace detail handles 100 spans; metrics handle 20 instruments and 1,000 recent points; backend buffers remain bounded under overload.  
**Constraints**: Do not merge with Structured Logs or Console Logs. Do not port historical producer middleware. Do not start workflow/activity spans in the diagnostics module or mutate `Activity.Current`. Do not add durable OTEL persistence, vendor exporters, Kubernetes/Docker APIs, or Studio UI in Core. Non-loopback OTLP ingestion requires explicit protection.  
**Scale/Scope**: One Core module, unit/integration tests, docs/wiki quickstart updates, API/hub contracts, and sample host wiring if needed for verification.

## Constitution Check

Evaluated against `.specify/memory/constitution.md` v1.1.0:

| Principle | Verdict | Evidence |
|-----------|---------|----------|
| I. Modular Architecture | PASS | Work is a focused module under `src/modules/Elsa.Diagnostics.OpenTelemetry` with contracts, services, endpoints, hub, options, permissions, and shell feature. |
| II. Composition & Extensibility | PASS | Ingestion, redaction, store, live feed, source registry, and collector configuration are explicit contracts/services. |
| III. Convention-Driven Design | PASS | Names follow Elsa module, endpoint, feature, permission, and test conventions with American English. |
| IV. Async & Pipeline Execution | PASS | Ingestion, provider queries, endpoints, live subscriptions, and shutdown paths are async/cancellation-aware. |
| V. Testing Discipline | PASS | Plan requires unit/integration coverage for ingestion, security, storage, API, SignalR, and workflow telemetry timing. |
| VI. Trunk-Based Development | PASS | Core backend module can land independently from Studio after API contracts stabilize. |
| VII. Simplicity, SRP, DRY & KISS | PASS | V1 uses bounded in-memory storage and excludes durable storage, vendor exporters, app launcher behavior, and producer middleware changes. |

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
│   ├── rest-api.md
│   ├── otlp-ingest.md
│   ├── signalr-hub.md
│   └── provider-contract.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
src/modules/
└── Elsa.Diagnostics.OpenTelemetry/
    ├── Contracts/
    │   ├── IOpenTelemetryIngestor.cs
    │   ├── IOpenTelemetryProvider.cs
    │   ├── IOpenTelemetryRedactor.cs
    │   ├── IOpenTelemetrySourceRegistry.cs
    │   ├── IOpenTelemetryStore.cs
    │   └── IOpenTelemetryLiveFeed.cs
    ├── Endpoints/OpenTelemetry/
    │   ├── CollectorConfiguration/Endpoint.cs
    │   ├── Logs/Endpoint.cs
    │   ├── Metrics/Endpoint.cs
    │   ├── Resources/Endpoint.cs
    │   ├── Storage/Endpoint.cs
    │   ├── Trace/Endpoint.cs
    │   └── Traces/Endpoint.cs
    ├── Extensions/
    ├── Features/OpenTelemetryFeature.cs
    ├── Ingestion/
    │   ├── Grpc/
    │   └── HttpProtobuf/
    ├── Models/
    ├── Options/OpenTelemetryDiagnosticsOptions.cs
    ├── Permissions/OpenTelemetryPermissions.cs
    ├── Providers/InMemory/
    ├── RealTime/OpenTelemetryHub.cs
    ├── Services/
    └── ShellFeatures/OpenTelemetryFeature.cs

test/unit/
└── Elsa.Diagnostics.OpenTelemetry.UnitTests/

test/integration/
└── Elsa.Diagnostics.OpenTelemetry.IntegrationTests/
```

**Structure Decision**: Add a new diagnostics module parallel to `Elsa.Diagnostics.StructuredLogs` and `Elsa.Diagnostics.ConsoleLogs`. The module owns collector, normalization, storage, APIs, and live contracts only.

## Phase 0 Output

See [research.md](./research.md).

Resolved decisions:

- Core module is the OTLP collector and diagnostics API boundary.
- Existing `Elsa.Workflows.Core` instrumentation remains the producer.
- Do not port historical `Elsa.OpenTelemetry` producer middleware from `elsa-extensions`.
- Support HTTP/protobuf as required; support gRPC when host/dependencies enable it and advertise disabled metadata otherwise.
- Use bounded in-memory storage in v1.
- Keep OTLP logs separate from `Elsa.Diagnostics.StructuredLogs`.
- Require protection for non-loopback ingestion.

## Phase 1 Output

- [data-model.md](./data-model.md)
- [contracts/rest-api.md](./contracts/rest-api.md)
- [contracts/otlp-ingest.md](./contracts/otlp-ingest.md)
- [contracts/signalr-hub.md](./contracts/signalr-hub.md)
- [contracts/provider-contract.md](./contracts/provider-contract.md)
- [quickstart.md](./quickstart.md)

## Post-Design Constitution Re-Check

| Principle | Verdict | Post-design evidence |
|-----------|---------|----------------------|
| I. Modular Architecture | PASS | Contracts and source tree keep all Core OpenTelemetry diagnostics behavior in one focused module. |
| II. Composition & Extensibility | PASS | Provider/store/live-feed/redactor/ingestor contracts isolate real variability points. |
| III. Convention-Driven Design | PASS | Endpoint and type names follow diagnostics module conventions. |
| IV. Async & Pipeline Execution | PASS | Provider, store, endpoint, and hub contracts are async/cancellation-aware. |
| V. Testing Discipline | PASS | Tasks include unit and integration tests for each buildable slice. |
| VI. Trunk-Based Development | PASS | Core slice remains independently reviewable from Studio. |
| VII. Simplicity, SRP, DRY & KISS | PASS | V1 avoids persistence and producer instrumentation rewrites. |

## Phase 2 Handoff

Use [tasks.md](./tasks.md) as the implementation backlog. Suggested order:

1. Create module/test projects, solution entries, options, permissions, feature, and shell feature.
2. Add models and shared provider contracts.
3. Add HTTP/protobuf ingestion, normalization, redaction, resource registry, bounded store, and provider facade.
4. Add REST APIs, SignalR hub, collector configuration, and security checks.
5. Add optional gRPC wrappers and disabled metadata handling.
6. Add docs, sample host wiring if needed, and run targeted tests/builds.

## Complexity Tracking

No constitution violations identified.

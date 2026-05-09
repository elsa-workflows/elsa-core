# Implementation Plan: Live Server Log Streaming

**Branch**: `003-live-server-logs` | **Date**: 2026-05-07 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/003-live-server-logs/spec.md`

## Summary

Add an opt-in backend server logs feature that captures structured `ILogger` events, redacts sensitive data, keeps bounded recent history, and exposes source-aware REST and SignalR contracts for Elsa Studio. The MVP ships with an in-memory provider for development and single-node deployments while every event carries source topology so Redis, OpenTelemetry, Loki, Seq, Elasticsearch, or other clustered providers can be added later without changing Studio.

## Technical Context

**Language/Version**: C# latest, nullable reference types enabled, implicit usings enabled.  
**Primary Dependencies**: `Microsoft.Extensions.Logging`, `Microsoft.AspNetCore.SignalR`, Elsa feature/module infrastructure, FastEndpoints through Elsa API endpoint patterns, existing Elsa identity/authorization features.  
**Storage**: Bounded in-memory ring buffer for MVP; no EF Core schema changes. Provider abstraction allows external/shared log backends later.  
**Testing**: xUnit unit tests for filters, redaction, source metadata, ring buffer, and in-memory provider; integration tests for REST endpoints, authorization, SignalR subscribe/update/dispose, and feature registration.  
**Target Platform**: ASP.NET Core Elsa Server on the repository's supported .NET target frameworks.  
**Project Type**: Modular .NET library inside the existing Elsa solution.  
**Performance Goals**: Sustain at least 10,000 small log events per minute in-process without unbounded memory growth; deliver live events to local subscribers within 1 second under normal development load.  
**Constraints**: Must be opt-in, bounded, authorized, redacted before buffering, and safe under subscriber backpressure. Kubernetes API access and durable retention are out of scope for MVP.  
**Scale/Scope**: New server logs module, backend contracts, one in-memory provider, REST endpoints, SignalR hub, feature registration, and setup docs.

## Constitution Check

Evaluated against `.specify/memory/constitution.md` v1.1.0:

| Principle | Verdict | Evidence |
|-----------|---------|----------|
| I. Modular Architecture | PASS | New code belongs in a focused server logs module under `src/modules/` with contracts, services, extensions, models, and feature registration. |
| II. Composition & Extensibility | PASS | `IServerLogProvider` is the provider boundary; redaction, limits, and source metadata are options-driven. |
| III. Convention-Driven Design | PASS | REST endpoints follow Elsa endpoint conventions; feature class and extension names follow existing module patterns; new prose uses American English. |
| IV. Async & Pipeline Execution | PASS | Provider query/subscribe operations, SignalR hub methods, and endpoint handlers are async and cancellation-aware. |
| V. Testing Discipline | PASS | Unit and integration test coverage is identified for security, redaction, bounded behavior, and live streaming. |
| VI. Trunk-Based Development | PASS | Tasks are sliced into backend-only increments that can merge before Studio UI work lands. |
| VII. Simplicity, SRP, DRY & KISS | PASS | MVP avoids durable storage, Kubernetes API integration, and premature external provider implementations. |

## Project Structure

### Documentation (this feature)

```text
specs/003-live-server-logs/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── signalr-hub.md
│   ├── rest-api.md
│   └── provider-contract.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
src/modules/
└── Elsa.ServerLogs/
    ├── Contracts/
    │   ├── IServerLogProvider.cs
    │   ├── IServerLogRedactor.cs
    │   └── IServerLogSourceRegistry.cs
    ├── Features/
    │   └── ServerLogStreamingFeature.cs
    ├── Logging/
    │   ├── ServerLogLoggerProvider.cs
    │   └── ServerLogLogger.cs
    ├── Providers/InMemory/
    │   ├── InMemoryServerLogProvider.cs
    │   └── RingBuffer.cs
    ├── RealTime/
    │   └── ServerLogsHub.cs
    ├── Endpoints/ServerLogs/
    │   ├── Recent/Endpoint.cs
    │   └── Sources/Endpoint.cs
    ├── Models/
    │   ├── ServerLogEvent.cs
    │   ├── ServerLogSource.cs
    │   ├── ServerLogFilter.cs
    │   └── ServerLogDroppedEventSummary.cs
    ├── Options/
    │   └── ServerLogStreamingOptions.cs
    └── Extensions/
        ├── ModuleExtensions.cs
        └── ApplicationBuilderExtensions.cs

test/unit/
└── Elsa.ServerLogs.UnitTests/
    ├── InMemoryServerLogProviderTests.cs
    ├── ServerLogFilterTests.cs
    └── ServerLogRedactorTests.cs

test/integration/
└── Elsa.ServerLogs.IntegrationTests/
    ├── ServerLogsEndpointTests.cs
    └── ServerLogsHubTests.cs
```

**Structure Decision**: Add a focused server logs module instead of placing log streaming inside `Elsa.Workflows.Api`. The module can be used by non-workflow Elsa hosts and can enrich events with workflow/tenant context when that context exists.

## Phase 0 Output

See [research.md](./research.md).

Resolved decisions:

- Structured `ILogger` capture instead of raw console redirection.
- SignalR live streaming plus REST recent-log backfill.
- Source-aware MVP with in-memory provider first and shared provider contract for clusters.
- Redaction before buffering.
- Source health from `LastSeen` and provider state.
- Deterministic merged ordering using timestamp plus sequence/receive order.

## Phase 1 Output

- [data-model.md](./data-model.md)
- [contracts/signalr-hub.md](./contracts/signalr-hub.md)
- [contracts/rest-api.md](./contracts/rest-api.md)
- [contracts/provider-contract.md](./contracts/provider-contract.md)
- [quickstart.md](./quickstart.md)

## Post-Design Constitution Re-Check

| Principle | Verdict | Post-design evidence |
|-----------|---------|----------------------|
| I. Modular Architecture | PASS | Contracts, provider, endpoints, and hub stay within `Elsa.ServerLogs`; external integrations use published contracts. |
| II. Composition & Extensibility | PASS | Future clustered providers can implement `IServerLogProvider` without Studio or hub contract changes. |
| III. Convention-Driven Design | PASS | Endpoint, feature, extension, model, and test names follow repository conventions. |
| IV. Async & Pipeline Execution | PASS | Provider and hub contracts are asynchronous and cancellation-aware. |
| V. Testing Discipline | PASS | Tasks include unit and integration coverage for the highest-risk behavior. |
| VI. Trunk-Based Development | PASS | Backend work is sliced into contracts, capture, provider, API/hub, registration, and cluster-readiness tasks. |
| VII. Simplicity, SRP, DRY & KISS | PASS | MVP limits itself to the in-memory provider plus extension points required by the source-aware contract. |

## Phase 2 Handoff

Use [tasks.md](./tasks.md) as the implementation backlog. Suggested PR order:

1. Contracts, models, and options.
2. Redaction and `ILoggerProvider` capture.
3. In-memory provider with bounded buffers and filtering.
4. REST endpoints and SignalR hub.
5. Feature registration, installed-feature visibility, and setup docs.
6. Source metadata and cluster-readiness provider tests.

## Complexity Tracking

No constitution violations identified.

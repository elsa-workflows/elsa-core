# Implementation Plan: Diagnostics Console Logs

**Branch**: `006-diagnostics-console-logs` | **Date**: 2026-05-18 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/006-diagnostics-console-logs/spec.md`

## Summary

Add an opt-in Core diagnostics console logs module that captures raw stdout/stderr line output, redacts it before provider boundaries, keeps bounded recent history, and exposes source-aware REST and SignalR contracts for Studio. The first provider is in-process and single-node, while source identity and provider contracts leave room for future shared aggregation without changing Studio-facing payloads.

## Technical Context

**Language/Version**: C# latest, nullable reference types enabled, implicit usings enabled.  
**Primary Dependencies**: `Microsoft.Extensions.Options`, `Microsoft.AspNetCore.SignalR`, Elsa feature/module infrastructure, FastEndpoints through Elsa API endpoint patterns, Elsa shell feature infrastructure, and existing Elsa identity/authorization patterns.  
**Storage**: Bounded in-memory recent buffer and bounded subscriber queues by default; no durable database schema. Providers receive redacted content only.  
**Testing**: xUnit unit tests for capture buffering, truncation, ANSI handling, redaction, filters, source health, provider behavior, and dropped-line accounting; integration tests for REST endpoints, permission enforcement, SignalR subscribe/update/unsubscribe, and feature registration.  
**Target Platform**: ASP.NET Core Elsa Server on the repository's supported .NET target frameworks.  
**Project Type**: Modular .NET library inside the existing Elsa solution.  
**Performance Goals**: Deliver complete stdout/stderr test lines to authorized subscribers within 1 second up to configured subscriber capacity; keep recent and live buffers bounded under sustained overload.  
**Constraints**: Feature is Core-only and separate from `Elsa.Diagnostics.StructuredLogs`; no direct Kubernetes, Docker, vendor sink, durable audit storage, or OpenTelemetry integration. Redaction and ANSI default stripping run before provider storage or streaming.  
**Scale/Scope**: New diagnostics console logs module, contracts, options, in-process capture/provider, REST endpoints, SignalR hub, permission, shell feature, README/quickstart, unit tests, and integration tests.

## Constitution Check

Evaluated against `.specify/memory/constitution.md` v1.1.0:

| Principle | Verdict | Evidence |
|-----------|---------|----------|
| I. Modular Architecture | PASS | The feature is a focused module under `src/modules/Elsa.Diagnostics.ConsoleLogs` with its own contracts, services, endpoints, real-time hub, options, permissions, and shell feature. |
| II. Composition & Extensibility | PASS | Console provider, redactor, source registry, capture tee, and options are explicit extension points; external providers can aggregate later without endpoint or hub changes. |
| III. Convention-Driven Design | PASS | The plan follows existing Elsa endpoint, feature, extension, permission, shell feature, and test project naming patterns. |
| IV. Async & Pipeline Execution | PASS | Provider queries, live streams, SignalR methods, and endpoint handlers are async and cancellation-aware. |
| V. Testing Discipline | PASS | The design calls for unit and integration tests around capture semantics, security, redaction, bounded buffers, endpoints, and hub behavior. |
| VI. Trunk-Based Development | PASS | Work is scoped to one Core diagnostics module and can merge independently from the paired Studio feature. |
| VII. Simplicity, SRP, DRY & KISS | PASS | The first slice uses one in-process provider and excludes durable storage, orchestrator APIs, vendor sinks, and OpenTelemetry exploration. |

## Project Structure

### Documentation (this feature)

```text
specs/006-diagnostics-console-logs/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── provider-contract.md
│   ├── rest-api.md
│   └── signalr-hub.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
src/modules/
└── Elsa.Diagnostics.ConsoleLogs/
    ├── Contracts/
    │   ├── IConsoleLogCapture.cs
    │   ├── IConsoleLogProvider.cs
    │   ├── IConsoleLogRedactor.cs
    │   └── IConsoleLogSourceRegistry.cs
    ├── Endpoints/ConsoleLogs/
    │   ├── Recent/Endpoint.cs
    │   └── Sources/Endpoint.cs
    ├── Extensions/
    ├── Features/ConsoleLogsFeature.cs
    ├── Models/
    ├── Options/ConsoleLogsOptions.cs
    ├── Permissions/ConsoleLogsPermissions.cs
    ├── Providers/InMemory/
    ├── RealTime/ConsoleLogsHub.cs
    ├── Services/
    └── ShellFeatures/ConsoleLogsFeature.cs

test/unit/
└── Elsa.Diagnostics.ConsoleLogs.UnitTests/

test/integration/
└── Elsa.Diagnostics.ConsoleLogs.IntegrationTests/
```

**Structure Decision**: Create a new Core diagnostics module parallel to `Elsa.Diagnostics.StructuredLogs`. Keep console capture, redaction, buffering, REST, SignalR, and provider abstractions in this module; do not add Studio code or durable/external provider projects.

## Phase 0 Output

See [research.md](./research.md).

Resolved decisions:

- Use a separate `Elsa.Diagnostics.ConsoleLogs` module instead of extending structured logs.
- Capture stdout/stderr through an in-process tee that preserves original console destinations.
- Buffer partial writes until newline, max line length, or idle flush timeout.
- Truncate overlong lines into one marked event and strip ANSI by default.
- Redact before provider boundaries; providers receive redacted content only.
- Use REST for recent backfill/source listing and SignalR for mutable live subscriptions.

## Phase 1 Output

- [data-model.md](./data-model.md)
- [contracts/rest-api.md](./contracts/rest-api.md)
- [contracts/signalr-hub.md](./contracts/signalr-hub.md)
- [contracts/provider-contract.md](./contracts/provider-contract.md)
- [quickstart.md](./quickstart.md)

## Post-Design Constitution Re-Check

| Principle | Verdict | Post-design evidence |
|-----------|---------|----------------------|
| I. Modular Architecture | PASS | Contracts and contracts docs keep the console logs surface inside one focused module. |
| II. Composition & Extensibility | PASS | Provider and source contracts allow shared aggregation later without changing REST or hub contracts. |
| III. Convention-Driven Design | PASS | Endpoint, hub, permission, option, and shell feature names use diagnostics console logs consistently. |
| IV. Async & Pipeline Execution | PASS | Provider, endpoint, and hub contracts are async and cancellation-aware. |
| V. Testing Discipline | PASS | Quickstart and plan name targeted build/test commands and the contract docs identify security and transport checks. |
| VI. Trunk-Based Development | PASS | Core-only artifacts are independent from Studio and external provider work. |
| VII. Simplicity, SRP, DRY & KISS | PASS | The design avoids durable storage and orchestrator integrations while preserving clear extension points. |

## Phase 2 Handoff

Use `/speckit-tasks` to generate the implementation backlog. Suggested order:

1. Create the module and test projects with feature/options/permission skeletons.
2. Add models, provider, source registry, redactor, capture tee, and bounded in-memory behavior.
3. Add REST endpoints and SignalR hub contracts.
4. Add shell feature, service registration, hub mapping, README, and sample host wiring.
5. Add unit and integration tests, then run targeted builds/tests.

## Complexity Tracking

No constitution violations identified.

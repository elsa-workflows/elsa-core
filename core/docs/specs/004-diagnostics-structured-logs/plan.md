# Implementation Plan: Diagnostics Structured Logs

**Branch**: `004-diagnostics-structured-logs` | **Date**: 2026-05-10 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/004-diagnostics-structured-logs/spec.md`

## Summary

Refactor the unpublished `Elsa.ServerLogs` module into `Elsa.Diagnostics.StructuredLogs`, preserving its structured `ILogger` capture, redaction, bounded in-memory recent history, source metadata, REST API, and SignalR stream while removing names that imply raw stdout/stderr console capture. Add semantic improvements for message template capture from `{OriginalFormat}` and active logging scope capture.

## Technical Context

**Language/Version**: C# latest, nullable reference types enabled, implicit usings enabled.  
**Primary Dependencies**: `Microsoft.Extensions.Logging`, `Microsoft.Extensions.Options`, `Microsoft.AspNetCore.SignalR`, Elsa feature/module infrastructure, FastEndpoints through Elsa API endpoint patterns, CShells shell feature infrastructure.  
**Storage**: Existing bounded in-memory ring buffer; no EF Core schema changes. Provider abstraction remains available for future shared backends.  
**Testing**: Existing xUnit unit and integration projects renamed with the module; add focused unit tests for message templates, scopes, route/permission constants, and shell option binding.  
**Target Platform**: ASP.NET Core Elsa Server on the repository's supported .NET target frameworks.  
**Project Type**: Modular .NET library inside the existing Elsa solution.  
**Performance Goals**: Preserve bounded memory behavior and live delivery expectations from the current module; no extra unbounded scope/property allocations beyond per-event dictionaries.  
**Constraints**: Breaking rename is acceptable before release; direct stdout/stderr capture and OpenTelemetry trace exploration are out of scope. Redaction must run before buffering or streaming.  
**Scale/Scope**: Rename one Core module, test projects, sample host wiring, routes, permissions, public contracts, shell feature identity, docs, and Speckit artifacts.

## Constitution Check

Evaluated against `.specify/memory/constitution.md` v1.1.0:

| Principle | Verdict | Evidence |
|-----------|---------|----------|
| I. Modular Architecture | PASS | The feature remains a focused module under `src/modules/Elsa.Diagnostics.StructuredLogs` with contracts, services, endpoints, hub, and feature registration. |
| II. Composition & Extensibility | PASS | Provider, redactor, source registry, and options boundaries are preserved under structured-log names. |
| III. Convention-Driven Design | PASS | Rename follows Elsa feature, extension, endpoint, shell feature, and test naming patterns. |
| IV. Async & Pipeline Execution | PASS | Provider query/subscribe APIs, hub methods, and endpoints remain async and cancellation-aware. |
| V. Testing Discipline | PASS | Unit/integration tests are renamed and expanded for the new semantic capture requirements. |
| VI. Trunk-Based Development | PASS | Work is scoped to a single unpublished feature refactor and can merge independently from Studio. |
| VII. Simplicity, SRP, DRY & KISS | PASS | No durable storage, console redirection, or OpenTelemetry explorer behavior is added. |

## Project Structure

### Documentation (this feature)

```text
specs/004-diagnostics-structured-logs/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── rest-api.md
│   ├── signalr-hub.md
│   └── provider-contract.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
src/modules/
└── Elsa.Diagnostics.StructuredLogs/
    ├── Contracts/
    │   ├── IStructuredLogProvider.cs
    │   ├── IStructuredLogRedactor.cs
    │   ├── IStructuredLogSourceRegistry.cs
    │   └── IStructuredLogStreamProvider.cs
    ├── Endpoints/StructuredLogs/
    │   ├── Recent/Endpoint.cs
    │   └── Sources/Endpoint.cs
    ├── Extensions/
    ├── Features/StructuredLogsFeature.cs
    ├── Logging/
    ├── Models/
    ├── Options/StructuredLogsOptions.cs
    ├── Permissions/StructuredLogsPermissions.cs
    ├── Providers/InMemory/
    ├── RealTime/StructuredLogsHub.cs
    ├── Services/
    └── ShellFeatures/StructuredLogsFeature.cs

test/unit/
└── Elsa.Diagnostics.StructuredLogs.UnitTests/

test/integration/
└── Elsa.Diagnostics.StructuredLogs.IntegrationTests/
```

**Structure Decision**: Move the existing module and tests to diagnostics structured-log names. Do not create new modules for console streaming or OpenTelemetry exploration in this feature.

## Phase 0 Output

See [research.md](./research.md).

Resolved decisions:

- Perform a consistent breaking rename instead of obsolete shims.
- Keep the existing in-memory provider and API/hub shape, with diagnostics structured-log routes.
- Capture message templates from `{OriginalFormat}` while excluding that key from structured properties.
- Capture active logging scopes through the logging provider's external scope provider.
- Preserve redaction, recursion guard, bounded buffers, source metadata, and dropped-event summaries.

## Phase 1 Output

- [data-model.md](./data-model.md)
- [contracts/rest-api.md](./contracts/rest-api.md)
- [contracts/signalr-hub.md](./contracts/signalr-hub.md)
- [contracts/provider-contract.md](./contracts/provider-contract.md)
- [quickstart.md](./quickstart.md)

## Post-Design Constitution Re-Check

| Principle | Verdict | Post-design evidence |
|-----------|---------|----------------------|
| I. Modular Architecture | PASS | All renamed code stays inside the structured logs module and exported contracts. |
| II. Composition & Extensibility | PASS | Provider/redactor/source registry contracts remain the extension points. |
| III. Convention-Driven Design | PASS | Public APIs use `StructuredLog`/`StructuredLogs` consistently. |
| IV. Async & Pipeline Execution | PASS | Contracts retain async signatures. |
| V. Testing Discipline | PASS | Tasks include focused regression tests for new semantic fields and renamed surfaces. |
| VI. Trunk-Based Development | PASS | Core-only implementation is independent of the separate Studio worker. |
| VII. Simplicity, SRP, DRY & KISS | PASS | Future diagnostics modules are referenced only in docs as out of scope. |

## Phase 2 Handoff

Use [tasks.md](./tasks.md) as the implementation backlog. Suggested order:

1. Rename projects, solution entries, namespaces, public types, routes, and permissions.
2. Rename feature, shell feature, options, extensions, README, and sample host wiring.
3. Add message template and scope capture in the logger provider.
4. Rename and expand tests.
5. Run targeted builds/tests and commit any fixes.

## Complexity Tracking

No constitution violations identified.

# Implementation Plan: Structured Log Persistence

**Branch**: `005-structured-log-persistence` | **Date**: 2026-05-12 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/005-structured-log-persistence/spec.md`

## Summary

Refactor diagnostics structured logs so storage is a replaceable concern. Preserve the existing bounded in-memory behavior as the default, introduce shared relational persistence abstractions, and add an opt-in SQLite durable store. Use FluentMigrator for schema creation and upgrades while keeping the append/query hot path explicit through Dapper or raw ADO.NET. Defer OTLP and vendor sink/exporter work.

## Technical Context

**Language/Version**: C# latest, nullable reference types enabled, implicit usings enabled.  
**Primary Dependencies**: Existing `Elsa.Diagnostics.StructuredLogs`, `Microsoft.Extensions.Logging`, `Microsoft.Extensions.Options`, `Microsoft.AspNetCore.SignalR`, Elsa feature/module infrastructure, FastEndpoints through Elsa API endpoint patterns, FluentMigrator runner packages, SQLite ADO.NET provider, and optionally Dapper for relational operations.  
**Storage**: Bounded in-memory store by default; opt-in SQLite durable store through shared relational persistence.  
**Testing**: Existing xUnit unit/integration projects for structured logs plus new unit/integration coverage for storage abstraction, SQLite persistence, migrations, filtering, and retention.  
**Target Platform**: ASP.NET Core Elsa Server on the repository's supported .NET target frameworks.  
**Project Type**: Modular .NET libraries inside the existing Elsa solution.  
**Performance Goals**: Preserve non-blocking logging behavior; avoid synchronous disk writes on `ILogger` call paths; keep recent queries indexed for common filters.  
**Constraints**: No EF Core persistence for this feature. OpenTelemetry, vendor exporters, raw console capture, and trace/metric exploration remain out of scope. Redaction must run before persistence.  
**Scale/Scope**: Core structured logs refactor, shared relational package, SQLite provider package, tests, docs, and Speckit artifacts.

## Constitution Check

Evaluated against `.specify/memory/constitution.md` v1.1.0:

| Principle | Verdict | Evidence |
|-----------|---------|----------|
| I. Modular Architecture | PASS | Core contracts stay in `Elsa.Diagnostics.StructuredLogs`; relational and SQLite persistence live in focused provider packages. |
| II. Composition & Extensibility | PASS | Store, live feed, dialect, migration runner, and retention services are replaceable. |
| III. Convention-Driven Design | PASS | Public APIs continue the `StructuredLog`/`StructuredLogs` naming established by feature 004. |
| IV. Async & Pipeline Execution | PASS | Append, query, migration, cleanup, and shutdown paths are async/cancellation-aware. |
| V. Testing Discipline | PASS | Tasks include targeted unit/integration tests for in-memory compatibility and SQLite durability. |
| VI. Trunk-Based Development | PASS | SQLite persistence can ship independently from future OTLP or vendor sink work. |
| VII. Simplicity, SRP, DRY & KISS | PASS | EF Core and broad vendor exporter support are intentionally excluded from this slice. |

## Project Structure

### Documentation (this feature)

```text
specs/005-structured-log-persistence/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── persistence-contract.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
src/modules/
├── Elsa.Diagnostics.StructuredLogs/
│   ├── Contracts/
│   │   ├── IStructuredLogProvider.cs
│   │   ├── IStructuredLogStore.cs
│   │   ├── IStructuredLogLiveFeed.cs
│   │   └── IStructuredLogSink.cs
│   ├── Providers/
│   │   └── InMemory/
│   └── Services/
│       └── DefaultStructuredLogProvider.cs
├── Elsa.Diagnostics.StructuredLogs.Persistence.Relational/
│   ├── Contracts/
│   ├── Features/
│   ├── Migrations/
│   ├── Models/
│   ├── Options/
│   ├── Services/
│   └── Stores/
└── Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite/
    ├── Extensions/
    ├── Features/
    ├── Options/
    └── Services/

test/unit/
├── Elsa.Diagnostics.StructuredLogs.UnitTests/
└── Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests/

test/integration/
└── Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests/
```

**Structure Decision**: Keep runtime capture, API, hub, and Studio-facing contracts in the existing structured logs module. Add relational and SQLite packages only for durable persistence. Do not create OTLP or vendor sink packages in this feature.

## Phase 0 Output

See [research.md](./research.md).

Resolved decisions:

- Keep in-memory storage as the default.
- Split storage/live-feed responsibilities from the existing provider facade.
- Add SQLite as the first durable provider.
- Build SQLite on a shared relational persistence package.
- Use FluentMigrator for schema management.
- Use explicit SQL/Dapper-style operations for appending, querying, and retention.
- Defer OpenTelemetry and vendor sink/exporter packages.

## Phase 1 Output

- [data-model.md](./data-model.md)
- [contracts/persistence-contract.md](./contracts/persistence-contract.md)
- [quickstart.md](./quickstart.md)

## Post-Design Constitution Re-Check

| Principle | Verdict | Post-design evidence |
|-----------|---------|----------------------|
| I. Modular Architecture | PASS | Persistence packages are separate from the core diagnostics structured logs module. |
| II. Composition & Extensibility | PASS | Future relational providers can reuse the relational store and provide provider-specific services. |
| III. Convention-Driven Design | PASS | Configuration and package names follow Elsa module conventions. |
| IV. Async & Pipeline Execution | PASS | Contracts use async append/query/migration/cleanup methods. |
| V. Testing Discipline | PASS | Story tasks include independent tests for each deliverable slice. |
| VI. Trunk-Based Development | PASS | The SQLite slice is independently releasable. |
| VII. Simplicity, SRP, DRY & KISS | PASS | The design avoids EF Core and skips exporter scope. |

## Phase 2 Handoff

Use [tasks.md](./tasks.md) as the implementation backlog. Suggested order:

1. Introduce storage/live-feed contracts while preserving current behavior.
2. Convert the in-memory provider into reusable in-memory store/live-feed pieces behind the existing facade.
3. Add shared relational persistence contracts, record model, dialect, migration runner, retention service, and store implementation.
4. Add SQLite provider registration, dialect, migration runner configuration, and tests.
5. Update README/quickstart and run targeted builds/tests.

## Complexity Tracking

No constitution violations identified.

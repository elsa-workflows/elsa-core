# Implementation Plan: Structured Log Persistence

**Branch**: `005-structured-log-persistence` | **Date**: 2026-05-13 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/005-structured-log-persistence/spec.md`

## Summary

Refactor diagnostics structured logs so queryable storage is replaceable while the existing `IStructuredLogProvider` remains the REST/SignalR facade. Keep bounded in-memory storage as the default and add opt-in SQLite durable storage backed by shared relational persistence. Use FluentMigrator for schema management, explicit SQL/Dapper-style runtime operations, async batched writes, a bounded drop-newest write queue, startup migrations enabled by default, UTC ISO-8601 timestamp text, and opt-in retention.

## Technical Context

**Language/Version**: C# latest, nullable reference types enabled, implicit usings enabled.
**Primary Dependencies**: Existing `Elsa.Diagnostics.StructuredLogs`, `Microsoft.Extensions.Logging`, `Microsoft.Extensions.Options`, `Microsoft.AspNetCore.SignalR`, Elsa feature/module infrastructure, FastEndpoints through Elsa API endpoint patterns, FluentMigrator runner packages, SQLite ADO.NET provider, and optionally Dapper for relational operations.
**Storage**: Bounded in-memory store by default; opt-in SQLite durable store through shared relational persistence. SQLite stores `Timestamp` and `ReceivedAt` as UTC ISO-8601 text and stores exception, scope, and property payloads as JSON text.
**Testing**: xUnit unit and integration tests under existing structured logs projects plus new relational unit tests and SQLite integration tests.
**Target Platform**: ASP.NET Core Elsa Server on the repository's supported .NET target frameworks.
**Project Type**: Modular .NET libraries inside the existing Elsa solution.
**Performance Goals**: Preserve non-blocking `ILogger` capture. SQLite writes use async batching and a bounded queue; full queues drop newly received events and report dropped-write counts.
**Constraints**: No EF Core persistence for this feature. OpenTelemetry, vendor exporters, raw console capture, and trace/metric exploration remain out of scope. Redaction must run before persistence. SQLite may lose queued-but-unflushed events after an ungraceful process crash.
**Scale/Scope**: Core structured logs storage refactor, shared relational package, SQLite provider package, tests, docs, and Speckit artifacts.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Verdict | Evidence |
|-----------|---------|----------|
| I. Modular Architecture | PASS | Core contracts stay in `Elsa.Diagnostics.StructuredLogs`; relational and SQLite persistence live in focused provider packages under `src/modules/`. |
| II. Composition & Extensibility | PASS | Store, live feed, dialect, migration runner, write queue, and retention services are replaceable. |
| III. Convention-Driven Design | PASS | Public APIs continue the `StructuredLog`/`StructuredLogs` naming established by feature 004 and use American English. |
| IV. Async & Pipeline Execution | PASS | Append, query, migration, cleanup, flush, and shutdown paths are async/cancellation-aware. |
| V. Testing Discipline | PASS | Plan includes unit/integration coverage for in-memory compatibility, SQLite durability, migrations, queue overflow, timestamp storage, and retention. |
| VI. Trunk-Based Development | PASS | SQLite persistence can ship independently from future OTLP or vendor sink work. |
| VII. Simplicity, SRP, DRY & KISS | PASS | EF Core and broad vendor exporter support are intentionally excluded; abstractions serve documented persistence extensibility. |

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
│   ├── Providers/InMemory/
│   └── Services/DefaultStructuredLogProvider.cs
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
- Use FluentMigrator for schema management and run SQLite migrations on startup by default with opt-out.
- Use explicit SQL/Dapper-style operations for appending, querying, and retention.
- Use async batched SQLite writes with graceful-shutdown flush.
- Use a bounded write queue that drops newest events when full and reports dropped-write counts.
- Store SQLite timestamps as UTC ISO-8601 text.
- Make retention opt-in; delete no records unless max age or max rows is configured.
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
| III. Convention-Driven Design | PASS | Configuration and package names follow Elsa module conventions and American English. |
| IV. Async & Pipeline Execution | PASS | Contracts and planned services use async append/query/migration/cleanup/flush methods. |
| V. Testing Discipline | PASS | Story tasks include independent tests for each deliverable slice and clarified edge behavior. |
| VI. Trunk-Based Development | PASS | The SQLite slice is independently releasable. |
| VII. Simplicity, SRP, DRY & KISS | PASS | The design avoids EF Core and skips exporter scope while keeping relational extensibility focused. |

## Phase 2 Handoff

Use [tasks.md](./tasks.md) as the implementation backlog. Suggested order:

1. Introduce storage/live-feed contracts while preserving current behavior.
2. Convert the in-memory provider into reusable in-memory store/live-feed pieces behind the existing facade.
3. Add shared relational persistence contracts, record model, dialect, migration runner, retention service, write queue, and store implementation.
4. Add SQLite provider registration, dialect, migration runner configuration, startup migration option, and tests.
5. Update README/quickstart and run targeted builds/tests.

## Complexity Tracking

No constitution violations identified.

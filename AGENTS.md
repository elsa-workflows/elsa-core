# AGENTS.md

Guidance for AI coding agents working in this repository.

## Project Overview

- Elsa Core is a .NET solution centered on `Elsa.sln`.
- Production code lives under `src/`.
- Tests live under `test/`, grouped by `unit`, `integration`, `component`, and `performance`.
- Build automation is implemented with NUKE in `build/Build.cs` and exposed through `build.sh`, `build.ps1`, and `build.cmd`.

## Working Rules

- Keep changes focused on the requested task. Do not mix unrelated cleanup, formatting, or dependency updates into functional changes.
- Preserve existing public APIs unless the task explicitly requires an API change.
- Update tests when behavior changes and add tests for new behavior where practical.
- Update documentation when changing externally visible behavior, configuration, APIs, or developer workflows.
- Do not delete generated-looking, artifact, or IDE files unless the task explicitly asks for cleanup.
- If the worktree contains unrelated user changes, leave them untouched.

## Agent Operating Principles

- Do not assume, hide confusion, or flatten uncertainty; surface questions, constraints, and tradeoffs explicitly.
- Write the minimum code that solves the defined problem; do not add speculative abstractions, features, or cleanup.
- Touch only the files and behavior required for the task; clean up only issues introduced by your own changes.
- Define success criteria before implementation, then iterate until the criteria are verified or clearly state what could not be verified.

## Build And Test Commands

- Build the default target: `./build.sh`
- Run NUKE test target: `./build.sh Test`
- Build with dotnet directly: `dotnet build Elsa.sln`
- Run all tests directly: `dotnet test Elsa.sln`
- Run a specific test project: `dotnet test test/unit/Elsa.Workflows.Core.UnitTests/Elsa.Workflows.Core.UnitTests.csproj`
- Run ElsaScript DSL integration tests: `./run-dsl-tests.sh`

Prefer targeted `dotnet test <project>` commands while iterating, then run a broader build or test command when the change affects shared infrastructure or cross-module behavior.

## Code Style

- C# uses `LangVersion` `latest`, nullable reference types, and implicit usings from `Directory.Build.props`.
- Projects under `src/` target `net8.0`, `net9.0`, and `net10.0` through `src/Directory.Build.props`.
- Follow `.editorconfig`: 4-space indentation for C#, file-scoped namespaces, braces required, `var` preferred, usings outside namespaces, and sorted system directives.
- Prefer clear domain names over abbreviations. Keep types small and responsibilities explicit.
- Avoid suppressing warnings unless compatibility or framework constraints make the warning unavoidable; explain the reason near the suppression.

## Repository Structure

- `src/apps/`: runnable app hosts and sample packages.
- `src/clients/`: client libraries.
- `src/common/`: shared infrastructure and testing support.
- `src/extensions/`: extension packages.
- `src/modules/`: Elsa modules and feature packages.
- `test/unit/`: unit tests.
- `test/integration/`: integration tests.
- `test/component/`: component tests.
- `test/performance/`: performance tests.
- `build/`: NUKE build project.
- `doc/`, `design/`, and `specs/`: documentation, design assets, and feature specifications.

## Testing Guidance

- Place tests near the relevant existing test project rather than creating a new project by default.
- Use the existing shared testing helpers under `src/common/Elsa.Testing.Shared*` when they fit the scenario.
- For regressions, add a failing test that demonstrates the bug before or alongside the fix.
- For multi-targeting issues, consider whether the test must run against all target frameworks or only the affected one.

## Dependency And Package Guidance

- Central package versions are managed in `Directory.Packages.props`; do not add ad hoc versions to individual project files unless the repository already uses that pattern for the package.
- Keep target framework changes centralized in `src/Directory.Build.props` unless a project has a specific reason to differ.
- Be cautious with package upgrades because this repository targets multiple frameworks and modules.

## Review Checklist

Before handing off changes, verify the following when applicable:

- The project or solution builds.
- Relevant unit or integration tests pass.
- Public API or behavior changes are documented.
- New code follows nullable annotations and existing style.
- No unrelated files were changed.

<!-- SPECKIT START -->
For additional context about technologies to be used, project structure,
shell commands, and other important information, read `specs/006-diagnostics-console-logs/plan.md`.
<!-- SPECKIT END -->

## Active Technologies
- C# latest, nullable reference types enabled, implicit usings enabled. + `Microsoft.Extensions.Logging`, `Microsoft.AspNetCore.SignalR`, Elsa feature/module infrastructure, FastEndpoints through Elsa API endpoint patterns, existing Elsa identity/authorization features. (003-live-server-logs)
- Bounded in-memory ring buffer for MVP; no EF Core schema changes. Provider abstraction allows external/shared log backends later. (003-live-server-logs)
- C# latest, nullable reference types enabled, implicit usings enabled. + `Microsoft.Extensions.Logging`, `Microsoft.Extensions.Options`, `Microsoft.AspNetCore.SignalR`, Elsa feature/module infrastructure, FastEndpoints through Elsa API endpoint patterns, CShells shell feature infrastructure. (004-diagnostics-structured-logs)
- Existing bounded in-memory ring buffer; no EF Core schema changes. Provider abstraction remains available for future shared backends. (004-diagnostics-structured-logs)
- C# latest, nullable reference types enabled, implicit usings enabled. + Existing `Elsa.Diagnostics.StructuredLogs`, `Microsoft.Extensions.Logging`, `Microsoft.Extensions.Options`, `Microsoft.AspNetCore.SignalR`, Elsa feature/module infrastructure, FastEndpoints through Elsa API endpoint patterns, FluentMigrator runner packages, SQLite ADO.NET provider, and optionally Dapper for relational operations. (005-structured-log-persistence)
- Bounded in-memory store by default; opt-in SQLite durable store through shared relational persistence. SQLite stores `Timestamp` and `ReceivedAt` as UTC ISO-8601 text and stores exception, scope, and property payloads as JSON text. (005-structured-log-persistence)
- C# latest, nullable reference types enabled, implicit usings enabled. + `Microsoft.Extensions.Options`, `Microsoft.AspNetCore.SignalR`, Elsa feature/module infrastructure, FastEndpoints through Elsa API endpoint patterns, Elsa shell feature infrastructure, and existing Elsa identity/authorization patterns. (006-diagnostics-console-logs)
- Bounded in-memory recent buffer and bounded subscriber queues by default; no durable database schema. Providers receive redacted content only. (006-diagnostics-console-logs)

## Recent Changes
- 006-diagnostics-console-logs: Plans raw stdout/stderr console capture with redaction-before-provider boundaries, bounded in-memory recent/live buffers, REST backfill/source endpoints, and a SignalR live hub.
- 005-structured-log-persistence: Plans pluggable structured log storage with in-memory default and opt-in SQLite persistence using FluentMigrator.
- 004-diagnostics-structured-logs: Refactors the unpublished server logs module into diagnostics structured logs and preserves bounded structured `ILogger` capture.
- 003-live-server-logs: Added C# latest, nullable reference types enabled, implicit usings enabled. + `Microsoft.Extensions.Logging`, `Microsoft.AspNetCore.SignalR`, Elsa feature/module infrastructure, FastEndpoints through Elsa API endpoint patterns, existing Elsa identity/authorization features.

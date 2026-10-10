# AGENTS.md

Guidance for AI coding agents working in this repository.

## Project Overview

- Elsa Core is a .NET solution centered on `Elsa.sln`.
- Products live under `core/`, `extensions/`, and `studio/`, each with `src/`, `test/`, and `docs/`.
- The root solution, solution filters, build automation, scripts and central policy are shared.
- Read the product documentation entrypoints: [Core](core/docs/README.md), [Extensions](extensions/docs/README.md), and [Studio](studio/docs/README.md).
- Build automation is implemented with NUKE in `build/Build.cs` and exposed through `build.sh`, `build.ps1`, and `build.cmd`.

## Working Rules

- Keep changes focused on the requested task. Do not mix unrelated cleanup, formatting, or dependency updates into functional changes.
- Preserve existing public APIs unless the task explicitly requires an API change.
- Update tests when behavior changes and add tests for new behavior where practical.
- Update documentation when changing externally visible behavior, configuration, APIs, or developer workflows.
- Do not delete generated-looking, artifact, or IDE files unless the task explicitly asks for cleanup.
- If the worktree contains unrelated user changes, leave them untouched.
- Before opening a PR, read `.github/reviewers.md`: Greptile is advisory (it reviews automatically; nobody waits for its score), request at most one additional advisory reviewer from the list once the PR is open, and merge only on the gate described there (Elsa 3 Code Review `APPROVE + HIGH @ <head sha>` plus green CI, merge pinned to that SHA).

## Agent Operating Principles

- Do not assume, hide confusion, or flatten uncertainty; surface questions, constraints, and tradeoffs explicitly.
- Write the minimum code that solves the defined problem; do not add speculative abstractions, features, or cleanup.
- Touch only the files and behavior required for the task; clean up only issues introduced by your own changes.
- Define success criteria before implementation, then iterate until the criteria are verified or clearly state what could not be verified.

## Build And Test Commands

- Build the default target: `./build.sh`
- Run NUKE test target: `./build.sh Test`
- Build with dotnet directly: `dotnet build Elsa.sln`
- Focus a product build with `dotnet build Elsa.Core.slnf`, `dotnet build Elsa.Extensions.slnf`, or `dotnet build Elsa.Studio.slnf`; see product docs for selection boundaries.
- Run all tests directly: `dotnet test Elsa.sln`
- Run a specific test project: `dotnet test core/test/unit/Elsa.Workflows.Core.UnitTests/Elsa.Workflows.Core.UnitTests.csproj`
- Run ElsaScript DSL integration tests: `./run-dsl-tests.sh`

Prefer targeted `dotnet test <project>` commands while iterating, then run a broader build or test command when the change affects shared infrastructure or cross-module behavior.

## Code Style

- C# uses `LangVersion` `latest`, nullable reference types, and implicit usings from `Directory.Build.props`.
- Product source libraries target `net8.0`, `net9.0`, and `net10.0` through their product `src/Directory.Build.props`; hosts and tests may select a specific framework.
- Follow `.editorconfig`: 4-space indentation for C#, file-scoped namespaces, braces required, `var` preferred, usings outside namespaces, and sorted system directives.
- Prefer clear domain names over abbreviations. Keep types small and responsibilities explicit.
- Avoid suppressing warnings unless compatibility or framework constraints make the warning unavoidable; explain the reason near the suppression.

## Repository Structure

- `core/src/apps/`, `core/src/clients/`, `core/src/common/`, `core/src/modules/`: Core hosts, clients, shared infrastructure and modules.
- `core/test/`: Core unit, integration, component and performance tests.
- `extensions/src/`, `extensions/test/`, `extensions/docs/`, `extensions/samples/`: Extensions-owned code, tests, documentation and samples.
- `studio/src/`, `studio/test/`, `studio/docs/`, `studio/samples/`: Studio-owned code, tests, documentation and samples. Read [Studio contributor guidance](studio/src/AGENTS.md) for Studio modules, hosts, frameworks and tests.
- `core/docs/specs/` and `studio/docs/specs/`: product feature plans and specifications.
- `docs/adr/`: shared architecture decisions; `docs/integration-program/`: retained consolidation and proof records.
- `build/`, `scripts/`, `docker/`, root solution/filter files and central `Directory.*` files: shared tooling and policy.
- `design/`: shared design assets; `core/docs/design/`: Core persistence design notes.

## Architecture Decision Records

- ADRs live in `docs/adr/`. New records are named `YYYY-MM-DD-slug.md`, and the `# ` heading carries the
  title alone with no numeric prefix. Do not continue the legacy `NNNN-` sequence: it collided on every
  concurrent branch, which is what the date prefix exists to stop.
- Records `0001`-`0027` keep their existing names and headings. Do not renumber or rename them.
- `docs/adr/toc.md` is generated. Never edit it by hand: run `scripts/adr/generate-toc.sh` and commit the
  result. `scripts/adr/generate-toc.sh --check` verifies it is current, and CI runs the same check.
- The rationale is recorded in
  [Identify new ADRs by date instead of a sequential number](docs/adr/2026-08-25-date-prefixed-adr-identifiers.md).

## Testing Guidance

- Place tests near the relevant existing test project rather than creating a new project by default.
- Use the existing shared testing helpers under `core/src/common/Elsa.Testing.Shared*` when they fit the scenario.
- For regressions, add a failing test that demonstrates the bug before or alongside the fix.
- For multi-targeting issues, consider whether the test must run against all target frameworks or only the affected one.

## Dependency And Package Guidance

- Central package versions are managed in the root and product `Directory.Packages.props` files; do not add ad hoc versions to individual project files unless the repository already uses that pattern for the package.
- Keep target framework changes centralized in the relevant product `src/Directory.Build.props` unless a project has a specific reason to differ.
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
shell commands, and other important information, read `core/docs/specs/013-user-tasks/plan.md`.
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
- C# latest, nullable reference types enabled, implicit usings enabled. + Elsa feature/module infrastructure, FastEndpoints through Elsa API endpoint patterns, existing Elsa identity/authorization patterns, Elsa workflow input metadata, `Microsoft.Extensions.Configuration`, `Microsoft.AspNetCore.DataProtection`, EF Core persistence infrastructure, mediator notifications, and optional JavaScript expression integration. (007-secrets-module)
- In-memory store for tests/development; Elsa-managed encrypted store with EF Core persistence for production; configuration-backed read-only store for deployment-managed values. No cloud vault or OS certificate store provider in v1. (007-secrets-module)
- C# latest, nullable reference types enabled, implicit usings enabled; paired Studio Blazor/Razor module work in the Studio repository. + Elsa feature/module infrastructure, FastEndpoints through Elsa API endpoint patterns, existing identity/authorization and tenancy services, workflow definition/instance abstractions, diagnostics/log abstractions, `Microsoft.Extensions.Options`, `Microsoft.Extensions.Logging`, OpenTelemetry, `System.Text.Json`, SignalR or SSE streaming, GitHub Copilot SDK isolated behind `Elsa.AI.Copilot`, and headless Copilot CLI JSON-RPC integration. (008-weaver-ai-copilot)
- Configurable conversation/session retention with in-memory support for development and tests; durable proposal and audit stores required for MVP using Elsa persistence provider abstractions and an EF Core provider package for production. (008-weaver-ai-copilot)
- C# latest, nullable reference types enabled, implicit usings enabled. + Elsa AI abstractions/host modules, `GitHub.Copilot.SDK` isolated behind `Elsa.AI.Copilot`, Activity Registry, workflow management/runtime abstractions, existing identity/tenancy services, FastEndpoints through Elsa endpoint patterns, `System.Text.Json`, `Microsoft.Extensions.Options`, and `Microsoft.Extensions.Logging`. (012-weaver-grounding-tools)
- Existing workflow definition/runtime stores and Activity Registry are read sources; durable proposal and audit stores remain the write/governance path; no new required database schema for the grounding MVP. (012-weaver-grounding-tools)

## Recent Changes
- 012-weaver-grounding-tools: Plans governed Weaver grounding tools for installed activities, workflow definitions, workflow proposals, workflow instances, incidents, and Studio capability discovery.
- 008-weaver-ai-copilot: Captures Weaver as a server-hosted, provider-isolated AI copilot platform with Studio chat, governed tools, proposal-only workflow mutations, audit, and extensibility.
- 006-diagnostics-console-logs: Plans raw stdout/stderr console capture with redaction-before-provider boundaries, bounded in-memory recent/live buffers, REST backfill/source endpoints, and a SignalR live hub.
- 005-structured-log-persistence: Plans pluggable structured log storage with in-memory default and opt-in SQLite persistence using FluentMigrator.
- 004-diagnostics-structured-logs: Refactors the unpublished server logs module into diagnostics structured logs and preserves bounded structured `ILogger` capture.
- 003-live-server-logs: Added C# latest, nullable reference types enabled, implicit usings enabled. + `Microsoft.Extensions.Logging`, `Microsoft.AspNetCore.SignalR`, Elsa feature/module infrastructure, FastEndpoints through Elsa API endpoint patterns, existing Elsa identity/authorization features.

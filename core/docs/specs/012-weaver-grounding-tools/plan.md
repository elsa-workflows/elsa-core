# Implementation Plan: Weaver Grounding Tools

**Branch**: `codex/weaver-grounding-tools` | **Date**: 2026-06-08 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/012-weaver-grounding-tools/spec.md`

## Summary

Ground Weaver in Elsa data by adding governed read-only and proposal-only tool families for installed activities, workflow definitions, workflow drafts, workflow instances, incidents, and Studio capability discovery. Copilot SDK continues to own the agent loop; Elsa Host supplies authorized, redacted, bounded tool callbacks and proposal validation.

## Technical Context

**Language/Version**: C# latest, nullable reference types enabled, implicit usings enabled.
**Primary Dependencies**: Elsa AI abstractions/host modules, `GitHub.Copilot.SDK` behind `Elsa.AI.Copilot`, Activity Registry (`IActivityRegistry`/activity descriptors), workflow management/runtime abstractions, existing identity/tenancy services, FastEndpoints through Elsa endpoint patterns, `System.Text.Json`, `Microsoft.Extensions.Options`, `Microsoft.Extensions.Logging`.
**Storage**: Existing workflow definition/runtime stores and Activity Registry are read sources; durable proposal and audit stores from Weaver remain the write/governance path; no new required database schema for the grounding MVP.
**Testing**: xUnit unit tests in `test/unit/Elsa.AI.Host.UnitTests`; integration tests in `test/integration/Elsa.AI.IntegrationTests`; component tests only if workflow runtime fixtures are needed for seeded incidents.
**Target Platform**: ASP.NET Core Elsa Server multi-targeting `net8.0`, `net9.0`, and `net10.0`.
**Project Type**: Modular .NET server libraries with REST/streaming APIs and provider-neutral Studio contracts.
**Performance Goals**: Tool metadata/capability responses under 250 ms p95 for typical catalogs; first grounded chat tool result within 3 seconds p95 under normal server load; tool result payloads bounded by configured AI context limits.
**Constraints**: Studio remains provider-agnostic; Copilot SDK types stay inside `Elsa.AI.Copilot`; all data access runs server-side; all tools enforce tenant/RBAC/ownership; mutation remains proposal-only; secrets are redacted before model context, streams, and audit; direct destructive operational actions are out of scope.
**Scale/Scope**: Activity discovery, workflow definition search/detail/graph summaries, workflow draft validation/proposals, runtime instance/incident inspection, capability discovery, tests, contracts, and quickstart documentation.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Modular Architecture**: Pass. Work remains inside `Elsa.AI.Host` and `Elsa.AI.Abstractions` with provider-specific runtime behavior isolated in `Elsa.AI.Copilot`.
- **Composition & Extensibility**: Pass. Grounding is expressed as `IAITool` and `IAIContextProvider` implementations registered by feature composition.
- **Convention-Driven Design**: Pass. Endpoint and service additions follow existing AI module patterns.
- **Async & Pipeline Execution**: Pass. Store/runtime reads and tool execution stay async.
- **Testing Discipline**: Pass. New tool families require unit and integration coverage.
- **Trunk-Based Development**: Pass. This plan is focused on one feature area.
- **Simplicity, SRP, DRY & KISS**: Pass. Start with deterministic Elsa tools and proposal flows; no vector database, broad provider abstraction, or direct operational action tools in MVP.

## Project Structure

### Documentation (this feature)

```text
specs/012-weaver-grounding-tools/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── rest-api.md
│   └── tool-catalog.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
src/modules/Elsa.AI.Abstractions/
├── Models/
│   └── AIGrounding*.cs
└── Contracts/

src/modules/Elsa.AI.Host/
├── Context/
├── Endpoints/AI/
│   ├── Capabilities/
│   ├── Tools/
│   └── Proposals/
├── Services/
├── Tools/
│   ├── Activities/
│   ├── Workflows/
│   └── Runtime/
└── Options/

test/unit/Elsa.AI.Host.UnitTests/
├── Grounding/
└── Tools/

test/integration/Elsa.AI.IntegrationTests/
```

**Structure Decision**: Add tool implementations under `Elsa.AI.Host/Tools` by domain. Shared DTOs that are part of provider-neutral tool results belong in `Elsa.AI.Abstractions/Models`; mapping services stay in `Elsa.AI.Host/Services`. Avoid a new module until the grounding surface grows beyond Weaver Host ownership.

## Complexity Tracking

No constitution violations.

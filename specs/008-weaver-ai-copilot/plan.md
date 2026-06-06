# Implementation Plan: Weaver AI Copilot Platform

**Branch**: `codex/008-weaver-ai-copilot` | **Date**: 2026-05-21 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/008-weaver-ai-copilot/spec.md`

## Summary

Introduce Weaver as Elsa's AI copilot platform: a server-hosted, provider-isolated AI orchestration layer with Studio chat, governed tool execution, context providers, streaming events, durable audit records, and durable proposal-only workflow mutations. The first delivery establishes `Elsa.AI.Abstractions`, `Elsa.AI.Host`, `Elsa.AI.Copilot`, durable proposal/audit persistence, and a paired `Elsa.Studio.AI` module, with read-only workflow/runtime tools and safe workflow proposal flows.

## Technical Context

**Language/Version**: C# latest, nullable reference types enabled, implicit usings enabled; paired Studio Blazor/Razor module work in the Studio repository.  
**Primary Dependencies**: Elsa feature/module infrastructure, FastEndpoints through Elsa API endpoint patterns, existing identity/authorization and tenancy services, workflow definition/instance abstractions, diagnostics/log abstractions, `Microsoft.Extensions.Options`, `Microsoft.Extensions.Logging`, OpenTelemetry, `System.Text.Json`, SignalR or SSE streaming, GitHub Copilot SDK isolated behind `Elsa.AI.Copilot`, and headless Copilot CLI JSON-RPC integration.  
**Storage**: Configurable conversation/session retention with in-memory support for development and tests; durable proposal and audit stores required for MVP using Elsa persistence provider abstractions and an EF Core provider package for production.  
**Testing**: xUnit unit tests for abstractions, tool metadata, authorization gates, context redaction, proposal lifecycle, persistence state transitions, explicit tool enablement, reconnect handling, and adapter mapping; integration tests for chat streaming, tool invocation, capabilities, proposal apply, tenant isolation, durable audit records, durable proposals, and scoped trend analysis; contract tests for Studio-facing API and stream event shapes.  
**Target Platform**: ASP.NET Core Elsa Server on supported .NET target frameworks, plus Elsa Studio SPA integration through server APIs only.  
**Project Type**: Modular .NET server libraries with REST/streaming APIs and a paired Studio UI module.  
**Performance Goals**: First streamed chat event within 3 seconds p95 after server accepts a turn under normal load; tool metadata/capabilities under 250 ms p95; proposal validation under 5 seconds p95 for typical workflow definitions; bounded server-side context payloads.  
**Constraints**: Studio is AI-agnostic; AI runtime is server-hosted; provider SDK types cannot leak into core abstractions, workflow models, or Studio contracts; AI writes are proposal-only; same authorized user may request/approve/apply proposals in MVP; all tools enforce tenant/RBAC/ownership server-side; proposal/admin/MCP tools require explicit administrator enablement; runtime trend analysis is scoped to attached references plus selected time range and diagnostics scope; secrets and sensitive config are redacted before model context, stream output, and audit records; implementation execution should use dedicated git worktrees for Core and paired Studio work rather than using a local primary checkout directly.  
**Scale/Scope**: Server AI abstractions, Copilot adapter, chat/session orchestration with configurable disconnect grace, stream event translation, tool registry, context providers, durable audit sink, durable proposal store, MVP workflow/runtime tools, proposal apply endpoint, Studio chat/proposal UX contracts, and extension APIs for third-party tools/agents/MCP registrations.

## Constitution Check

Evaluated against `.specify/memory/constitution.md` v1.1.0:

| Principle | Verdict | Evidence |
|-----------|---------|----------|
| I. Modular Architecture | PASS | AI abstractions, host/orchestration, provider adapter, and Studio UI are separate modules with published contracts. |
| II. Composition & Extensibility | PASS | Tool registry, context providers, proposal store, audit sink, provider adapter, agents, and MCP registrations are explicit extension points. |
| III. Convention-Driven Design | PASS | Endpoints use Elsa endpoint patterns; modules, features, contracts, and tests follow repository naming conventions and American English. |
| IV. Async & Pipeline Execution | PASS | Chat, streaming, tool execution, context resolution, validation, audit, and persistence contracts are async and cancellation-aware. |
| V. Testing Discipline | PASS | Plan includes unit, integration, and contract coverage for governance, streaming, proposal safety, and adapter boundaries. |
| VI. Trunk-Based Development | PASS | This plan is a single feature concern; Core and Studio repository work are documented as separate implementation surfaces. |
| VII. Simplicity, SRP, DRY & KISS | PASS | MVP defers administrative mutation tools and limits durable persistence to proposal and audit governance records; provider volatility is contained in one adapter module. |

## Project Structure

### Documentation (this feature)

```text
specs/008-weaver-ai-copilot/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── runtime-contract.md
│   ├── rest-api.md
│   ├── studio-contract.md
│   └── tool-governance.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
src/modules/
├── Elsa.AI.Abstractions/
│   ├── Contracts/
│   ├── Models/
│   ├── Options/
│   └── Extensions/
├── Elsa.AI.Host/
│   ├── Context/
│   ├── Endpoints/AI/
│   ├── Extensions/
│   ├── Features/
│   ├── Models/
│   ├── Permissions/
│   ├── Proposals/
│   ├── Services/
│   ├── ShellFeatures/
│   ├── Streaming/
│   └── Tools/
├── Elsa.AI.Copilot/
│   ├── Adapters/
│   ├── Extensions/
│   ├── Features/
│   ├── Models/
│   ├── Options/
│   ├── ShellFeatures/
│   └── Services/
└── Elsa.AI.Persistence.EFCore/
    ├── Configurations/
    ├── Entities/
    ├── Extensions/
    ├── Features/
    ├── Migrations/
    ├── ShellFeatures/
    └── Stores/

test/unit/
├── Elsa.AI.Abstractions.UnitTests/
├── Elsa.AI.Host.UnitTests/
├── Elsa.AI.Copilot.UnitTests/
└── Elsa.AI.Persistence.EFCore.UnitTests/

test/integration/
└── Elsa.AI.IntegrationTests/
```

### Paired Studio Repository

```text
src/modules/
└── Elsa.Studio.AI/
    ├── Client/
    ├── Contracts/
    ├── Extensions/
    ├── Feature.cs
    ├── Services/
    ├── UI/
    │   ├── Components/
    │   └── Pages/
    └── _Imports.razor
```

**UI Prototype Reference**: Review `elsa-extensions` branch `origin/feat/ai` at commit `93f0e09d71e57f5daff1e2d593f0a51faaa80417` and its parent chain before implementing Studio UI. Useful patterns include the Razor/MudBlazor Agents menu placement under `/ai/*`, management tables, route structure, Refit client interfaces, validators, and agent configuration tabs for general metadata, input/output variables, services, plugins, and execution settings. Do not carry forward raw API key reveal, provider-specific service configuration as the primary experience, or an agent-management-first flow; Weaver's first screen remains the chat/proposal experience.

**Structure Decision**: Keep provider-neutral contracts in `Elsa.AI.Abstractions`, server orchestration, APIs, built-in tools, proposals, and audit in `Elsa.AI.Host`, and Copilot SDK/CLI integration in `Elsa.AI.Copilot`. The Studio module consumes only REST and streaming contracts; if the Studio repository is not present, its implementation tasks become a sibling-repository follow-up.

## Phase 0 Output

See [research.md](./research.md).

Resolved decisions:

- Use a server-hosted AI runtime with Studio sending only references.
- Isolate GitHub Copilot SDK and headless CLI JSON-RPC behind `Elsa.AI.Copilot`.
- Translate provider stream events into Elsa-owned stream contracts.
- Use proposal-only writes for workflow creation and updates.
- Use configurable conversation retention, but require durable proposal and audit stores for MVP.
- Allow the same authorized user to request, approve, reject, and apply proposals in MVP, with explicit actions and durable audit records.
- Require explicit administrator enablement for proposal, administrative, and MCP-backed tools; read-only module tools may be available by default.
- Continue disconnected chat turns for a configurable grace window and allow authorized clients to reconnect to durable outputs.
- Limit runtime trend analysis by default to attached references plus explicit time range and diagnostics scope.
- Treat MCP and custom agents as governed extension registrations, not unrestricted runtime configuration.

## Phase 1 Output

- [data-model.md](./data-model.md)
- [contracts/runtime-contract.md](./contracts/runtime-contract.md)
- [contracts/rest-api.md](./contracts/rest-api.md)
- [contracts/studio-contract.md](./contracts/studio-contract.md)
- [contracts/tool-governance.md](./contracts/tool-governance.md)
- [quickstart.md](./quickstart.md)

## Post-Design Constitution Re-Check

| Principle | Verdict | Post-design evidence |
|-----------|---------|----------------------|
| I. Modular Architecture | PASS | Contracts separate abstractions, host behavior, provider adapter, and Studio-facing UI contracts. |
| II. Composition & Extensibility | PASS | Tool, context, agent, provider, MCP, proposal, audit, and streaming contracts are replaceable. |
| III. Convention-Driven Design | PASS | Contract and API names align with Elsa module and endpoint conventions. |
| IV. Async & Pipeline Execution | PASS | Design contracts use async APIs and stream abstractions for all I/O and tool work. |
| V. Testing Discipline | PASS | Quickstart and tasks define unit, integration, and contract verification before implementation. |
| VI. Trunk-Based Development | PASS | Implementation can ship incrementally by independently testable user story phases. |
| VII. Simplicity, SRP, DRY & KISS | PASS | MVP excludes administrative tools, generic agent runtime reimplementation, direct Studio/provider coupling, and durable conversation persistence as a requirement. |

## Phase 2 Handoff

[tasks.md](./tasks.md) has been generated and analysis-remediated to reflect durable proposal/audit persistence, explicit tool enablement, reconnect handling, scoped trend analysis, ShellFeature registration, provider/BYOK configuration, OpenTelemetry, redaction boundaries, retention tests, and worktree-based execution. Suggested MVP cut is Phase 1 setup, Phase 2 foundation, durable proposal/audit storage, and User Story 1 chat/tool streaming. Proposal generation is the next reviewable increment.

## Complexity Tracking

No constitution violations identified.

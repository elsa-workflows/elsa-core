# Implementation Plan: Shell Reload API Endpoints

**Branch**: `001-shell-reload-api` | **Date**: 2026-03-08 | **Spec**: `/Users/sipke/Projects/Elsa/elsa-core/main/specs/001-shell-reload-api/spec.md`
**Input**: Feature specification from `/specs/001-shell-reload-api/spec.md`

## Summary

Add two administrative shell reload endpoints and a matching typed API client surface so operators can refresh shell-backed feature configuration without restarting the host. The implementation will use a dedicated shell reload orchestration service in `Elsa.Workflows.Api` to validate shell IDs, reject concurrent reloads, execute the current full-reload fallback for targeted requests, and return detailed per-shell outcomes that satisfy the clarified spec.

## Technical Context

**Language/Version**: C# latest on .NET 10.0 primary, with existing multi-target support for .NET 8.0 and .NET 9.0  
**Primary Dependencies**: FastEndpoints, Elsa.Api.Common abstractions, CShells, CShells.FastEndpoints.Abstractions, Refit client contracts, xUnit component test infrastructure  
**Storage**: No new persistent storage; uses the existing CShells shell settings provider and in-memory shell settings cache  
**Testing**: xUnit component tests in `Elsa.Workflows.ComponentTests`; no existing dedicated `Elsa.Workflows.Api` unit test project  
**Target Platform**: ASP.NET Core modular server host exposing Elsa REST APIs through shell-aware FastEndpoints  
**Project Type**: Modular .NET library feature plus first-party API client contract  
**Performance Goals**: Keep shell reload as a synchronous admin operation within the existing API client timeout budget of 1 minute; do not add background polling or queued work  
**Constraints**: Preserve existing shell-aware endpoint discovery, keep the feature scoped to current modules, reject concurrent reload requests, return per-shell outcomes, and keep targeted reload on full-reload fallback semantics until upstream CShells adds a selective reload API  
**Scale/Scope**: Two new endpoints, one shared response contract, one orchestration service, one API client resource, and focused component-test coverage in the existing server fixture

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Pre-Phase 0 Gate Review**

- **I. Modular Architecture**: PASS. Changes stay inside existing bounded areas: `Elsa.Workflows.Api`, `Elsa.Api.Client`, and `Elsa.Workflows.ComponentTests`.
- **II. Composition & Extensibility**: PASS. A dedicated orchestration contract isolates current fallback behavior from future upstream targeted reload support.
- **III. Convention-Driven Design**: PASS. Endpoints will remain one-class-per-endpoint with collocated request/response models and standard permission configuration.
- **IV. Async & Pipeline Execution**: PASS. Endpoint and orchestration flows remain async-only and use existing middleware/endpoint infrastructure.
- **V. Testing Discipline**: PASS. Feature verification will use the existing component-test host and endpoint-style tests already used for REST APIs.
- **VI. Trunk-Based Development**: PASS. The change is one concern and includes API contract artifacts for downstream client work.
- **VII. Simplicity & Focus**: PASS. No new module is introduced; one orchestration service is the minimum additional abstraction needed to satisfy busy-state handling, fallback semantics, and detailed outcomes.

**Post-Phase 1 Design Review**

- PASS. The design keeps the concern localized, introduces no extra module boundary, and uses only one new service abstraction because direct endpoint-to-`IShellManager` calls cannot satisfy partial-success reporting, busy rejection, and requested-shell strictness together.

## Project Structure

### Documentation (this feature)

```text
specs/001-shell-reload-api/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── shell-reload-api.yaml
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── modules/
│   └── Elsa.Workflows.Api/
│       ├── Contracts/
│       ├── Services/
│       └── Endpoints/
│           └── Shells/
│               ├── Reload/
│               └── ReloadAll/
└── clients/
  └── Elsa.Api.Client/
    └── Resources/
      └── Shells/
        ├── Contracts/
        ├── Requests/
        ├── Responses/
        └── Models/

test/
└── component/
  └── Elsa.Workflows.ComponentTests/
    └── Scenarios/
      └── RestApis/
        └── Endpoints/
          └── Shells/
```

**Structure Decision**: Extend the existing workflow API module and first-party API client instead of adding a new shell-management module. This aligns with the repository’s established pattern where administrative REST endpoints and typed client contracts evolve together while component tests validate behavior through the hosted server fixture.

## Complexity Tracking

No constitutional violations requiring justification.


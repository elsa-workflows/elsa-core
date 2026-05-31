# Implementation Plan: Workflow JSON Type Hardening

**Branch**: `codex/7541-workflow-json-hardening` | **Date**: 2026-05-29 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/010-workflow-json-hardening/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Reintroduce workflow JSON hardening through a workflow-specific type registry and resolver. Existing registered legacy CLR names remain readable, new workflow JSON emits aliases when available, and public descriptor APIs use the same identifier contract they accept.

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: C# latest with nullable reference types, multi-targeting through existing project settings
**Primary Dependencies**: System.Text.Json, Elsa feature/module infrastructure, existing expression registry for expression-only behavior
**Storage**: Existing workflow JSON payloads only; no schema changes
**Testing**: Targeted `dotnet test` for workflow core/runtime/api test projects
**Target Platform**: Elsa server/library consumers on supported .NET target frameworks
**Project Type**: Modular .NET library and API modules
**Performance Goals**: Type lookup remains dictionary-based and does not add reflection scans to normal serialization
**Constraints**: No arbitrary CLR type loading; compatibility only for explicitly registered workflow JSON legacy names
**Scale/Scope**: Workflow definitions, workflow state, trigger/bookmark payloads, incident strategy descriptor contract, and module/extension registrations

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

PASS. The change stays inside existing workflow modules, adds one explicit extensibility point for serialization trust, preserves public compatibility, and includes focused tests. No new persistence provider or unrelated module is introduced.

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
src/modules/Elsa.Workflows.Core/
├── Contracts/
├── Extensions/
├── Options/
├── Serialization/
└── Features/

src/modules/Elsa.Workflows.Runtime/
├── Features/
├── ShellFeatures/
└── Services/

src/modules/Elsa.Workflows.Api/
└── Endpoints/IncidentStrategies/

test/unit/Elsa.Workflows.Core.UnitTests/
test/unit/Elsa.Workflows.Runtime.UnitTests/
test/integration/Elsa.Workflows.IntegrationTests/
```

**Structure Decision**: Extend the existing workflow core serialization surface and update runtime/API registrations in place. Tests stay in the nearest existing workflow test projects.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |

# Implementation Plan: State Machine Designer

**Branch**: `005-state-machine-designer` | **Date**: 2026-05-16 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/005-state-machine-designer/spec.md`

## Summary

Add Studio-side designer support for the backend `StateMachine` activity from elsa-core issue #5085 / PR #7457. The implementation should register a dedicated diagram designer for `Elsa.StateMachine`, map state-machine JSON into a graph of state nodes and transition route cards, expose transition trigger/condition/action slots and state entry/exit slots through existing Studio editing patterns, and keep this designer isolated from the full-screen root Flowchart designer.

## Technical Context

**Language/Version**: C# latest and Razor components in the existing Blazor Studio solution.  
**Primary Dependencies**: Elsa Studio Workflows module, Elsa Studio Workflows Designer module, existing X6/ReactFlow designer models and interop, activity registry, activity picker, activity property/expression editors, MudBlazor UI patterns.  
**Storage**: Workflow definition activity JSON only; no Studio persistence or backend schema changes.  
**Testing**: Targeted unit tests for state-machine graph mapping and validation when a suitable test project is available; targeted `dotnet build` for affected Studio projects; sample-host/manual verification from quickstart.  
**Target Platform**: Blazor Server and Blazor WebAssembly Studio hosts.  
**Project Type**: Elsa Studio Workflows designer feature inside existing module boundaries.  
**Performance Goals**: Keep graph interactions responsive for at least 50 states and 100 transitions; avoid re-rendering the root Flowchart when editing nested state-machine details; preserve save/read operations without JSON loss.  
**Constraints**: Do not modify the separate elsa-core backend repository; keep backend PR #7457 separate; preserve existing Flowchart designer behavior; support read-only views; handle missing backend activity descriptor gracefully.  
**Scale/Scope**: One dedicated diagram designer provider, state-machine graph mapper, state/transition selection UI, slot editing integration, validation feedback, tests where practical, and docs.

## Constitution Check

Evaluated against `.specify/memory/constitution.md` v1.0.0:

| Principle | Verdict | Evidence |
|-----------|---------|----------|
| I. Modular Studio Features | PASS | Work stays in Workflows/Designer module boundaries with a dedicated provider and mapper for StateMachine. |
| II. Backend Capability Awareness | PASS | Designer depends on the advertised `Elsa.StateMachine` activity descriptor and must degrade cleanly when unavailable. |
| III. UX Consistency and Density | PASS | UX uses existing diagram toolbar, details panel, activity picker, and expression editor patterns rather than a new shell. |
| IV. Async, Disposal, and Real-Time Discipline | PASS | Any JS interop or graph subscriptions are scoped to components and disposed like existing designers. |
| V. Testing and Verification | PASS | Mapper validation, targeted build, and sample-host read/write scenarios are called out. |
| VI. Focused Change Sets | PASS | No backend, package upgrade, or unrelated Flowchart refactor is planned. |
| VII. Simplicity, DRY, and Maintainability | PASS | Reuse graph primitives and activity editing services; add abstractions only around state-machine mapping and validation. |

## Project Structure

### Documentation (this feature)

```text
specs/005-state-machine-designer/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── state-machine-designer-contract.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
src/modules/
├── Elsa.Studio.Workflows/
│   ├── DiagramDesigners/
│   │   └── StateMachines/
│   │       ├── StateMachineDiagramDesigner.cs
│   │       ├── StateMachineDiagramDesignerProvider.cs
│   │       └── StateMachineDesignerWrapper.razor(.cs)
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs
├── Elsa.Studio.Workflows.Designer/
│   ├── Contracts/
│   │   └── IStateMachineMapper.cs
│   ├── Models/
│   │   ├── StateMachineGraph.cs
│   │   ├── StateMachineStateNode.cs
│   │   ├── StateMachineTransitionEdge.cs
│   │   └── StateMachineValidationIssue.cs
│   └── Services/
│       ├── StateMachineMapper.cs
│       └── StateMachineValidator.cs
└── Elsa.Studio.Workflows.Core/
    └── UI/Models/ # only if shared display context additions are required

src/modules/Elsa.Studio.Workflows.Designer.Tests/ # create only if mapper tests need a new test project
```

**Structure Decision**: Put the Studio orchestration and provider under `Elsa.Studio.Workflows` next to Flowchart diagram designers, and put reusable graph mapping/validation under `Elsa.Studio.Workflows.Designer` next to existing X6 mapper services. Avoid touching Flowchart components except where a stable shared interface is required.

## Phase 0 Output

See [research.md](./research.md).

Resolved decisions:

- Register a dedicated `StateMachineDiagramDesignerProvider` for `Elsa.StateMachine`.
- Introduce state-machine-specific mapper/validator services instead of overloading `FlowchartMapper`.
- Use state names as the graph identity for MVP because backend transitions reference `from`/`to` names.
- Start with graph inspection and read/write mapping before advanced embedded slot UX.
- Keep nested child activity slot editing out of the root Flowchart component path.

## Phase 1 Output

- [data-model.md](./data-model.md)
- [contracts/state-machine-designer-contract.md](./contracts/state-machine-designer-contract.md)
- [quickstart.md](./quickstart.md)

## Post-Design Constitution Re-Check

| Principle | Verdict | Post-design evidence |
|-----------|---------|----------------------|
| I. Modular Studio Features | PASS | Provider, wrapper, mapper, validator, and optional tests have explicit module locations. |
| II. Backend Capability Awareness | PASS | Contract and tasks require descriptor gating and missing-backend handling. |
| III. UX Consistency and Density | PASS | Quickstart uses existing workflow editor interactions and compact diagram controls. |
| IV. Async, Disposal, and Real-Time Discipline | PASS | Component and JS graph lifetime must follow existing designer disposal patterns. |
| V. Testing and Verification | PASS | Tasks include mapper tests, affected project build, and sample-host verification. |
| VI. Focused Change Sets | PASS | No elsa-core changes and no unrelated Flowchart redesign are included. |
| VII. Simplicity, DRY, and Maintainability | PASS | Dedicated mapper prevents fragile Flowchart conditionals while reusing shared graph primitives where possible. |

## Phase 2 Handoff

Use [tasks.md](./tasks.md) as the implementation backlog. Suggested implementation order:

1. Add mapper/validator tests and plain graph models for StateMachine JSON.
2. Register a read-only StateMachine designer provider and wrapper that renders state nodes and transition route cards.
3. Add save/read mapping for state and transition graph edits.
4. Add transition details editing for trigger, condition, and action slots.
5. Add state details editing for entry and exit slots.
6. Verify root Flowchart behavior is unchanged.

## Complexity Tracking

No constitution violations identified.

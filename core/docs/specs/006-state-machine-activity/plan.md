# Implementation Plan: State Machine Activity

**Branch**: `006-state-machine-activity` | **Date**: 2026-05-16 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/006-state-machine-activity/spec.md`

## Summary

Add a server-side `StateMachine` activity to Elsa Workflows Core. The activity owns named states and directed transitions, schedules state entry before outbound triggers, accepts a transition when its trigger completes and condition evaluates true, cancels competing triggers, runs transition action, exits source state, enters target state, and updates current state. States with no valid outbound transitions are terminal and complete the state machine. False transition conditions re-arm the failed trigger while leaving competing triggers active.

## Technical Context

**Language/Version**: C# latest with nullable reference types  
**Primary Dependencies**: Elsa Workflows Core activity model, expression inputs, scheduler callbacks, existing cancellation behavior  
**Storage**: Workflow activity state only; no persistence schema changes  
**Testing**: xUnit in `test/unit/Elsa.Activities.UnitTests` with `ActivityTestFixture`  
**Target Platform**: Elsa server-side workflow runtime  
**Project Type**: .NET library module  
**Performance Goals**: Schedule only current-state entry and outbound triggers; no graph-wide execution scan on each transition; false conditions reschedule only the failed trigger  
**Constraints**: Preserve existing public APIs and scheduler semantics; keep Studio designer work out of this change  
**Scale/Scope**: Backend activity, models, focused unit tests, terminal-state semantics, and trigger re-arm semantics

## Constitution Check

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Modular Architecture | PASS | Feature lives in existing `Elsa.Workflows.Core` activity module. |
| II. Composition & Extensibility | PASS | Uses existing activity slots and expression-capable condition input. |
| III. Convention-Driven Design | PASS | Activity metadata, `[Input]`, `[Port]`, and model conventions follow nearby activities. |
| IV. Async & Pipeline Execution | PASS | Scheduling uses existing async callbacks and activity execution context APIs. |
| V. Testing Discipline | PASS | Adds focused unit tests in the existing activity test project. |
| VI. Trunk-Based Development | PASS | Scope is one coherent backend concern. |
| VII. Simplicity, SRP, DRY & KISS | PASS | One activity plus two simple models; no speculative designer or persistence abstraction. |

## Project Structure

### Documentation (this feature)

```text
specs/006-state-machine-activity/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
└── tasks.md
```

### Source Code (repository root)

```text
src/modules/Elsa.Workflows.Core/
├── Activities/StateMachine/Activities/StateMachine.cs
└── Activities/StateMachine/Models/
    ├── StateMachineState.cs
    └── Transition.cs

test/unit/Elsa.Activities.UnitTests/
└── StateMachine/StateMachineTests.cs
```

**Structure Decision**: Implement inside `Elsa.Workflows.Core` because state-machine control flow is a core workflow activity like `Sequence`, `Switch`, and `Flowchart`.

## Complexity Tracking

No constitution violations.

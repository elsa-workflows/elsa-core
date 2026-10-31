# Tasks: State Machine Activity

**Input**: Design documents from `/specs/006-state-machine-activity/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Required by FR-014.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: No project setup needed; reuse existing workflow core and activity unit test projects.

- [X] T001 Confirm existing activity and test project structure in `src/modules/Elsa.Workflows.Core/Activities/` and `test/unit/Elsa.Activities.UnitTests/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add the activity model types used by all stories.

- [X] T002 [P] Create `StateMachineState` model in `src/modules/Elsa.Workflows.Core/Activities/StateMachine/Models/StateMachineState.cs`
- [X] T003 [P] Create `Transition` model in `src/modules/Elsa.Workflows.Core/Activities/StateMachine/Models/Transition.cs`
- [X] T004 Create `StateMachine` activity skeleton in `src/modules/Elsa.Workflows.Core/Activities/StateMachine/Activities/StateMachine.cs`

---

## Phase 3: User Story 1 - Run a State Through Entry and Triggers (Priority: P1) MVP

**Goal**: Initial state entry runs before outbound transition triggers and the machine stays running.

**Independent Test**: Execute a two-state machine and inspect scheduled activities and current state.

### Tests for User Story 1

- [X] T005 [P] [US1] Add tests for initial state entry and outbound trigger scheduling in `test/unit/Elsa.Activities.UnitTests/StateMachine/StateMachineTests.cs`

### Implementation for User Story 1

- [X] T006 [US1] Implement initial state resolution, entry scheduling, and outbound trigger scheduling in `src/modules/Elsa.Workflows.Core/Activities/StateMachine/Activities/StateMachine.cs`

---

## Phase 4: User Story 2 - Complete a Winning Transition (Priority: P2)

**Goal**: A true transition condition runs action, source exit, target entry, updates current state, and schedules target triggers.

**Independent Test**: Invoke completion callbacks for trigger/action/exit/entry and verify the state path.

### Tests for User Story 2

- [X] T007 [P] [US2] Add tests for true conditions, missing conditions, false conditions, and empty slots in `test/unit/Elsa.Activities.UnitTests/StateMachine/StateMachineTests.cs`

### Implementation for User Story 2

- [X] T008 [US2] Implement transition condition evaluation and action/exit/entry progression in `src/modules/Elsa.Workflows.Core/Activities/StateMachine/Activities/StateMachine.cs`

---

## Phase 5: User Story 3 - Cancel Competing Triggers (Priority: P3)

**Goal**: Accepted transitions cancel competing outbound trigger contexts.

**Independent Test**: Execute a state with two triggers, accept one trigger, and verify the losing trigger context is canceled.

### Tests for User Story 3

- [X] T009 [P] [US3] Add competing trigger cancellation test in `test/unit/Elsa.Activities.UnitTests/StateMachine/StateMachineTests.cs`

### Implementation for User Story 3

- [X] T010 [US3] Implement competing trigger tracking and cancellation in `src/modules/Elsa.Workflows.Core/Activities/StateMachine/Activities/StateMachine.cs`

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T011 Run `dotnet test test/unit/Elsa.Activities.UnitTests/Elsa.Activities.UnitTests.csproj`
- [X] T012 Update task checkboxes in `specs/006-state-machine-activity/tasks.md`
- [X] T013 [P] Update Spec Kit artifacts for terminal-state clarification in `specs/006-state-machine-activity/`
- [X] T014 [P] Update Spec Kit artifacts for false-condition trigger re-arm clarification in `specs/006-state-machine-activity/`
- [X] T015 [P] Add false-condition trigger re-arm assertion in `test/unit/Elsa.Activities.UnitTests/StateMachine/StateMachineTests.cs`
- [X] T016 Implement false-condition trigger re-arm behavior in `src/modules/Elsa.Workflows.Core/Activities/StateMachine/Activities/StateMachine.cs`

## Dependencies & Execution Order

- Phase 1 before all implementation.
- Phase 2 before user stories.
- User Story 1 before User Story 2.
- User Story 2 before User Story 3.
- Polish after all desired user stories.

## Parallel Opportunities

- T002 and T003 can run in parallel.
- Test additions are in one file and should be coordinated if worked in parallel.

## Implementation Strategy

Complete the backend MVP first: model types, initial entry, outbound trigger scheduling, transition progression, and cancellation semantics. Validate with focused unit tests.

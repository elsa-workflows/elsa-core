# Feature Specification: State Machine Activity

**Feature Branch**: `006-state-machine-activity`  
**Created**: 2026-05-16  
**Status**: Draft  
**Input**: GitHub issue #5085, "State Machine Activity"

## Clarifications

### Session 2026-05-16

- Q: What should a state machine do after it enters a state that has no valid outbound transitions? → A: Complete the `StateMachine` when the current state has no valid outbound transitions.
- Q: What should happen to the transition trigger whose condition evaluates false? → A: Keep the failed transition trigger active so it can fire again later.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Run a State Through Entry and Triggers (Priority: P1)

A workflow author can model a state machine with an initial state, entry action, exit action, and outbound transitions whose trigger activities wait for external or delayed stimuli.

**Why this priority**: This is the minimum useful server-side state machine behavior and proves that Elsa can keep a workflow instance active while a state waits for transition triggers.

**Independent Test**: Execute a state machine with two states and one transition, then verify that initial state entry and outbound trigger scheduling happen without completing the state machine.

**Acceptance Scenarios**:

1. **Given** a state machine with an initial state and entry action, **When** the state machine executes, **Then** the entry action is scheduled before outbound transition triggers.
2. **Given** a current state with outbound transitions, **When** entry completes, **Then** each outbound transition trigger is scheduled and the state machine remains running.

---

### User Story 2 - Complete a Winning Transition (Priority: P2)

A transition trigger can win, evaluate its condition, run its action, exit the source state, enter the target state, and update the current state.

**Why this priority**: This completes the central state transition behavior from the issue.

**Independent Test**: Simulate trigger and action completion for a transition whose condition is true, then verify current state, action, source exit, target entry, and target triggers.

**Acceptance Scenarios**:

1. **Given** a transition whose trigger completed and condition is true, **When** the transition is accepted, **Then** its action is scheduled.
2. **Given** an accepted transition action completes, **When** source exit and target entry complete, **Then** the current state is the target state and target outbound triggers are scheduled.
3. **Given** a transition has no condition, **When** its trigger completes, **Then** the transition is treated as eligible.

---

### User Story 3 - Cancel Competing Triggers (Priority: P3)

When one outbound transition wins, competing outbound transition triggers from the same source state are canceled so the state machine cannot take multiple outbound paths.

**Why this priority**: This preserves deterministic state-machine semantics with multiple pending triggers.

**Independent Test**: Execute a state with two outbound transitions, complete one trigger, and verify the other trigger context is canceled.

**Acceptance Scenarios**:

1. **Given** multiple outbound transition triggers are pending, **When** one trigger wins, **Then** all other outbound transition trigger contexts from that source state are canceled.
2. **Given** a transition condition evaluates false, **When** its trigger completes, **Then** no transition action is scheduled, competing triggers remain pending, and the failed transition trigger remains active for a future attempt.

### Edge Cases

- A missing initial state completes the state machine without scheduling children.
- A missing source or target state on a transition prevents that transition from being scheduled.
- A missing trigger means that transition cannot be scheduled.
- A missing entry, exit, or action slot is treated as an empty step that immediately advances the state machine.
- A state with no valid outbound transitions is terminal and completes the state machine after its entry action completes.
- A trigger whose transition condition evaluates false is re-armed so the same transition can be attempted again later.
- Duplicate state names are resolved by the first state in declaration order.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a `StateMachine` activity with `States`, `Transitions`, `InitialState`, and observable `CurrentState` values.
- **FR-002**: System MUST provide a `State` model with `Name`, optional `Entry`, and optional `Exit` activity slots.
- **FR-003**: System MUST provide a `Transition` model with `Name`, `DisplayName`, `From`, `To`, optional `Trigger`, optional `Condition`, and optional `Action`.
- **FR-004**: State machine execution MUST start from `CurrentState` when set, otherwise `InitialState`.
- **FR-005**: Entering a state MUST schedule its entry action before outbound transition triggers.
- **FR-006**: After state entry completes, the state machine MUST schedule all valid outbound transition triggers for the current state.
- **FR-007**: A completed trigger MUST evaluate its transition condition; missing conditions MUST be treated as true.
- **FR-008**: An eligible transition MUST run its action before leaving the source state.
- **FR-009**: A completed transition action MUST schedule the source state's exit action and then the target state's entry action.
- **FR-010**: A completed transition MUST update `CurrentState` to the target state before scheduling the target state's outbound triggers.
- **FR-011**: When one transition is accepted, all other pending outbound transition triggers for the same source state MUST be canceled.
- **FR-012**: A false transition condition MUST leave the state machine in the same state, MUST NOT cancel competing triggers, and MUST keep the failed transition trigger active for a future attempt.
- **FR-013**: A state with no valid outbound transitions MUST complete the state machine after any entry action completes.
- **FR-014**: Unit tests MUST cover initial execution, true and false transition conditions, empty slots, terminal states, and competing trigger cancellation.

### Key Entities *(include if feature involves data)*

- **StateMachine**: Activity that owns state declarations, transition declarations, and current state progress.
- **State**: Named state with optional entry and exit activities.
- **Transition**: Directed path from one state to another with trigger, condition, and action slots.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A two-state workflow can wait in its initial state with at least one pending transition trigger.
- **SC-002**: A trigger with a true condition moves the machine to its target state and schedules the next state's triggers.
- **SC-003**: A trigger with a false condition leaves the state unchanged and keeps other outbound trigger work active.
- **SC-004**: A transition into a state with no valid outbound transitions completes the state machine.
- **SC-005**: Unit tests for the activity pass without external services.

## Assumptions

- This change covers server-side workflow execution in `elsa-core`; the dedicated Studio designer from issue #5085 remains separate work.
- States and transitions are stored as activity properties using existing Elsa serialization patterns.
- Transition source and target references use state names for the first server-side implementation.
- Trigger bookmark cleanup relies on canceling the losing trigger activity execution contexts through existing Elsa cancellation behavior.

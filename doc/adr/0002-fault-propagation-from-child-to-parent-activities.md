# 2. Fault Propagation from Child to Parent Activities

Date: 2025-04-01

## Status

Accepted

Amended by [3. Signal-Driven Fault Propagation](0003-signal-driven-fault-propagation.md)

## Context
When an activity throws an exception during execution, the `ExceptionHandlingMiddleware` is responsible for handling the fault. In addition to logging the exception and recording an incident, it also explicitly faults all ancestor activities using the `GetAncestors()` method and calling `TransitionTo(ActivityStatus.Faulted)` on each one.

At the time of implementing this behavior, the rationale was likely to bubble up the fault status to allow Elsa Studio to display that a given activity has one or more faulting descendant activities.

However, this behavior causes unintended side effects when the faulted activity is later resumed and completes successfully. Since the parent (e.g., a `Flowchart`) was faulted earlier, it remains in a `Faulted` state, unaware that the child it depends on has now succeeded. As a result, the Flowchart itself never reaches `Completed`, and the entire workflow instance is stuck in `Faulted` or `Running`, depending on the selected fault strategy.

## Decision
A new field will be introduced that represents an aggregate count of the faults occurring in descendant activities. 

## Consequences
### Positive:
- Allows composite activities to reflect a failed state immediately and consistently when one of their children fails.
- 

### Negative:
- Parent activities may enter a `Faulted` state prematurely.
- If the child activity is later resumed and completes successfully, the parent remains unaware and cannot transition to `Completed`, causing the entire workflow to hang in a `Faulted` state.
- This is especially problematic for long-running, resumable workflows where faults are transient and may later be resolved.

## Alternatives Considered
- Query descendants at runtime to get an aggregate fault count instead. However, this is an expensive operation that can be time consuming.

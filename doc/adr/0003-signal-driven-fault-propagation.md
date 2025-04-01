# 3. Signal-Driven Fault Propagation

Date: 2025-04-01

## Status

Accepted

Amends [2. Fault Propagation from Child to Parent Activities](0002-fault-propagation-from-child-to-parent-activities.md)

## Context
The existing implementation of fault handling in Elsa Workflows automatically transitions all ancestor activities to the `Faulted` state when a child activity throws an exception. This behavior is implemented in the `ExceptionHandlingMiddleware`.

While this ensures that faults are immediately visible at higher levels of the workflow, it creates unintended side effects:
- Container activities such as `Flowchart` may become stuck in a `Faulted` state, even after the faulted child activity has been resumed and completed successfully.
- It removes control from container activities to determine how they want to react to child faults.

## Decision
Responsibility for handling child faults is moved to the ancestor activities. Faults are no longer automatically bubbled up and applied by the middleware.

Instead, a new signal is introduced and bubbled up: `ActivityFaulted`

When an activity faults, it emits a signal that container activities may listen to. Ancestor activities can handle this signal to opt into fault propagation and decide their own behavior (e.g., whether to transition to `Faulted` or ignore).

The `ExceptionHandlingMiddleware` no longer faults ancestors by default.

To make sure that the workflow instance viewer can still show a visual indicator that a parent activity contains a faulted descendant activity, an update will be made that allows a child activity to notify ancestors using a fault count.
This allows multiple descendants to propagate a fault, while also allowing a child to clear the fault and thereby decrementing the fault count of its ancestors.

## Consequences

### Positive:
- Gives ancestor activities full control over how they react to child faults.
- Prevents workflows from being stuck in a `Faulted` state when recovery is possible.
- Makes the system more flexible and composable for complex fault-tolerant scenarios.

### Negative:
- Existing ancestor activities must be updated to handle fault propagation explicitly if they previously relied on automatic behavior.
- Slightly more boilerplate for developers creating custom container activities.
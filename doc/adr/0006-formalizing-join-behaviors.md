# 6. Formalizing Join Behaviors

Date: 2025-10-18

## Status

Accepted

## Context

In Elsa workflows, there are two ways that multiple execution branches can converge:

1. **Explicit joins** - Using the `Join` activity, which allows users to explicitly configure join behavior
2. **Implicit joins** - Multiple connections in a flowchart leading into a single activity (also called a "merge")

Prior to Elsa 3.5, join behavior attempted to wait only for active branches using a counting mechanism. However, this implementation did not work correctly in all cases, particularly in more complex workflow structures. This caused issues where workflows could become blocked waiting for branches that would never execute, leading to deadlocks and incomplete workflow executions.

There was a need to improve the join implementation and formalize the expected join behavior across various scenarios to ensure consistent and predictable workflow execution, particularly in complex workflow structures.

## Decision

### Join Modes

There are two join modes available:

1. **WaitAll** - The join waits for all active inbound branches to complete before proceeding
2. **WaitAny** - The join proceeds as soon as any active inbound branch completes

### Explicit Joins

When using the `Join` activity, users can explicitly select either `WaitAll` or `WaitAny` mode.

### Implicit Joins

When multiple connections in a flowchart lead into a single activity, this is interpreted as an implicit join with `WaitAll` mode.

### Active Branch Semantics

**The key improvement in Elsa 3.5 is a more robust implementation that correctly tracks and waits only for active branches, not all possible inbound branches.**

An **active branch** is defined as a branch that has been traversed during the current workflow instance. For example:
- Activity A connects via its "Done" outcome to activities B and C
- When A executes and activates the "Done" outcome, both B and C are scheduled for execution
- Both connections are now considered "activated" (active branches)

### WaitAll Behavior

A `WaitAll` join completes when all active inbound branches have reached the join. Inactive branches (branches that were never traversed) are not considered and do not block the join.

### WaitAny Behavior

A `WaitAny` join completes as soon as any active inbound branch reaches it. When this happens:
- Other inbound branches are ignored
- If an inbound source activity was already scheduled for execution, it will be cancelled

**Use case example:** A race between a `Delay` activity (waiting 5 minutes) and a `UserTask` activity (waiting for user action):
- If the user acts within 5 minutes, the `UserTask` completes first, the join proceeds, and the `Delay` activity is cancelled
- If the user does not act within 5 minutes, the `Delay` completes first, the join proceeds, and the `UserTask` is cancelled (preventing delayed user actions from having any effect)

## Consequences

### Benefits

- **Prevents deadlocks** - The improved implementation correctly handles complex workflow structures that previously caused deadlocks
- **Predictable behavior** - Formalized semantics make workflow execution behavior clear and consistent
- **Enables complex patterns** - `WaitAny` enables race conditions and timeout patterns with proper cleanup
- **Improved workflow completion** - Workflows that were incomplete in Elsa 3.2 and earlier will now complete successfully

### Breaking Changes

- **Backward compatibility impact** - While the intent was always to wait only for active branches, the improved implementation in Elsa 3.5 may cause some workflows to behave differently than they did in earlier versions
- Workflows that were incomplete in Elsa 3.2 and earlier versions may now complete successfully in Elsa 3.5+
- Users with existing workflows should review their join behaviors, particularly in complex workflow structures, to ensure they work as expected with the improved implementation

### Risks and Mitigations

- **Migration risk** - Existing workflows may need to be reviewed to ensure they work correctly with active-branch semantics
- **Documentation** - Clear documentation and examples are needed to help users understand the difference between active and inactive branches
- **Testing** - Comprehensive testing of complex workflow structures is required to ensure consistent behavior across various scenarios
